// Mirrors Runtimes/SilkNetGum/GumService.Silk.cs: this whole file's content only makes sense when
// this project's STRIDE define is active (always true when compiled here -- see StrideGum.csproj --
// but kept guarded for consistency with the other per-runtime GumService.<Platform>.cs files).
#if STRIDE
#nullable enable

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using System;
using System.Diagnostics;
using ICursor = Gum.Wireframe.ICursor;

namespace Gum;

/// <summary>
/// Stride's GumService, as a subclass of <see cref="GumServiceSkiaBase"/>. Overrides the
/// CreateCursor/CreateKeyboard/ApplyGamePadState capability hooks with real
/// <see cref="Stride.Input.InputManager"/>-backed implementations. Unlike every other Gum runtime,
/// the caller never calls Update/Draw themselves: Stride's rendering is driven by its
/// GraphicsCompositor pipeline rather than a user-owned Draw() method, so <see cref="Initialize"/>
/// registers an internal <see cref="GumSceneRenderer"/> that ticks Update/Draw automatically every
/// frame. The public contract is still just one call -- <c>GumService.Default.Initialize(game)</c> --
/// simpler than the Initialize-then-per-frame-Update/Draw shape elsewhere, not a broken version of
/// it. Mirrors Gum/Runtimes/SilkNetGum/GumService.Silk.cs for the input side.
/// </summary>
public class GumService : GumServiceSkiaBase, IGumService
{
    private static GumService? _default;

    /// <summary>
    /// The singleton service instance.
    /// </summary>
    public static GumService Default => _default ??= new GumService();

    private InputManager? _inputManager;
    private GraphicsDevice? _graphicsDevice;

    // Owns the Skia-to-Stride bridge: Gum renders into _skSurface (CPU-side), which is copied into
    // _skiaTexture each frame and blitted into the composited frame via _spriteBatch. GumService owns
    // this state (not GumSceneRenderer) so Initialize can create it synchronously -- Root is usable
    // immediately after Initialize returns, matching every other runtime's Initialize-then-build-UI
    // pattern, rather than deferring setup until Stride's own SceneRendererBase.InitializeCore fires.
    private SKSurface? _skSurface;
    private Texture? _skiaTexture;
    private SpriteBatch? _spriteBatch;
    private int _surfaceWidth;
    private int _surfaceHeight;

    private readonly Stopwatch _clock = new();

    /// <summary>
    /// Gets the default cursor, which represents the mouse.
    /// </summary>
    public Cursor Cursor => (FormsUtilities.Cursor as Cursor)!;

    /// <summary>
    /// Gets the default keyboard.
    /// </summary>
    public Keyboard Keyboard => (FormsUtilities.Keyboard as Keyboard)!;

    /// <inheritdoc/>
    ICursor? IGumService.CreateCursor()
    {
        var cursor = new Cursor();
        if (_inputManager != null)
        {
            cursor.AttachStrideInput(_inputManager.Mouse);
            _inputManager.AddListener(cursor);
        }
        return cursor;
    }

    /// <inheritdoc/>
    IInputReceiverKeyboard? IGumService.CreateKeyboard()
    {
        // A real desktop Stride window always exposes at least one keyboard. For the degenerate
        // (headless) case, return an inert device-less Keyboard rather than null: FormsUtilities.Update
        // ticks keyboard.Activity() unconditionally, so a null here would NRE on the first Update.
        if (_inputManager == null || _inputManager.Keyboards.Count == 0)
        {
            return new Keyboard();
        }
        return new Keyboard(_inputManager.Keyboards[0], _inputManager);
    }

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time)
    {
        var strideGamePad = _inputManager != null ? _inputManager.GetGamePadByIndex(index) : null;
        if (strideGamePad != null)
        {
            GamePadDriver.Apply(gamepad, strideGamePad, time);
        }
        else
        {
            gamepad.SetConnected(false);
            gamepad.Activity(time);
        }
    }

    /// <summary>
    /// Initializes Gum for a Stride application, optionally loading a Gum project. Call this once,
    /// after <c>game.AddGraphicsCompositor()</c>/<c>AddCleanUIStage()</c> (or equivalent) has already
    /// run -- a <c>GraphicsCompositor</c> must exist on <c>game.SceneSystem</c> before this call, since
    /// (by default) Initialize registers Gum's own scene renderer into it. <see cref="GumServiceSkiaBase.Root"/>
    /// is usable immediately after this call returns; no further setup call is needed, and you never
    /// call Update/Draw yourself -- Stride drives both automatically every frame from here on.
    /// </summary>
    /// <param name="game">The Stride game to initialize Gum into.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    /// <param name="registerSceneRenderer">
    /// When <see langword="true"/> (the default), Initialize constructs and registers a single
    /// <see cref="GumSceneRenderer"/> for you -- the simple path, and all you need for one UI layer.
    /// Pass <see langword="false"/> to skip that and construct/place <see cref="GumSceneRenderer"/>
    /// instances yourself (e.g. <c>game.AddSceneRenderer(new GumSceneRenderer())</c>, or inserted at a
    /// specific point in the compositor) -- for control over ordering relative to other renderers, or
    /// multiple Gum draw passes. The two are mutually exclusive by construction: Initialize either
    /// registers the renderer for you, or it doesn't -- there's no risk of it applying twice.
    /// </param>
    public void Initialize(Game game, string? gumProjectFile = null, bool registerSceneRenderer = true)
    {
        _graphicsDevice = game.GraphicsDevice;

        var backBuffer = game.GraphicsDevice.Presenter.BackBuffer;
        RecreateSurface(backBuffer.Width, backBuffer.Height);

        InitializeCore(_skSurface!.Canvas, game.Input, _surfaceWidth, _surfaceHeight, gumProjectFile);

        if (registerSceneRenderer)
        {
            game.AddSceneRenderer(new GumSceneRenderer());
        }

        _clock.Start();
    }

    /// <summary>
    /// The Gum-facing half of <see cref="Initialize(Game, string?, bool)"/>: independent of any live
    /// Stride <see cref="GraphicsDevice"/>/<see cref="Texture"/>/<see cref="GumSceneRenderer"/>, so
    /// StrideGum.Tests can drive Gum through this overload with an in-memory <see cref="SKSurface"/>
    /// the same way SilkNetGum.Tests drives <see cref="GumServiceSkiaBase.Initialize(SKCanvas, int,
    /// int, string?)"/> directly -- no real window/graphics context needed for that half of the
    /// contract. Not part of the public API: production callers always go through <see cref="Initialize(Game, string?, bool)"/>.
    /// </summary>
    internal void InitializeCore(SKCanvas canvas, InputManager inputManager, int width, int height, string? gumProjectFile)
    {
        // Stored before base.Initialize, which calls FormsUtilities.InitializeDefaults, which
        // dispatches back to this instance's CreateCursor/CreateKeyboard overrides above.
        _inputManager = inputManager;
        Clipboard = new StrideGumClipboard();

        base.Initialize(canvas, width, height, gumProjectFile);
    }

    private void RecreateSurface(int width, int height)
    {
        if (width <= 0 || height <= 0 || _graphicsDevice == null) return;

        _surfaceWidth = width;
        _surfaceHeight = height;

        _skSurface?.Dispose();
        _skiaTexture?.Dispose();

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        _skSurface = SKSurface.Create(info);

        _skiaTexture = Texture.New2D(
            _graphicsDevice,
            width,
            height,
            PixelFormat.R8G8B8A8_UNorm,
            TextureFlags.ShaderResource,
            1,
            GraphicsResourceUsage.Dynamic);

        _spriteBatch ??= new SpriteBatch(_graphicsDevice);
    }

    /// <summary>
    /// Called once per frame by the internal <see cref="GumSceneRenderer"/>: handles backbuffer
    /// resize, ticks Update, renders Gum into the Skia surface, and blits it into the composited
    /// frame. Not part of the public API -- see the class remarks for why Update/Draw aren't
    /// separately exposed to Stride callers the way they are on every other Gum runtime.
    /// </summary>
    internal void DrawStrideFrame(RenderDrawContext drawContext)
    {
        var commandList = drawContext.CommandList;
        var backBuffer = commandList.RenderTarget;
        if (backBuffer.Width != _surfaceWidth || backBuffer.Height != _surfaceHeight)
        {
            RecreateSurface(backBuffer.Width, backBuffer.Height);
            SystemManagers.Default.Canvas = _skSurface!.Canvas;
            HandleResize(_surfaceWidth, _surfaceHeight);
        }

        if (_skSurface == null || _skiaTexture == null || _spriteBatch == null) return;

        Update(_clock.Elapsed.TotalSeconds);

        var canvas = _skSurface.Canvas;
        canvas.Clear(SKColors.Empty);
        Draw();
        canvas.Flush();

        SKPixmap pixmap = _skSurface.PeekPixels();
        IntPtr pixelPointer = pixmap.GetPixels();
        int byteSize = _surfaceWidth * _surfaceHeight * 4;

        unsafe
        {
            _skiaTexture.SetData(commandList, new Span<byte>((void*)pixelPointer, byteSize));
        }

        commandList.SetRenderTarget(drawContext.CommandList.DepthStencilBuffer, backBuffer);

        _spriteBatch.Begin(drawContext.GraphicsContext);
        _spriteBatch.Draw(_skiaTexture, new RectangleF(0, 0, _surfaceWidth, _surfaceHeight), color: Color.White);
        _spriteBatch.End();
    }

    internal void DisposeStrideResources()
    {
        _skSurface?.Dispose();
        _skiaTexture?.Dispose();
        _spriteBatch?.Dispose();
    }

    /// <summary>
    /// Per-frame tick, called automatically by <see cref="DrawStrideFrame"/> -- not called by
    /// application code (see the class remarks). Runs the base's deferred-queue/animation tick, then
    /// pumps Forms input (cursor/keyboard activity, control events).
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
    public override void Update(double totalSeconds)
    {
        base.Update(totalSeconds);
        FormsUtilities.Update(totalSeconds, Root);
    }
}

/// <summary>
/// The Stride <see cref="SceneRendererBase"/> that drives <see cref="GumService"/> every frame:
/// ticks Update, renders Gum into its Skia surface, and blits it into the composited frame.
/// Stateless -- every instance drives the same <see cref="GumService.Default"/> -- so it's safe to
/// construct as many as you want. By default, <see cref="GumService.Initialize(Game, string?, bool)"/>
/// constructs and registers exactly one of these for you; pass <c>registerSceneRenderer: false</c> to
/// that call instead and construct/place instances yourself for control over ordering or multiple
/// draw passes (e.g. <c>game.AddSceneRenderer(new GumSceneRenderer())</c>).
/// </summary>
public sealed class GumSceneRenderer : SceneRendererBase
{
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext) =>
        GumService.Default.DrawStrideFrame(drawContext);

    protected override void Destroy()
    {
        GumService.Default.DisposeStrideResources();
        base.Destroy();
    }
}
#endif
