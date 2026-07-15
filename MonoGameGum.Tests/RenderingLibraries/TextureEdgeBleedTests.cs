using Microsoft.Xna.Framework;
using RenderingLibrary.Content;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Covers issue #3691: <see cref="TextureEdgeBleed.Bleed"/> fills fully-transparent texels that
/// border a visible texel with the neighbor's color (keeping alpha 0) so a non-premultiplied Linear
/// pipeline no longer darkens edges toward black.
/// </summary>
public class TextureEdgeBleedTests
{
    [Fact]
    public void Bleed_ShouldCopyNeighborColorIntoTransparentTexel_WhileKeepingAlphaZero()
    {
        // Left texel: opaque white "glyph". Right texel: transparent black "outside".
        Color[] pixels =
        {
            new Color((byte)255, (byte)255, (byte)255, (byte)255),
            new Color((byte)0,   (byte)0,   (byte)0,   (byte)0),
        };

        TextureEdgeBleed.Bleed(pixels, width: 2, height: 1);

        // The transparent texel now carries the white glyph color, but stays fully transparent.
        pixels[1].R.ShouldBe((byte)255);
        pixels[1].G.ShouldBe((byte)255);
        pixels[1].B.ShouldBe((byte)255);
        pixels[1].A.ShouldBe((byte)0);

        // The opaque texel is untouched.
        pixels[0].ShouldBe(new Color((byte)255, (byte)255, (byte)255, (byte)255));
    }

    [Fact]
    public void Bleed_ShouldLeaveTransparentTexelBlack_WhenNoVisibleNeighborExists()
    {
        // A transparent texel with only transparent neighbors is never sampled adjacent to a visible
        // texel, so it must stay black (nothing to bleed from).
        Color[] pixels =
        {
            new Color((byte)0, (byte)0, (byte)0, (byte)0),
            new Color((byte)0, (byte)0, (byte)0, (byte)0),
            new Color((byte)0, (byte)0, (byte)0, (byte)0),
        };

        TextureEdgeBleed.Bleed(pixels, width: 3, height: 1);

        pixels[1].ShouldBe(new Color((byte)0, (byte)0, (byte)0, (byte)0));
    }
}
