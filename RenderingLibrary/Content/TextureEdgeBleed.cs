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
    public static void BleedRgba(Span<byte> rgba, int width, int height) => RgbaPixels.BleedTransparentEdges(rgba, width, height);
}
