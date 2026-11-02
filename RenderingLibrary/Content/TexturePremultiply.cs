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
    public static void PremultiplyRgba(Span<byte> rgba) => RgbaPixels.Premultiply(rgba);
}
