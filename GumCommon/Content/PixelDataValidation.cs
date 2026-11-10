#nullable enable

using System;

namespace RenderingLibrary.Content;

/// <summary>
/// The argument rule shared by every backend's pixel-data texture applier, so a bad call fails the
/// same way on Skia, raylib and the XNA family before any texture is touched.
/// </summary>
internal static class PixelDataValidation
{
    /// <summary>
    /// Throws <see cref="ArgumentException"/> unless <paramref name="width"/> and <paramref name="height"/>
    /// are both positive and <paramref name="rgba"/> holds exactly width * height * 4 bytes (tightly packed
    /// RGBA). A zero or negative size is rejected because no texture can have it. Throws
    /// <see cref="ArgumentNullException"/> for null pixels.
    /// </summary>
    public static void EnsureRgbaLength(byte[] rgba, int width, int height)
    {
        if (rgba == null)
        {
            throw new ArgumentNullException(nameof(rgba));
        }

        long expected = (long)width * height * 4;
        if (width <= 0 || height <= 0 || rgba.Length != expected)
        {
            throw new ArgumentException(
                $"Pixel data is {rgba.Length} bytes, but a {width}x{height} texture needs {expected} (width * height * 4).",
                nameof(rgba));
        }
    }
}
