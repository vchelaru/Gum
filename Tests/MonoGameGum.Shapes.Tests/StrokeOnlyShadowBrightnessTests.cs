using Microsoft.Xna.Framework;
using Gum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #7297 — a stroke-only shape's shadow is the blurred ring. SkiaGum blurs a ring of width
// StrokeWidth with a Gaussian of sigma 0.4 * blur; Apos draws a ~0-width ring with a ramp, so the
// starting alpha (the "alphaScale") decides the brightness. It used to be StrokeWidth / (2 * blur),
// which left a thin ring 2.3x to 3.1x fainter than Skia one pixel past the edge.
public class StrokeOnlyShadowBrightnessTests
{
    // Measured with the real MonoGame render (gumcli screenshot) and the real SkiaGum render of a
    // white stroke-only 100x100 rectangle with a white shadow over black, edge alpha one pixel
    // out. Apos's ramp keeps about 0.7 (thin ring) to 0.9 (stroke 12) of the starting alpha at
    // that pixel, so the starting alpha is the Skia edge alpha divided by that.
    [Theory]
    [InlineData(4f, 8f, 0.358f)]
    [InlineData(4f, 16f, 0.228f)]
    [InlineData(4f, 32f, 0.122f)]
    [InlineData(4f, 56f, 0.071f)]
    [InlineData(12f, 16f, 0.443f)]
    [InlineData(12f, 32f, 0.320f)]
    [InlineData(12f, 56f, 0.203f)]
    public void ComputeStrokeShadowDrawParameters_ThinRing_StartingAlphaTracksSkiaRingEdgeAlpha(
        float strokeWidth, float blur, float skiaEdgeAlpha)
    {
        Circle sut = new() { IsFilled = false, StrokeWidth = strokeWidth, DropshadowBlurX = blur };

        (float _, Color color) = sut.ComputeStrokeShadowDrawParameters(Color.White);

        float apposEdgeAlpha = color.A / 255f * (strokeWidth > 8f ? 0.89f : 0.7f);
        apposEdgeAlpha.ShouldBe(skiaEdgeAlpha, tolerance: skiaEdgeAlpha * 0.15f);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_StrokeJustBelowTwiceBlur_IsContinuousWithFullAlpha()
    {
        Circle sut = new() { IsFilled = false, StrokeWidth = 7.9f, DropshadowBlurX = 4f };

        (float _, Color color) = sut.ComputeStrokeShadowDrawParameters(Color.White);

        color.A.ShouldBeGreaterThan((byte)250);
    }
}
