using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using XnaAndWinforms;
using Color = Microsoft.Xna.Framework.Color;

namespace WpfRenderSurfaceHostHarness;

/// <summary>
/// Throwaway visual proof for #3833: a WPF-native <c>Image</c>/<c>WriteableBitmap</c>
/// surface, driven purely by <see cref="WpfRenderSurfaceHost"/>'s own render-loop timer, displaying
/// content drawn with an XNA/KNI <see cref="GraphicsDevice"/> - no WinForms, no
/// <c>WindowsFormsHost</c>. This window owns the <see cref="GraphicsDevice"/> and
/// <see cref="RenderTarget2D"/>; the host only knows about the bitmap/pixel-push pipeline. Not part
/// of the Gum tool - this is a standalone check that the approach renders correctly on screen.
/// </summary>
public partial class MainWindow : Window
{
    private const int InitialSurfaceWidth = 800;
    private const int InitialSurfaceHeight = 600;

    private readonly Stopwatch _clock = new Stopwatch();
    private readonly WpfRenderSurfaceHost _host = new WpfRenderSurfaceHost();

    private GraphicsDevice? _graphicsDevice;
    private RenderTarget2D? _renderTarget;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _whitePixel;

    // Current render-target size in pixels - starts at the initial values above, but changes on
    // every live resize (see Window_SizeChanged). This is the actual buffer size being copied
    // GPU->CPU->WriteableBitmap each frame, so resizing the window is the real stress test for
    // whether that copy cost scales badly with larger canvases.
    private int _surfaceWidth = InitialSurfaceWidth;
    private int _surfaceHeight = InitialSurfaceHeight;

    // Actual measured throughput, not a guess - counts real DrawFrame calls against wall-clock
    // time, updated roughly twice a second so the number is legible.
    private readonly Stopwatch _fpsWindow = new Stopwatch();
    private int _framesSinceFpsUpdate;

    // High-level phase breakdown - bisects the pipeline into GPU draw / GPU->CPU readback / WPF
    // push, so a flat total fps number (or one that doesn't move with buffer size) can be traced to
    // which stage actually dominates, instead of guessing. One reused Stopwatch, three accumulators;
    // deliberately not more granular than this until a phase is shown to actually be the culprit.
    private readonly Stopwatch _phaseTimer = new Stopwatch();
    private double _drawMsAccum;
    private double _readbackMsAccum;
    private double _pushMsAccum;

    // The three phases above only cover time actually spent inside our own code. If their sum is
    // far below the measured frame period (1000/fps), the rest is happening between DrawFrame
    // calls - either the DispatcherTimer isn't ticking as often as requested, or WPF's own
    // (asynchronous, separate-thread) compositor work isn't visible to a stopwatch wrapped around
    // the synchronous PushFrame call. This measures that gap directly instead of inferring it.
    private double? _previousFrameStartMs;
    private double _frameGapMsAccum;

    public MainWindow()
    {
        InitializeComponent();

        IntPtr windowHandle = new WindowInteropHelper(this).EnsureHandle();
        CreateGraphicsResources(windowHandle);

        // Insert below the XAML-declared FpsText so the overlay stays on top.
        RootGrid.Children.Insert(0, _host.ImageElement);
        _host.RenderFrame += DrawFrame;
        _host.Initialize(_surfaceWidth, _surfaceHeight);

        _clock.Start();
        _fpsWindow.Start();
        Closed += (_, _) => DisposeResources();
        SizeChanged += Window_SizeChanged;
        MouseLeftButtonDown += (_, _) => CopyStatsToClipboardAndDebugOutput();
    }

    // Click-to-report: the stats overlay is a screenshot otherwise, which is slower to read back
    // and paste than plain text. Prints to the VS Output window (via Debug.WriteLine, visible
    // whenever running under the debugger) and the clipboard, so either works.
    private void CopyStatsToClipboardAndDebugOutput()
    {
        string stats = FpsText.Text.Replace('\n', ' ');
        Debug.WriteLine($"[WpfRenderSurfaceHostHarness] {stats}");
        SetClipboardTextWithRetry(stats);
    }

    // The clipboard is a single systemwide resource - OpenClipboard legitimately fails with
    // CLIPBRD_E_CANT_OPEN when another process (a clipboard manager, an AV scanner, etc.) is
    // holding it at that instant. Retrying briefly is the standard fix, not a one-shot try/catch.
    // The text is already in the VS Output window regardless (see caller), so a clipboard failure
    // after exhausting retries is logged, not thrown - it shouldn't crash the harness.
    private static void SetClipboardTextWithRetry(string text, int maxAttempts = 5, int delayMs = 50)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return;
            }
            catch (System.Runtime.InteropServices.COMException) when (attempt < maxAttempts)
            {
                System.Threading.Thread.Sleep(delayMs);
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Debug.WriteLine($"[WpfRenderSurfaceHostHarness] Clipboard.SetText failed after {maxAttempts} attempts: {ex.Message}");
            }
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        int newWidth = Math.Max(1, (int)e.NewSize.Width);
        int newHeight = Math.Max(1, (int)e.NewSize.Height);

        if (newWidth == _surfaceWidth && newHeight == _surfaceHeight)
        {
            return;
        }

        _surfaceWidth = newWidth;
        _surfaceHeight = newHeight;

        // The render target is the only thing that needs recreating at the new pixel size -
        // GraphicsDevice.SetRenderTarget adjusts the viewport to match automatically, and this
        // harness never presents to a swap chain (it only reads the target back to the CPU), so
        // the device's own PresentationParameters backbuffer size is irrelevant here.
        _renderTarget?.Dispose();
        _renderTarget = new RenderTarget2D(
            _graphicsDevice!, _surfaceWidth, _surfaceHeight, mipMap: false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8,
            preferredMultiSampleCount: 1, RenderTargetUsage.PreserveContents);

        _host.Resize(_surfaceWidth, _surfaceHeight);
    }

    private void DisposeResources()
    {
        _host.Dispose();
        _whitePixel?.Dispose();
        _spriteBatch?.Dispose();
        _renderTarget?.Dispose();
        _graphicsDevice?.Dispose();
    }

    private void CreateGraphicsResources(IntPtr windowHandle)
    {
        PresentationParameters presentationParameters = new PresentationParameters
        {
            BackBufferWidth = _surfaceWidth,
            BackBufferHeight = _surfaceHeight,
            BackBufferFormat = SurfaceFormat.Color,
            DepthStencilFormat = DepthFormat.Depth24Stencil8,
            DeviceWindowHandle = windowHandle,
            PresentationInterval = PresentInterval.Immediate,
            RenderTargetUsage = RenderTargetUsage.PreserveContents,
            IsFullScreen = false,
        };

        _graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.FL10_0, presentationParameters);
        _renderTarget = new RenderTarget2D(
            _graphicsDevice, _surfaceWidth, _surfaceHeight, mipMap: false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8,
            preferredMultiSampleCount: 1, RenderTargetUsage.PreserveContents);
        _spriteBatch = new SpriteBatch(_graphicsDevice);

        _whitePixel = new Texture2D(_graphicsDevice, width: 1, height: 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    // Draws animated content into the render target, reads it back, and pushes it into the WPF
    // WriteableBitmap - proving the whole GraphicsDevice -> RenderTarget2D -> WriteableBitmap ->
    // Image pipeline updates correctly on screen every frame.
    private void DrawFrame()
    {
        Debug.Assert(_graphicsDevice != null && _renderTarget != null && _spriteBatch != null && _whitePixel != null,
            "CreateGraphicsResources must run before DrawFrame.");

        double frameStartMs = _clock.Elapsed.TotalMilliseconds;
        if (_previousFrameStartMs is { } previousMs)
        {
            _frameGapMsAccum += frameStartMs - previousMs;
        }
        _previousFrameStartMs = frameStartMs;

        float seconds = (float)_clock.Elapsed.TotalSeconds;

        _graphicsDevice.SetRenderTarget(_renderTarget);
        _graphicsDevice.Clear(HueToColor(seconds * 40f % 360f));

        // A square orbiting the center, to prove sprite content (not just the clear color) updates.
        float radius = 200f;
        float x = _surfaceWidth / 2f + radius * (float)Math.Cos(seconds) - 25f;
        float y = _surfaceHeight / 2f + radius * (float)Math.Sin(seconds) - 25f;

        _phaseTimer.Restart();
        _spriteBatch.Begin();
        _spriteBatch.Draw(_whitePixel, new Microsoft.Xna.Framework.Rectangle((int)x, (int)y, 50, 50), Color.White);
        _spriteBatch.End();
        _graphicsDevice.SetRenderTarget(null);
        _drawMsAccum += _phaseTimer.Elapsed.TotalMilliseconds;

        _phaseTimer.Restart();
        _renderTarget.GetData(_host.RawImageBuffer);
        _readbackMsAccum += _phaseTimer.Elapsed.TotalMilliseconds;

        _phaseTimer.Restart();
        _host.PushFrame(_renderTarget.Format);
        _pushMsAccum += _phaseTimer.Elapsed.TotalMilliseconds;

        UpdateFpsDisplay();
    }

    private void UpdateFpsDisplay()
    {
        _framesSinceFpsUpdate++;

        double elapsedSeconds = _fpsWindow.Elapsed.TotalSeconds;
        if (elapsedSeconds < 0.5)
        {
            return;
        }

        double measuredFps = _framesSinceFpsUpdate / elapsedSeconds;
        double avgDrawMs = _drawMsAccum / _framesSinceFpsUpdate;
        double avgReadbackMs = _readbackMsAccum / _framesSinceFpsUpdate;
        double avgPushMs = _pushMsAccum / _framesSinceFpsUpdate;
        // One fewer gap sample than frames (no "previous" for the window's first frame) - close
        // enough over a ~0.5s window to not bother correcting for.
        double avgGapMs = _frameGapMsAccum / _framesSinceFpsUpdate;

        FpsText.Text =
            $"FPS: {measuredFps:0.0}  ({_surfaceWidth}x{_surfaceHeight})\n" +
            $"draw: {avgDrawMs:0.00}ms  readback: {avgReadbackMs:0.00}ms  push: {avgPushMs:0.00}ms\n" +
            $"frame-to-frame gap: {avgGapMs:0.00}ms  (unaccounted-for: {avgGapMs - avgDrawMs - avgReadbackMs - avgPushMs:0.00}ms)";

        _framesSinceFpsUpdate = 0;
        _drawMsAccum = 0;
        _readbackMsAccum = 0;
        _pushMsAccum = 0;
        _frameGapMsAccum = 0;
        _fpsWindow.Restart();
    }

    // Cheap HSV(hue, 1, 1)->RGB conversion so the clear color visibly cycles frame to frame.
    private static Color HueToColor(float hueDegrees)
    {
        float c = 1f;
        float x = c * (1f - Math.Abs((hueDegrees / 60f % 2f) - 1f));
        (float r, float g, float b) = hueDegrees switch
        {
            < 60f => (c, x, 0f),
            < 120f => (x, c, 0f),
            < 180f => (0f, c, x),
            < 240f => (0f, x, c),
            < 300f => (x, 0f, c),
            _ => (c, 0f, x),
        };
        return new Color(r, g, b);
    }
}
