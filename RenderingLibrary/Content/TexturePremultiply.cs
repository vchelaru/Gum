using Microsoft.Xna.Framework;

namespace RenderingLibrary.Content;

/// <summary>
/// Straight-alpha to premultiplied-alpha conversion for loaded textures (issue #6154).
/// </summary>
public static class TexturePremultiply
{
    /// <summary>
    /// Scales each pixel's color by its alpha, in place. Fully transparent texels become (0,0,0,0).
    /// </summary>
    /// <param name="pixels">Pixel data in any order; null or empty is a no-op.</param>
    public static void Premultiply(Color[]? pixels)
    {
        if (pixels == null)
        {
            return;
        }

        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            if (pixel.A == 255)
            {
                continue;
            }

            int alpha = pixel.A;
            pixels[i] = new Color(
                (byte)((pixel.R * alpha + 127) / 255),
                (byte)((pixel.G * alpha + 127) / 255),
                (byte)((pixel.B * alpha + 127) / 255),
                pixel.A);
        }
    }

    /// <summary>
    /// Scales each RGBA byte quad's color by its alpha, in place, with the same rounding as the
    /// <see cref="Color"/> overload. Fully transparent texels become (0,0,0,0).
    /// </summary>
    /// <param name="rgba">Tightly packed RGBA bytes; null is a no-op.</param>
    public static void Premultiply(byte[]? rgba)
    {
        if (rgba == null)
        {
            return;
        }

        for (int i = 0; i + 3 < rgba.Length; i += 4)
        {
            int alpha = rgba[i + 3];
            if (alpha == 255)
            {
                continue;
            }

            rgba[i] = (byte)((rgba[i] * alpha + 127) / 255);
            rgba[i + 1] = (byte)((rgba[i + 1] * alpha + 127) / 255);
            rgba[i + 2] = (byte)((rgba[i + 2] * alpha + 127) / 255);
        }
    }
}
