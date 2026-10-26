using System;

namespace RenderingLibrary.Content;

/// <summary>
/// Pixels produced by an <see cref="IImageDecoder"/>: tightly packed row-major RGBA8 bytes.
/// </summary>
public sealed class DecodedImage
{
    /// <summary>
    /// Creates a decoded image. <paramref name="rgba"/> must hold exactly
    /// <paramref name="width"/> * <paramref name="height"/> * 4 bytes.
    /// </summary>
    public DecodedImage(byte[] rgba, int width, int height, bool isPremultiplied = false)
    {
        if (rgba == null) throw new ArgumentNullException(nameof(rgba));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (rgba.Length != width * height * 4)
        {
            throw new ArgumentException(
                $"Expected {width * height * 4} RGBA bytes for a {width}x{height} image but got {rgba.Length}.",
                nameof(rgba));
        }

        Rgba = rgba;
        Width = width;
        Height = height;
        IsPremultiplied = isPremultiplied;
    }

    /// <summary>Row-major RGBA8 bytes, 4 per pixel.</summary>
    public byte[] Rgba { get; }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>
    /// True when the color channels are already multiplied by alpha. Gum then skips its own
    /// premultiply, and un-premultiplies when the renderer uses straight alpha.
    /// </summary>
    public bool IsPremultiplied { get; private set; }

    /// <summary>
    /// When <see cref="IsPremultiplied"/>, divides each pixel's color by its alpha in place (rounding
    /// to nearest, clamped to 255) and clears the flag. Pixels with alpha 0 stay as they are. Loses
    /// precision at low alpha, so Gum logs a warning when it has to do this.
    /// </summary>
    /// <returns>True when the pixels were converted.</returns>
    public bool ConvertToStraightAlpha()
    {
        if (!IsPremultiplied)
        {
            return false;
        }

        byte[] rgba = Rgba;
        for (int i = 0; i + 3 < rgba.Length; i += 4)
        {
            int alpha = rgba[i + 3];
            if (alpha == 255 || alpha == 0)
            {
                continue;
            }

            int half = alpha / 2;
            rgba[i] = (byte)Math.Min(255, (rgba[i] * 255 + half) / alpha);
            rgba[i + 1] = (byte)Math.Min(255, (rgba[i + 1] * 255 + half) / alpha);
            rgba[i + 2] = (byte)Math.Min(255, (rgba[i + 2] * 255 + half) / alpha);
        }
        IsPremultiplied = false;
        return true;
    }

    /// <summary>
    /// The warning Gum logs when <see cref="ConvertToStraightAlpha"/> ran because the renderer uses
    /// straight alpha.
    /// </summary>
    internal static string GetUnpremultipliedWarning(string fileName) =>
        $"Gum: {fileName} was decoded as premultiplied alpha but the renderer uses straight alpha, so it " +
        "was un-premultiplied on load, which loses precision at low alpha.";
}
