using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace XnaAndWinforms;

/// <summary>
/// The real, GPU-independent implementation of <see cref="IRenderTargetPixelBufferWriter"/>. Locks
/// <paramref name="bitmap"/>'s backing memory and copies/converts <c>rawImage</c> into it according
/// to <see cref="RenderTargetPixelBufferConverter.GetStrategy"/>. Extracted from
/// <see cref="GraphicsDeviceControl"/>'s <c>PaintRendertarget</c> method.
/// </summary>
public class BitmapPixelBufferWriter : IRenderTargetPixelBufferWriter
{
    public void WriteToBitmap(byte[] rawImage, SurfaceFormat sourceFormat, Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        PixelBufferConversionStrategy? strategy =
            RenderTargetPixelBufferConverter.GetStrategy(sourceFormat, bitmap.PixelFormat);

        if (strategy == null)
        {
            throw new NotSupportedException(
                $"No pixel buffer conversion from {sourceFormat} to {bitmap.PixelFormat}.");
        }

        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, width, height);
        BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

        try
        {
            if (strategy == PixelBufferConversionStrategy.ByteSwapRgbaToBgra)
            {
                CopyAndConvertRgbaToBgra(width, height, rawImage, bmpData.Scan0, bmpData.Stride);
            }
            else
            {
                int rowSize = width * 4;
                int rowStride = bmpData.Stride;

                Parallel.For(0, height, (y) =>
                {
                    int srcOffset = y * rowSize;
                    int dstOffset = y * rowStride;
                    Marshal.Copy(rawImage, srcOffset, bmpData.Scan0 + dstOffset, rowSize);
                });
            }
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }

    private static unsafe void CopyAndConvertRgbaToBgra(int width, int height, byte[] data, IntPtr buffer, int rowStride)
    {
        int rowSize = width * 4;

        fixed (void* pData = &data[0])
        {
            byte* src = (byte*)pData;
            byte* dst = (byte*)buffer;

            Parallel.For(0, height, (y) =>
            {
                int srcOffset = y * rowSize;
                int dstOffset = y * rowStride;

                for (int x = 0; x < width; x++)
                {
                    int i = x * 4;
                    dst[dstOffset + i + 0] = src[srcOffset + i + 2];
                    dst[dstOffset + i + 1] = src[srcOffset + i + 1];
                    dst[dstOffset + i + 2] = src[srcOffset + i + 0];
                    dst[dstOffset + i + 3] = src[srcOffset + i + 3];
                }
            });
        }
    }
}
