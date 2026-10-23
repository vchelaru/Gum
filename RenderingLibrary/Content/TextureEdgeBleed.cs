using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;

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
    public static void Bleed(Color[]? pixels, int width, int height)
    {
        if (pixels == null)
        {
            return;
        }

        BleedRgba(MemoryMarshal.AsBytes(pixels.AsSpan()), width, height);
    }

    /// <summary>
    /// Same as <see cref="Bleed(Color[], int, int)"/> over tightly packed row-major RGBA bytes.
    /// </summary>
    /// <param name="rgba">Row-major RGBA bytes (length must be 4 * <paramref name="width"/> * <paramref name="height"/>).</param>
    /// <param name="width">Texture width in texels.</param>
    /// <param name="height">Texture height in texels.</param>
    public static void BleedRgba(Span<byte> rgba, int width, int height)
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
