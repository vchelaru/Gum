using Microsoft.Xna.Framework.Graphics;
using Shouldly;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XnaAndWinforms;
using Xunit;

namespace GumToolUnitTests.Wireframe;

public class WriteableBitmapPixelBufferWriterTests
{
    private const int Width = 2;
    private const int Height = 2;

    // Fills every pixel with the same 4 raw bytes, in the order they'd appear in the buffer
    // read back from RenderTarget2D.GetData.
    private static byte[] CreateRawImage(byte byte0, byte byte1, byte byte2, byte byte3)
    {
        byte[] rawImage = new byte[Width * Height * 4];
        for (int i = 0; i < rawImage.Length; i += 4)
        {
            rawImage[i + 0] = byte0;
            rawImage[i + 1] = byte1;
            rawImage[i + 2] = byte2;
            rawImage[i + 3] = byte3;
        }
        return rawImage;
    }

    // WriteableBitmap has no GetPixel; read the top-left pixel's raw bytes back out via CopyPixels.
    private static byte[] GetFirstPixelBytes(WriteableBitmap bitmap)
    {
        byte[] pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect(0, 0, 1, 1), pixel, 4, 0);
        return pixel;
    }

    [Fact]
    public void WriteToBitmap_Bgra32Source_CopiesBytesDirectly()
    {
        // Bgra32 source already matches the bitmap's BGRA memory layout, so no conversion is needed.
        byte[] rawImage = CreateRawImage(byte0: 40, byte1: 50, byte2: 60, byte3: 255);
        WriteableBitmap bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32, null);
        WriteableBitmapPixelBufferWriter writer = new WriteableBitmapPixelBufferWriter();

        writer.WriteToBitmap(rawImage, SurfaceFormat.Bgra32, bitmap);

        byte[] pixel = GetFirstPixelBytes(bitmap);
        pixel[0].ShouldBe((byte)40); // B
        pixel[1].ShouldBe((byte)50); // G
        pixel[2].ShouldBe((byte)60); // R
        pixel[3].ShouldBe((byte)255); // A
    }

    [Fact]
    public void WriteToBitmap_ColorSource_ByteSwapsRgbaToBgra()
    {
        // Color source is RGBA; the bitmap's BGRA layout requires swapping R and B.
        byte[] rawImage = CreateRawImage(byte0: 10, byte1: 20, byte2: 30, byte3: 255);
        WriteableBitmap bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32, null);
        WriteableBitmapPixelBufferWriter writer = new WriteableBitmapPixelBufferWriter();

        writer.WriteToBitmap(rawImage, SurfaceFormat.Color, bitmap);

        byte[] pixel = GetFirstPixelBytes(bitmap);
        pixel[0].ShouldBe((byte)30); // B
        pixel[1].ShouldBe((byte)20); // G
        pixel[2].ShouldBe((byte)10); // R
        pixel[3].ShouldBe((byte)255); // A
    }

    [Fact]
    public void WriteToBitmap_UnsupportedSourceFormat_Throws()
    {
        byte[] rawImage = CreateRawImage(byte0: 1, byte1: 2, byte2: 3, byte3: 4);
        WriteableBitmap bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32, null);
        WriteableBitmapPixelBufferWriter writer = new WriteableBitmapPixelBufferWriter();

        Should.Throw<NotSupportedException>(() => writer.WriteToBitmap(rawImage, SurfaceFormat.Rgba64, bitmap));
    }

    [Fact]
    public void WriteToBitmap_UnsupportedDestinationFormat_Throws()
    {
        byte[] rawImage = CreateRawImage(byte0: 1, byte1: 2, byte2: 3, byte3: 4);
        WriteableBitmap bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgr24, null);
        WriteableBitmapPixelBufferWriter writer = new WriteableBitmapPixelBufferWriter();

        Should.Throw<NotSupportedException>(() => writer.WriteToBitmap(rawImage, SurfaceFormat.Color, bitmap));
    }
}
