// KNI's GumService, as a subclass of GumServiceSkiaInteractiveBase. It overrides the
// CreateCursor/CreateKeyboard/ApplyGamePadState capability hooks with KNI-backed implementations;
// the per-frame Forms input pump comes from the base, and the typed Default/Cursor/Keyboard
// accessors from Runtimes/SkiaHost/GumService.SkiaHost.cs. Rendering, .gumx loading, hot reload and
// the rest come from GumServiceSkiaBase. It follows MonoGameGum's shape: Initialize(game) reads the
// size from the game and needs no canvas, Update takes a GameTime, and GameTime exposes the last one.
// Draw() has no arguments either: the service owns the SkiaGameRendering render target (see
// KniSkiaTarget), recreates it when the back buffer size changes, and composites it onto the back
// buffer, so a game never touches a Skia or SkiaGameRendering type.
//
// Not mirrored from KniGum: Initialize(game, SystemManagers). Skia has one global
// SystemManagers.Default, so there is no SystemManagers to pass in.
#if KNISKIA
#nullable enable

using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using BatchGraphics = global::RenderingLibrary.Graphics;
using SkiaGum.Helpers;
using SkiaSharp;
using XnaGame = Microsoft.Xna.Framework.Game;
using XnaGameTime = Microsoft.Xna.Framework.GameTime;
using XnaGameWindow = Microsoft.Xna.Framework.GameWindow;
using XnaGraphicsDevice = Microsoft.Xna.Framework.Graphics.GraphicsDevice;
using ICursor = Gum.Wireframe.ICursor;

namespace Gum;

public partial class GumService : GumServiceSkiaInteractiveBase, IGumService, BatchGraphics.IGumBatchFrameHost
{
    private XnaGameWindow? _window;
    private XnaGame? _game;
    private readonly List<GraphicalUiElement> _gameTimeRoots = new();
    private XnaGraphicsDevice? _graphicsDevice;
    private SkiaTargetHolder<IKniSkiaTarget>? _targetHolder;
    private bool _managesTarget;

    /// <summary>
    /// The GameTime of the most recent Update call, or null before the first Update.
    /// </summary>
    public XnaGameTime? GameTime { get; private set; }

    /// <inheritdoc/>
    float? IGumService.GameTime =>
        GameTime != null ? (float?)GameTime.TotalGameTime.TotalSeconds : null;

    /// <summary>
    /// The game this service was initialized with.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if Initialize has not been called with a game.</exception>
    public XnaGame Game => _game ?? throw new InvalidOperationException(
        "GumService has not been initialized. Call GumService.Initialize(game) first.");

    private partial Cursor CreateHostCursor() => new Cursor(_window);

    private partial Keyboard? CreateHostKeyboard() => new Keyboard(_window);

    // Same messages as KniGum/MonoGameGum: the host-agnostic Initialize needs a Game here, too.
    void IGumService.Initialize() =>
        throw new NotSupportedException(
            "This runtime requires a Game instance. Call " +
            "GumService.Default.Initialize(Game) on the concrete GumService instead.");

    void IGumService.Initialize(string gumProjectFile) =>
        throw new NotSupportedException(
            "This runtime requires a Game instance. Call " +
            "GumService.Default.Initialize(Game, gumProjectFile) on the concrete GumService instead.");

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time) =>
        GamePadDriver.Apply(gamepad, index, time);

    #region Initialize

    /// <summary>
    /// Initializes Gum for a KNI desktop game, optionally loading a Gum project. The size comes from the
    /// game's graphics device, so call this from <c>Game.Initialize</c>
    /// or later and build the first screen right away. <see cref="Draw()"/> creates and resizes the
    /// Skia render target itself, and the service releases it when the game exits.
    /// </summary>
    /// <param name="game">The game, used for its graphics device and its window's typed-text events.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    /// <returns>The loaded project, or null if no project is loaded.</returns>
    public GumProjectSave? Initialize(XnaGame game, string? gumProjectFile = null)
    {
        (int width, int height) = GetSize(game);
        return InitializeHost(game, game.Window, null, width, height, gumProjectFile);
    }

    /// <summary>
    /// Initializes Gum for a KNI desktop game with the given default Forms visuals, as
    /// <c>Initialize(Game, DefaultVisualsVersion)</c> does in KniGum. Only
    /// <see cref="DefaultVisualsVersion.V3"/> (and <see cref="DefaultVisualsVersion.Newest"/>, which is
    /// V3) exists.
    /// </summary>
    /// <param name="game">The game, used for its graphics device and its window's typed-text events.</param>
    /// <param name="defaultVisualsVersion">The default Forms visuals to register.</param>
    /// <exception cref="NotSupportedException"><paramref name="defaultVisualsVersion"/> is not supported on Skia.</exception>
    public void Initialize(XnaGame game, DefaultVisualsVersion defaultVisualsVersion)
    {
        (int width, int height) = GetSize(game);
        InitializeHost(game, game.Window, null, width, height, null, defaultVisualsVersion);
    }

    /// <summary>
    /// Initializes Gum from a graphics device, with no <c>Game</c>, as <c>Initialize(GraphicsDevice, ...)</c>
    /// does in KniGum. Typed text does not arrive without a window; use
    /// <see cref="Initialize(XnaGame, string?)"/> for an interactive game.
    /// </summary>
    /// <param name="graphicsDevice">The device whose viewport sets the root container and canvas size.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    /// <returns>The loaded project, or null if no project is loaded.</returns>
    public GumProjectSave? Initialize(XnaGraphicsDevice graphicsDevice, string? gumProjectFile = null)
    {
        (int width, int height) = GetSize(graphicsDevice);
        _graphicsDevice = graphicsDevice;
        return InitializeHost(null, null, null, width, height, gumProjectFile);
    }

    /// <summary>
    /// Initializes without a game, so no typed text arrives. A KNI <c>Game</c> needs a platform
    /// package, so tests that have none initialize through this.
    /// </summary>
    internal GumProjectSave? InitializeWithoutWindow(SKCanvas canvas, int width, int height, string? gumProjectFile = null) =>
        InitializeHost(null, null, canvas, width, height, gumProjectFile);

    /// <summary>
    /// Initializes with a window but no game, so a test can raise typed text without a platform package.
    /// </summary>
    internal GumProjectSave? InitializeWithWindow(XnaGameWindow window, int width, int height, string? gumProjectFile = null) =>
        InitializeHost(null, window, null, width, height, gumProjectFile);

    /// <summary>
    /// Initializes without a game or canvas, as <see cref="Initialize(XnaGame, string?)"/> does once it
    /// has read the size from the game.
    /// </summary>
    internal GumProjectSave? InitializeWithoutWindow(
        int width, int height, string? gumProjectFile = null,
        DefaultVisualsVersion visualsVersion = DefaultVisualsVersion.Newest) =>
        InitializeHost(null, null, null, width, height, gumProjectFile, visualsVersion);

    private static (int width, int height) GetSize(XnaGame game)
    {
        if (game.GraphicsDevice == null)
        {
            throw new InvalidOperationException(
                "game.GraphicsDevice cannot be null. " +
                "Be sure to call Initialize in the Game's Initialize method or later " +
                "so that the Game has a valid GraphicsDevice");
        }

        return GetSize(game.GraphicsDevice);
    }

    private static (int width, int height) GetSize(XnaGraphicsDevice? graphicsDevice)
    {
        if (graphicsDevice == null)
        {
            throw new ArgumentNullException(nameof(graphicsDevice));
        }

        return (graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
    }

    private GumProjectSave? InitializeHost(
        XnaGame? game, XnaGameWindow? window, SKCanvas? canvas, int width, int height, string? gumProjectFile,
        DefaultVisualsVersion visualsVersion = DefaultVisualsVersion.Newest)
    {
        InitializeWithDevices(canvas, width, height, gumProjectFile, () =>
        {
            ReleaseGame();
            _game = game;
            _window = window;
            // No canvas means the service owns the render target. A test that hands in a canvas keeps
            // drawing into it.
            _managesTarget = canvas == null;
            BatchGraphics.GumBatch.FrameHost = _managesTarget ? this : null;
            if (game != null)
            {
                game.Exiting += OnGameExiting;
            }
            Clipboard = new global::Gum.Clipboard.MonoGameGumClipboard();
        }, visualsVersion);

        return string.IsNullOrEmpty(gumProjectFile) ? null : ObjectFinder.Self.GumProjectSave;
    }

    #endregion

    /// <summary>
    /// Replaces the window size read from the game's back buffer. Set by tests, which have no game.
    /// </summary>
    internal Func<(int width, int height)>? WindowSizeOverride { get; set; }

    /// <summary>
    /// The back buffer size, as in MonoGameGum, so <see cref="GumServiceSkiaBase.EnableZoomToWindow"/>
    /// and <see cref="GumServiceSkiaBase.EnableExpandToWindow"/> follow the window.
    /// </summary>
    protected override (int width, int height) GetWindowSize()
    {
        if (WindowSizeOverride != null)
        {
            return WindowSizeOverride();
        }

        XnaGraphicsDevice? graphicsDevice = _game?.GraphicsDevice ?? _graphicsDevice;
        if (graphicsDevice == null)
        {
            return base.GetWindowSize();
        }

        Microsoft.Xna.Framework.Graphics.PresentationParameters presentation = graphicsDevice.PresentationParameters;
        return (presentation.BackBufferWidth, presentation.BackBufferHeight);
    }

    #region Update

    /// <summary>
    /// Performs every-frame updates including cursor update, keyboard update, gamepad updates, and
    /// raising events on all controls.
    /// </summary>
    /// <param name="gameTime">The GameTime obtained from the Game class in the Update call.</param>
    public void Update(XnaGameTime gameTime)
    {
        _gameTimeRoots.Clear();
        if (Root != null)
        {
            _gameTimeRoots.Add(Root);
        }
        Update(gameTime, _gameTimeRoots);
    }

    /// <summary>
    /// Performs every-frame updates from the total seconds since startup, for code that works through
    /// <see cref="IGumService"/>. It builds the <see cref="XnaGameTime"/> and calls
    /// <see cref="Update(XnaGameTime)"/>, so <see cref="GameTime"/> and an inactive window behave the same.
    /// </summary>
    /// <param name="totalSeconds">
    /// Total elapsed time in seconds since startup. A value that is not a finite number is a frame of zero
    /// length, and a very large one is clamped.
    /// </param>
    public override void Update(double totalSeconds)
    {
        // The elapsed time is the gap since the previous Update; the first call advances by zero.
        FrameClock.ToTimeSpans(totalSeconds, GameTime?.TotalGameTime, out TimeSpan total, out TimeSpan elapsed);
        Update(new XnaGameTime(total, elapsed));
    }

    /// <summary>
    /// Performs every-frame updates for <paramref name="root"/> instead of <see cref="GumServiceSkiaBase.Root"/>.
    /// </summary>
    /// <param name="gameTime">The GameTime obtained from the Game class in the Update call.</param>
    /// <param name="root">The root to animate and pump Forms input for.</param>
    public void Update(XnaGameTime gameTime, GraphicalUiElement root)
    {
        _gameTimeRoots.Clear();
        _gameTimeRoots.Add(root);
        Update(gameTime, _gameTimeRoots);
    }

    /// <summary>
    /// Performs every-frame updates for <paramref name="roots"/> instead of <see cref="GumServiceSkiaBase.Root"/>.
    /// </summary>
    /// <param name="gameTime">The GameTime obtained from the Game class in the Update call.</param>
    /// <param name="roots">The roots to animate and pump Forms input for.</param>
    public void Update(XnaGameTime gameTime, IEnumerable<GraphicalUiElement> roots)
    {
        GameTime = gameTime;

        double totalSeconds = gameTime.TotalGameTime.TotalSeconds;
        if (IsGameActive())
        {
            UpdateRoots(totalSeconds, roots);
        }
        else
        {
            // Same as MonoGameGum: an inactive window gets no input, but the frame still advances.
            UpdateFrame(totalSeconds, roots);
        }
    }

    /// <summary>
    /// Reports whether the game's window has focus. Replaced in tests, which have no game.
    /// </summary>
    internal Func<bool>? IsGameActiveOverride { get; set; }

    private bool IsGameActive() => IsGameActiveOverride != null
        ? IsGameActiveOverride()
        : _game == null || _game.IsActive;

    #endregion

    #region Draw

    /// <summary>
    /// Creates the render target. Replaced in tests, which have no graphics device.
    /// </summary>
    internal Func<int, int, IKniSkiaTarget>? TargetFactoryOverride { get; set; }

    /// <summary>
    /// Draws Gum and composites it onto the back buffer, as <c>GumService.Draw()</c> does in MonoGameGum.
    /// The service creates its SkiaGameRendering render target on the first call, recreates it (and
    /// re-runs layout through <see cref="GumServiceSkiaBase.HandleResize"/>) whenever the back buffer
    /// size changes, and clears it to transparent each frame, so clear the back buffer first.
    /// </summary>
    /// <remarks>
    /// Initialized with a canvas (an internal test path), this draws into that canvas instead, as the
    /// base does.
    /// </remarks>
    public override void Draw()
    {
        if (_frameOpen)
        {
            // Recover for the next frame instead of drawing nothing.
            CloseFrame(present: false);
            throw new InvalidOperationException(
                "GumService.Draw() was called while a GumBatch pass was open: call GumBatch.End before Draw.");
        }

        if (!TryOpenFrame(out SKCanvas? _))
        {
            if (!_managesTarget || !HasGraphicsDevice())
            {
                base.Draw();
            }
            return;
        }

        try
        {
            base.Draw();
        }
        catch
        {
            CloseFrame(present: false);
            throw;
        }

        // Composites the target onto the back buffer at the origin, at native size.
        CloseFrame(present: true);
    }

    private bool _frameOpen;

    private bool HasGraphicsDevice() =>
        (_game?.GraphicsDevice ?? _graphicsDevice) != null || TargetFactoryOverride != null;

    // Creates or resizes the target and begins a pass on it. False when the service does not own the
    // target, a pass is already open, or the window has no size (minimized).
    private bool TryOpenFrame(out SKCanvas? frameCanvas)
    {
        frameCanvas = null;
        XnaGraphicsDevice? graphicsDevice = _game?.GraphicsDevice ?? _graphicsDevice;
        if (!_managesTarget || _frameOpen || (graphicsDevice == null && TargetFactoryOverride == null))
        {
            return false;
        }

        (int width, int height) = GetWindowSize();
        _targetHolder ??= CreateTargetHolder(graphicsDevice);

        if (_targetHolder.Target == null)
        {
            // The first target matches the size Gum was initialized with, so there is nothing to re-layout.
            if (!_targetHolder.Create(width, height))
            {
                return false;
            }
        }
        else
        {
            _targetHolder.EnsureSize(width, height);
        }

        frameCanvas = _targetHolder.BeginFrame();
        _frameOpen = frameCanvas != null;
        return _frameOpen;
    }

    // Separate from TryOpenFrame so the lambdas' capture of graphicsDevice allocates only when the holder
    // is created, not on every frame.
    private SkiaTargetHolder<IKniSkiaTarget> CreateTargetHolder(XnaGraphicsDevice? graphicsDevice) =>
        new SkiaTargetHolder<IKniSkiaTarget>(
            (w, h) => TargetFactoryOverride != null
                ? TargetFactoryOverride(w, h)
                : new KniSkiaTarget(graphicsDevice!, w, h),
            canvas => SystemManagers.Default.Canvas = canvas,
            HandleResize);

    private void CloseFrame(bool present)
    {
        if (!_frameOpen)
        {
            return;
        }

        _frameOpen = false;
        if (present)
        {
            _targetHolder!.Target!.End();
        }
        else
        {
            _targetHolder!.Target!.EndWithoutDrawing();
        }
    }

    // GumBatch.Begin/End outside Draw: each cycle is its own pass, composited in call order.
    bool BatchGraphics.IGumBatchFrameHost.TryBeginFrame() => TryOpenFrame(out SKCanvas? _);

    void BatchGraphics.IGumBatchFrameHost.EndFrame(bool present) => CloseFrame(present);

    private void OnGameExiting(object? sender, EventArgs e) => ReleaseRenderTarget();

    private void ReleaseGame()
    {
        if (_game != null)
        {
            _game.Exiting -= OnGameExiting;
        }
    }

    // The shared SkiaRenderer backend goes with the target, since the service is what created both.
    private void ReleaseRenderTarget()
    {
        bool hadTarget = _targetHolder?.Target != null;
        _targetHolder?.Dispose();
        _targetHolder = null;
        if (hadTarget && TargetFactoryOverride == null)
        {
            SkiaGameRendering.SkiaRenderer.Dispose();
        }
    }

    #endregion

    /// <inheritdoc/>
    public override void Uninitialize()
    {
        base.Uninitialize();

        ReleaseRenderTarget();
        ReleaseGame();
        _managesTarget = false;
        BatchGraphics.GumBatch.FrameHost = null;
        GameTime = null;
        _game = null;
        _graphicsDevice = null;
        _window = null;
    }
}
#endif
