using Microsoft.Xna.Framework;
using Gum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #7292 — a dropshadow with blur N must visibly reach N pixels past the shape edge (the
// documented contract, and what raylib and Skia draw). The Apos ramp used to be centered on the
// edge with total width N, so it reached only N/2. These tests pin the OUTWARD REACH of the
// Apos.Shapes draw parameters: the outer end of the alpha ramp is `radius + aaSize / zoom`
// (Apos reads aaSize in screen pixels).
public class DropshadowExtentTests
{
    [Theory]
    [InlineData(50f, 8f, 1f)]
    [InlineData(50f, 32f, 1f)]
    [InlineData(20f, 32f, 1f)]   // truncated inner ramp (blur > radius)
    [InlineData(20f, 56f, 1f)]
    [InlineData(10f, 100f, 1f)]
    [InlineData(50f, 32f, 2f)]   // zoomed: reach stays a constant world distance
    public void ComputeShadowDrawGeometry_OuterRampEnd_IsBlurPastHostEdge(float hostRadius, float blur, float zoom)
    {
        Circle sut = new() { IsFilled = true, DropshadowBlurX = blur };

        (float effRadius, int effAaSize, float _) = sut.ComputeShadowDrawGeometry(hostRadius, zoom);

        float reach = effRadius + effAaSize / zoom - hostRadius;
        reach.ShouldBe(blur, tolerance: 0.5f / zoom);
    }

    [Theory]
    [InlineData(50f, 8f)]
    [InlineData(50f, 32f)]
    public void ComputeShadowDrawGeometry_StandardBlur_HalfAlphaLineSitsOnHostEdge(float hostRadius, float blur)
    {
        Circle sut = new() { IsFilled = true, DropshadowBlurX = blur };

        (float effRadius, int effAaSize, float alphaScale) = sut.ComputeShadowDrawGeometry(hostRadius, 1f);

        alphaScale.ShouldBe(1f);
        (effRadius + effAaSize / 2f).ShouldBe(hostRadius, tolerance: 0.5f);
    }

    [Theory]
    [InlineData(10f, 4f, 1f)]    // stroke wider than 2 * blur: ring shrinks by the blur each side
    [InlineData(4f, 8f, 1f)]     // thin stroke: clamped to the epsilon band
    [InlineData(4f, 32f, 1f)]
    [InlineData(4f, 32f, 2f)]
    public void StrokeOnlyCircle_ShadowRampEnd_IsBlurPastBodyOuterEdge(float strokeWidth, float blur, float zoom)
    {
        Circle sut = new() { IsFilled = false, StrokeWidth = strokeWidth, DropshadowBlurX = blur };
        const float hostRadius = 50f;

        (float effStroke, Color _) = sut.ComputeStrokeShadowDrawParameters(Color.White);
        float shadowRadius = sut.ComputeStrokeShadowDrawRadius(hostRadius, effStroke);
        int aaSize = sut.GetShadowAntiAliasSize(zoom);

        float reach = shadowRadius + aaSize / zoom - hostRadius;
        reach.ShouldBe(blur, tolerance: 0.5f / zoom);
    }

    [Theory]
    [InlineData(100f, 60f, 8f)]
    [InlineData(200f, 40f, 32f)]  // Neon-button-like: blur > half height, truncated ramp
    [InlineData(120f, 50f, 200f)]
    public void FilledRectangle_ShadowRampEnd_IsBlurPastHostEdgeOnBothAxes(float width, float height, float blur)
    {
        RoundedRectangle sut = new() { IsFilled = true, DropshadowBlurX = blur };

        (Vector2 size, int aaSize, float _) =
            sut.ComputeShadowDrawParameters(new Vector2(width, height), effectiveShadowStrokeWidth: 0f, cameraZoom: 1f);

        (size.X / 2f + aaSize - width / 2f).ShouldBe(blur, tolerance: 0.5f);
        (size.Y / 2f + aaSize - height / 2f).ShouldBe(blur, tolerance: 0.5f);
    }

    [Theory]
    [InlineData(10f, 4f)]
    [InlineData(4f, 32f)]
    public void StrokeOnlyRectangle_ShadowRampEnd_IsBlurPastHostEdge(float strokeWidth, float blur)
    {
        RoundedRectangle sut = new() { IsFilled = false, StrokeWidth = strokeWidth, DropshadowBlurX = blur };

        (float effStroke, Color _) = sut.ComputeStrokeShadowDrawParameters(Color.White);
        (Vector2 size, int aaSize, float _) =
            sut.ComputeShadowDrawParameters(new Vector2(100, 60), effStroke, cameraZoom: 1f);

        // Apos anchors a rectangle stroke at its outer edge, so the ramp ends at size/2 + aaSize
        // from the center.
        (size.X / 2f + aaSize - 50f).ShouldBe(blur, tolerance: 0.5f);
    }

    [Fact]
    public void FilledCircle_PeakAlpha_NotDimmedUntilBlurExceedsDiameter()
    {
        // A blur of 32 on a 40-px-tall shape must keep the full edge alpha (the Neon button
        // glow); only a blur larger than the diameter spreads the shape's energy thin.
        Circle sut = new() { IsFilled = true, DropshadowBlurX = 32f };

        (float _, int _, float alphaScale) = sut.ComputeShadowDrawGeometry(hostRadius: 20f, cameraZoom: 1f);

        // Edge alpha = anchorAlpha * (1 - smoothstep(R / (R + B))) lands at 0.5; the energy
        // factor must be 1 here, so anchorAlpha stays at its anchor-only value.
        float t = 20f / (20f + 32f);
        float smooth = 3f * t * t - 2f * t * t * t;
        alphaScale.ShouldBe(0.5f / (1f - smooth), tolerance: 0.001f);
    }
}
