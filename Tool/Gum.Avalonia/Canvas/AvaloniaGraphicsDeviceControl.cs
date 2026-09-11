using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
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
/// Sizes its surface from <see cref="Visual.Bounds"/>, which are device-independent units, and
/// the input adapter reports the pointer in the same units, so cursor and camera math agree. On a
/// scaled display the canvas renders at fewer pixels than the monitor has and Avalonia scales the
/// bitmap up, matching the WPF head.
/// </remarks>
public class AvaloniaGraphicsDeviceControl : Grid, IDisposable, IRenderTargetFrameClient
{
    private const double FrameTimerHertz = 60.0;

    private readonly AvaloniaRenderSurface _surface;
    private readonly Image _image;
    private readonly TextBlock _errorTextBlock;
    private readonly DispatcherTimer _frameTimer;

    private ISharedRenderDeviceHost? _deviceHost;
    private RenderTargetFrameLoop? _frameLoop;
    private float _desiredFramesPerSecondBeforeInit = 30;

    /// <summary>Creates the control and, outside the designer, the shared device.</summary>
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
        _image = new Image
        {
            Stretch = Stretch.None,
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

        if (!Design.IsDesignMode)
        {
            InitializeDevice();
        }
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
    public GraphicsDevice GraphicsDevice => _deviceHost!.GraphicsDevice;

    /// <summary>This control's share of the process-wide device, as the render-host contract.</summary>
    public IRenderDeviceHost RenderDeviceHost => _deviceHost!;

    /// <summary>A provider holding the device service, for content managers.</summary>
    public IServiceProvider Services => _deviceHost!.Services;

    /// <summary>Raised once per rendered frame, after <see cref="PreDrawUpdate"/> and before <see cref="Draw"/>.</summary>
    public event Action? XnaUpdate;

    /// <summary>Raised by the default <see cref="Draw"/> implementation.</summary>
    public event Action? XnaDraw;

    /// <summary>Raised when a frame throws. The failure is also shown on the canvas.</summary>
    public event Action<Exception>? ErrorOccurred;

    private void InitializeDevice()
    {
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
        _image.Width = width;
        _image.Height = height;
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
        if (_frameLoop == null || !IsVisible || !IsEffectivelyVisible)
        {
            return;
        }
        _frameLoop.TryRenderFrame((int)Bounds.Width, (int)Bounds.Height, this);
    }

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
