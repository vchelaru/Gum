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
    private const int SurfaceWidth = 800;
    private const int SurfaceHeight = 600;

    private readonly Stopwatch _clock = new Stopwatch();
    private readonly WpfRenderSurfaceHost _host = new WpfRenderSurfaceHost();

    private GraphicsDevice? _graphicsDevice;
    private RenderTarget2D? _renderTarget;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _whitePixel;

    public MainWindow()
    {
        InitializeComponent();

        IntPtr windowHandle = new WindowInteropHelper(this).EnsureHandle();
        CreateGraphicsResources(windowHandle);

        RootGrid.Children.Add(_host.ImageElement);
        _host.RenderFrame += DrawFrame;
        _host.Initialize(SurfaceWidth, SurfaceHeight, desiredFramesPerSecond: 30);

        _clock.Start();
        Closed += (_, _) => DisposeResources();
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
            BackBufferWidth = SurfaceWidth,
            BackBufferHeight = SurfaceHeight,
            BackBufferFormat = SurfaceFormat.Color,
            DepthStencilFormat = DepthFormat.Depth24Stencil8,
            DeviceWindowHandle = windowHandle,
            PresentationInterval = PresentInterval.Immediate,
            RenderTargetUsage = RenderTargetUsage.PreserveContents,
            IsFullScreen = false,
        };

        _graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.FL10_0, presentationParameters);
        _renderTarget = new RenderTarget2D(
            _graphicsDevice, SurfaceWidth, SurfaceHeight, mipMap: false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8,
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

        float seconds = (float)_clock.Elapsed.TotalSeconds;

        _graphicsDevice.SetRenderTarget(_renderTarget);
        _graphicsDevice.Clear(HueToColor(seconds * 40f % 360f));

        // A square orbiting the center, to prove sprite content (not just the clear color) updates.
        float radius = 200f;
        float x = SurfaceWidth / 2f + radius * (float)Math.Cos(seconds) - 25f;
        float y = SurfaceHeight / 2f + radius * (float)Math.Sin(seconds) - 25f;

        _spriteBatch.Begin();
        _spriteBatch.Draw(_whitePixel, new Microsoft.Xna.Framework.Rectangle((int)x, (int)y, 50, 50), Color.White);
        _spriteBatch.End();

        _graphicsDevice.SetRenderTarget(null);
        _renderTarget.GetData(_host.RawImageBuffer);
        _host.PushFrame(_renderTarget.Format);
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
