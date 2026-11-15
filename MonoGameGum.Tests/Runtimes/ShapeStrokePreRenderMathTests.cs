using Gum.DataTypes;
using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #7350: Apos draws a stroke no thinner than its 1 px anti-alias hairline, so a thinner authored
// stroke is drawn as the hairline at alpha = width (coverage = width * alpha), as Skia's fractional width.
public class ShapeStrokePreRenderMathTests
{
    private static ShapeStrokePreRenderMath.Result Compute(
        float strokeWidth, float cameraZoom = 1f, bool isAntialiased = true, bool strokeSupportsAntialiasing = true,
        DimensionUnitType units = DimensionUnitType.Absolute)
    {
        return ShapeStrokePreRenderMath.Compute(
            strokeWidth, 0f, 0f, units, isAntialiased, strokeSupportsAntialiasing, cameraZoom, strokeColorAlpha: 255);
    }

    [Theory]
    [InlineData(0.25f, 0.25f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(0.99f, 0.99f)]
    public void StrokeUnderOnePixel_ScalesAlphaByWidth(float width, float expectedScale)
    {
        ShapeStrokePreRenderMath.Result result = Compute(width);

        result.StrokeAlphaScale.ShouldBe(expectedScale, 0.0001f);
        result.StrokeWidth.ShouldBe(0.01f);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(12f)]
    public void StrokeOfOnePixelOrMore_IsNotScaled(float width)
    {
        Compute(width).StrokeAlphaScale.ShouldBe(1f);
    }

    [Fact]
    public void StrokeUnderOnePixel_AtZoomTwo_UsesScreenPixels()
    {
        // 0.5 world units at zoom 2 is exactly one screen pixel: full alpha. 0.25 is half a pixel.
        Compute(0.5f, cameraZoom: 2f).StrokeAlphaScale.ShouldBe(1f);
        Compute(0.25f, cameraZoom: 2f).StrokeAlphaScale.ShouldBe(0.5f, 0.0001f);
    }

    [Fact]
    public void HiddenStroke_StaysHiddenAndUnscaled()
    {
        ShapeStrokePreRenderMath.Result result = Compute(0f);

        result.StrokeWidth.ShouldBe(0f);
        result.StrokeAlphaScale.ShouldBe(1f);
    }

    [Fact]
    public void NotAntialiased_IsNotScaled()
    {
        Compute(0.5f, isAntialiased: false).StrokeAlphaScale.ShouldBe(1f);
        Compute(0.5f, strokeSupportsAntialiasing: false).StrokeAlphaScale.ShouldBe(1f);
    }
}
