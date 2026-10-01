using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using InputLibrary;
using Microsoft.Xna.Framework.Graphics;
using XnaAndWinforms;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// Hosts KNI rendering inside an Avalonia visual tree: draws into the render target of a
/// <see cref="GameRenderDeviceHost"/> (sharing the process-wide device), reads it back, and shows
/// it through an <see cref="Image"/> over a <see cref="AvaloniaRenderSurface"/>. Frames run on the
/// UI thread from a render-priority timer through the same <see cref="RenderTargetFrameLoop"/> the
/// WPF control uses, but only when a <see cref="CanvasFrameGate"/> says something changed. Derived
/// classes override <see cref="PreDrawUpdate"/> and <see cref="Draw"/>.
/// </summary>
/// <remarks>
/// <see cref="Visual.Bounds"/> are device-independent units (DIU); the render target and backing
/// bitmap are sized in physical pixels (DIU * <see cref="RenderScaling"/>), so one render-target
/// pixel maps to exactly one physical screen pixel - the canvas is crisp on a scaled display
/// instead of being stretched by Avalonia's own compositing (#4811, parity with the WPF head's
/// #4681/#4682 fix). The bitmap itself always stays at 96 DPI and is displayed with
/// <see cref="Stretch.Fill"/> against an explicit DIU-sized <see cref="Image.Width"/>/
/// <see cref="Image.Height"/> rather than a non-96 DPI stamp - Avalonia's compositor double-scales
/// a <see cref="Stretch.None"/>-displayed bitmap whose DPI isn't 96
/// (<see href="https://github.com/AvaloniaUI/Avalonia/issues/17235"/>).
/// <see cref="AvaloniaInputHostAdapter"/> converts pointer/bounds coordinates to the same
/// physical-pixel units so hit-testing stays in sync with what's drawn.
/// </remarks>
public class AvaloniaGraphicsDeviceControl : Grid, IDisposable, IRenderTargetFrameClient, ICanvasHost
{
    private const double FrameTimerHertz = 60.0;

    private readonly AvaloniaRenderSurface _surface;
    private readonly Image _image;
    private readonly TextBlock _errorTextBlock;
    private readonly DispatcherTimer _frameTimer;

    private ISharedRenderDeviceHost? _deviceHost;
    private RenderTargetFrameLoop? _frameLoop;
    private AvaloniaInputHostAdapter? _inputHost;
    private readonly ICanvasRedrawScheduler _redrawScheduler;
    private readonly CanvasFrameGate _frameGate;
    private float _desiredFramesPerSecondBeforeInit = 30;
    private WindowBase? _hostWindow;

    /// <summary>
    /// Creates the control, drawing only when <paramref name="redrawScheduler"/> or its own surface
    /// says something changed. The shared device is taken on first use, not here.
    /// </summary>
    public AvaloniaGraphicsDeviceControl(ICanvasRedrawScheduler redrawScheduler)
    {
        _redrawScheduler = redrawScheduler;
        _frameGate = new CanvasFrameGate(redrawScheduler);
        _surface = new AvaloniaRenderSurface();

        Focusable = true;
        ClipToBounds = true;
        // A null Background makes a Panel invisible to hit testing, which would leave the canvas
        // unable to receive pointer input or take focus.
        Background = Brushes.Transparent;

        // Anchored top-left so pixel (0,0) of the bitmap stays at the control's origin while a
        // resize is in flight, matching the coordinate space the cursor and camera math assume.
        // Stretch.Fill (not None) is what actually applies the physical-pixel-to-DIU scale via
        // the explicit Width/Height set in HandleRenderTargetRecreated - see the class remarks.
        _image = new Image
        {
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false,
        };
        RenderOptions.SetBitmapInterpolationMode(_image, BitmapInterpolationMode.None);
        Children.Add(_image);

        _errorTextBlock = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black,
            Background = Brushes.CornflowerBlue,
            IsVisible = false,
        };
        Children.Add(_errorTextBlock);

        _frameTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / FrameTimerHertz),
        };
        _frameTimer.Tick += (_, _) => HandleRenderFrame();
    }

    /// <summary>
    /// The frame rate this control aims for. The timer fires at its own cadence; frames beyond
    /// this rate are skipped.
    /// </summary>
    public float DesiredFramesPerSecond
    {
        get => _frameLoop?.DesiredFramesPerSecond ?? _desiredFramesPerSecondBeforeInit;
        set
        {
            _desiredFramesPerSecondBeforeInit = value;
            if (_frameLoop != null)
            {
                _frameLoop.DesiredFramesPerSecond = value;
            }
        }
    }

    /// <summary>The shared device this control draws with.</summary>
    public GraphicsDevice GraphicsDevice => DeviceHost.GraphicsDevice;

    /// <summary>This control's share of the process-wide device, as the render-host contract.</summary>
    public IRenderDeviceHost RenderDeviceHost => DeviceHost;

    /// <summary>
    /// The current display's render scale (1.0 at 100%). Read fresh via <see cref="TopLevel.GetTopLevel"/>
    /// rather than cached, so it stays correct if the control moves to a monitor with a different
    /// scale factor. 1.0 before the control is attached to a window.
    /// </summary>
    protected double RenderScaling => TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

    /// <inheritdoc/>
    public double DisplayScale => DisplayScaleOverride ?? RenderScaling;

    /// <summary>
    /// Replaces the render scale as <see cref="DisplayScale"/> when set. Tests use it to run the
    /// canvas at 200%, which the headless platform cannot do.
    /// </summary>
    internal double? DisplayScaleOverride { get; set; }

    /// <summary>A provider holding the device service, for content managers.</summary>
    public IServiceProvider Services => DeviceHost.Services;

    /// <inheritdoc/>
    public IInputHostControl InputHost => InputAdapter;

    private AvaloniaInputHostAdapter InputAdapter => _inputHost ??= new AvaloniaInputHostAdapter(this);

    /// <summary>
    /// Raised when the canvas loses keyboard focus or its window deactivates. A key released after
    /// that never reaches the canvas, so anything tracking a held key (Space to pan) lets go here.
    /// </summary>
    public event Action? KeyboardInputLost
    {
        add => InputAdapter.KeyboardInputLost += value;
        remove => InputAdapter.KeyboardInputLost -= value;
    }

    /// <summary>Raised once per rendered frame, after <see cref="PreDrawUpdate"/> and before <see cref="Draw"/>.</summary>
    public event Action? XnaUpdate;

    /// <summary>Raised by the default <see cref="Draw"/> implementation.</summary>
    public event Action? XnaDraw;

    /// <summary>Raised when a frame throws. The failure is also shown on the canvas.</summary>
    public event Action<Exception>? ErrorOccurred;

    // Plugin StartUp builds this control long before the head draws anything, and creating the
    // KNI device is not something to do speculatively: on a machine without a usable GL driver
    // the attempt fails, and KNI's half-built device then crashes the process from its
    // finalizer. So the device is taken on first use, the first frame or the first caller
    // asking for it, never in the constructor.
    private ISharedRenderDeviceHost DeviceHost
    {
        get
        {
            EnsureDevice();
            return _deviceHost!;
        }
    }

    private void EnsureDevice()
    {
        if (_deviceHost != null)
        {
            return;
        }
        _deviceHost = new GameRenderDeviceHost();
        _deviceHost.RenderTargetRecreated += HandleRenderTargetRecreated;
        _frameLoop = new RenderTargetFrameLoop(_deviceHost, new FrameRateThrottle())
        {
            DesiredFramesPerSecond = _desiredFramesPerSecondBeforeInit,
        };
    }

    private void HandleRenderTargetRecreated(int width, int height)
    {
        _surface.Resize(width, height);
        _image.Source = _surface.Bitmap;
        // The image's layout size is DIU (matching the control's own Bounds); Stretch.Fill scales
        // the physical-pixel bitmap down to it, which is what makes one bitmap pixel land on
        // exactly one physical screen pixel once Avalonia's own compositor scales the DIU box back
        // up for display (#4811, see the class remarks).
        double scale = RenderScaling;
        _image.Width = width / scale;
        _image.Height = height / scale;
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // Coming back to the window (after editing a file it watches in another app, say) redraws.
        _hostWindow = TopLevel.GetTopLevel(this) as WindowBase;
        if (_hostWindow != null)
        {
            _hostWindow.Activated += HandleHostWindowActivated;
        }
        _frameTimer.Start();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _frameTimer.Stop();
        if (_hostWindow != null)
        {
            _hostWindow.Activated -= HandleHostWindowActivated;
            _hostWindow = null;
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void HandleHostWindowActivated(object? sender, EventArgs e) => _redrawScheduler.RequestRedraw();

    /// <inheritdoc/>
    /// <remarks>Enter and exit are direct events, which the app-wide input hook doesn't see; a hover
    /// highlight has to clear when the pointer leaves.</remarks>
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        _redrawScheduler.RequestRedraw();
        base.OnPointerEntered(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        _redrawScheduler.RequestRedraw();
        base.OnPointerExited(e);
    }

    /// <summary>
    /// Takes keyboard focus when clicked. The canvas only receives keys while focused, and clicking
    /// it is how the user hands focus over from the rest of the UI.
    /// </summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        base.OnPointerPressed(e);
    }

    private void HandleRenderFrame()
    {
        if (Design.IsDesignMode)
        {
            return;
        }
        WindowState? hostWindowState = (TopLevel.GetTopLevel(this) as Window)?.WindowState;
        if (!ShouldRenderFrame(IsVisible && IsEffectivelyVisible, hostWindowState))
        {
            _frameGate.MarkSkipped();
            return;
        }
        EnsureDevice();

        // Bounds.Width/Height are DIU; the frame loop sizes the render target and viewport
        // directly from these, so they must be physical pixels here or the canvas renders at the
        // DIU resolution instead of the display's real one (#4811, parity with #4681).
        double scale = RenderScaling;
        int width = ToPhysicalPixelSize(Bounds.Width, scale);
        int height = ToPhysicalPixelSize(Bounds.Height, scale);

        // Drawing and reading the frame back is nearly all of the canvas's cost, and an idle
        // canvas would otherwise pay it every frame (#4989). A skipped frame skips the per-frame
        // update too: input polling picks up where it left off, and anything that changes on its
        // own keeps the scheduler asking for frames.
        if (!_frameGate.ShouldDraw(width, height, _frameLoop!.Error.HasErrors))
        {
            return;
        }
        if (_frameLoop.TryRenderFrame(width, height, this))
        {
            _frameGate.MarkDrawn(width, height);
            FramePresented?.Invoke();
        }
    }

    /// <summary>
    /// Draws one frame now, whatever the frame timer, the rate throttle and the redraw gate would
    /// say. For tests that drive the canvas one frame per input event, since the canvas polls its
    /// input once per frame.
    /// </summary>
    internal bool RenderFrameNow()
    {
        EnsureDevice();
        double scale = RenderScaling;
        int width = ToPhysicalPixelSize(Bounds.Width, scale);
        int height = ToPhysicalPixelSize(Bounds.Height, scale);
        float desiredFramesPerSecond = _frameLoop!.DesiredFramesPerSecond;
        _frameLoop.DesiredFramesPerSecond = 0;
        bool rendered;
        try
        {
            rendered = _frameLoop.TryRenderFrame(width, height, this);
        }
        finally
        {
            // A failed frame drops the loop to its slow retry rate, which must stand.
            if (!_frameLoop.Error.HasErrors)
            {
                _frameLoop.DesiredFramesPerSecond = desiredFramesPerSecond;
            }
        }
        if (rendered)
        {
            _frameGate.MarkDrawn(width, height);
            FramePresented?.Invoke();
        }
        return rendered;
    }

    /// <summary>Raised after each frame is drawn and pushed to the displayed bitmap.</summary>
    public event Action? FramePresented;

    /// <summary>
    /// Asks for a frame and completes once one has been drawn and presented, so whatever changed
    /// before the call is on screen. Used by the unattended screenshot (#5170).
    /// </summary>
    public Task NextFramePresentedAsync()
    {
        TaskCompletionSource presented = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void HandlePresented()
        {
            FramePresented -= HandlePresented;
            presented.TrySetResult();
        }
        FramePresented += HandlePresented;
        _redrawScheduler.RequestRedraw();
        return presented.Task;
    }

    /// <summary>
    /// Whether a frame should be rendered for a canvas with the given visibility inside a host
    /// window in the given state (null when the canvas isn't in a <see cref="Window"/>). A
    /// minimized window keeps its content "visible" as far as <see cref="Visual.IsEffectivelyVisible"/>
    /// goes, so it's checked explicitly here: rendering while minimized is wasted work, and the
    /// window's bounds change on the way down and back up, which re-creates the render target,
    /// bitmap and readback buffer on every minimize/restore (#4852). Pure/static so it's
    /// unit-testable without a live window.
    /// </summary>
    public static bool ShouldRenderFrame(bool isEffectivelyVisible, WindowState? hostWindowState) =>
        isEffectivelyVisible && hostWindowState != WindowState.Minimized;

    /// <summary>
    /// Converts a device-independent (DIU) size to a physical pixel count for the given render
    /// scale, rounding to the nearest pixel and clamping to a minimum of 1 (a render target/bitmap
    /// cannot be zero-sized). Pure/static so it's unit-testable without a live Avalonia visual tree.
    /// </summary>
    public static int ToPhysicalPixelSize(double diuSize, double dpiScale) =>
        Math.Max(1, (int)Math.Round(diuSize * dpiScale));

    void IRenderTargetFrameClient.PreDrawUpdate() => PreDrawUpdate();

    void IRenderTargetFrameClient.Draw()
    {
        XnaUpdate?.Invoke();
        Draw();
    }

    void IRenderTargetFrameClient.Present(RenderTarget2D renderTarget)
    {
        renderTarget.GetData(_surface.RawImageBuffer);
        _surface.Push(renderTarget.Format, forceOpaque: IsFrameOpaque);
        _image.InvalidateVisual();
    }

    void IRenderTargetFrameClient.ShowError(string? message)
    {
        _errorTextBlock.Text = message ?? string.Empty;
        _errorTextBlock.IsVisible = message != null;
    }

    void IRenderTargetFrameClient.ReportError(Exception exception) => ErrorOccurred?.Invoke(exception);

    /// <summary>
    /// Whether the frame just drawn covers the whole canvas with an opaque background. When true,
    /// the frame is shown fully opaque (see <see cref="AvaloniaRenderSurface.Push"/>); when false,
    /// its alpha is kept and the window behind the canvas shows through transparent pixels.
    /// </summary>
    protected virtual bool IsFrameOpaque => false;

    /// <summary>Derived classes override this to run per-frame logic before drawing.</summary>
    protected virtual void PreDrawUpdate()
    {
    }

    /// <summary>Derived classes override this to draw themselves using the GraphicsDevice.</summary>
    protected virtual void Draw()
    {
        XnaDraw?.Invoke();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _frameTimer.Stop();
        _surface.Dispose();
        if (_deviceHost != null)
        {
            _deviceHost.RenderTargetRecreated -= HandleRenderTargetRecreated;
            _deviceHost.Dispose();
            _deviceHost = null;
        }
        _frameLoop = null;
        GC.SuppressFinalize(this);
    }
}
