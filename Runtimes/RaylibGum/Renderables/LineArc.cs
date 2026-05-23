using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Numerics;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

/// <summary>
/// Raylib arc renderable. Issue #2866 — fills the gap that left arcs out of the raylib gallery
/// when the rest of the shape suite landed in #2757. Models an arc as a stroked band along a
/// circular curve inscribed in <see cref="IPositionedSizedObject.Width"/> /
/// <see cref="IPositionedSizedObject.Height"/>; <see cref="Thickness"/> controls how wide the
/// band is, with <c>Thickness = min(W,H)/2</c> collapsing the band to a true pie wedge (see the
/// <see cref="Thickness"/> docs for the wedge configuration).
/// </summary>
/// <remarks>
/// Single-primitive design (see #2866 PR comment): both the thin-stroke and the wedge
/// configurations come out of one <c>DrawRing</c> call. No <c>IsFilled</c> flag, no separate
/// fill/stroke slots — matches the post-#2891 Skia <c>Arc</c> contract where arcs are stroked
/// only and the wedge is reached via Thickness = W/2.
/// <para>
/// Gradients are not implemented in this pass (deferred follow-up to #2866). The per-segment
/// triangle-fan approach that <c>LineCircle</c> uses for radial/linear gradients on a full disk
/// applies cleanly to a stroked arc band, but lands separately so this PR stays scoped to
/// achieving the gallery-parity baseline.
/// </para>
/// </remarks>
public class LineArc : InvisibleRenderable
{
    /// <summary>
    /// Color slot used as the stroke color when <see cref="StrokeColor"/> is <c>null</c>.
    /// Defaults to white so a freshly-constructed <see cref="LineArc"/> with no explicit color
    /// renders visibly against the gallery's dark background.
    /// </summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// Explicit stroke-pass color. When <c>null</c>, the stroke pass uses <see cref="Color"/>.
    /// Matches the legacy/explicit color pattern <see cref="LineCircle"/> uses, so the shared
    /// <c>ArcRuntime</c> raylib branch can route a single <c>StrokeColor</c> setter consistently
    /// across both shapes.
    /// </summary>
    public Color? StrokeColor { get; set; }

    /// <summary>
    /// Angle, in degrees, at which the arc begins. A value of 0 points right (3 o'clock),
    /// increasing values sweep counter-clockwise — matches the Gum convention shared with the
    /// Skia and Apos.Shapes <c>Arc</c> renderables.
    /// </summary>
    public float StartAngle { get; set; }

    /// <summary>
    /// How far the arc sweeps from <see cref="StartAngle"/>, in degrees. 360 produces a full
    /// ring. Defaults to 90, matching the post-#2891 <c>ArcRuntime</c> default.
    /// </summary>
    public float SweepAngle { get; set; } = 90f;

    /// <summary>
    /// Width of the stroked band along the arc curve, in pixels. See <c>ArcRuntime.Thickness</c>
    /// for the full visual-progression docs (thin line → fat band → pie wedge at <c>W/2</c>).
    /// Defaults to 10 to match the cross-backend <c>ArcRuntime</c> constructor default.
    /// </summary>
    public float Thickness { get; set; } = 10f;

    /// <summary>
    /// raylib has no per-shape stroke cap analog to Skia's <c>SKPaint.StrokeCap</c> — the
    /// <c>DrawRing</c> primitive always produces flat (butt) radial ends. <c>IsEndRounded = true</c>
    /// is therefore a visual no-op on raylib in this pass; the property is preserved for API
    /// parity with the Skia/Apos branches so the shared <c>ArcRuntime</c> can push the value
    /// uniformly. A future pass could approximate rounded caps with two <c>DrawCircleSector</c>
    /// half-disks at each end, but the issue body does not call for it and the gallery's
    /// end-cap row is omitted on raylib for the same reason.
    /// </summary>
    public bool IsEndRounded { get; set; }

    /// <summary>
    /// Length in world-space pixels of each dash segment around the arc curve. A value of 0
    /// (the default) draws a solid stroke. Both <see cref="StrokeDashLength"/> and
    /// <see cref="StrokeGapLength"/> must be &gt; 0 for dashed rendering to engage. Implemented
    /// via a per-dash <c>DrawRing</c> sub-arc loop, same pattern <see cref="LineCircle"/> uses
    /// for dashed circles.
    /// </summary>
    public float StrokeDashLength { get; set; }

    /// <summary>
    /// Length in world-space pixels of each gap between dashes. Ignored when
    /// <see cref="StrokeDashLength"/> is 0.
    /// </summary>
    public float StrokeGapLength { get; set; }

    /// <summary>
    /// When <c>true</c>, a dropshadow pass renders behind the stroke using
    /// <see cref="DropshadowColor"/>, offset by <see cref="DropshadowOffsetX"/> /
    /// <see cref="DropshadowOffsetY"/>, with an isotropic blur radius of
    /// <c>max(DropshadowBlurX, DropshadowBlurY)</c>. Approximated the same way
    /// <see cref="LineCircle"/> approximates its dropshadow — concentric semi-transparent bands
    /// of decreasing alpha. Inherits the band-stacking overshoot tracked in #2865; the arc
    /// renderable will pick up the fix for free when #2865 lands its RT + separable-blur
    /// replacement.
    /// </summary>
    public bool HasDropshadow { get; set; }

    /// <summary>Color of the dropshadow band (alpha channel scales the falloff).</summary>
    public Color DropshadowColor { get; set; } = new Color((byte)0, (byte)0, (byte)0, (byte)255);

    /// <summary>X offset of the dropshadow band in world-space pixels.</summary>
    public float DropshadowOffsetX { get; set; }

    /// <summary>Y offset of the dropshadow band in world-space pixels.</summary>
    public float DropshadowOffsetY { get; set; }

    /// <inheritdoc cref="SkiaGum.GueDeriving.SkiaShapeRuntime.DropshadowBlurX"/>
    /// <remarks>raylib note: treated as isotropic with <see cref="DropshadowBlurY"/> —
    /// rendering collapses anisotropic blur to <c>max(BlurX, BlurY)</c>.</remarks>
    public float DropshadowBlurX { get; set; }

    /// <inheritdoc cref="SkiaGum.GueDeriving.SkiaShapeRuntime.DropshadowBlurX"/>
    /// <remarks>raylib note: treated as isotropic with <see cref="DropshadowBlurX"/> —
    /// rendering collapses anisotropic blur to <c>max(BlurX, BlurY)</c>.</remarks>
    public float DropshadowBlurY { get; set; }

    /// <inheritdoc/>
    public override int Alpha
    {
        get => Color.A;
        set
        {
            if (value != Color.A)
            {
                Color = new Color(Color.R, Color.G, Color.B, (byte)value);
            }
        }
    }

    /// <summary>Red channel of the legacy <see cref="Color"/> slot.</summary>
    public int Red
    {
        get => Color.R;
        set
        {
            if (value != Color.R)
            {
                Color = new Color((byte)value, Color.G, Color.B, Color.A);
            }
        }
    }

    /// <summary>Green channel of the legacy <see cref="Color"/> slot.</summary>
    public int Green
    {
        get => Color.G;
        set
        {
            if (value != Color.G)
            {
                Color = new Color(Color.R, (byte)value, Color.B, Color.A);
            }
        }
    }

    /// <summary>Blue channel of the legacy <see cref="Color"/> slot.</summary>
    public int Blue
    {
        get => Color.B;
        set
        {
            if (value != Color.B)
            {
                Color = new Color(Color.R, Color.G, (byte)value, Color.A);
            }
        }
    }

    /// <inheritdoc cref="LineArc"/>
    public LineArc() : this(null) { }

    /// <inheritdoc cref="LineArc"/>
    public LineArc(SystemManagers? _) { }

    /// <inheritdoc/>
    public override void Render(ISystemManagers managers)
    {
        if (!Visible)
        {
            return;
        }

        // Curve is inscribed in the bounding box, anchored at the box's center. Mirrors the
        // way Skia's path.AddArc(boundingRect, ...) treats the rect — the arc fits the smaller
        // of W/H so non-square bounds still render a circular (not elliptical) arc.
        float halfW = Width * 0.5f;
        float halfH = Height * 0.5f;
        float outerRadius = System.MathF.Min(halfW, halfH);
        if (outerRadius <= 0f)
        {
            return;
        }
        // Thickness > 2 * outerRadius is undefined territory (matches the Skia/Apos contract —
        // see ArcRuntime.Thickness docs). Clamp so the inner radius doesn't go negative and the
        // wedge configuration (Thickness = outerRadius) still works cleanly.
        float thickness = System.MathF.Min(Thickness, outerRadius * 2f);
        float innerRadius = System.MathF.Max(0f, outerRadius - thickness);

        float cx = this.GetAbsoluteLeft() + halfW;
        float cy = this.GetAbsoluteTop() + halfH;

        // raylib's DrawRing uses degrees with 0 at 3 o'clock and increasing angles sweeping
        // clockwise on screen (y-down). Gum's convention is 0 at 3 o'clock with increasing
        // angles sweeping counter-clockwise (the renderable inherits this from the Skia/Apos
        // Arc primitives). Negate both StartAngle and SweepAngle to flip CCW→CW; raylib's
        // DrawRing handles startAngle > endAngle by stepping with a negative delta, so the arc
        // direction stays correct visually.
        float startAngleDeg = -StartAngle;
        float endAngleDeg = -StartAngle - SweepAngle;
        int segments = ComputeSegments(outerRadius, System.MathF.Abs(SweepAngle));

        Color strokeColor = StrokeColor ?? Color;

        if (HasDropshadow)
        {
            DrawDropshadow(cx, cy, innerRadius, outerRadius, startAngleDeg, endAngleDeg,
                segments, strokeColor.A);
        }

        bool dashed = StrokeDashLength > 0f && StrokeGapLength > 0f && SweepAngle != 0f;
        if (dashed)
        {
            DrawDashed(new Vector2(cx, cy), innerRadius, outerRadius, strokeColor);
        }
        else
        {
            DrawRing(new Vector2(cx, cy), innerRadius, outerRadius,
                startAngleDeg, endAngleDeg, segments, strokeColor);
        }
    }

    /// <summary>
    /// Translate the user's dash/gap pixel lengths into arc-angle sweeps along the curve and
    /// emit one <c>DrawRing</c> per dash. raylib has no built-in path-effect dash (Skia has
    /// <c>SKPathEffect.CreateDash</c>); the loop walks the SweepAngle in dash + gap angular
    /// steps. Same pattern <see cref="LineCircle"/> uses for its dashed ring.
    /// </summary>
    private void DrawDashed(Vector2 center, float innerRadius, float outerRadius, Color strokeColor)
    {
        // Use the band's centerline circumference so dash pixel-lengths feel consistent at
        // different thicknesses (matches the visual the Skia dashed-stroke path produces, which
        // centers dashes on the stroked path).
        float curveRadius = (innerRadius + outerRadius) * 0.5f;
        if (curveRadius <= 0f)
        {
            // Wedge configuration (innerRadius = 0) — the curve has zero "length" at the
            // center, so fall back to the outer radius. A dashed wedge is unusual anyway; this
            // keeps the math defined rather than dividing by zero.
            curveRadius = outerRadius;
        }
        float circumference = 2f * System.MathF.PI * curveRadius;
        float dashAngleDeg = (StrokeDashLength / circumference) * 360f;
        float gapAngleDeg = (StrokeGapLength / circumference) * 360f;
        float patternAngleDeg = dashAngleDeg + gapAngleDeg;
        if (patternAngleDeg <= 0f)
        {
            return;
        }

        // Per-dash segment count proportional to the dash arc — ~4° per segment is smooth at
        // typical radii. Floor at 1 so tiny dashes still render at least one triangle pair.
        int segmentsPerDash = System.Math.Max(1, (int)(dashAngleDeg / 4f));

        // Walk SweepAngle in the renderable's native CCW direction, mapping each dash's
        // [startGum, endGum] into raylib's negated angle space. Caps the loop at the absolute
        // sweep so dashes do not overflow past the arc end.
        float sweepAbs = System.MathF.Abs(SweepAngle);
        float sweepSign = SweepAngle >= 0f ? 1f : -1f;
        float currentAngle = 0f;
        while (currentAngle < sweepAbs)
        {
            float dashEnd = System.MathF.Min(currentAngle + dashAngleDeg, sweepAbs);
            float dashStartGum = StartAngle + sweepSign * currentAngle;
            float dashEndGum = StartAngle + sweepSign * dashEnd;
            float dashStartRl = -dashStartGum;
            float dashEndRl = -dashEndGum;
            DrawRing(center, innerRadius, outerRadius, dashStartRl, dashEndRl,
                segmentsPerDash, strokeColor);
            currentAngle += patternAngleDeg;
        }
    }

    /// <summary>
    /// Concentric band approximation of a blurred dropshadow silhouette. Mirrors the
    /// <see cref="LineCircle"/> approach — same source-over inversion to land each band's
    /// per-pixel alpha on a linear (1 - t) target profile, same #2851 body-alpha multiply, same
    /// inheritance of the #2865 stacking overshoot. The shape difference is that the arc's
    /// silhouette is a stroke band rather than a filled disk, so each "band" here is itself a
    /// <c>DrawRing</c> whose width grows outward from the nominal thickness.
    /// </summary>
    private void DrawDropshadow(float cx, float cy, float innerRadius, float outerRadius,
        float startAngleDeg, float endAngleDeg, int segments, byte bodyAlpha)
    {
        float shadowCx = cx + DropshadowOffsetX;
        float shadowCy = cy + DropshadowOffsetY;
        float blur = System.MathF.Max(DropshadowBlurX, DropshadowBlurY);

        // Issue #2851: scale shadow alpha by the body alpha so fading the arc also fades the
        // shadow. The renderable reads stroke alpha as "the body" — arcs have no separate fill.
        Color effectiveDropshadowColor = new Color(
            DropshadowColor.R,
            DropshadowColor.G,
            DropshadowColor.B,
            (byte)(DropshadowColor.A * bodyAlpha / 255));

        Vector2 shadowCenter = new Vector2(shadowCx, shadowCy);

        if (blur <= 0f)
        {
            // Hard offset silhouette — single DrawRing matching the nominal band.
            DrawRing(shadowCenter, innerRadius, outerRadius, startAngleDeg, endAngleDeg,
                segments, effectiveDropshadowColor);
            return;
        }

        // Match LineCircle's band layout: bands span [R - blur, R + blur] (total width 2*blur)
        // outward from the nominal silhouette edge. For an arc band, "outward" is both
        // directions — the outer edge expands outward, the inner edge expands inward. Each
        // ring's width grows by 2 * (bandSpan - j*bandThickness) so the outermost (j=0) ring is
        // the widest with the lowest alpha and the innermost (j=N-1) ring is barely wider than
        // the nominal band with the highest alpha. The final solid-core DrawRing matches the
        // nominal band exactly to close the alpha gap.
        const int blurRings = 32;
        float bandSpan = blur;
        float totalExtent = 2f * bandSpan;
        float bandThickness = totalExtent / blurRings;
        float f = effectiveDropshadowColor.A / 255f;
        float prevP = 1f;
        for (int j = blurRings - 1; j >= 0; j--)
        {
            float tOuter = (j + 1f) / blurRings;
            float profileAlpha = System.MathF.Max(0f, 1f - tOuter);
            float targetAlpha = f * profileAlpha;
            float currP = 1f - targetAlpha;
            float beta = prevP > 0f ? 1f - currP / prevP : 0f;
            prevP = currP;
            byte bandAlpha = (byte)(255f * beta + 0.5f);
            if (bandAlpha == 0)
            {
                continue;
            }
            Color bandColor = new Color(effectiveDropshadowColor.R, effectiveDropshadowColor.G,
                effectiveDropshadowColor.B, bandAlpha);
            float ringOuter = outerRadius + bandSpan - (j * bandThickness);
            float ringInner = System.MathF.Max(0f, innerRadius - bandSpan + (j * bandThickness));
            if (ringOuter > ringInner)
            {
                DrawRing(shadowCenter, ringInner, ringOuter, startAngleDeg, endAngleDeg,
                    segments, bandColor);
            }
        }

        // Solid core matching the nominal band closes the remaining alpha gap between the
        // band stack's accumulated alpha (prevP) and the target final alpha (1 - f). Same
        // source-over inversion LineCircle uses for its inner core.
        if (prevP > 1f - f)
        {
            float coreBeta = 1f - (1f - f) / prevP;
            byte coreAlpha = (byte)(255f * coreBeta + 0.5f);
            if (coreAlpha > 0)
            {
                Color coreColor = new Color(effectiveDropshadowColor.R,
                    effectiveDropshadowColor.G, effectiveDropshadowColor.B, coreAlpha);
                DrawRing(shadowCenter, innerRadius, outerRadius, startAngleDeg, endAngleDeg,
                    segments, coreColor);
            }
        }
    }

    /// <summary>
    /// Segment count proportional to the curve's arc length so the polyline approximation
    /// stays smooth at large radii and wide sweeps without paying the cost of dense
    /// tessellation on tiny / narrow arcs. ~3° per segment is the visual floor; framebuffer
    /// MSAA covers the rest. Mirrors the segment heuristic <see cref="LineCircle"/> uses.
    /// </summary>
    private static int ComputeSegments(float radius, float sweepDegrees)
    {
        if (sweepDegrees <= 0f)
        {
            return 1;
        }
        int fromRadius = (int)(radius * 2f * (sweepDegrees / 360f));
        int fromAngle = (int)(sweepDegrees / 3f);
        return System.Math.Max(8, System.Math.Max(fromRadius, fromAngle));
    }
}
