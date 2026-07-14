using Microsoft.Xna.Framework;

namespace RenderingLibrary.Content;

/// <summary>
/// Alpha-preserving edge dilation for loaded textures (issue #3691).
/// </summary>
/// <remarks>
/// A non-premultiplied pipeline sampled with <c>TextureFilter.Linear</c> darkens the edges of any
/// texture whose transparent texels are black (RGB 0,0,0) — the shape of a KernSmith-baked font
/// atlas — because bilinear sampling interpolates the transparent texel's black RGB into the visible
/// edge. This "bleeds" the neighboring visible color into fully-transparent (A==0) texels' RGB while
/// leaving alpha untouched, so the sampler interpolates color-to-color instead of color-to-black.
///
/// Bilinear filtering only ever blends a texel with its immediate neighbor, so a single pass — fill
/// every A==0 texel that borders a non-transparent texel — is sufficient at any scale.
/// </remarks>
public static class TextureEdgeBleed
{
    /// <summary>
    /// Bleeds neighboring visible color into fully-transparent texels, in place. Alpha is preserved.
    /// </summary>
    /// <param name="pixels">Row-major pixel data (length must be <paramref name="width"/> * <paramref name="height"/>).</param>
    /// <param name="width">Texture width in texels.</param>
    /// <param name="height">Texture height in texels.</param>
    public static void Bleed(Color[] pixels, int width, int height)
    {
        if (pixels == null || width <= 0 || height <= 0)
        {
            return;
        }

        // Read from a snapshot so bleeding never cascades within the single pass (results are
        // independent of iteration order, and a just-filled texel can't seed its neighbors).
        Color[] source = (Color[])pixels.Clone();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                if (source[index].A != 0)
                {
                    // Visible (or partially visible) texels already carry the right color.
                    continue;
                }

                int r = 0, g = 0, b = 0, count = 0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= height)
                    {
                        continue;
                    }

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        int nx = x + dx;
                        if (nx < 0 || nx >= width)
                        {
                            continue;
                        }

                        Color neighbor = source[(ny * width) + nx];
                        if (neighbor.A != 0)
                        {
                            r += neighbor.R;
                            g += neighbor.G;
                            b += neighbor.B;
                            count++;
                        }
                    }
                }

                if (count > 0)
                {
                    // Keep alpha 0 — only the color the sampler interpolates toward changes.
                    pixels[index] = new Color((byte)(r / count), (byte)(g / count), (byte)(b / count), (byte)0);
                }
            }
        }
    }
}
