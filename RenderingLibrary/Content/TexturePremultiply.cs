using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;

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

        PremultiplyRgba(MemoryMarshal.AsBytes(pixels.AsSpan()));
    }

    /// <summary>
    /// Scales each RGBA byte quad's color by its alpha, in place, rounding to nearest
    /// (alpha 128 turns 255 into 128). Fully transparent texels become (0,0,0,0).
    /// </summary>
    /// <param name="rgba">Tightly packed RGBA bytes in any texel order.</param>
    public static void PremultiplyRgba(Span<byte> rgba)
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
    /// Divides each RGBA byte quad's color by its alpha, in place, rounding to nearest and clamping
    /// to 255. The inverse of <see cref="PremultiplyRgba"/> up to rounding; texels with alpha 0 are left as they are.
    /// </summary>
    /// <param name="rgba">Tightly packed premultiplied RGBA bytes in any texel order.</param>
    public static void UnpremultiplyRgba(Span<byte> rgba)
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
}
