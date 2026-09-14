using Microsoft.Xna.Framework.Graphics;
using Moq;
using Shouldly;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XnaAndWinforms;
using Xunit;

namespace GumToolUnitTests.Wireframe;

public class WpfRenderSurfaceHostTests
{
    // Wires the mock surface's Bitmap getter to whatever Resize was most recently called with,
    // mirroring how the real WriteableBitmapRenderSurface behaves.
    private static Mock<IWriteableBitmapRenderSurface> CreateSurfaceMock()
    {
        Mock<IWriteableBitmapRenderSurface> surface = new Mock<IWriteableBitmapRenderSurface>();
        WriteableBitmap? bitmap = null;
        byte[] rawImageBuffer = new byte[0];
        surface.Setup(s => s.Resize(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>()))
            .Callback<int, int, double>((width, height, dpiScale) =>
            {
                bitmap = new WriteableBitmap(width, height, 96 * dpiScale, 96 * dpiScale, PixelFormats.Pbgra32, null);
                rawImageBuffer = new byte[width * height * 4];
            });
        surface.Setup(s => s.Bitmap).Returns(() => bitmap);
        surface.Setup(s => s.RawImageBuffer).Returns(() => rawImageBuffer);
        return surface;
    }

    [StaFact]
    public void Dispose_StopsTimer()
    {
        Mock<IWriteableBitmapRenderSurface> surface = CreateSurfaceMock();
        WpfRenderSurfaceHost host = new WpfRenderSurfaceHost(surface.Object);
        host.Initialize(width: 4, height: 4, dpiScale: 1.0);

        host.Dispose();

        host.IsRunning.ShouldBeFalse();
    }

    [StaFact]
    public void Initialize_StartsTimer()
    {
        Mock<IWriteableBitmapRenderSurface> surface = CreateSurfaceMock();
        WpfRenderSurfaceHost host = new WpfRenderSurfaceHost(surface.Object);

        host.Initialize(width: 4, height: 4, dpiScale: 1.0);

        host.IsRunning.ShouldBeTrue();
    }

    [StaFact]
    public void Initialize_SetsImageElementSourceToSurfaceBitmap()
    {
        Mock<IWriteableBitmapRenderSurface> surface = CreateSurfaceMock();
        WpfRenderSurfaceHost host = new WpfRenderSurfaceHost(surface.Object);

        host.Initialize(width: 4, height: 4, dpiScale: 1.0);

        host.ImageElement.Source.ShouldBeSameAs(surface.Object.Bitmap);
    }

    [StaFact]
    public void PushFrame_DelegatesToSurface()
    {
        Mock<IWriteableBitmapRenderSurface> surface = CreateSurfaceMock();
        WpfRenderSurfaceHost host = new WpfRenderSurfaceHost(surface.Object);
        host.Initialize(width: 4, height: 4, dpiScale: 1.0);

        host.PushFrame(SurfaceFormat.Color);

        surface.Verify(s => s.Push(SurfaceFormat.Color), Times.Once);
    }

    [StaFact]
    public void Resize_UpdatesImageElementSource()
    {
        Mock<IWriteableBitmapRenderSurface> surface = CreateSurfaceMock();
        WpfRenderSurfaceHost host = new WpfRenderSurfaceHost(surface.Object);
        host.Initialize(width: 4, height: 4, dpiScale: 1.0);

        host.Resize(width: 8, height: 8, dpiScale: 1.0);

        host.ImageElement.Source.ShouldBeSameAs(surface.Object.Bitmap);
        ((WriteableBitmap)host.ImageElement.Source).PixelWidth.ShouldBe(8);
    }

    [StaFact]
    public void Resize_PassesDpiScaleToSurface()
    {
        Mock<IWriteableBitmapRenderSurface> surface = CreateSurfaceMock();
        WpfRenderSurfaceHost host = new WpfRenderSurfaceHost(surface.Object);
        host.Initialize(width: 4, height: 4, dpiScale: 1.0);

        host.Resize(width: 8, height: 8, dpiScale: 2.0);

        surface.Verify(s => s.Resize(8, 8, 2.0), Times.Once);
    }
}
