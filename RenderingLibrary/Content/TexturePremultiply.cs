using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RenderingLibrary.Content;

/// <summary>
/// Straight-alpha to premultiplied-alpha conversion for loaded textures (issue #6154), and back for saving.
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

    /// <summary>
    /// Writes a texture holding premultiplied pixels (a target drawn with
    /// <see cref="RenderingLibrary.Graphics.Renderer.IsDrawingToTransparentTarget"/>) as a straight-alpha PNG,
    /// which is what PNG stores: each pixel's color is divided by its alpha first.
    /// </summary>
    public static void SaveAsUnpremultipliedPng(Texture2D premultiplied, Stream stream)
    {
        int width = premultiplied.Width;
        int height = premultiplied.Height;
        Color[] pixels = new Color[width * height];
        premultiplied.GetData(pixels);
        RgbaPixels.Unpremultiply(MemoryMarshal.AsBytes(pixels.AsSpan()));

        using Texture2D straight = new Texture2D(premultiplied.GraphicsDevice, width, height, false, SurfaceFormat.Color);
        straight.SetData(pixels);
        straight.SaveAsPng(stream, width, height);
    }
}
