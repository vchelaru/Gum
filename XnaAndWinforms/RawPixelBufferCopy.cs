using System;
using System.Runtime.InteropServices;

namespace XnaAndWinforms;

/// <summary>
/// The pointer/stride-level byte copy loops shared by every <see cref="PixelBufferConversionStrategy"/>
/// consumer (the WPF and Avalonia bitmap writers). The destination type doesn't
/// matter here - callers pass a raw pointer and stride into whatever backing memory they've already
/// locked, so this class has no dependency on any UI framework.
/// </summary>
/// <remarks>
/// Both copies run entirely on the calling thread, on purpose. They're called from the UI thread
/// once per frame, and a <c>Parallel.For</c> there makes the UI thread block on the thread pool:
/// if the pool is stalled (e.g. its workers are waiting on the UI thread themselves), the loop
/// never returns even after every row has been copied - a permanent freeze (#4852). A
/// full-canvas copy is a few megabytes, well under a frame single-threaded.
/// </remarks>
public static class RawPixelBufferCopy
{
    /// <summary>
    /// Copies <paramref name="source"/> into <paramref name="destination"/> row by row, honoring
    /// <paramref name="destinationStride"/> (which may differ from the tightly-packed row size).
    /// </summary>
    public static void CopyDirect(byte[] source, IntPtr destination, int destinationStride, int width, int height)
    {
        int rowSize = width * 4;

        if (destinationStride == rowSize)
        {
            Marshal.Copy(source, 0, destination, rowSize * height);
            return;
        }

        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(source, y * rowSize, destination + y * destinationStride, rowSize);
        }
    }

    /// <summary>
    /// Copies <paramref name="source"/> into <paramref name="destination"/> row by row, swapping the
    /// red and blue channels of each pixel (RGBA source -> BGRA destination).
    /// </summary>
    public static unsafe void CopyAndConvertRgbaToBgra(byte[] source, IntPtr destination, int destinationStride, int width, int height)
    {
        int rowSize = width * 4;

        fixed (byte* src = source)
        {
            byte* dst = (byte*)destination;

            for (int y = 0; y < height; y++)
            {
                byte* srcRow = src + y * rowSize;
                byte* dstRow = dst + y * destinationStride;

                for (int i = 0; i < rowSize; i += 4)
                {
                    dstRow[i + 0] = srcRow[i + 2];
                    dstRow[i + 1] = srcRow[i + 1];
                    dstRow[i + 2] = srcRow[i + 0];
                    dstRow[i + 3] = srcRow[i + 3];
                }
            }
        }
    }
}
