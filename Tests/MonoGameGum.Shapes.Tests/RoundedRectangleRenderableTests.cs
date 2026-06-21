using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #2979 — port Circle's #2950/#2977 dropshadow geometry to the RoundedRectangle renderable
// (the core renderable that draws the v3 Rectangle: IsFilled=true → fill slot, false → stroke
// slot). Apos.Shapes' DrawRectangle anchors the stroke at the OUTER edge (like DrawCircle), so
// the old naive shadow pass (size -= blur) marched the outline inward as blur grew, making a
// stroke-only Rectangle visibly "shrink." ComputeShadowDrawParameters mirrors Circle.Render's
// split: filled rectangles use the disk strict-anchor (ComputeShadowDrawGeometry keyed off the
// smaller half-dimension, with the negative clamp + alpha fade at extreme blur); stroke-only
// rectangles anchor the band centerline at the body stroke centerline (only the AA halo grows).
public class RoundedRectangleRenderableTests
{
    // Issue (Apos.Shapes filled-rectangle transparent-stroke bleed) — FillInset is the
    // rectangle analog of Circle.FillRadiusInset (#2834): pull the filled body's outer edge
    // inside the companion stroke band so a semi-transparent stroke shows the background
    // through it, not the fill. The body pass shrinks symmetrically about the center (so the
    // shape stays put); the shadow pass must NOT inherit the inset, or the shadow comes up
    // short of the body's outer edge (mirror of #2958 on the circle).

    [Fact]
    public void ComputeFillDrawRect_BodyPass_NoInset_ReturnsRect()
    {
        RoundedRectangle sut = new() { FillInset = 0f };

        (Vector2 position, Vector2 size) =
            sut.ComputeFillDrawRect(new Vector2(10, 10), new Vector2(100, 60), isShadowPass: false);

        position.ShouldBe(new Vector2(10, 10));
        size.ShouldBe(new Vector2(100, 60));
    }

    [Fact]
    public void ComputeFillDrawRect_BodyPass_WithInset_ShrinksAndRecentersAboutCenter()
    {
        RoundedRectangle sut = new() { FillInset = 4f };

        (Vector2 position, Vector2 size) =
            sut.ComputeFillDrawRect(new Vector2(10, 10), new Vector2(100, 60), isShadowPass: false);

        // Each side pulls in by FillInset → size shrinks by 2 * inset, top-left moves in by inset.
        size.ShouldBe(new Vector2(92, 52));
        position.ShouldBe(new Vector2(14, 14));
    }

    [Fact]
    public void ComputeFillDrawRect_BodyPass_InsetExceedsHalfDimension_ClampsToZero()
    {
        RoundedRectangle sut = new() { FillInset = 60f };

        (Vector2 _, Vector2 size) =
            sut.ComputeFillDrawRect(new Vector2(10, 10), new Vector2(100, 50), isShadowPass: false);

        // 100 - 120 and 50 - 120 both clamp to 0 rather than inverting the rect.
        size.X.ShouldBe(0f);
        size.Y.ShouldBe(0f);
    }

    [Fact]
    public void ComputeFillDrawRect_ShadowPass_IgnoresInset()
    {
        RoundedRectangle sut = new() { FillInset = 4f };

        (Vector2 position, Vector2 size) =
            sut.ComputeFillDrawRect(new Vector2(10, 10), new Vector2(100, 60), isShadowPass: true);

        position.ShouldBe(new Vector2(10, 10));
        size.ShouldBe(new Vector2(100, 60));
    }

    // Issue #3268 — concentric-corner fix. ComputeFillDrawRect insets the fill by FillInset on
    // every side, which moves each corner's center inward by (inset, inset). Keeping the full
    // CornerRadius there leaves the fill arc NON-concentric with the stroke's inner edge, opening
    // a background gap that is ~0 along the straight edges and widest at the 45° point of each
    // corner. The fix reduces the fill's corner radius by FillInset (clamped at 0) so the corner
    // center stays put — the classic "inner radius = outer radius − inset" rule. The shadow pass
    // draws the fill at full size (no inset), so it must keep the full radius.

    [Fact]
    public void ComputeFillCornerRadius_BodyPass_NoInset_ReturnsRadius()
    {
        RoundedRectangle sut = new() { CornerRadius = 18f, FillInset = 0f };

        sut.ComputeFillCornerRadius(isShadowPass: false).ShouldBe(18f);
    }

    [Fact]
    public void ComputeFillCornerRadius_BodyPass_WithInset_ReducesByInset()
    {
        RoundedRectangle sut = new() { CornerRadius = 18f, FillInset = 4f };

        // Concentric rule: inner radius = outer radius − inset.
        sut.ComputeFillCornerRadius(isShadowPass: false).ShouldBe(14f);
    }

    [Fact]
    public void ComputeFillCornerRadius_BodyPass_InsetExceedsRadius_ClampsToZero()
    {
        RoundedRectangle sut = new() { CornerRadius = 3f, FillInset = 10f };

        sut.ComputeFillCornerRadius(isShadowPass: false).ShouldBe(0f);
    }

    [Fact]
    public void ComputeFillCornerRadius_ShadowPass_IgnoresInset()
    {
        RoundedRectangle sut = new() { CornerRadius = 18f, FillInset = 4f };

        // Shadow fill draws at full size, so it keeps the full radius.
        sut.ComputeFillCornerRadius(isShadowPass: true).ShouldBe(18f);
    }

    [Fact]
    public void ComputeFillCornerRadii_BodyPass_WithInset_ReducesEachCorner()
    {
        RoundedRectangle sut = new()
        {
            CornerRadius = 18f,
            CustomRadiusTopLeft = 20f,
            CustomRadiusTopRight = 16f,
            CustomRadiusBottomRight = 12f,
            CustomRadiusBottomLeft = 8f,
            FillInset = 4f,
        };

        var (topLeft, topRight, bottomRight, bottomLeft) = sut.ComputeFillCornerRadii(isShadowPass: false);

        topLeft.ShouldBe(16f);
        topRight.ShouldBe(12f);
        bottomRight.ShouldBe(8f);
        bottomLeft.ShouldBe(4f);
    }

    [Fact]
    public void ComputeFillCornerRadii_NullCorner_FallsBackToCornerRadiusThenReduces()
    {
        // A null per-corner override resolves to CornerRadius (matching HasCustomCorners) and is
        // then reduced by the inset like any other corner.
        RoundedRectangle sut = new()
        {
            CornerRadius = 18f,
            CustomRadiusTopLeft = 20f,
            CustomRadiusTopRight = null,
            CustomRadiusBottomRight = null,
            CustomRadiusBottomLeft = null,
            FillInset = 4f,
        };

        var (topLeft, topRight, bottomRight, bottomLeft) = sut.ComputeFillCornerRadii(isShadowPass: false);

        topLeft.ShouldBe(16f);
        topRight.ShouldBe(14f);   // (18 fallback) − 4
        bottomRight.ShouldBe(14f);
        bottomLeft.ShouldBe(14f);
    }

    [Fact]
    public void ComputeFillCornerRadii_InsetExceedsCorner_ClampsToZero()
    {
        RoundedRectangle sut = new()
        {
            CornerRadius = 18f,
            CustomRadiusTopLeft = 2f,
            FillInset = 10f,
        };

        var (topLeft, _, _, _) = sut.ComputeFillCornerRadii(isShadowPass: false);

        topLeft.ShouldBe(0f);
    }

    [Fact]
    public void ComputeFillCornerRadii_ShadowPass_IgnoresInset()
    {
        RoundedRectangle sut = new()
        {
            CornerRadius = 18f,
            CustomRadiusTopLeft = 20f,
            FillInset = 4f,
        };

        var (topLeft, topRight, bottomRight, bottomLeft) = sut.ComputeFillCornerRadii(isShadowPass: true);

        // Full size on the shadow pass → full custom radius (20) and full fallback (18).
        topLeft.ShouldBe(20f);
        topRight.ShouldBe(18f);
        bottomRight.ShouldBe(18f);
        bottomLeft.ShouldBe(18f);
    }

    // Bug: the pixel-center AA offset (position += 0.5, size -= 1) that aligns an antialiased
    // edge to the SCREEN pixel grid (Apos issue #12) was applied in WORLD units, ignoring camera
    // zoom. Like the AA halo (#2936) it must be divided by cameraZoom so it stays a constant
    // on-screen size — otherwise a filled rectangle inset visibly grows as the Gum tool zooms in
    // and the fill pulls away from an equally-sized NineSlice. At zoom 1 the values match the
    // historical 0.5 / 1.0.

    [Fact]
    public void ApplyAntiAliasInset_AaOff_ReturnsUnchanged()
    {
        RoundedRectangle sut = new();

        (Vector2 position, Vector2 size) =
            sut.ApplyAntiAliasInset(new Vector2(10, 10), new Vector2(100, 60), antiAliasSize: 0, cameraZoom: 1f);

        position.ShouldBe(new Vector2(10, 10));
        size.ShouldBe(new Vector2(100, 60));
    }

    [Fact]
    public void ApplyAntiAliasInset_AaOn_Zoom1_InsetsHalfScreenPixel()
    {
        RoundedRectangle sut = new();

        (Vector2 position, Vector2 size) =
            sut.ApplyAntiAliasInset(new Vector2(10, 10), new Vector2(100, 60), antiAliasSize: 1, cameraZoom: 1f);

        position.ShouldBe(new Vector2(10.5f, 10.5f));
        size.ShouldBe(new Vector2(99f, 59f));
    }

    [Fact]
    public void ApplyAntiAliasInset_AaOn_Zoom2_InsetScaledToScreenPixel()
    {
        RoundedRectangle sut = new();

        (Vector2 position, Vector2 size) =
            sut.ApplyAntiAliasInset(new Vector2(10, 10), new Vector2(100, 60), antiAliasSize: 1, cameraZoom: 2f);

        // Half a SCREEN pixel = 0.25 world units at zoom 2; the full trim is twice that.
        position.ShouldBe(new Vector2(10.25f, 10.25f));
        size.ShouldBe(new Vector2(99.5f, 59.5f));
    }

    [Fact]
    public void ComputeShadowDrawParameters_Filled_ZeroBlur_SizeUnchanged()
    {
        RoundedRectangle sut = new() { IsFilled = true, DropshadowBlurX = 0f };

        (Vector2 size, int aaSize, float alphaScale) =
            sut.ComputeShadowDrawParameters(new Vector2(100, 60), effectiveShadowStrokeWidth: 0f, cameraZoom: 1f);

        size.X.ShouldBe(100f);
        size.Y.ShouldBe(60f);
        aaSize.ShouldBe(0);
        alphaScale.ShouldBe(1f);
    }

    [Fact]
    public void ComputeShadowDrawParameters_Filled_StandardBlur_ShrinksByBlurEachDimension()
    {
        // Standard case (blur <= 2 * minHalf): minHalf = 30, blur = 10 <= 60. Each side pulls in
        // blur/2 so the 50% line lands on the original edge (CSS box-shadow), i.e. each dimension
        // shrinks by the full blur. aaSize = blur.
        RoundedRectangle sut = new() { IsFilled = true, DropshadowBlurX = 10f };

        (Vector2 size, int aaSize, float alphaScale) =
            sut.ComputeShadowDrawParameters(new Vector2(100, 60), effectiveShadowStrokeWidth: 0f, cameraZoom: 1f);

        size.X.ShouldBe(90f);
        size.Y.ShouldBe(50f);
        aaSize.ShouldBe(10);
        alphaScale.ShouldBe(1f);
    }

    [Fact]
    public void ComputeShadowDrawParameters_Filled_ExtremeBlur_ClampsMinDimensionAndScalesAlpha()
    {
        // blur > 2 * minHalf: minHalf = 25 (Height 50), blur = 200 > 50. The disk anchor truncates
        // the inner ramp (effRadius = 0 → inset = minHalf = 25), so the min dimension collapses to
        // 0 and alpha scales below 1. The other dimension pulls in 2 * minHalf = 50.
        RoundedRectangle sut = new() { IsFilled = true, DropshadowBlurX = 200f };

        (Vector2 size, int aaSize, float alphaScale) =
            sut.ComputeShadowDrawParameters(new Vector2(120, 50), effectiveShadowStrokeWidth: 0f, cameraZoom: 1f);

        size.Y.ShouldBe(0f);
        size.X.ShouldBe(70f);
        alphaScale.ShouldBeLessThan(1f);
        aaSize.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ComputeShadowDrawParameters_StrokeOnly_BlurLessThanStroke_ShrinksByBlur()
    {
        // blur < StrokeWidth: effectiveShadowStrokeWidth = StrokeWidth - blur = 10 - 4 = 6. The
        // band centerline must stay on the body centerline, so each side pulls in
        // (StrokeWidth - effShStroke)/2 = blur/2 = 2 → each dimension shrinks by the blur (4).
        RoundedRectangle sut = new() { IsFilled = false, StrokeWidth = 10f, DropshadowBlurX = 4f };

        (Vector2 size, int aaSize, float alphaScale) =
            sut.ComputeShadowDrawParameters(new Vector2(100, 60), effectiveShadowStrokeWidth: 6f, cameraZoom: 1f);

        size.X.ShouldBe(96f);
        size.Y.ShouldBe(56f);
        aaSize.ShouldBe(4);
        alphaScale.ShouldBe(1f);
    }

    [Fact]
    public void ComputeShadowDrawParameters_StrokeOnly_LargeBlur_AnchorsAtStrokeCenterlineNotInward()
    {
        // This is the #2977 bug for rectangles. At blur >= StrokeWidth,
        // ComputeStrokeShadowDrawParameters clamps the effective shadow stroke width to a ~0
        // epsilon. The band centerline must stay anchored at the body stroke centerline, so the
        // shadow box shrinks by StrokeWidth (NOT by the ever-growing blur). A naive size -= blur
        // model would keep contracting; assert it does NOT.
        RoundedRectangle sut = new() { IsFilled = false, StrokeWidth = 10f, DropshadowBlurX = 200f };

        (Vector2 size, int aaSize, float _) =
            sut.ComputeShadowDrawParameters(new Vector2(100, 60), effectiveShadowStrokeWidth: 0.01f, cameraZoom: 1f);

        // Anchored: shrink by StrokeWidth (minus the epsilon band), independent of blur.
        size.X.ShouldBe(100f - 10f + 0.01f, tolerance: 0.001f);
        size.Y.ShouldBe(60f - 10f + 0.01f, tolerance: 0.001f);
        // Halo grows with blur.
        aaSize.ShouldBe(200);
    }

    [Fact]
    public void ComputeShadowDrawParameters_AaSizeScalesByZoom()
    {
        RoundedRectangle sut = new() { IsFilled = true, DropshadowBlurX = 10f };

        (Vector2 _, int aaSize, float _) =
            sut.ComputeShadowDrawParameters(new Vector2(100, 60), effectiveShadowStrokeWidth: 0f, cameraZoom: 2f);

        aaSize.ShouldBe(20);
    }

    [Fact]
    public void ComputeShadowDrawParameters_NegativeBlur_TreatedAsZero()
    {
        // DropshadowBlurX clamps negatives to 0 at the setter (#2977), so a negative blur renders
        // identically to 0: no shrink, no halo.
        RoundedRectangle sut = new() { IsFilled = true, DropshadowBlurX = -50f };

        (Vector2 size, int aaSize, float alphaScale) =
            sut.ComputeShadowDrawParameters(new Vector2(100, 60), effectiveShadowStrokeWidth: 0f, cameraZoom: 1f);

        size.X.ShouldBe(100f);
        size.Y.ShouldBe(60f);
        aaSize.ShouldBe(0);
        alphaScale.ShouldBe(1f);
    }

    // Issue #2998 — gradient visibility keys off the gradient STOP alphas (Alpha1 / Alpha2), not the
    // slot's solid Color. ShouldPaintGradient lives on the shared RenderableShapeBase (also covered
    // by CircleRenderableTests); these duplicate the contract on RoundedRectangle because it is the
    // shape the Forest Glade buttons / list rows actually use — the originally-reported regression.

    [Fact]
    public void ShouldPaintGradient_BothGradientStopsTransparent_OpaqueSolid_False()
    {
        RoundedRectangle sut = new() { UseGradient = true, Color = new Color(255, 255, 255, 255), Alpha1 = 0, Alpha2 = 0 };

        sut.ShouldPaintGradient(forcedColor: null).ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintGradient_VisibleGradientStops_TransparentSolid_True()
    {
        RoundedRectangle sut = new() { UseGradient = true, Color = new Color(0, 0, 0, 0), Alpha1 = 255, Alpha2 = 255 };

        sut.ShouldPaintGradient(forcedColor: null).ShouldBeTrue();
    }
}
