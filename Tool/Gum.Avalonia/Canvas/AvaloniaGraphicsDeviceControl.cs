using System;
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
/// WPF control uses. Derived classes override <see cref="PreDrawUpdate"/> and <see cref="Draw"/>.
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
    private float _desiredFramesPerSecondBeforeInit = 30;

    /// <summary>Creates the control. The shared device is taken on first use, not here.</summary>
    public AvaloniaGraphicsDeviceControl()
    {
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
    public double DisplayScale => RenderScaling;

    /// <summary>A provider holding the device service, for content managers.</summary>
    public IServiceProvider Services => DeviceHost.Services;

    /// <inheritdoc/>
    public IInputHostControl InputHost => _inputHost ??= new AvaloniaInputHostAdapter(this);

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
        _frameTimer.Start();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _frameTimer.Stop();
        base.OnDetachedFromVisualTree(e);
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
            return;
        }
        EnsureDevice();

        // Bounds.Width/Height are DIU; the frame loop sizes the render target and viewport
        // directly from these, so they must be physical pixels here or the canvas renders at the
        // DIU resolution instead of the display's real one (#4811, parity with #4681).
        double scale = RenderScaling;
        int width = ToPhysicalPixelSize(Bounds.Width, scale);
        int height = ToPhysicalPixelSize(Bounds.Height, scale);
        _frameLoop!.TryRenderFrame(width, height, this);
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
        _surface.Push(renderTarget.Format);
        _image.InvalidateVisual();
    }

    void IRenderTargetFrameClient.ShowError(string? message)
    {
        _errorTextBlock.Text = message ?? string.Empty;
        _errorTextBlock.IsVisible = message != null;
    }

    void IRenderTargetFrameClient.ReportError(Exception exception) => ErrorOccurred?.Invoke(exception);

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
