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
    public bool IsPremultiplied { get; }
}
