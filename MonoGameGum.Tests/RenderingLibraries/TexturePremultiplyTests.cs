using Microsoft.Xna.Framework;
using RenderingLibrary.Content;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Covers issue #6154: <see cref="TexturePremultiply.Premultiply"/> converts straight-alpha pixels to
/// premultiplied in place, so a texture loaded for an <c>AlphaBlend</c> pipeline reads back with
/// color scaled by alpha.
/// </summary>
public class TexturePremultiplyTests
{
    [Fact]
    public void Premultiply_ShouldScaleColorByAlpha_AndZeroFullyTransparentTexels()
    {
        Color[] pixels =
        {
            new Color((byte)255, (byte)0, (byte)0, (byte)128),
            new Color((byte)255, (byte)0, (byte)0, (byte)0),
            new Color((byte)10, (byte)200, (byte)255, (byte)255),
        };

        TexturePremultiply.Premultiply(pixels);

        pixels[0].ShouldBe(new Color((byte)128, (byte)0, (byte)0, (byte)128));
        pixels[1].ShouldBe(new Color((byte)0, (byte)0, (byte)0, (byte)0));
        pixels[2].ShouldBe(new Color((byte)10, (byte)200, (byte)255, (byte)255));
    }

    [Fact]
    public void Premultiply_ShouldNotThrow_WhenPixelsAreNullOrEmpty()
    {
        TexturePremultiply.Premultiply(null);
        TexturePremultiply.Premultiply(System.Array.Empty<Color>());
    }

    [Fact]
    public void Premultiply_ShouldNeverProduceColorAboveAlpha()
    {
        Color[] pixels = new Color[256];
        for (int alpha = 0; alpha < 256; alpha++)
        {
            pixels[alpha] = new Color((byte)255, (byte)255, (byte)255, (byte)alpha);
        }

        TexturePremultiply.Premultiply(pixels);

        for (int alpha = 0; alpha < 256; alpha++)
        {
            pixels[alpha].R.ShouldBe((byte)alpha);
            pixels[alpha].A.ShouldBe((byte)alpha);
        }
    }
}
