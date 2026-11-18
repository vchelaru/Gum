using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using Gum.RenderingLibrary;
using Raylib_cs;
using RaylibGum.Tests.Helpers;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// <see cref="Blend.Subtract"/> (#8263): rgb = max(dest - src * srcAlpha, 0), dest alpha kept.
/// </summary>
public class SubtractBlendTests : BaseTestClass
{
    private const int Size = 8;

    [Theory]
    [InlineData(255, 255, 123)]
    [InlineData(82, 255, 0)]
    [InlineData(255, 128, 189)]
    public void Subtract_InRenderTarget_GivesClampedDifference(int behind, int sourceAlpha, int expected)
    {
        byte b = (byte)behind;
        PixelGrid pixels = RaylibPixelHarness.Render(Size, Size, new Color(b, b, b, (byte)255), container =>
            container.Children.Add(GraySprite((byte)sourceAlpha)));

        Color pixel = pixels[Size / 2, Size / 2];
        string message = $"was {pixel.R},{pixel.G},{pixel.B},{pixel.A}";
        System.Math.Abs(pixel.R - expected).ShouldBeLessThanOrEqualTo(1, message);
        System.Math.Abs(pixel.G - expected).ShouldBeLessThanOrEqualTo(1, message);
        System.Math.Abs(pixel.B - expected).ShouldBeLessThanOrEqualTo(1, message);
        pixel.A.ShouldBe((byte)255, message);
    }

    // The container composites its premultiplied bake, so its Subtract must subtract that color once.
    [Fact]
    public void Subtract_OnRenderTargetContainer_WithTranslucentContent_SubtractsOnce()
    {
        PixelGrid pixels = RaylibPixelHarness.Render(Size, Size, new Color((byte)255, (byte)255, (byte)255, (byte)255), outer =>
        {
            ContainerRuntime container = new();
            container.Width = Size;
            container.Height = Size;
            container.IsRenderTarget = true;
            container.Blend = Blend.Subtract;
            SpriteRuntime sprite = GraySprite(128);
            sprite.Blend = Blend.Normal;
            container.Children.Add(sprite);
            outer.Children.Add(container);
        });

        Color pixel = pixels[Size / 2, Size / 2];
        string message = $"was {pixel.R},{pixel.G},{pixel.B},{pixel.A}";
        System.Math.Abs(pixel.R - 189).ShouldBeLessThanOrEqualTo(1, message);
        System.Math.Abs(pixel.G - 189).ShouldBeLessThanOrEqualTo(1, message);
        System.Math.Abs(pixel.B - 189).ShouldBeLessThanOrEqualTo(1, message);
        pixel.A.ShouldBe((byte)255, message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToGlBlendFactorsSeparate_Subtract_ReverseSubtractsColorAndKeepsDestinationAlpha(bool isPremultiplyingContext)
    {
        (int srcRgb, int dstRgb, int srcAlpha, int dstAlpha, int eqRgb, int eqAlpha) =
            Blend.Subtract.ToGlBlendFactorsSeparate(isPremultiplyingContext);

        // GL_SRC_ALPHA, GL_ONE, GL_ZERO, GL_ONE, GL_FUNC_REVERSE_SUBTRACT, GL_FUNC_ADD
        (srcRgb, dstRgb, srcAlpha, dstAlpha, eqRgb, eqAlpha).ShouldBe((0x0302, 1, 0, 1, 0x800B, 0x8006));
    }

    private static SpriteRuntime GraySprite(byte alpha)
    {
        Image image = GenImageColor(4, 4, new Color((byte)132, (byte)132, (byte)132, alpha));
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = Size;
        sprite.Height = Size;
        sprite.Texture = texture;
        sprite.Blend = Blend.Subtract;
        return sprite;
    }
}
