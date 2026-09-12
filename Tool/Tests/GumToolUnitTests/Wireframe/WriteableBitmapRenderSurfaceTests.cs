using Microsoft.Xna.Framework.Graphics;
using Moq;
using Shouldly;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XnaAndWinforms;
using Xunit;

namespace GumToolUnitTests.Wireframe;

public class WriteableBitmapRenderSurfaceTests
{
    [Fact]
    public void Push_AfterResize_DelegatesRawImageBufferAndFormatToPixelBufferWriter()
    {
        Mock<IWriteableBitmapPixelBufferWriter> pixelBufferWriter = new Mock<IWriteableBitmapPixelBufferWriter>();
        WriteableBitmapRenderSurface surface = new WriteableBitmapRenderSurface(pixelBufferWriter.Object);
        surface.Resize(width: 4, height: 3, dpiScale: 1.0);

        surface.Push(SurfaceFormat.Color);

        pixelBufferWriter.Verify(w => w.WriteToBitmap(surface.RawImageBuffer, SurfaceFormat.Color, surface.Bitmap!), Times.Once);
    }

    [Fact]
    public void Push_BeforeResize_Throws()
    {
        Mock<IWriteableBitmapPixelBufferWriter> pixelBufferWriter = new Mock<IWriteableBitmapPixelBufferWriter>();
        WriteableBitmapRenderSurface surface = new WriteableBitmapRenderSurface(pixelBufferWriter.Object);

        Should.Throw<InvalidOperationException>(() => surface.Push(SurfaceFormat.Color));
    }

    [Fact]
    public void Resize_AllocatesRawImageBufferSizedForDimensions()
    {
        Mock<IWriteableBitmapPixelBufferWriter> pixelBufferWriter = new Mock<IWriteableBitmapPixelBufferWriter>();
        WriteableBitmapRenderSurface surface = new WriteableBitmapRenderSurface(pixelBufferWriter.Object);

        surface.Resize(width: 8, height: 5, dpiScale: 1.0);

        surface.RawImageBuffer.Length.ShouldBe(8 * 5 * 4);
    }

    [Fact]
    public void Resize_CreatesBitmapWithGivenDimensions()
    {
        Mock<IWriteableBitmapPixelBufferWriter> pixelBufferWriter = new Mock<IWriteableBitmapPixelBufferWriter>();
        WriteableBitmapRenderSurface surface = new WriteableBitmapRenderSurface(pixelBufferWriter.Object);

        surface.Resize(width: 10, height: 6, dpiScale: 1.0);

        WriteableBitmap? bitmap = surface.Bitmap;
        bitmap.ShouldNotBeNull();
        bitmap!.PixelWidth.ShouldBe(10);
        bitmap.PixelHeight.ShouldBe(6);
        bitmap.Format.ShouldBe(PixelFormats.Pbgra32);
        surface.Width.ShouldBe(10);
        surface.Height.ShouldBe(6);
    }

    [Fact]
    public void Resize_NonPositiveWidthOrHeight_Throws()
    {
        Mock<IWriteableBitmapPixelBufferWriter> pixelBufferWriter = new Mock<IWriteableBitmapPixelBufferWriter>();
        WriteableBitmapRenderSurface surface = new WriteableBitmapRenderSurface(pixelBufferWriter.Object);

        Should.Throw<ArgumentOutOfRangeException>(() => surface.Resize(width: 0, height: 4, dpiScale: 1.0));
        Should.Throw<ArgumentOutOfRangeException>(() => surface.Resize(width: 4, height: -1, dpiScale: 1.0));
    }

    [Fact]
    public void Resize_SameDimensionsAndDpiScaleTwice_DoesNotReallocateBitmap()
    {
        Mock<IWriteableBitmapPixelBufferWriter> pixelBufferWriter = new Mock<IWriteableBitmapPixelBufferWriter>();
        WriteableBitmapRenderSurface surface = new WriteableBitmapRenderSurface(pixelBufferWriter.Object);
        surface.Resize(width: 7, height: 7, dpiScale: 1.0);
        WriteableBitmap? firstBitmap = surface.Bitmap;
        byte[] firstBuffer = surface.RawImageBuffer;

        surface.Resize(width: 7, height: 7, dpiScale: 1.0);

        surface.Bitmap.ShouldBeSameAs(firstBitmap);
        surface.RawImageBuffer.ShouldBeSameAs(firstBuffer);
    }

    // #4681: on a display scaled to e.g. 200% (dpiScale 2.0), the render target/bitmap is sized in
    // physical pixels while the WPF Image element that hosts it is still laid out in device-
    // independent units. Without this, WPF's own DPI compositing sees a 96-DPI bitmap and stretches
    // it to fill the (larger) physical-pixel area, blurring it - "1 pixel becomes 2 pixels".
    // Declaring the bitmap's DPI to match the display avoids that extra stretch.
    [Fact]
    public void Resize_SetsBitmapDpiToMatchDpiScale()
    {
        Mock<IWriteableBitmapPixelBufferWriter> pixelBufferWriter = new Mock<IWriteableBitmapPixelBufferWriter>();
        WriteableBitmapRenderSurface surface = new WriteableBitmapRenderSurface(pixelBufferWriter.Object);

        surface.Resize(width: 10, height: 6, dpiScale: 2.0);

        WriteableBitmap? bitmap = surface.Bitmap;
        bitmap.ShouldNotBeNull();
        bitmap!.DpiX.ShouldBe(192.0);
        bitmap.DpiY.ShouldBe(192.0);
    }

    // A window dragged from a 100%-scaled monitor to a 200%-scaled one fires a resize with the same
    // pixel dimensions but a different dpiScale - the dimension-only cache check above would
    // otherwise skip reallocating and leave the bitmap's DPI metadata stale.
    [Fact]
    public void Resize_SameDimensionsButDifferentDpiScale_ReallocatesBitmap()
    {
        Mock<IWriteableBitmapPixelBufferWriter> pixelBufferWriter = new Mock<IWriteableBitmapPixelBufferWriter>();
        WriteableBitmapRenderSurface surface = new WriteableBitmapRenderSurface(pixelBufferWriter.Object);
        surface.Resize(width: 7, height: 7, dpiScale: 1.0);

        surface.Resize(width: 7, height: 7, dpiScale: 2.0);

        surface.Bitmap!.DpiX.ShouldBe(192.0);
    }
}
