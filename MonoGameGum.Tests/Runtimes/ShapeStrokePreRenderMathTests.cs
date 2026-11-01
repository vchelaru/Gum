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

    // Issue #7381: between 1 and 2 screen px the push is the measured inverse of Apos's coverage. It must
    // never shrink as the width grows, and meet the plain width - 1 at both ends (no step).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StrokeBetweenOneAndTwoPixels_PushIsMonotoneAndContinuous(bool isLine)
    {
        float previous = Push(1f, isLine);
        previous.ShouldBe(0.01f);
        for (float width = 1.001f; width < 2f; width += 0.001f)
        {
            float pushed = Push(width, isLine);
            pushed.ShouldBeGreaterThanOrEqualTo(previous);
            previous = pushed;
        }
        Push(1.9999f, isLine).ShouldBe(1f, 0.01f);
        Push(2f, isLine).ShouldBe(1f, 0.0001f);
        Push(1.001f, isLine).ShouldBe(0.01f, 0.05f);
    }

    [Fact]
    public void StrokeBetweenOneAndTwoPixels_PushesMoreThanWidthMinusOne()
    {
        // Apos covers less than the pushed value implies, so 1.25 px needs more than 0.25 pushed.
        Push(1.25f, isLine: false).ShouldBeGreaterThan(0.25f);
        // The Line's rectangle adds coverage, so 1.5 px needs less than 0.5 pushed.
        Push(1.5f, isLine: true).ShouldBeLessThan(0.5f);
    }

    [Theory]
    [InlineData(2f, 1f)]
    [InlineData(3f, 2f)]
    [InlineData(12f, 11f)]
    public void StrokeOfTwoPixelsOrMore_IsWidthMinusOne(float width, float expected)
    {
        Push(width, isLine: false).ShouldBe(expected);
        Push(width, isLine: true).ShouldBe(expected);
    }

    [Fact]
    public void StrokeBetweenOneAndTwoPixels_AtZoomTwo_UsesScreenPixels()
    {
        // 0.75 world units at zoom 2 is 1.5 screen px, pushed in screen px then divided back to world units.
        float atZoomOne = Push(1.5f, isLine: false);
        ShapeStrokePreRenderMath.Compute(0.75f, 0f, 0f, DimensionUnitType.Absolute, true, true, 2f,
            strokeColorAlpha: 255).StrokeWidth.ShouldBe(atZoomOne / 2f, 0.0001f);
        // One world unit at zoom 2 is exactly 2 screen px: the plain width - 1 (0.5 world).
        ShapeStrokePreRenderMath.Compute(1f, 0f, 0f, DimensionUnitType.Absolute, true, true, 2f,
            strokeColorAlpha: 255).StrokeWidth.ShouldBe(0.5f);
    }

    private static float Push(float width, bool isLine)
    {
        return ShapeStrokePreRenderMath.Compute(
            width, 0f, 0f, DimensionUnitType.Absolute, true, true, 1f, strokeColorAlpha: 255, isLineShape: isLine).StrokeWidth;
    }
}
