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
/// Issue #3454 — the stroked band can also be painted with a linear or radial gradient
/// (<see cref="UseGradient"/>), matching the Skia/Apos <c>Arc</c>. The band is tessellated into
/// an annular-sector triangle strip whose vertices sample the gradient by world position — the
/// arc-restricted version of the concentric-band approach <c>LineCircle</c> uses for a full disk.
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
    /// When <c>true</c>, the band's two radial endpoints are capped with semicircular bulges so
    /// the arc reads as a rounded-end stroke, matching Skia's <c>SKStrokeCap.Round</c> on
    /// <c>Arc</c>. raylib has no per-shape stroke cap, so the effect is synthesized in
    /// <see cref="Render(ISystemManagers)"/> by drawing one <c>DrawCircleSector</c> half-disk per
    /// endpoint (issue #2895). Critically, only the *outer* half-disk is drawn — the half whose
    /// flat diameter aligns with the band's flat radial end and whose bulge points opposite the
    /// band's tangent. Drawing a full <c>DrawCircle</c> would overlap the band's flat end and
    /// double-composite under non-opaque alpha, leaving a visible darker crescent at each cap.
    /// MSAA in this project is post-resolve so the seam between the band's radial edge and the
    /// cap's diameter resolves cleanly without overdraw stitching.
    /// <para>
    /// Skipped automatically when <c>|SweepAngle| &gt;= 360</c> (a full ring has no visible
    /// endpoints) or when the geometry collapses (<c>Thickness &lt;= 0</c>). In the dashed
    /// configuration each dash receives caps at both of its own ends, mirroring Skia's
    /// per-dash rounding via <c>SKPathEffect.CreateDash</c> + <c>SKStrokeCap.Round</c>.
    /// </para>
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
    /// When <c>true</c>, the stroked band is painted with a gradient from <see cref="Color1"/>
    /// to <see cref="Color2"/> instead of a solid stroke color. Both <see cref="GradientType.Linear"/>
    /// and <see cref="GradientType.Radial"/> are supported; rendering goes through an <c>rlgl</c>
    /// annular-sector triangle strip with per-vertex colors computed from the gradient axis
    /// (<see cref="GradientX1"/>/<see cref="GradientY1"/> → <see cref="GradientX2"/>/<see cref="GradientY2"/>)
    /// and, for radial, <see cref="GradientInnerRadius"/>/<see cref="GradientOuterRadius"/>.
    /// Mirrors <see cref="LineCircle.UseGradient"/>. Ignored on the dashed path (dashes stay solid).
    /// </summary>
    public bool UseGradient { get; set; }

    /// <inheritdoc cref="LineCircle.GradientType"/>
    public GradientType GradientType { get; set; }

    /// <summary>
    /// Gradient start color (linear: at the axis start; radial: at the inner radius). Per the
    /// #3009 model the shared <c>ArcRuntime</c> mirrors the arc's primary <see cref="Color"/> into
    /// this each frame, so the gradient start follows the body color.
    /// </summary>
    public Color Color1 { get; set; } = Color.White;

    /// <summary>Gradient end color (linear: at the axis end; radial: at the outer radius).</summary>
    public Color Color2 { get; set; } = Color.White;

    /// <inheritdoc cref="LineCircle.GradientX1"/>
    public float GradientX1 { get; set; }

    /// <inheritdoc cref="LineCircle.GradientY1"/>
    public float GradientY1 { get; set; }

    /// <inheritdoc cref="LineCircle.GradientX2"/>
    public float GradientX2 { get; set; }

    /// <inheritdoc cref="LineCircle.GradientY2"/>
    public float GradientY2 { get; set; }

    /// <inheritdoc cref="LineCircle.GradientInnerRadius"/>
    public float GradientInnerRadius { get; set; }

    /// <inheritdoc cref="LineCircle.GradientOuterRadius"/>
    public float GradientOuterRadius { get; set; }

    /// <summary>
    /// Issue #3454 — true when the gradient pass should paint. The arc is stroke-only (no fill
    /// slot), so unlike <see cref="LineCircle.ShouldPaintFillGradient"/> this gates only on
    /// <see cref="UseGradient"/> plus at least one visible gradient stop; there is no IsFilled /
    /// FillColor slot to enable.
    /// </summary>
    public bool ShouldPaintGradient =>
        UseGradient && (Color1.A > 0 || Color2.A > 0);

    /// <inheritdoc cref="LineCircle.GetRotatedGradientEndpoints(float)"/>
    /// <remarks>Pivots around the arc's bbox center (Width/2, Height/2) rather than
    /// (Radius, Radius) so non-square bounds rotate correctly.</remarks>
    public (float x1, float y1, float x2, float y2) GetRotatedGradientEndpoints(float rotationDegrees)
    {
        if (rotationDegrees == 0f)
        {
            return (GradientX1, GradientY1, GradientX2, GradientY2);
        }
        float rotRad = rotationDegrees * System.MathF.PI / 180f;
        float cos = System.MathF.Cos(rotRad);
        float sin = System.MathF.Sin(rotRad);
        float pivotX = Width * 0.5f;
        float pivotY = Height * 0.5f;
        float dx1 = GradientX1 - pivotX;
        float dy1 = GradientY1 - pivotY;
        float dx2 = GradientX2 - pivotX;
        float dy2 = GradientY2 - pivotY;
        return (
            pivotX + dx1 * cos + dy1 * sin,
            pivotY - dx1 * sin + dy1 * cos,
            pivotX + dx2 * cos + dy2 * sin,
            pivotY - dx2 * sin + dy2 * cos);
    }

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
        else if (ShouldPaintGradient)
        {
            // Gradient axis lives in bbox-local coords (origin = bbox top-left); rotate it around
            // the bbox center so it tracks the arc under self-rotation, then paint the annular
            // sector as a per-vertex-colored triangle strip. bboxLeft/Top = (cx - halfW, cy - halfH)
            // = the arc's absolute top-left.
            float bboxLeft = cx - halfW;
            float bboxTop = cy - halfH;
            (float gx1, float gy1, float gx2, float gy2) =
                GetRotatedGradientEndpoints(this.GetAbsoluteRotation());
            DrawGradientBand(new Vector2(cx, cy), innerRadius, outerRadius,
                startAngleDeg, endAngleDeg, segments, bboxLeft, bboxTop, gx1, gy1, gx2, gy2);
            if (IsEndRounded && System.MathF.Abs(SweepAngle) < 360f && thickness > 0f)
            {
                // Sample the gradient at each cap's center so a rounded end blends with the band
                // it caps instead of jumping to a single solid stroke color.
                const float deg2rad = System.MathF.PI / 180f;
                float midRadius = (innerRadius + outerRadius) * 0.5f;
                float scx = cx + midRadius * System.MathF.Cos(startAngleDeg * deg2rad);
                float scy = cy + midRadius * System.MathF.Sin(startAngleDeg * deg2rad);
                float ecx = cx + midRadius * System.MathF.Cos(endAngleDeg * deg2rad);
                float ecy = cy + midRadius * System.MathF.Sin(endAngleDeg * deg2rad);
                Color startCap = ComputeGradientColor(scx, scy, bboxLeft, bboxTop, gx1, gy1, gx2, gy2, outerRadius);
                Color endCap = ComputeGradientColor(ecx, ecy, bboxLeft, bboxTop, gx1, gy1, gx2, gy2, outerRadius);
                DrawCaps(new Vector2(cx, cy), innerRadius, outerRadius,
                    startAngleDeg, endAngleDeg, startCap, endCap);
            }
        }
        else
        {
            DrawRing(new Vector2(cx, cy), innerRadius, outerRadius,
                startAngleDeg, endAngleDeg, segments, strokeColor);
            if (IsEndRounded && System.MathF.Abs(SweepAngle) < 360f && thickness > 0f)
            {
                DrawCaps(new Vector2(cx, cy), innerRadius, outerRadius,
                    startAngleDeg, endAngleDeg, strokeColor, strokeColor);
            }
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
            // Per-dash rounded caps — Skia parity: SKStrokeCap.Round + SKPathEffect.CreateDash
            // rounds both ends of every dash, not just the overall stroke endpoints. Same guard
            // as the solid path: skip when thickness collapses or the dash isn't actually a dash.
            if (IsEndRounded && outerRadius > innerRadius && dashEnd > currentAngle)
            {
                DrawCaps(center, innerRadius, outerRadius, dashStartRl, dashEndRl, strokeColor, strokeColor);
            }
            currentAngle += patternAngleDeg;
        }
    }

    /// <summary>
    /// Issue #2895 — synthesizes rounded end caps for the stroked band by drawing one
    /// <c>DrawCircleSector</c> half-disk per endpoint. The half-disk's flat diameter lies along
    /// the band's radial endpoint and its bulge points opposite the band's tangent, so it lands
    /// flush against the band's flat end without overlapping it. This is what keeps the cap
    /// alpha-correct at non-opaque stroke colors — a full <c>DrawCircle</c> would composite
    /// twice over the band's last few pixels and produce a darker crescent under the cap.
    /// </summary>
    /// <remarks>
    /// Angles are in raylib's negated-CW space (the same space <see cref="Render(ISystemManagers)"/>
    /// already converts to). Cap bulge direction derives from <c>sign(SweepAngle)</c>: at the
    /// start endpoint the band's interior is at <c>startAngleRl + sweepSign*-90°</c> (raylib
    /// y-down, so "up" is -90 etc.), so the cap bulges at <c>startAngleRl + sweepSign*90°</c>;
    /// symmetric flip at the end endpoint. The 180° sector spans <c>[B-90°, B+90°]</c> in raylib
    /// CW terms, which is identical to the radial axis at the endpoint.
    /// </remarks>
    private void DrawCaps(Vector2 center, float innerRadius, float outerRadius,
        float startAngleRl, float endAngleRl, Color startColor, Color endColor)
    {
        float thickness = outerRadius - innerRadius;
        if (thickness <= 0f)
        {
            return;
        }
        float capRadius = thickness * 0.5f;
        float midRadius = (innerRadius + outerRadius) * 0.5f;
        // sweepSign computed in Gum space (positive = CCW); the negated-angle conversion in
        // Render flips it to raylib-space, but the bulge offset is symmetric so we can stay in
        // Gum-sign terms and trust the +/- 90° to come out right against raylib-space angles.
        float sweepSign = SweepAngle >= 0f ? 1f : -1f;

        // ~4° per cap segment matches the smoothness floor ComputeSegments uses; bumped on fat
        // caps so the half-disk silhouette doesn't read as a polygon at large thickness.
        int capSegments = System.Math.Max(8, (int)(capRadius * 0.8f));

        const float deg2rad = System.MathF.PI / 180f;

        // Start endpoint cap. Under a gradient, startColor/endColor are the gradient samples at
        // each endpoint; on the solid path they are the same stroke color.
        float startCx = center.X + midRadius * System.MathF.Cos(startAngleRl * deg2rad);
        float startCy = center.Y + midRadius * System.MathF.Sin(startAngleRl * deg2rad);
        float startBulge = startAngleRl + sweepSign * 90f;
        DrawCircleSector(new Vector2(startCx, startCy), capRadius,
            startBulge - 90f, startBulge + 90f, capSegments, startColor);

        // End endpoint cap — bulge flips because the band tangent at the end points the other
        // way down the band relative to the start.
        float endCx = center.X + midRadius * System.MathF.Cos(endAngleRl * deg2rad);
        float endCy = center.Y + midRadius * System.MathF.Sin(endAngleRl * deg2rad);
        float endBulge = endAngleRl - sweepSign * 90f;
        DrawCircleSector(new Vector2(endCx, endCy), capRadius,
            endBulge - 90f, endBulge + 90f, capSegments, endColor);
    }

    /// <summary>
    /// Number of concentric annular sub-bands the gradient band is split into between
    /// <c>innerRadius</c> and <c>outerRadius</c>. Radial subdivision keeps a radial gradient
    /// smooth across a thick band (or a wedge, where innerRadius collapses to 0); a linear
    /// gradient is unaffected by the count since every vertex samples by world position. Mirrors
    /// <c>LineCircle.RadialLayers</c>.
    /// </summary>
    private const int GradientRadialLayers = 8;

    /// <summary>
    /// Issue #3454 — paints the arc's annular sector (<c>[innerRadius, outerRadius]</c> ×
    /// <c>[startAngleRl, endAngleRl]</c>) as an <c>rlgl</c> triangle strip whose vertices sample
    /// the gradient at their world position. The angular loop always steps from the smaller to the
    /// larger raylib-space angle so the triangle winding stays front-facing under raylib's default
    /// backface culling regardless of the sweep's sign — the sector covers the same region either
    /// way and the gradient color is position-based, so iteration direction is free. This is the
    /// arc-restricted analog of <see cref="LineCircle.DrawGradientFan"/> (a full disk); the arc
    /// tessellates only the swept angular range and only the stroked band, not the whole disc.
    /// </summary>
    private void DrawGradientBand(Vector2 center, float innerRadius, float outerRadius,
        float startAngleRl, float endAngleRl, int segments,
        float bboxLeft, float bboxTop, float gx1, float gy1, float gx2, float gy2)
    {
        const float deg2rad = System.MathF.PI / 180f;
        float aMin = System.MathF.Min(startAngleRl, endAngleRl);
        float aMax = System.MathF.Max(startAngleRl, endAngleRl);
        int safeSegments = System.Math.Max(1, segments);

        Rlgl.Begin((int)DrawMode.Triangles);
        for (int layer = 0; layer < GradientRadialLayers; layer++)
        {
            float rOuter = innerRadius + (outerRadius - innerRadius) * (GradientRadialLayers - layer) / GradientRadialLayers;
            float rInner = innerRadius + (outerRadius - innerRadius) * (GradientRadialLayers - layer - 1) / GradientRadialLayers;
            for (int i = 0; i < safeSegments; i++)
            {
                float a0 = (aMin + (aMax - aMin) * (i / (float)safeSegments)) * deg2rad;
                float a1 = (aMin + (aMax - aMin) * ((i + 1) / (float)safeSegments)) * deg2rad;
                float c0 = System.MathF.Cos(a0), s0 = System.MathF.Sin(a0);
                float c1 = System.MathF.Cos(a1), s1 = System.MathF.Sin(a1);

                float ox0 = center.X + c0 * rOuter, oy0 = center.Y + s0 * rOuter;
                float ox1 = center.X + c1 * rOuter, oy1 = center.Y + s1 * rOuter;
                float ix0 = center.X + c0 * rInner, iy0 = center.Y + s0 * rInner;
                float ix1 = center.X + c1 * rInner, iy1 = center.Y + s1 * rInner;

                // Winding matches LineCircle.DrawGradientFan (front-facing under default culling).
                EmitGradientVertex(ix0, iy0, bboxLeft, bboxTop, gx1, gy1, gx2, gy2, outerRadius);
                EmitGradientVertex(ox1, oy1, bboxLeft, bboxTop, gx1, gy1, gx2, gy2, outerRadius);
                EmitGradientVertex(ox0, oy0, bboxLeft, bboxTop, gx1, gy1, gx2, gy2, outerRadius);

                EmitGradientVertex(ix0, iy0, bboxLeft, bboxTop, gx1, gy1, gx2, gy2, outerRadius);
                EmitGradientVertex(ix1, iy1, bboxLeft, bboxTop, gx1, gy1, gx2, gy2, outerRadius);
                EmitGradientVertex(ox1, oy1, bboxLeft, bboxTop, gx1, gy1, gx2, gy2, outerRadius);
            }
        }
        Rlgl.End();
    }

    private void EmitGradientVertex(float worldX, float worldY, float bboxLeft, float bboxTop,
        float gx1, float gy1, float gx2, float gy2, float outerRadius)
    {
        Color color = ComputeGradientColor(worldX, worldY, bboxLeft, bboxTop, gx1, gy1, gx2, gy2, outerRadius);
        Rlgl.Color4ub(color.R, color.G, color.B, color.A);
        Rlgl.Vertex2f(worldX, worldY);
    }

    /// <summary>
    /// Interpolates <see cref="Color1"/> → <see cref="Color2"/> for a world-space point. Linear
    /// projects the point onto the (rotation-adjusted) axis; radial normalizes the distance from
    /// the center against the <c>[InnerRadius, OuterRadius]</c> band. Matches
    /// <see cref="LineCircle.EmitGradientVertex"/>'s math, but a default (0) radial outer radius
    /// falls back to the arc's <paramref name="outerRadius"/> rather than a disc <c>Radius</c>.
    /// </summary>
    private Color ComputeGradientColor(float worldX, float worldY, float bboxLeft, float bboxTop,
        float gx1, float gy1, float gx2, float gy2, float outerRadius)
    {
        float localX = worldX - bboxLeft;
        float localY = worldY - bboxTop;

        float t;
        if (GradientType == GradientType.Linear)
        {
            float dx = gx2 - gx1;
            float dy = gy2 - gy1;
            float lenSq = dx * dx + dy * dy;
            if (lenSq <= 0f)
            {
                return Color1;
            }
            t = ((localX - gx1) * dx + (localY - gy1) * dy) / lenSq;
        }
        else
        {
            float dx = localX - gx1;
            float dy = localY - gy1;
            float dist = System.MathF.Sqrt(dx * dx + dy * dy);
            float outer = GradientOuterRadius > 0f ? GradientOuterRadius : outerRadius;
            float span = outer - GradientInnerRadius;
            if (span <= 0f)
            {
                return Color2;
            }
            t = (dist - GradientInnerRadius) / span;
        }
        if (t < 0f) t = 0f;
        else if (t > 1f) t = 1f;

        byte r = (byte)(Color1.R + (Color2.R - Color1.R) * t);
        byte g = (byte)(Color1.G + (Color2.G - Color1.G) * t);
        byte b = (byte)(Color1.B + (Color2.B - Color1.B) * t);
        byte a = (byte)(Color1.A + (Color2.A - Color1.A) * t);
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Issue #2865: render-to-texture + separable Gaussian replaces the band-stack approximation
    /// (which inherited the same source-over inversion overshoot LineRectangle and LineCircle
    /// did). Bounds are the conservative full-circle bbox — a tightly-fit arc bbox would save
    /// some RT pixels on a thin sweep but needs more math; the conservative version is always
    /// correct. The silhouette callback paints a white DrawRing matching the nominal stroke band.
    /// Cap-in-shadow is not painted — matches the prior band approach (which didn't include
    /// rounded caps in the shadow either), tracked as a follow-up for full Skia parity.
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

        if (blur <= 0f)
        {
            // Hard offset silhouette — single DrawRing matching the nominal band.
            DrawRing(new Vector2(shadowCx, shadowCy), innerRadius, outerRadius,
                startAngleDeg, endAngleDeg, segments, effectiveDropshadowColor);
            return;
        }

        float diameter = outerRadius * 2f;
        Color silhouetteColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);
        global::RenderingLibrary.Graphics.Renderer.Self.ShadowBlur.Draw(
            this,
            shadowCx - outerRadius,
            shadowCy - outerRadius,
            diameter,
            diameter,
            blur,
            effectiveDropshadowColor,
            global::RenderingLibrary.Graphics.Renderer.Self.ActiveCamera2D,
            global::RenderingLibrary.Graphics.Renderer.Self.ActiveRenderTexture,
            (px, py) =>
            {
                DrawRing(
                    new Vector2(px + outerRadius, py + outerRadius),
                    innerRadius, outerRadius,
                    startAngleDeg, endAngleDeg,
                    segments, silhouetteColor);
            });
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
