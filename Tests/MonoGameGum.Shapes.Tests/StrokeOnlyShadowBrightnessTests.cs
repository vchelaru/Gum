using Microsoft.Xna.Framework;
using Gum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #7297 — a stroke-only shape's shadow is the blurred ring. SkiaGum blurs a ring of width
// StrokeWidth with a Gaussian of sigma 0.4 * blur; Apos draws a ~0-width ring with a ramp, so the
// starting alpha decides the brightness. It used to be StrokeWidth / (2 * blur), which left a thin
// ring 2.3x to 3.1x fainter than Skia one pixel past the edge.
public class StrokeOnlyShadowBrightnessTests
{
    // The Skia and MonoGame columns are measured, not computed here: edge alpha one pixel past the
    // outer edge of a white stroke-only 100x100 rectangle with a white shadow over black, from the
    // real SkiaGum render and the real MonoGame render (gumcli screenshot --backend monogame).
    // There is no GPU render helper in this test project, so the MonoGame number cannot be
    // recomputed here; the starting alpha the render used is pinned instead, so a change to the
    // formula fails this test until the render is measured again.
    //
    // The user's stroke width S reaches the renderable as S - 1 (Apos adds a one pixel fringe;
    // ShapeStrokePreRenderMath), which is what the starting alpha is computed from.
    [Theory]
    [InlineData(2f, 8f, 60, 0.22f, 0.22f)]
    [InlineData(2f, 16f, 31, 0.118f, 0.122f)]
    [InlineData(2f, 32f, 15, 0.063f, 0.059f)]
    [InlineData(2f, 56f, 9, 0.039f, 0.035f)]
    [InlineData(4f, 8f, 108, 0.361f, 0.357f)]
    [InlineData(4f, 16f, 61, 0.227f, 0.227f)]
    [InlineData(4f, 32f, 31, 0.122f, 0.122f)]
    [InlineData(4f, 56f, 18, 0.075f, 0.071f)]
    [InlineData(12f, 8f, 201, 0.439f, 0.439f)]
    [InlineData(12f, 16f, 143, 0.451f, 0.443f)]
    [InlineData(12f, 32f, 88, 0.325f, 0.318f)]
    [InlineData(12f, 56f, 53, 0.22f, 0.204f)]
    [InlineData(24f, 16f, 206, 0.471f, 0.471f)]
    [InlineData(24f, 32f, 144, 0.467f, 0.455f)]
    [InlineData(24f, 56f, 99, 0.384f, 0.353f)]
    public void ComputeStrokeShadowDrawParameters_ThinRing_StartingAlphaIsTheMeasuredOne(
        float userStrokeWidth, float blur, int startingAlphaUsedInRender, float skiaEdgeAlpha, float monoGameEdgeAlpha)
    {
        Circle sut = new() { IsFilled = false, StrokeWidth = userStrokeWidth - 1f, DropshadowBlurX = blur };

        (float _, Color color) = sut.ComputeStrokeShadowDrawParameters(Color.White);

        ((float)color.A).ShouldBe((float)startingAlphaUsedInRender, 1f);
        monoGameEdgeAlpha.ShouldBe(skiaEdgeAlpha, tolerance: skiaEdgeAlpha * 0.15f);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_StrokeJustBelowTwiceBlur_IsContinuousWithFullAlpha()
    {
        Circle sut = new() { IsFilled = false, StrokeWidth = 7.9f, DropshadowBlurX = 4f };

        (float _, Color color) = sut.ComputeStrokeShadowDrawParameters(Color.White);

        color.A.ShouldBeGreaterThan((byte)250);
    }
}
