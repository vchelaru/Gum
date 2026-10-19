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
}
