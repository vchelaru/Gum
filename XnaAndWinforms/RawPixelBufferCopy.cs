using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace XnaAndWinforms;

/// <summary>
/// The pointer/stride-level byte copy loops shared by every <see cref="PixelBufferConversionStrategy"/>
/// consumer (currently <see cref="BitmapPixelBufferWriter"/> and <see cref="WriteableBitmapPixelBufferWriter"/>).
/// Neither destination type matters here - callers pass a raw pointer and stride into whatever backing
/// memory they've already locked, so this class has no dependency on GDI+ or WPF.
/// </summary>
internal static class RawPixelBufferCopy
{
    /// <summary>
    /// Copies <paramref name="source"/> into <paramref name="destination"/> row by row, honoring
    /// <paramref name="destinationStride"/> (which may differ from the tightly-packed row size).
    /// </summary>
    public static void CopyDirect(byte[] source, IntPtr destination, int destinationStride, int width, int height)
    {
        int rowSize = width * 4;

        Parallel.For(0, height, (y) =>
        {
            int srcOffset = y * rowSize;
            int dstOffset = y * destinationStride;
            Marshal.Copy(source, srcOffset, destination + dstOffset, rowSize);
        });
    }

    /// <summary>
    /// Copies <paramref name="source"/> into <paramref name="destination"/> row by row, swapping the
    /// red and blue channels of each pixel (RGBA source -> BGRA destination).
    /// </summary>
    public static unsafe void CopyAndConvertRgbaToBgra(byte[] source, IntPtr destination, int destinationStride, int width, int height)
    {
        int rowSize = width * 4;

        fixed (void* pSource = &source[0])
        {
            byte* src = (byte*)pSource;
            byte* dst = (byte*)destination;

            Parallel.For(0, height, (y) =>
            {
                int srcOffset = y * rowSize;
                int dstOffset = y * destinationStride;

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
