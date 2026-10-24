using System;

namespace RenderingLibrary.Content;

/// <summary>
/// Alpha conversions over tightly packed row-major RGBA bytes, shared by the runtimes' texture loaders
/// and by <c>gumcli pack</c>, which bakes them into raw package textures.
/// </summary>
public static class RgbaPixels
{
    /// <summary>
    /// Scales each texel's color by its alpha, in place, rounding to nearest (alpha 128 turns 255 into
    /// 128). Fully transparent texels become (0,0,0,0).
    /// </summary>
    public static void Premultiply(Span<byte> rgba)
    {
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

    /// <summary>
    /// Divides each texel's color by its alpha, in place, rounding to nearest. The inverse of
    /// <see cref="Premultiply"/>, lossy at low alpha. Texels with alpha 0 keep their bytes, since
    /// their color is gone.
    /// </summary>
    public static void Unpremultiply(Span<byte> rgba)
    {
        for (int i = 0; i + 3 < rgba.Length; i += 4)
        {
            int alpha = rgba[i + 3];
            if (alpha == 255 || alpha == 0)
            {
                continue;
            }

            int half = alpha / 2;
            rgba[i] = (byte)System.Math.Min(255, (rgba[i] * 255 + half) / alpha);
            rgba[i + 1] = (byte)System.Math.Min(255, (rgba[i + 1] * 255 + half) / alpha);
            rgba[i + 2] = (byte)System.Math.Min(255, (rgba[i + 2] * 255 + half) / alpha);
        }
    }

    /// <summary>
    /// Bleeds neighboring visible color into fully-transparent texels, in place, keeping alpha 0, so a
    /// straight-alpha pipeline sampled with Linear filtering does not darken edges toward black
    /// (issue #3691). One pass is enough: bilinear sampling only blends a texel with its neighbor.
    /// </summary>
    /// <param name="rgba">Row-major RGBA bytes (length must be 4 * <paramref name="width"/> * <paramref name="height"/>).</param>
    /// <param name="width">Texture width in texels.</param>
    /// <param name="height">Texture height in texels.</param>
    public static void BleedTransparentEdges(Span<byte> rgba, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // Read from a snapshot so bleeding never cascades within the single pass (results are
        // independent of iteration order, and a just-filled texel can't seed its neighbors).
        byte[] source = rgba.ToArray();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = ((y * width) + x) * 4;
                if (source[index + 3] != 0)
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

                        int neighbor = ((ny * width) + nx) * 4;
                        if (source[neighbor + 3] != 0)
                        {
                            r += source[neighbor];
                            g += source[neighbor + 1];
                            b += source[neighbor + 2];
                            count++;
                        }
                    }
                }

                if (count > 0)
                {
                    // Keep alpha 0: only the color the sampler interpolates toward changes.
                    rgba[index] = (byte)(r / count);
                    rgba[index + 1] = (byte)(g / count);
                    rgba[index + 2] = (byte)(b / count);
                }
            }
        }
    }
}
