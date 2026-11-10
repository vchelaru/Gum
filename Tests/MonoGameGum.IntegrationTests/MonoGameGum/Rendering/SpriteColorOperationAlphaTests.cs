using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;
using static MonoGameGum.IntegrationTests.MonoGameGum.Rendering.AlphaModeRenderHarness;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// #7837: a sprite's Alpha applies once for every color operation, on the straight-alpha and the
/// premultiplied pipeline, for opaque and half-transparent texels. Expected values are worked out by
/// hand: coverage = texelAlpha * Alpha, and the color is texel * tint (Modulate) or the tint alone
/// (ColorTextureAlpha). Add composites the untinted texel and then adds tint * coverage.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class SpriteColorOperationAlphaTests : BaseTestClass
{
    private static readonly XnaColor[] Texels =
    {
        new XnaColor((byte)255, (byte)255, (byte)255, (byte)255), // opaque white
        new XnaColor((byte)255, (byte)255, (byte)255, (byte)128), // half-transparent white
        new XnaColor((byte)200, (byte)100, (byte)50, (byte)255),  // opaque color
    };

    private static readonly XnaColor Tint = new XnaColor((byte)255, (byte)128, (byte)0, (byte)255);
    private const int Alpha = 128;

    private const int SpriteX = 10;
    private const int SpriteY = 8;

    public override void Dispose()
    {
        Renderer.UsePremultipliedAlpha = false;
        base.Dispose();
    }

    [Theory]
    [InlineData(false, ColorOperation.Modulate)]
    [InlineData(true, ColorOperation.Modulate)]
    [InlineData(false, ColorOperation.ColorTextureAlpha)]
    [InlineData(true, ColorOperation.ColorTextureAlpha)]
    [InlineData(false, ColorOperation.Add)]
    [InlineData(true, ColorOperation.Add)]
    public void HalfAlphaTintedSprite_TexelCenters_MatchHandComputedComposite(bool premultiplied, ColorOperation colorOperation)
    {
        XnaColor[] pixels = RenderSprite(premultiplied, colorOperation);

        for (int texel = 0; texel < Texels.Length; texel++)
        {
            XnaColor pixel = SampleTexel(pixels, texel);
            MaxChannelDifference(pixel, ExpectedComposite(Texels[texel], colorOperation)).ShouldBeLessThanOrEqualTo(3,
                $"texel {texel}, premultiplied {premultiplied}, {colorOperation}: got {pixel}");
        }
    }

    private static (float R, float G, float B) ExpectedComposite(XnaColor straightTexel, ColorOperation colorOperation)
    {
        float coverage = straightTexel.A / 255f * (Alpha / 255f);

        float Channel(byte texel, byte tint, byte backdrop)
        {
            float tintRatio = tint / 255f;
            float source = colorOperation switch
            {
                ColorOperation.ColorTextureAlpha => tint,
                ColorOperation.Add => texel,
                _ => texel * tintRatio,
            };
            float composite = source * coverage + backdrop * (1 - coverage);
            if (colorOperation == ColorOperation.Add)
            {
                composite = System.Math.Min(255, composite + tint * coverage);
            }
            return composite;
        }

        return (
            Channel(straightTexel.R, Tint.R, Backdrop.R),
            Channel(straightTexel.G, Tint.G, Backdrop.G),
            Channel(straightTexel.B, Tint.B, Backdrop.B));
    }

    private static XnaColor SampleTexel(XnaColor[] pixels, int texel) =>
        pixels[((SpriteY + 8) * Width) + SpriteX + (texel * 16) + 8];

    private static XnaColor[] RenderSprite(bool premultiplied, ColorOperation colorOperation) =>
        Render(premultiplied, Texels.Length, 1, Texels, texture =>
        {
            SpriteRuntime sprite = new();
            sprite.Texture = texture;
            sprite.WidthUnits = DimensionUnitType.Absolute;
            sprite.HeightUnits = DimensionUnitType.Absolute;
            sprite.Width = Texels.Length * 16;
            sprite.Height = 16;
            sprite.X = SpriteX;
            sprite.Y = SpriteY;
            sprite.Color = Tint;
            sprite.Alpha = Alpha;
            sprite.ColorOperation = colorOperation;
            return sprite;
        });
}
