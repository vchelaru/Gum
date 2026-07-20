using Microsoft.Xna.Framework.Graphics;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XnaAndWinforms;

/// <summary>
/// The real implementation of <see cref="IWriteableBitmapPixelBufferWriter"/>. Locks
/// <paramref name="bitmap"/>'s back buffer and copies/converts <c>rawImage</c> into it using the
/// same <see cref="RawPixelBufferCopy"/> primitives <see cref="BitmapPixelBufferWriter"/> uses for
/// GDI+ - the only difference is the destination's locking API and pixel format check.
/// </summary>
public class WriteableBitmapPixelBufferWriter : IWriteableBitmapPixelBufferWriter
{
    /// <inheritdoc/>
    public void WriteToBitmap(byte[] rawImage, SurfaceFormat sourceFormat, WriteableBitmap bitmap)
    {
        if (bitmap.Format != PixelFormats.Pbgra32 && bitmap.Format != PixelFormats.Bgra32)
        {
            throw new NotSupportedException(
                $"No pixel buffer conversion from {sourceFormat} to {bitmap.Format}.");
        }

        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;

        PixelBufferConversionStrategy? strategy =
            RenderTargetPixelBufferConverter.GetStrategyForBgraDestination(sourceFormat);

        if (strategy == null)
        {
            throw new NotSupportedException(
                $"No pixel buffer conversion from {sourceFormat} to {bitmap.Format}.");
        }

        bitmap.Lock();

        try
        {
            if (strategy == PixelBufferConversionStrategy.ByteSwapRgbaToBgra)
            {
                RawPixelBufferCopy.CopyAndConvertRgbaToBgra(rawImage, bitmap.BackBuffer, bitmap.BackBufferStride, width, height);
            }
            else
            {
                RawPixelBufferCopy.CopyDirect(rawImage, bitmap.BackBuffer, bitmap.BackBufferStride, width, height);
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            bitmap.Unlock();
        }
    }
}
