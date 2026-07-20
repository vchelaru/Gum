using System;
using System.Drawing;
using System.Drawing.Imaging;
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
    /// <inheritdoc/>
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
                RawPixelBufferCopy.CopyAndConvertRgbaToBgra(rawImage, bmpData.Scan0, bmpData.Stride, width, height);
            }
            else
            {
                RawPixelBufferCopy.CopyDirect(rawImage, bmpData.Scan0, bmpData.Stride, width, height);
            }
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }
}
