#if XNALIKE
using System;
using Gum.DataTypes;

namespace Gum.GueDeriving;

/// <summary>
/// Shared per-frame stroke math for the Blend push and the <c>PreRender</c> stroke-width /
/// dash-gap / fill-inset calculations duplicated (near-byte-identical) between
/// <see cref="CircleRuntime"/> and <see cref="RectangleRuntime"/>. Unlike
/// <see cref="ShapeGradientState"/> / <see cref="ShapeDropshadowState"/> this is pure
/// computation with no per-instance backing state to survive a <c>Clone()</c> — a static class,
/// not a struct.
/// </summary>
static class ShapeStrokePreRenderMath
{
    /// <summary>
    /// Issue #2937 — pushes a blend mode to both slots (blend is folded into each renderable's
    /// <c>BatchKey</c>; a fill/stroke disagreement would split them across two ShapeBatches and
    /// render the stroke with the wrong blend).
    /// </summary>
    public static void PushBlend(Gum.RenderingLibrary.Blend value, IBlendedRenderable? fill, IBlendedRenderable? stroke)
    {
        if (fill != null) fill.Blend = value;
        if (stroke != null) stroke.Blend = value;
    }

    /// <summary>
    /// Result of <see cref="Compute"/>: values ready to push onto the stroke/fill slots in
    /// <c>PreRender</c>. <see cref="FillInset"/> is intentionally unnamed after either
    /// <c>FillRadiusInset</c> (Circle) or <c>FillInset</c> (Rectangle) — the caller assigns it to
    /// its own differently-named property.
    /// </summary>
    public readonly struct Result
    {
        /// <summary>AA-compensated stroke width, ready to push to the stroke slot's <c>StrokeWidth</c>.</summary>
        public float StrokeWidth { get; init; }

        /// <summary>ScreenPixel-scaled dash length, ready to push to a dash-capable stroke slot.</summary>
        public float DashLength { get; init; }

        /// <summary>ScreenPixel-scaled gap length, ready to push to a dash-capable stroke slot.</summary>
        public float GapLength { get; init; }

        /// <summary>Fill inset value, ready to push to the fill slot's radius/edge inset property.</summary>
        public float FillInset { get; init; }

        /// <summary>
        /// Fraction of the stroke color's alpha to draw, ready to push to the stroke slot's
        /// <c>StrokeAlphaScale</c>. 1 except for an antialiased stroke thinner than Apos's 1 px
        /// hairline (issue #7350), where it is the authored width in screen pixels.
        /// </summary>
        public float StrokeAlphaScale { get; init; }
    }

    /// <summary>
    /// Computes the AA-compensated stroke width, the ScreenPixel-scaled dash/gap lengths, and
    /// the fill-inset value shared by <see cref="CircleRuntime.PreRender"/> and
    /// <see cref="RectangleRuntime.PreRender"/>. Body is today's <c>PreRender</c> math verbatim
    /// (both runtimes agreed byte-for-byte before this extraction).
    /// </summary>
    /// <param name="strokeWidth">The user-set stroke width (runtime's <c>_strokeWidth</c> field), in <paramref name="strokeWidthUnits"/>.</param>
    /// <param name="strokeDashLength">The user-set dash length, in <paramref name="strokeWidthUnits"/>.</param>
    /// <param name="strokeGapLength">The user-set gap length, in <paramref name="strokeWidthUnits"/>.</param>
    /// <param name="strokeWidthUnits">Unit of measurement for <paramref name="strokeWidth"/> / dash / gap.</param>
    /// <param name="isAntialiased">The runtime's <c>IsAntialiased</c> value.</param>
    /// <param name="strokeSupportsAntialiasing">Whether the stroke slot implements <see cref="IAntialiasedRenderable"/>.</param>
    /// <param name="cameraZoom">Current camera zoom (1 when there is no camera).</param>
    /// <param name="strokeColorAlpha">The stroke slot's current color alpha, gating the fill inset.</param>
    /// <param name="isLineShape">True for the Apos Line, which draws a rectangle whose anti-aliasing covers more than the outline shapes (issue #7381).</param>
    public static Result Compute(
        float strokeWidth, float strokeDashLength, float strokeGapLength,
        DimensionUnitType strokeWidthUnits, bool isAntialiased, bool strokeSupportsAntialiasing,
        float cameraZoom, byte strokeColorAlpha, bool isLineShape = false)
    {
        // Issue #2834/#2938 — the fill-inset gate below must read the ORIGINAL, unscaled
        // user-set stroke width (not the post-ScreenPixel-scaled local reassigned just below).
        // Capture it before the division so the gate is unaffected by ScreenPixel/zoom.
        float userStrokeWidth = strokeWidth;

        if (strokeWidthUnits == DimensionUnitType.ScreenPixel)
        {
            // Mirrors AposShapeRuntime.PreRender — dash and gap scale alongside stroke
            // width so a "1 px dotted" pattern stays 1 px on screen regardless of zoom.
            // (Dividing by cameraZoom is a no-op when there is no camera, since callers resolve
            // cameraZoom to 1f in that case — no separate "camera != null" guard needed here.)
            strokeWidth /= cameraZoom;
            strokeDashLength /= cameraZoom;
            strokeGapLength /= cameraZoom;
        }

        // Two distinct cases for what to push to the renderable's StrokeWidth — don't collapse
        // them, the difference is load-bearing:
        //
        // 1. User explicitly set StrokeWidth <= 0 → push a literal 0 (#2950 follow-up).
        //    StrokeWidth = 0 is the canonical hide-stroke gate since #2938 made StrokeColor
        //    non-nullable, so the user wants NO stroke at all. The renderable's
        //    HasVisibleOutput predicate then short-circuits Render to skip the stroke-slot
        //    draw entirely. **Do NOT route this case through the AA-compensation path
        //    below** — the epsilon floor would push 0.01, the renderable's HasVisibleOutput
        //    would return true (StrokeWidth > 0), and Apos's 1 px AA fringe would render a
        //    hairline of stroke color the user thought they had disabled.
        //
        // 2. User set a positive StrokeWidth → subtract the 1 px Apos AA contribution
        //    (#2790). Apos.Shapes' DrawCircle/DrawRectangle renders aaSize pixels of AA halo
        //    OUTSIDE the nominal thickness; Render passes aaSize = 1 when IsAntialiased is
        //    true. Skia fits AA WITHIN the thickness, so the same user-set StrokeWidth would
        //    otherwise read 1 px wider on Apos than on Skia. The result is floored at a tiny
        //    positive epsilon — NOT to hide a "0 means don't draw" case (that's handled above
        //    by case 1), but to keep thin strokes like StrokeWidth = 1 visible: after
        //    subtracting the AA contribution the math would be 0, which Apos refuses to draw
        //    even with aaSize > 0. The epsilon push pairs with the 1 px AA halo to produce the
        //    intended ~1 px visible stroke. Gated by strokeSupportsAntialiasing so the core
        //    stroke default (no AA concept) still receives the raw value.
        const float aposAaContribution = 1f;
        const float aposMinThicknessEpsilon = 0.01f;
        // Issue #2936 — aposAaContribution is in SCREEN pixels (Apos's hardcoded 1 px AA
        // halo). Convert to world units before mixing with strokeWidth, which has already been
        // resolved to world units above. At cameraZoom = 1 this is a no-op (original #2790
        // behavior preserved); at cameraZoom > 1 the world value shrinks proportionally, which
        // is what closes the gap below.
        float aposAaContributionWorld = aposAaContribution / cameraZoom;

        float renderableStrokeWidth;
        // Issue #7350 — Apos cannot draw a stroke thinner than its 1 px hairline, while Skia draws the
        // fractional width as fractional coverage (a 0.5 px stroke is half intensity over 1 px). Coverage
        // is width * alpha, so a stroke under 1 screen px draws as the hairline at alpha = width.
        float strokeAlphaScale = 1f;
        if (strokeWidth <= 0)
        {
            renderableStrokeWidth = 0f;
        }
        else if (isAntialiased && strokeSupportsAntialiasing)
        {
            float screenStrokeWidth = strokeWidth * cameraZoom;
            if (screenStrokeWidth > 1f && screenStrokeWidth < 2f)
            {
                // Issue #7381 - between 1 and 2 screen px the pushed (width - 1) is a thin Apos stroke whose
                // coverage is not linear in the pushed value, so push the value that covers the authored width.
                renderableStrokeWidth = Math.Max(aposMinThicknessEpsilon,
                    ThinStrokeCompensation.GetPushedScreenWidth(screenStrokeWidth, isLineShape) / cameraZoom);
            }
            else
            {
                renderableStrokeWidth = Math.Max(aposMinThicknessEpsilon, strokeWidth - aposAaContributionWorld);
            }
            if (strokeWidth < aposAaContributionWorld)
            {
                strokeAlphaScale = strokeWidth / aposAaContributionWorld;
            }
        }
        else
        {
            renderableStrokeWidth = strokeWidth;
        }

        // Issue #2834 — when both slots are visible, compute a radius/edge inset for the fill so
        // its rendered outer AA halo sits inside the stroke's opaque band. Two separate
        // antialiased Apos draws at the same radius composite their AA pixels, producing a red
        // fringe outside the white stroke (the Apos symptom; Skia shows a mirror-image pink
        // halo on the inside).
        //
        // Inset per side = max(renderableStrokeWidth, aposAaContribution when AA on).
        // renderableStrokeWidth alone aligns fine for thick strokes, but hairline (1 px)
        // strokes push a sub-pixel epsilon to Apos so the AA halo (still ~1 px) dominates and
        // would re-create the overlap without the floor.
        //
        // Gated on stroke visibility: alpha 0 OR StrokeWidth 0 means the stroke isn't drawn,
        // and inset would render a thin background ring/gap where the stroke would have been.
        // Issue #2938 made StrokeWidth = 0 the canonical hide-stroke gate (StrokeColor is now
        // non-nullable); the alpha guard stays as the pre-existing #2834 path.
        float fillInset = 0f;
        if (strokeColorAlpha > 0 && userStrokeWidth > 0)
        {
            fillInset = renderableStrokeWidth;
            if (isAntialiased && strokeSupportsAntialiasing)
            {
                // #2936: aaContribution in WORLD units (= 1 screen px / zoom). At Zoom > 1 this
                // is < 1 world unit, so the inset no longer over-shrinks the fill relative to
                // the visible stroke band's inward extent.
                fillInset = Math.Max(fillInset, aposAaContributionWorld);
            }
        }

        return new Result
        {
            StrokeWidth = renderableStrokeWidth,
            DashLength = strokeDashLength,
            GapLength = strokeGapLength,
            FillInset = fillInset,
            StrokeAlphaScale = strokeAlphaScale,
        };
    }
}

/// <summary>
/// Issue #7381 - Apos's coverage for a stroke pushed at <c>width - 1</c> is not linear for authored widths
/// between 1 and 2 screen px (a 1.25 px Rectangle covers 1.16, a 1.5 px Line covers 1.69), while Skia
/// covers exactly the authored width. The tables are Apos's measured total coverage across a stroke's
/// cross-section (<c>AposShapeStrokeWidthPixelTests</c>, shape edge on a pixel boundary) at pushed values
/// 0, 0.05 ... 1; the push for an authored width is found by inverting them with linear interpolation.
/// </summary>
static class ThinStrokeCompensation
{
    // Pushed value i * 0.05 covers Coverage[i] screen px. Both tables rise strictly, so the inverse is monotone.
    private const float PushStep = 0.05f;

    private static readonly float[] OutlineCoverage =
    {
        1.000f, 1.008f, 1.027f, 1.063f, 1.106f, 1.157f, 1.216f, 1.282f, 1.353f, 1.424f, 1.502f,
        1.576f, 1.647f, 1.718f, 1.784f, 1.843f, 1.898f, 1.941f, 1.973f, 1.992f, 2.000f,
    };

    // The Line draws a rectangle (or capsule) whose anti-aliased ends of the cross-section add coverage.
    private static readonly float[] LineCoverage =
    {
        1.016f, 1.075f, 1.149f, 1.224f, 1.298f, 1.369f, 1.435f, 1.502f, 1.569f, 1.631f, 1.690f,
        1.741f, 1.792f, 1.839f, 1.878f, 1.914f, 1.945f, 1.969f, 1.988f, 1.996f, 2.000f,
    };

    /// <summary>
    /// The width, in screen px, to push to Apos so the stroke covers <paramref name="authoredScreenWidth"/> px.
    /// Defined for 1 to 2 px; 1 maps to 0 and 2 maps to 1, matching <c>width - 1</c> at both ends.
    /// </summary>
    public static float GetPushedScreenWidth(float authoredScreenWidth, bool isLine)
    {
        float[] coverage = isLine ? LineCoverage : OutlineCoverage;
        if (authoredScreenWidth <= coverage[0])
        {
            return 0f;
        }
        for (int i = 1; i < coverage.Length; i++)
        {
            if (authoredScreenWidth <= coverage[i])
            {
                float t = (authoredScreenWidth - coverage[i - 1]) / (coverage[i] - coverage[i - 1]);
                return (i - 1 + t) * PushStep;
            }
        }
        return (coverage.Length - 1) * PushStep;
    }
}
#endif
