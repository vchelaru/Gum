// Silk.NET's GumService, as a subclass of GumServiceSkiaInteractiveBase. Supplies the
// CreateHostCursor/CreateHostKeyboard partial methods and the ApplyGamePadState hook with real
// Silk.NET.Input-backed implementations. The per-frame Forms input pump in Update comes from the base, and the typed
// Default/Cursor/Keyboard accessors from Runtimes/SkiaHost/GumService.SkiaHost.cs. Everything else
// -- rendering, .gumx loading, non-interactive Forms controls, hot reload, window-fit, the sync
// context -- comes from GumServiceSkiaBase, the same way SkiaGum.Wpf/SkiaGum.Maui/any
// bring-your-own-canvas consumer gets it, so this class also serves as the reference example for
// writing a custom interactive Skia host.
#if SILK
#nullable enable

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;

namespace Gum;

public partial class GumService : GumServiceSkiaInteractiveBase, IGumService
{
    private IInputContext? _inputContext;

    // Owned by Initialize(IWindow, ...): the Skia GPU context and the surface wrapping the window's framebuffer.
    private IWindow? _window;
    private GRGlInterface? _glInterface;
    private GRContext? _grContext;
    private GRBackendRenderTarget? _renderTarget;
    private SKSurface? _surface;
    private FramebufferResizeTracker? _resizeTracker;
    private Action<Vector2D<int>>? _resizeHandler;

    /// <summary>
    /// Initializes Gum for a Silk.NET window, optionally loading a Gum project. Creates the input
    /// context, the Skia GPU context and the surface over the window's framebuffer, follows window
    /// resizes, and registers the gamepads with Forms controls. Call after <c>window.Initialize()</c>.
    /// </summary>
    /// <remarks>
    /// The window must be created by <c>Silk.NET.Windowing</c> (<c>Window.Create</c>), not wrapped from a
    /// native handle with <c>SdlWindowing.CreateFrom</c>: a wrapped window never delivers input events
    /// (see #3652). The game keeps its own clear and <c>window.GLContext.SwapBuffers()</c> around
    /// <see cref="Update"/> and <see cref="Draw()"/>.
    /// </remarks>
    /// <param name="window">An initialized window with an OpenGL or OpenGL ES context.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(IWindow window, string? gumProjectFile = null) =>
        Initialize(window, DefaultVisualsVersion.Newest, gumProjectFile);

    /// <summary>
    /// Initializes Gum for a Silk.NET window with the given default Forms visuals, optionally loading a
    /// Gum project. See <see cref="Initialize(IWindow, string?)"/> for what it sets up.
    /// </summary>
    /// <param name="window">An initialized window with an OpenGL or OpenGL ES context.</param>
    /// <param name="visualsVersion">The default Forms visuals to register. Skia supports V3 only.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    /// <exception cref="NotSupportedException"><paramref name="visualsVersion"/> is not supported on Skia.</exception>
    public void Initialize(IWindow window, DefaultVisualsVersion visualsVersion, string? gumProjectFile = null)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window));
        }
        if (window.GLContext == null)
        {
            throw new InvalidOperationException(
                "The window has no OpenGL context. Call window.Initialize() first, with an OpenGL or OpenGL ES API in the window options.");
        }

        // Before any window resource is created, so an unsupported version leaves the service untouched.
        ResolveVisualsVersion(visualsVersion);

        ReleaseWindowResources();
        _window = window;

        // Skia asks for optional OpenGL functions that a driver may not have, and GetProcAddress throws for those.
        // TryGetProcAddress returns false instead, which Skia treats as "not supported".
        _glInterface = GRGlInterface.Create(
            name => window.GLContext!.TryGetProcAddress(name, out nint address) ? address : 0);
        _grContext = GRContext.CreateGl(_glInterface);

        Vector2D<int> size = window.FramebufferSize;
        CreateSurface(size.X, size.Y);

        Initialize(_surface!.Canvas, window.CreateInput(), size.X, size.Y, gumProjectFile, visualsVersion);

        FrameworkElement.GamePadsForUiControl.Clear();
        UseGamepadDefaults();

        FramebufferResizeTracker tracker = new FramebufferResizeTracker(
            () => (window.FramebufferSize.X, window.FramebufferSize.Y), size.X, size.Y);
        _resizeTracker = tracker;
        _resizeHandler = _ => tracker.Request();
        window.Resize += _resizeHandler;
    }

    // The surface declares its own logical size, separate from the GL viewport. With a BottomLeft origin
    // (GL framebuffers are bottom-up) Skia uses that height to flip rows, so a stale size shifts everything
    // drawn by the size delta (#3657). It is recreated on every resize.
    private void CreateSurface(int width, int height)
    {
        _surface?.Dispose();
        _renderTarget?.Dispose();

        _renderTarget = new GRBackendRenderTarget(width, height, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8
        _surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
    }

    /// <summary>
    /// The per-frame tick. Applies a window resize that happened since the last frame (recreating the
    /// surface and re-laying out), then updates Gum.
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
    public override void Update(double totalSeconds)
    {
        // Applied here and not in the resize event: dragging a border on Windows runs a nested message loop
        // that raises the event while the GL context is not safe to touch (#3657).
        if (_resizeTracker != null && _resizeTracker.TryConsume(out int width, out int height))
        {
            CreateSurface(width, height);
            // SystemManagers.Canvas is read at draw time; the old canvas was just disposed with its surface.
            SystemManagers.Default.Canvas = _surface!.Canvas;
            HandleResize(width, height);
        }

        base.Update(totalSeconds);
    }

    /// <summary>
    /// Draws Gum into the window's framebuffer. Resets Skia's cached GL state first (the game may have
    /// used GL since the last frame), then flushes. The game clears before and swaps buffers after.
    /// </summary>
    public override void Draw()
    {
        _grContext?.ResetContext();
        base.Draw();
        _surface?.Canvas.Flush();
    }

    /// <inheritdoc/>
    public override void Uninitialize()
    {
        base.Uninitialize();
        ReleaseWindowResources();
    }

    private void ReleaseWindowResources()
    {
        if (_window != null && _resizeHandler != null)
        {
            _window.Resize -= _resizeHandler;
        }
        _resizeHandler = null;
        _resizeTracker = null;
        _window = null;

        _surface?.Dispose();
        _renderTarget?.Dispose();
        _grContext?.Dispose();
        _glInterface?.Dispose();
        _surface = null;
        _renderTarget = null;
        _grContext = null;
        _glInterface = null;
    }

    private partial Cursor CreateHostCursor()
    {
        Cursor cursor = new Cursor();
        if (_inputContext != null)
        {
            cursor.AttachSilkInput(_inputContext);
        }
        return cursor;
    }

    // Real desktop Silk contexts always expose at least one keyboard.
    private partial Keyboard? CreateHostKeyboard() =>
        _inputContext != null && _inputContext.Keyboards.Count > 0 ? new Keyboard(_inputContext.Keyboards[0]) : null;

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time)
    {
        if (_inputContext != null && index < _inputContext.Gamepads.Count)
        {
            Gum.Input.GamePadDriver.Apply(gamepad, _inputContext.Gamepads[index], time);
        }
        else
        {
            gamepad.SetConnected(false);
            gamepad.Activity(time);
        }
    }

    /// <summary>
    /// Initializes Gum for a Silk.NET application, optionally loading a Gum project. The canvas size
    /// is read from <see cref="SKCanvas.DeviceClipBounds"/>; use the explicit-size overload if that
    /// does not match the window.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="inputContext">
    /// The Silk input context, from <c>window.CreateInput()</c> on a window created via
    /// <see cref="Silk.NET.Windowing.Window.Create"/>. Do NOT build this from
    /// <c>SdlWindowing.CreateFrom(existingHandle)</c> wrapping a window you created yourself —
    /// that path skips the view's normal initialization, so it never subscribes to receive input
    /// events; the resulting context looks valid but silently never delivers clicks, key presses,
    /// or typed text (see #3652).
    /// </param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(SKCanvas canvas, IInputContext inputContext, string? gumProjectFile = null)
    {
        var bounds = canvas.DeviceClipBounds;
        Initialize(canvas, inputContext, bounds.Width, bounds.Height, gumProjectFile);
    }

    /// <summary>
    /// Initializes Gum for a Silk.NET application with an explicit canvas size, optionally loading a
    /// Gum project.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="inputContext">
    /// The Silk input context. See the remarks on the <see cref="Initialize(SKCanvas, IInputContext, string?)"/>
    /// overload for a gotcha around how this must be constructed.
    /// </param>
    /// <param name="width">The width to use for the root container and canvas coordinate space.</param>
    /// <param name="height">The height to use for the root container and canvas coordinate space.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(SKCanvas canvas, IInputContext inputContext, int width, int height, string? gumProjectFile = null) =>
        Initialize(canvas, inputContext, width, height, gumProjectFile, DefaultVisualsVersion.Newest);

    private void Initialize(
        SKCanvas canvas, IInputContext inputContext, int width, int height, string? gumProjectFile,
        DefaultVisualsVersion visualsVersion)
    {
        InitializeWithDevices(canvas, width, height, gumProjectFile, () =>
        {
            _inputContext = inputContext;
            Clipboard = CreateClipboard(inputContext);
        }, visualsVersion);
    }

    // The first keyboard's ClipboardText is the OS clipboard on every desktop Silk backend; with no
    // keyboard attached, get returns null and set does nothing.
    internal static IGumClipboard CreateClipboard(IInputContext inputContext) => new DelegateClipboard(
        () => inputContext.Keyboards.Count > 0 ? inputContext.Keyboards[0].ClipboardText : null,
        text =>
        {
            if (inputContext.Keyboards.Count > 0)
            {
                inputContext.Keyboards[0].ClipboardText = text;
            }
        });
}
#endif
