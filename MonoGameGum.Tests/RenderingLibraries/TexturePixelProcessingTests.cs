using Microsoft.Xna.Framework;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Covers issue #6522: the load-time premultiply and edge bleed run on RGBA bytes, so the KNI
/// BlazorGL path (decode with StbImageSharp, process, one SetData) gives the same pixels as the
/// GetData/SetData path over <see cref="Color"/>[].
/// </summary>
public class TexturePixelProcessingTests
{
    // 3x2 RGBA texels at alpha 0, 1, 127, 128, 254 and 255.
    private const string AlphaRampPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAMAAAACCAYAAACddGYaAAAAI0lEQVR4nGPgEpFjOJFixPg/hbme4f///w3/TzD+4+Rg/w8AeQgK2zFeG9AAAAAASUVORK5CYII=";

    [Fact]
    public void Bleed_ShouldGiveSamePixels_ForColorArrayAndRgbaBytes()
    {
        // 5x2. Visible texels at index 1 and 7; index 4 and 9 have no visible neighbor.
        byte[] input =
        {
            0, 0, 0, 0,     100, 50, 0, 255,  0, 0, 0, 0,  0, 0, 0, 0,  9, 8, 7, 0,
            40, 40, 40, 0,  0, 0, 0, 0,  200, 0, 100, 128,  0, 0, 0, 0,  0, 0, 0, 0,
        };
        byte[] expected =
        {
            100, 50, 0, 0,  100, 50, 0, 255,  150, 25, 50, 0,  200, 0, 100, 0,  9, 8, 7, 0,
            100, 50, 0, 0,  150, 25, 50, 0,  200, 0, 100, 128,  200, 0, 100, 0,  0, 0, 0, 0,
        };
        Color[] colors = ToColors(input);
        byte[] bytes = (byte[])input.Clone();

        TextureEdgeBleed.Bleed(colors, width: 5, height: 2);
        TextureEdgeBleed.BleedRgba(bytes, width: 5, height: 2);

        ToBytes(colors).ShouldBe(expected);
        bytes.ShouldBe(expected);
    }

    [Fact]
    public void Decode_ShouldKeepStraightPixels_IncludingColorUnderAlphaZero()
    {
        byte[] png = Convert.FromBase64String(AlphaRampPngBase64);

        byte[] rgba = StbTextureDecoder.Decode(new MemoryStream(png), out int width, out int height);

        width.ShouldBe(3);
        height.ShouldBe(2);
        rgba.ShouldBe(new byte[]
        {
            10, 20, 30, 0,  200, 100, 50, 1,  255, 100, 3, 127,
            255, 255, 255, 128,  255, 200, 1, 254,  9, 8, 7, 255,
        });
    }

    [Fact]
    public void Premultiply_ShouldGiveSamePixels_ForColorArrayAndRgbaBytes()
    {
        byte[] input =
        {
            10, 20, 30, 0,  200, 100, 50, 1,  255, 100, 3, 127,
            255, 255, 255, 128,  255, 200, 1, 254,  9, 8, 7, 255,
        };
        byte[] expected =
        {
            0, 0, 0, 0,  1, 0, 0, 1,  127, 50, 1, 127,
            128, 128, 128, 128,  254, 199, 1, 254,  9, 8, 7, 255,
        };
        Color[] colors = ToColors(input);
        byte[] bytes = (byte[])input.Clone();

        TexturePremultiply.Premultiply(colors);
        TexturePremultiply.PremultiplyRgba(bytes);

        ToBytes(colors).ShouldBe(expected);
        bytes.ShouldBe(expected);
    }

    [Fact]
    public void ProcessLoadedPixels_ShouldBleed_OnlyWhenStraightAlphaAndBleedEnabled()
    {
        byte[] bleedOn = { 255, 255, 255, 255, 0, 0, 0, 0 };
        byte[] bleedOff = { 255, 255, 255, 255, 0, 0, 0, 0 };

        ContentLoader.ProcessLoadedPixels(bleedOn, width: 2, height: 1,
            usePremultipliedAlpha: false, bleedTransparentEdges: true);
        ContentLoader.ProcessLoadedPixels(bleedOff, width: 2, height: 1,
            usePremultipliedAlpha: false, bleedTransparentEdges: false);

        bleedOn.ShouldBe(new byte[] { 255, 255, 255, 255, 255, 255, 255, 0 });
        bleedOff.ShouldBe(new byte[] { 255, 255, 255, 255, 0, 0, 0, 0 });
    }

    [Fact]
    public void ProcessLoadedPixels_ShouldPremultiplyAndNotBleed_WhenPremultipliedAlpha()
    {
        byte[] rgba = { 255, 255, 255, 128, 50, 60, 70, 0 };

        ContentLoader.ProcessLoadedPixels(rgba, width: 2, height: 1,
            usePremultipliedAlpha: true, bleedTransparentEdges: true);

        rgba.ShouldBe(new byte[] { 128, 128, 128, 128, 0, 0, 0, 0 });
    }

    private static Color[] ToColors(byte[] rgba)
    {
        Color[] colors = new Color[rgba.Length / 4];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], rgba[i * 4 + 3]);
        }
        return colors;
    }

    private static byte[] ToBytes(Color[] colors)
    {
        byte[] rgba = new byte[colors.Length * 4];
        for (int i = 0; i < colors.Length; i++)
        {
            rgba[i * 4] = colors[i].R;
            rgba[i * 4 + 1] = colors[i].G;
            rgba[i * 4 + 2] = colors[i].B;
            rgba[i * 4 + 3] = colors[i].A;
        }
        return rgba;
    }
}
