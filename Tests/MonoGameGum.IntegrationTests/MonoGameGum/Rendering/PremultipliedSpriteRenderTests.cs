using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;
using static MonoGameGum.IntegrationTests.MonoGameGum.Rendering.AlphaModeRenderHarness;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// #7585: a Sprite draws the same pixels on the straight-alpha pipeline and on the premultiplied one, for
/// a texture with transparent, half transparent and opaque texels (non-black color under the transparent
/// ones), plain, tinted and at half alpha. Expected values are worked out by hand, not read from Gum.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class PremultipliedSpriteRenderTests : BaseTestClass
{
    // Three texels across, each stretched to 16 pixels; sampled at the middle of each one.
    private static readonly XnaColor[] Texels =
    {
        new XnaColor((byte)255, (byte)0, (byte)255, (byte)0),     // transparent, bright magenta underneath
        new XnaColor((byte)40, (byte)200, (byte)40, (byte)128),   // half transparent
        new XnaColor((byte)200, (byte)100, (byte)50, (byte)255),  // opaque
    };

    private const int SpriteX = 10;
    private const int SpriteY = 8;
    private const int SpriteWidth = 48;
    private const int SpriteHeight = 16;

    public override void Dispose()
    {
        Renderer.UsePremultipliedAlpha = false;
        base.Dispose();
    }

    [Theory]
    [InlineData(false, Variant.Plain)]
    [InlineData(false, Variant.Tint)]
    [InlineData(false, Variant.HalfAlpha)]
    [InlineData(true, Variant.Plain)]
    [InlineData(true, Variant.Tint)]
    [InlineData(true, Variant.HalfAlpha)]
    public void Sprite_TexelCenters_MatchHandComputedComposite(bool premultiplied, Variant variant)
    {
        XnaColor[] pixels = RenderSprite(premultiplied, variant);

        for (int texel = 0; texel < 3; texel++)
        {
            XnaColor pixel = pixels[((SpriteY + 8) * Width) + SpriteX + (texel * 16) + 8];
            MaxChannelDifference(pixel, Expected(Texels[texel], variant)).ShouldBeLessThanOrEqualTo(3,
                $"texel {texel}, premultiplied {premultiplied}, {variant}: got {pixel}");
        }
    }

    [Theory]
    [InlineData(Variant.Plain)]
    [InlineData(Variant.Tint)]
    [InlineData(Variant.HalfAlpha)]
    public void Sprite_Premultiplied_MatchesStraight(Variant variant)
    {
        XnaColor[] straight = RenderSprite(false, variant);
        XnaColor[] premultiplied = RenderSprite(true, variant);

        MaxChannelDifference(straight, premultiplied).ShouldBeLessThanOrEqualTo(3);
    }

    [Theory]
    [InlineData(false, Variant.Plain)]
    [InlineData(false, Variant.Tint)]
    [InlineData(true, Variant.Plain)]
    [InlineData(true, Variant.Tint)]
    [InlineData(true, Variant.HalfAlpha)]
    public void Sprite_TransparentTexels_LeaveNoBrightFringe(bool premultiplied, Variant variant)
    {
        XnaColor[] pixels = RenderSprite(premultiplied, variant);

        // Nothing may be brighter than the brightest thing legitimately composited: the backdrop or the
        // opaque texel (200, 100, 50). The transparent texel's magenta (255, 0, 255) must never show,
        // on its own or blended in at its edge.
        for (int y = SpriteY; y < SpriteY + SpriteHeight; y++)
        {
            for (int x = SpriteX; x < SpriteX + SpriteWidth; x++)
            {
                XnaColor p = pixels[(y * Width) + x];
                p.R.ShouldBeLessThanOrEqualTo((byte)203, $"({x},{y}) {p}");
                p.B.ShouldBeLessThanOrEqualTo((byte)93, $"({x},{y}) {p}");
            }
        }
    }

    private static XnaColor[] RenderSprite(bool premultiplied, Variant variant) =>
        Render(premultiplied, 3, 1, Texels, texture =>
        {
            SpriteRuntime sprite = new();
            sprite.Texture = texture;
            sprite.WidthUnits = DimensionUnitType.Absolute;
            sprite.HeightUnits = DimensionUnitType.Absolute;
            sprite.Width = SpriteWidth;
            sprite.Height = SpriteHeight;
            sprite.X = SpriteX;
            sprite.Y = SpriteY;
            Apply(variant, c => sprite.Color = c, a => sprite.Alpha = a);
            return sprite;
        });
}
