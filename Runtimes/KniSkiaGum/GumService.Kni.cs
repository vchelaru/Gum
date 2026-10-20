// KNI's GumService, as a subclass of GumServiceSkiaInteractiveBase. It overrides the
// CreateCursor/CreateKeyboard/ApplyGamePadState capability hooks with KNI-backed implementations;
// the per-frame Forms input pump comes from the base, and the typed Default/Cursor/Keyboard
// accessors from Runtimes/SkiaHost/GumService.SkiaHost.cs. Rendering, .gumx loading, hot reload and
// the rest come from GumServiceSkiaBase. It follows MonoGameGum's shape: Initialize(game) reads the
// size from the game and needs no canvas, Update takes a GameTime, and GameTime exposes the last one.
// The caller draws Skia into its own canvas (typically a SkiaGameRendering SkiaRenderTarget2D) and
// passes that SKCanvas to Draw each frame.
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
using SkiaSharp;
using XnaGame = Microsoft.Xna.Framework.Game;
using XnaGameTime = Microsoft.Xna.Framework.GameTime;
using XnaGameWindow = Microsoft.Xna.Framework.GameWindow;
using ICursor = Gum.Wireframe.ICursor;

namespace Gum;

public partial class GumService : GumServiceSkiaInteractiveBase, IGumService
{
    private XnaGameWindow? _window;
    private XnaGame? _game;
    private readonly List<GraphicalUiElement> _gameTimeRoots = new();

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

    /// <inheritdoc/>
    ICursor? IGumService.CreateCursor() => new Cursor();

    /// <inheritdoc/>
    IInputReceiverKeyboard? IGumService.CreateKeyboard() => new Keyboard(_window);

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time) =>
        GamePadDriver.Apply(gamepad, index, time);

    #region Initialize

    /// <summary>
    /// Initializes Gum for a KNI desktop game, optionally loading a Gum project. The size comes from the
    /// game's graphics device and no canvas is needed, so call this from <c>Game.Initialize</c> or later
    /// and build the first screen right away. The canvas is supplied by the first
    /// <see cref="GumServiceSkiaBase.Draw(SKCanvas)"/>.
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
    /// Initializes Gum for a KNI desktop game with an explicit size, such as the fixed resolution of a
    /// render target, instead of the size of the game's graphics device. No canvas is needed.
    /// </summary>
    /// <param name="game">The game, used for its window's typed-text events.</param>
    /// <param name="width">The width to use for the root container and canvas coordinate space.</param>
    /// <param name="height">The height to use for the root container and canvas coordinate space.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    /// <returns>The loaded project, or null if no project is loaded.</returns>
    public GumProjectSave? Initialize(XnaGame game, int width, int height, string? gumProjectFile = null) =>
        InitializeHost(game, game.Window, null, width, height, gumProjectFile);

    /// <summary>
    /// Initializes Gum for a KNI desktop game and draws into a canvas that already exists. The canvas
    /// size is read from <see cref="SKCanvas.DeviceClipBounds"/>; use the explicit-size overload if that
    /// does not match the window. Most games should use <see cref="Initialize(XnaGame, string?)"/>
    /// instead, which needs no canvas.
    /// </summary>
    /// <param name="game">The game, used for its window's typed-text events.</param>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    /// <returns>The loaded project, or null if no project is loaded.</returns>
    public GumProjectSave? Initialize(XnaGame game, SKCanvas canvas, string? gumProjectFile = null)
    {
        SKRectI bounds = canvas.DeviceClipBounds;
        return Initialize(game, canvas, bounds.Width, bounds.Height, gumProjectFile);
    }

    /// <summary>
    /// Initializes Gum for a KNI desktop game with an explicit canvas size, optionally loading a Gum
    /// project.
    /// </summary>
    /// <param name="game">The game, used for its window's typed-text events.</param>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="width">The width to use for the root container and canvas coordinate space.</param>
    /// <param name="height">The height to use for the root container and canvas coordinate space.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    /// <returns>The loaded project, or null if no project is loaded.</returns>
    public GumProjectSave? Initialize(XnaGame game, SKCanvas canvas, int width, int height, string? gumProjectFile = null) =>
        InitializeHost(game, game.Window, canvas, width, height, gumProjectFile);

    /// <summary>
    /// Initializes without a game, so no typed text arrives. A KNI <c>Game</c> needs a platform
    /// package, so tests that have none initialize through this.
    /// </summary>
    internal GumProjectSave? InitializeWithoutWindow(SKCanvas canvas, int width, int height, string? gumProjectFile = null) =>
        InitializeHost(null, null, canvas, width, height, gumProjectFile);

    /// <summary>
    /// Initializes without a game or canvas, as <see cref="Initialize(XnaGame, string?)"/> does once it
    /// has read the size from the game.
    /// </summary>
    internal GumProjectSave? InitializeWithoutWindow(int width, int height, string? gumProjectFile = null) =>
        InitializeHost(null, null, null, width, height, gumProjectFile);

    private static (int width, int height) GetSize(XnaGame game)
    {
        if (game.GraphicsDevice == null)
        {
            throw new InvalidOperationException(
                "game.GraphicsDevice cannot be null. " +
                "Be sure to call Initialize in the Game's Initialize method or later " +
                "so that the Game has a valid GraphicsDevice");
        }

        return (game.GraphicsDevice.Viewport.Width, game.GraphicsDevice.Viewport.Height);
    }

    private GumProjectSave? InitializeHost(
        XnaGame? game, XnaGameWindow? window, SKCanvas? canvas, int width, int height, string? gumProjectFile)
    {
        // Stored before InitializeCore, which calls FormsUtilities.InitializeDefaults, which
        // dispatches back to this instance's CreateCursor/CreateKeyboard overrides above.
        _game = game;
        _window = window;
        Clipboard = new global::Gum.Clipboard.MonoGameGumClipboard();

        InitializeCore(canvas, width, height, gumProjectFile);

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

        if (_game?.GraphicsDevice == null)
        {
            return base.GetWindowSize();
        }

        Microsoft.Xna.Framework.Graphics.PresentationParameters presentation = _game.GraphicsDevice.PresentationParameters;
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

    /// <inheritdoc/>
    public override void Uninitialize()
    {
        base.Uninitialize();

        GameTime = null;
        _game = null;
        _window = null;
    }
}
#endif
