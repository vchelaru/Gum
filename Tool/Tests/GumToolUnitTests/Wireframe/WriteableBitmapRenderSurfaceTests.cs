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
        surface.Resize(width: 4, height: 3);

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

        surface.Resize(width: 8, height: 5);

        surface.RawImageBuffer.Length.ShouldBe(8 * 5 * 4);
    }

    [Fact]
    public void Resize_CreatesBitmapWithGivenDimensions()
    {
        Mock<IWriteableBitmapPixelBufferWriter> pixelBufferWriter = new Mock<IWriteableBitmapPixelBufferWriter>();
        WriteableBitmapRenderSurface surface = new WriteableBitmapRenderSurface(pixelBufferWriter.Object);

        surface.Resize(width: 10, height: 6);

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

        Should.Throw<ArgumentOutOfRangeException>(() => surface.Resize(width: 0, height: 4));
        Should.Throw<ArgumentOutOfRangeException>(() => surface.Resize(width: 4, height: -1));
    }

    [Fact]
    public void Resize_SameDimensionsTwice_DoesNotReallocateBitmap()
    {
        Mock<IWriteableBitmapPixelBufferWriter> pixelBufferWriter = new Mock<IWriteableBitmapPixelBufferWriter>();
        WriteableBitmapRenderSurface surface = new WriteableBitmapRenderSurface(pixelBufferWriter.Object);
        surface.Resize(width: 7, height: 7);
        WriteableBitmap? firstBitmap = surface.Bitmap;
        byte[] firstBuffer = surface.RawImageBuffer;

        surface.Resize(width: 7, height: 7);

        surface.Bitmap.ShouldBeSameAs(firstBitmap);
        surface.RawImageBuffer.ShouldBeSameAs(firstBuffer);
    }
}
