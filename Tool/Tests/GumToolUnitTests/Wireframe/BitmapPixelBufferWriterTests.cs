using Microsoft.Xna.Framework.Graphics;
using Shouldly;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using XnaAndWinforms;
using Xunit;

namespace GumToolUnitTests.Wireframe;

public class BitmapPixelBufferWriterTests
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

    [Fact]
    public void WriteToBitmap_Bgra32Source_CopiesBytesDirectly()
    {
        // Bgra32 source already matches the bitmap's BGRA memory layout, so no conversion is needed.
        byte[] rawImage = CreateRawImage(byte0: 40, byte1: 50, byte2: 60, byte3: 255);
        Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        BitmapPixelBufferWriter writer = new BitmapPixelBufferWriter();

        writer.WriteToBitmap(rawImage, SurfaceFormat.Bgra32, bitmap);

        Color pixel = bitmap.GetPixel(0, 0);
        pixel.B.ShouldBe((byte)40);
        pixel.G.ShouldBe((byte)50);
        pixel.R.ShouldBe((byte)60);
        pixel.A.ShouldBe((byte)255);
    }

    [Fact]
    public void WriteToBitmap_ColorSource_ByteSwapsRgbaToBgra()
    {
        // Color source is RGBA; the bitmap's BGRA layout requires swapping R and B.
        byte[] rawImage = CreateRawImage(byte0: 10, byte1: 20, byte2: 30, byte3: 255);
        Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        BitmapPixelBufferWriter writer = new BitmapPixelBufferWriter();

        writer.WriteToBitmap(rawImage, SurfaceFormat.Color, bitmap);

        Color pixel = bitmap.GetPixel(0, 0);
        pixel.R.ShouldBe((byte)10);
        pixel.G.ShouldBe((byte)20);
        pixel.B.ShouldBe((byte)30);
        pixel.A.ShouldBe((byte)255);
    }

    [Fact]
    public void WriteToBitmap_UnsupportedFormatCombination_Throws()
    {
        byte[] rawImage = CreateRawImage(byte0: 1, byte1: 2, byte2: 3, byte3: 4);
        Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
        BitmapPixelBufferWriter writer = new BitmapPixelBufferWriter();

        Should.Throw<NotSupportedException>(() => writer.WriteToBitmap(rawImage, SurfaceFormat.Color, bitmap));
    }
}
