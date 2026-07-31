using Microsoft.Xna.Framework.Graphics;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace XnaAndWinforms;

/// <summary>
/// Hosts XNA/KNI rendering inside a WPF visual tree - the WPF counterpart to
/// <see cref="GraphicsDeviceControl"/>. Draws into the <see cref="RenderTarget2D"/> of a
/// <see cref="ISharedRenderDeviceHost"/> (so it shares the one process-wide
/// <see cref="Microsoft.Xna.Framework.Graphics.GraphicsDevice"/> with every other client), reads it
/// back, and pushes it into a <see cref="IWpfRenderSurfaceHost"/>'s <c>WriteableBitmap</c>.
/// Derived classes override <see cref="PreDrawUpdate"/> and <see cref="Draw"/>.
/// </summary>
/// <remarks>
/// Sizes its surface from <see cref="FrameworkElement.ActualWidth"/>/<see cref="FrameworkElement.ActualHeight"/>,
/// which are device-independent units. Cursor coordinates arrive through the same units (see
/// <c>InputLibrary.WpfInputHostAdapter</c>), so the two stay consistent; the cost is that on a
/// scaled display the canvas renders at fewer pixels than the monitor has and WPF scales the bitmap up.
/// </remarks>
public class WpfGraphicsDeviceControl : Grid, IDisposable
{
    #region Fields

    private readonly IWpfRenderSurfaceHost _surfaceHost;
    private readonly IFrameRateThrottle _frameRateThrottle;
    private readonly RenderingError _renderError;
    private readonly Stopwatch _frameClock;
    private readonly TextBlock _errorTextBlock;

    private ISharedRenderDeviceHost? _deviceHost;
    private double _lastFrameMilliseconds;
    private bool _isRenderingFrame;

    #endregion

    #region Properties

    /// <summary>
    /// The frame rate this control aims for. The underlying WPF render pass fires at the
    /// compositor's cadence; frames beyond this rate are skipped.
    /// </summary>
    public float DesiredFramesPerSecond { get; set; }

    /// <summary>Gets the shared GraphicsDevice this control draws with.</summary>
    public GraphicsDevice GraphicsDevice => _deviceHost!.GraphicsDevice;

    /// <summary>
    /// Gets this control's share of the process-wide graphics device, for code that only needs the
    /// render-host contract rather than the control itself.
    /// </summary>
    public IRenderDeviceHost RenderDeviceHost => _deviceHost!;

    /// <summary>
    /// Gets an IServiceProvider containing the IGraphicsDeviceService, for components such as
    /// ContentManager which look the device up through it.
    /// </summary>
    public IServiceProvider Services => _deviceHost!.Services;

    #endregion

    #region Events

    /// <summary>Raised once per rendered frame, after <see cref="PreDrawUpdate"/> and before <see cref="Draw"/>.</summary>
    public event Action? XnaUpdate;

    /// <summary>Raised by the default <see cref="Draw"/> implementation.</summary>
    public event Action? XnaDraw;

    /// <summary>Raised when a frame throws. The failure is also shown on the canvas.</summary>
    public event Action<Exception>? ErrorOccurred;

    #endregion

    public WpfGraphicsDeviceControl()
        : this(new WpfRenderSurfaceHost(), new FrameRateThrottle())
    {
    }

    public WpfGraphicsDeviceControl(IWpfRenderSurfaceHost surfaceHost, IFrameRateThrottle frameRateThrottle)
    {
        _surfaceHost = surfaceHost;
        _frameRateThrottle = frameRateThrottle;
        _renderError = new RenderingError();
        _frameClock = new Stopwatch();
        _lastFrameMilliseconds = double.NegativeInfinity;
        DesiredFramesPerSecond = 30;

        Focusable = true;
        ClipToBounds = true;
        // A null Background makes a Panel invisible to hit testing, which would leave the canvas
        // unable to receive mouse input or take focus.
        Background = Brushes.Transparent;

        // Anchored top-left rather than the default stretch/center so pixel (0,0) of the bitmap
        // stays at the control's origin while a resize is in flight, matching the coordinate space
        // the cursor and camera math assume.
        _surfaceHost.ImageElement.HorizontalAlignment = HorizontalAlignment.Left;
        _surfaceHost.ImageElement.VerticalAlignment = VerticalAlignment.Top;
        _surfaceHost.ImageElement.IsHitTestVisible = false;
        Children.Add(_surfaceHost.ImageElement);

        _errorTextBlock = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Black,
            Background = Brushes.CornflowerBlue,
            Visibility = Visibility.Collapsed,
        };
        Children.Add(_errorTextBlock);

        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            InitializeDevice();
        }

        _frameClock.Start();
    }

    #region Initialization

    private void InitializeDevice()
    {
        int width = Math.Max(1, (int)ActualWidth);
        int height = Math.Max(1, (int)ActualHeight);

        _deviceHost = new SharedRenderDeviceHost(GetWindowHandle(), width, height);
        _deviceHost.RenderTargetRecreated += HandleRenderTargetRecreated;

        _surfaceHost.Initialize(width, height);
        _surfaceHost.RenderFrame += HandleRenderFrame;
    }

    /// <summary>
    /// The window handle the graphics device presents against. This control is normally constructed
    /// before it is attached to a window, so it falls back to the application's main window.
    /// </summary>
    protected virtual IntPtr GetWindowHandle()
    {
        if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
        {
            return hwndSource.Handle;
        }

        Window? mainWindow = Application.Current?.MainWindow;
        return mainWindow != null ? new WindowInteropHelper(mainWindow).EnsureHandle() : IntPtr.Zero;
    }

    // The bitmap the frame is pushed into and the raw readback buffer are both sized to the render
    // target, so the host recreating it is what drives resizing them.
    private void HandleRenderTargetRecreated(int width, int height) => _surfaceHost.Resize(width, height);

    #endregion

    #region Render loop

    private void HandleRenderFrame()
    {
        if (_isRenderingFrame || _deviceHost == null || !IsVisible)
        {
            return;
        }

        int width = (int)ActualWidth;
        int height = (int)ActualHeight;
        if (width < 1 || height < 1)
        {
            return;
        }

        double nowMilliseconds = _frameClock.Elapsed.TotalMilliseconds;
        if (!_frameRateThrottle.ShouldRenderFrame(nowMilliseconds - _lastFrameMilliseconds, DesiredFramesPerSecond))
        {
            return;
        }
        _lastFrameMilliseconds = nowMilliseconds;

        _isRenderingFrame = true;
        try
        {
            RenderFrame(width, height);
        }
        finally
        {
            _isRenderingFrame = false;
        }
    }

    private void RenderFrame(int width, int height)
    {
        PreDrawUpdate();

        if (!_renderError.HasErrors)
        {
            BeginDraw(width, height);
        }

        if (!_renderError.HasErrors)
        {
            try
            {
                XnaUpdate?.Invoke();
                Draw();
                EndDraw();
                ShowError(null);
            }
            catch (Exception exception)
            {
                ErrorOccurred?.Invoke(exception);
                _renderError.Message = exception.ToString();
            }
        }
        else
        {
            ShowError(_renderError.ProcessedMessage);
            DesiredFramesPerSecond = 0.5f;
        }
    }

    private void BeginDraw(int width, int height)
    {
        if (!_renderError.HasErrors || _renderError.GraphicsDeviceNeedsReset)
        {
            _deviceHost!.EnsureSurfaceSize(width, height, _renderError);
        }

        if (!_renderError.HasErrors)
        {
            GraphicsDevice.SetRenderTarget(_deviceHost!.RenderTarget);
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);

            GraphicsDevice.Viewport = new Viewport
            {
                X = 0,
                Y = 0,
                Width = width,
                Height = height,
                MinDepth = 0,
                MaxDepth = 1,
            };
        }
    }

    private void EndDraw()
    {
        try
        {
            GraphicsDevice.SetRenderTarget(null);

            RenderTarget2D? renderTarget = _deviceHost!.RenderTarget;
            if (renderTarget != null)
            {
                renderTarget.GetData(_surfaceHost.RawImageBuffer);
                _surfaceHost.PushFrame(renderTarget.Format);
            }
        }
        catch
        {
            // The device can be lost mid-frame; the next BeginDraw handles the reset, so the
            // dropped frame is swallowed here the same way GraphicsDeviceControl does.
        }
    }

    private void ShowError(string? message)
    {
        _errorTextBlock.Text = message ?? string.Empty;
        _errorTextBlock.Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
    }

    #endregion

    #region Protected Virtual Methods

    /// <summary>Derived classes override this to run per-frame logic before drawing.</summary>
    protected virtual void PreDrawUpdate()
    {
    }

    /// <summary>Derived classes override this to draw themselves using the GraphicsDevice.</summary>
    protected virtual void Draw()
    {
        XnaDraw?.Invoke();
    }

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        _surfaceHost.RenderFrame -= HandleRenderFrame;
        _surfaceHost.Dispose();

        if (_deviceHost != null)
        {
            _deviceHost.RenderTargetRecreated -= HandleRenderTargetRecreated;
            _deviceHost.Dispose();
            _deviceHost = null;
        }

        GC.SuppressFinalize(this);
    }
}
