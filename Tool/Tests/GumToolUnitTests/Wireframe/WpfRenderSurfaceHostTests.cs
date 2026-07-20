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
        surface.Setup(s => s.Resize(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((width, height) =>
            {
                bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
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
        host.Initialize(width: 4, height: 4);

        host.Dispose();

        host.IsRunning.ShouldBeFalse();
    }

    [StaFact]
    public void Initialize_StartsTimer()
    {
        Mock<IWriteableBitmapRenderSurface> surface = CreateSurfaceMock();
        WpfRenderSurfaceHost host = new WpfRenderSurfaceHost(surface.Object);

        host.Initialize(width: 4, height: 4);

        host.IsRunning.ShouldBeTrue();
    }

    [StaFact]
    public void Initialize_SetsImageElementSourceToSurfaceBitmap()
    {
        Mock<IWriteableBitmapRenderSurface> surface = CreateSurfaceMock();
        WpfRenderSurfaceHost host = new WpfRenderSurfaceHost(surface.Object);

        host.Initialize(width: 4, height: 4);

        host.ImageElement.Source.ShouldBeSameAs(surface.Object.Bitmap);
    }

    [StaFact]
    public void PushFrame_DelegatesToSurface()
    {
        Mock<IWriteableBitmapRenderSurface> surface = CreateSurfaceMock();
        WpfRenderSurfaceHost host = new WpfRenderSurfaceHost(surface.Object);
        host.Initialize(width: 4, height: 4);

        host.PushFrame(SurfaceFormat.Color);

        surface.Verify(s => s.Push(SurfaceFormat.Color), Times.Once);
    }

    [StaFact]
    public void Resize_UpdatesImageElementSource()
    {
        Mock<IWriteableBitmapRenderSurface> surface = CreateSurfaceMock();
        WpfRenderSurfaceHost host = new WpfRenderSurfaceHost(surface.Object);
        host.Initialize(width: 4, height: 4);

        host.Resize(width: 8, height: 8);

        host.ImageElement.Source.ShouldBeSameAs(surface.Object.Bitmap);
        ((WriteableBitmap)host.ImageElement.Source).PixelWidth.ShouldBe(8);
    }
}
