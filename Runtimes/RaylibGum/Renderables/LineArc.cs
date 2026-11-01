using Gum.Converters;
using Gum.DataTypes;
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
public class LineArc : GradientShapeRenderable
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

    /// <summary>
    /// Issue #3454 — true when the gradient pass should paint. The arc is stroke-only (no fill
    /// slot), so unlike <see cref="LineCircle.ShouldPaintFillGradient"/> this gates only on
    /// <see cref="UseGradient"/> plus at least one visible gradient stop; there is no IsFilled /
    /// FillColor slot to enable.
    /// </summary>
    public bool ShouldPaintGradient =>
        UseGradient && (Color1.A > 0 || Color2.A > 0);

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

    /// <summary>Horizontal blur radius of the dropshadow, in pixels: how far the soft falloff visibly extends.</summary>
    /// <remarks>raylib note: treated as isotropic with <see cref="DropshadowBlurY"/> —
    /// rendering collapses anisotropic blur to <c>max(BlurX, BlurY)</c>.</remarks>
    public float DropshadowBlurX { get; set; }

    /// <summary>Vertical blur radius of the dropshadow, in pixels: how far the soft falloff visibly extends.</summary>
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

        // The arc is an ellipse inscribed in the box (like Skia's path.AddArc(boundingRect, ...)), turned
        // about the box's top-left corner (Gum's rotation origin). A square box is a circle, which keeps
        // the DrawRing path; any other box is a constant-thickness band along the ellipse (BandGeometry).
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

        float rotationDegrees = this.GetAbsoluteRotation();
        float rotRad = rotationDegrees * (System.MathF.PI / 180f);
        // The box center after turning about the top-left corner (counterclockwise on screen, y down).
        float cx = this.GetAbsoluteLeft() + halfW * System.MathF.Cos(rotRad) + halfH * System.MathF.Sin(rotRad);
        float cy = this.GetAbsoluteTop() - halfW * System.MathF.Sin(rotRad) + halfH * System.MathF.Cos(rotRad);

        // raylib's DrawRing uses degrees with 0 at 3 o'clock and increasing angles sweeping
        // clockwise on screen (y-down). Gum's convention is 0 at 3 o'clock with increasing
        // angles sweeping counter-clockwise (the renderable inherits this from the Skia/Apos
        // Arc primitives). Negate both StartAngle and SweepAngle to flip CCW→CW; raylib's
        // DrawRing handles startAngle > endAngle by stepping with a negative delta, so the arc
        // direction stays correct visually.
        float startAngleDeg = -StartAngle;
        float endAngleDeg = -StartAngle - SweepAngle;
        bool isCircle = halfW == halfH;
        int segments = ComputeSegments(System.MathF.Max(halfW, halfH), System.MathF.Abs(SweepAngle));
        bool hasCaps = IsEndRounded && System.MathF.Abs(SweepAngle) < 360f && thickness > 0f;

        Color strokeColor = StrokeColor ?? Color;
        BandGeometry band = new BandGeometry(cx, cy, rotationDegrees, halfW, halfH, thickness);

        if (HasDropshadow)
        {
            DrawDropshadow(band, isCircle, innerRadius, outerRadius, startAngleDeg, endAngleDeg,
                segments, hasCaps, strokeColor.A);
        }

        bool dashed = StrokeDashLength > 0f && StrokeGapLength > 0f && SweepAngle != 0f;
        if (dashed)
        {
            // Dashes stay on the circle inscribed in the box (min(W, H)); only the center follows rotation.
            DrawDashed(new Vector2(cx, cy), rotationDegrees, innerRadius, outerRadius, strokeColor);
        }
        else if (ShouldPaintGradient)
        {
            // Each vertex samples the gradient at its box-local position against the unrotated
            // endpoints, so the gradient follows the shape.
            GradientSampler sampler = new GradientSampler(this);
            DrawBand(band, startAngleDeg, endAngleDeg, segments, GradientRadialLayers, sampler, strokeColor);
            if (hasCaps)
            {
                // Sample the gradient at each cap's center so a rounded end blends with the band
                // it caps instead of jumping to a single solid stroke color.
                DrawCaps(band, startAngleDeg, endAngleDeg, sampler, strokeColor);
            }
        }
        else
        {
            DrawSolid(band, isCircle, innerRadius, outerRadius, startAngleDeg, endAngleDeg,
                segments, hasCaps, strokeColor);
        }
    }

    /// <summary>
    /// Translate the user's dash/gap pixel lengths into arc-angle sweeps along the curve and
    /// emit one <c>DrawRing</c> per dash. raylib has no built-in path-effect dash (Skia has
    /// <c>SKPathEffect.CreateDash</c>); the loop walks the SweepAngle in dash + gap angular
    /// steps. Same pattern <see cref="LineCircle"/> uses for its dashed ring.
    /// </summary>
    private void DrawDashed(Vector2 center, float rotationDegrees, float innerRadius, float outerRadius, Color strokeColor)
    {
        // Use the band's centerline circumference so dash pixel-lengths feel consistent at
        // different thicknesses (matches the visual the Skia dashed-stroke path produces, which
        // centers dashes on the stroked path).
        BandGeometry circleBand = new BandGeometry(center.X, center.Y, rotationDegrees, outerRadius, outerRadius, outerRadius - innerRadius);
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
            DrawRing(center, innerRadius, outerRadius, dashStartRl - rotationDegrees, dashEndRl - rotationDegrees,
                segmentsPerDash, strokeColor);
            // Per-dash rounded caps — Skia parity: SKStrokeCap.Round + SKPathEffect.CreateDash
            // rounds both ends of every dash, not just the overall stroke endpoints. Same guard
            // as the solid path: skip when thickness collapses or the dash isn't actually a dash.
            if (IsEndRounded && outerRadius > innerRadius && dashEnd > currentAngle)
            {
                DrawCaps(circleBand, dashStartRl, dashEndRl, null, strokeColor);
            }
            currentAngle += patternAngleDeg;
        }
    }

    /// <summary>
    /// Paints the band (and its rounded caps) in one solid color: a <c>DrawRing</c> for a circle, a
    /// <see cref="DrawBand"/> strip for any other box. Shared by the body and by the shadow silhouette.
    /// </summary>
    private void DrawSolid(in BandGeometry band, bool isCircle, float innerRadius, float outerRadius,
        float startAngleRl, float endAngleRl, int segments, bool hasCaps, Color color)
    {
        if (isCircle)
        {
            // DrawRing takes screen angles, so the turn is added to the box-local ones.
            DrawRing(new Vector2(band.Cx, band.Cy), innerRadius, outerRadius,
                startAngleRl - band.RotationDegrees, endAngleRl - band.RotationDegrees, segments, color);
        }
        else
        {
            DrawBand(band, startAngleRl, endAngleRl, segments, 1, null, color);
        }
        if (hasCaps)
        {
            DrawCaps(band, startAngleRl, endAngleRl, null, color);
        }
    }

    /// <summary>
    /// Issue #2895 — synthesizes rounded end caps for the stroked band by drawing one
    /// <c>DrawCircleSector</c> half-disk per endpoint, centered on the band's centerline end and
    /// bulging along the centerline's tangent (what Skia's <c>SKStrokeCap.Round</c> does). The
    /// half-disk's flat diameter lies along the band's endpoint so it lands flush against the flat end
    /// without overlapping it, which keeps the cap alpha-correct at non-opaque stroke colors — a full
    /// <c>DrawCircle</c> would composite twice over the band's last few pixels.
    /// </summary>
    /// <remarks>
    /// Angles are in raylib's negated-CW space. With a sampler, each cap takes the gradient color at its
    /// center; otherwise <paramref name="solid"/>.
    /// </remarks>
    private void DrawCaps(in BandGeometry band, float startAngleRl, float endAngleRl,
        GradientSampler? sampler, Color solid)
    {
        if (band.Thickness <= 0f)
        {
            return;
        }
        // The band runs from the start angle to the end angle, so the start cap bulges against that
        // direction and the end cap along it.
        float travel = endAngleRl >= startAngleRl ? 1f : -1f;
        DrawCap(band, startAngleRl, -travel, sampler, solid);
        DrawCap(band, endAngleRl, travel, sampler, solid);
    }

    private static void DrawCap(in BandGeometry band, float angleRl, float bulgeSign,
        GradientSampler? sampler, Color solid)
    {
        const float deg2rad = System.MathF.PI / 180f;
        float angleRad = angleRl * deg2rad;
        band.Point(angleRad, 0.5f, out float sx, out float sy, out float lx, out float ly);
        band.Tangent(angleRad, out float tx, out float ty);
        float bulgeDeg = System.MathF.Atan2(ty * bulgeSign, tx * bulgeSign) / deg2rad;
        float capRadius = band.Thickness * 0.5f;
        // ~4° per cap segment matches the smoothness floor ComputeSegments uses; bumped on fat
        // caps so the half-disk silhouette doesn't read as a polygon at large thickness.
        int capSegments = System.Math.Max(8, (int)(capRadius * 0.8f));
        Color color = sampler.HasValue
            ? sampler.Value.Sample(lx + band.HalfW, ly + band.HalfH)
            : solid;
        DrawCircleSector(new Vector2(sx, sy), capRadius, bulgeDeg - 90f, bulgeDeg + 90f, capSegments, color);
    }

    /// <summary>
    /// Number of concentric sub-bands the gradient band is split into between its outer and inner
    /// edge. Radial subdivision keeps a radial gradient smooth across a thick band (or a wedge, where
    /// the inner edge collapses to the center); a linear gradient is unaffected by the count since
    /// every vertex samples by position. Mirrors <c>LineCircle.RadialLayers</c>.
    /// </summary>
    private const int GradientRadialLayers = 8;

    /// <summary>
    /// Paints the band as an <c>rlgl</c> triangle strip of <paramref name="layers"/> sub-bands. With a
    /// sampler, each vertex takes the gradient at its box-local position; without one the band is
    /// <paramref name="solid"/>. Each triangle is wound front-facing under raylib's default backface
    /// culling whatever the sweep's sign, so a band whose inner edge folds over itself (a thick stroke
    /// on a narrow ellipse) still fills. This is the arc-restricted analog of
    /// <see cref="LineCircle.DrawGradientFan"/>.
    /// </summary>
    private void DrawBand(in BandGeometry band, float startAngleRl, float endAngleRl, int segments,
        int layers, GradientSampler? sampler, Color solid)
    {
        const float deg2rad = System.MathF.PI / 180f;
        float aMin = System.MathF.Min(startAngleRl, endAngleRl);
        float aMax = System.MathF.Max(startAngleRl, endAngleRl);
        int safeSegments = System.Math.Max(1, segments);

        Rlgl.Begin((int)DrawMode.Triangles);
        for (int layer = 0; layer < layers; layer++)
        {
            float fOuter = layer / (float)layers;
            float fInner = (layer + 1) / (float)layers;
            for (int i = 0; i < safeSegments; i++)
            {
                float a0 = (aMin + (aMax - aMin) * (i / (float)safeSegments)) * deg2rad;
                float a1 = (aMin + (aMax - aMin) * ((i + 1) / (float)safeSegments)) * deg2rad;

                BandVertex inner0 = band.Vertex(a0, fInner, sampler, solid);
                BandVertex outer1 = band.Vertex(a1, fOuter, sampler, solid);
                BandVertex outer0 = band.Vertex(a0, fOuter, sampler, solid);
                BandVertex inner1 = band.Vertex(a1, fInner, sampler, solid);

                EmitTriangle(inner0, outer1, outer0);
                EmitTriangle(inner0, inner1, outer1);
            }
        }
        Rlgl.End();
    }

    // Front-facing under raylib's default culling is a negative cross product in y-down screen space.
    private static void EmitTriangle(BandVertex v0, BandVertex v1, BandVertex v2)
    {
        float cross = (v1.X - v0.X) * (v2.Y - v0.Y) - (v1.Y - v0.Y) * (v2.X - v0.X);
        if (cross > 0f)
        {
            (v1, v2) = (v2, v1);
        }
        Rlgl.Color4ub(v0.Color.R, v0.Color.G, v0.Color.B, v0.Color.A);
        Rlgl.Vertex2f(v0.X, v0.Y);
        Rlgl.Color4ub(v1.Color.R, v1.Color.G, v1.Color.B, v1.Color.A);
        Rlgl.Vertex2f(v1.X, v1.Y);
        Rlgl.Color4ub(v2.Color.R, v2.Color.G, v2.Color.B, v2.Color.A);
        Rlgl.Vertex2f(v2.X, v2.Y);
    }

    private readonly struct BandVertex
    {
        public BandVertex(float x, float y, Color color)
        {
            X = x; Y = y; Color = color;
        }
        public readonly float X;
        public readonly float Y;
        public readonly Color Color;
    }

    /// <summary>The gradient axis captured for one frame so each vertex samples the same endpoints.</summary>
    private readonly struct GradientSampler
    {
        private readonly LineArc _owner;
        private readonly float _gx1, _gy1, _gx2, _gy2;

        public GradientSampler(LineArc owner)
        {
            _owner = owner;
            _gx1 = owner.EffectiveGradientX1;
            _gy1 = owner.EffectiveGradientY1;
            _gx2 = owner.EffectiveGradientX2;
            _gy2 = owner.EffectiveGradientY2;
        }

        /// <summary>The gradient color at a box-local point (origin at the box's top-left, unrotated).</summary>
        public Color Sample(float localX, float localY) =>
            _owner.ComputeGradientColor(localX, localY, _gx1, _gy1, _gx2, _gy2);
    }

    /// <summary>
    /// Where the arc's band lies on screen: a stroke of constant thickness along the ellipse inscribed
    /// in the box, inset by half the thickness so the stroke's outer edge meets the box (what Skia draws
    /// for <c>AddArc(boundingRect)</c> stroked at that width), with the box turned about its top-left
    /// corner by the rotation. Angles are parametric (the ellipse point at angle t is
    /// <c>(A cos t, B sin t)</c>), like Skia's, and <c>f</c> runs from 0 on the outer edge to 1 on the
    /// inner edge. For a square box this is the plain circular ring.
    /// </summary>
    private readonly struct BandGeometry
    {
        public readonly float Cx, Cy, RotationDegrees, Cos, Sin, HalfW, HalfH, Thickness, A, B;

        public BandGeometry(float cx, float cy, float rotationDegrees, float halfW, float halfH, float thickness)
        {
            float rotRad = rotationDegrees * (System.MathF.PI / 180f);
            Cx = cx; Cy = cy;
            RotationDegrees = rotationDegrees;
            Cos = System.MathF.Cos(rotRad);
            Sin = System.MathF.Sin(rotRad);
            HalfW = halfW; HalfH = halfH; Thickness = thickness;
            A = System.MathF.Max(0f, halfW - thickness * 0.5f);
            B = System.MathF.Max(0f, halfH - thickness * 0.5f);
        }

        private BandGeometry(BandGeometry other, float cx, float cy)
        {
            Cx = cx; Cy = cy;
            RotationDegrees = other.RotationDegrees;
            Cos = other.Cos; Sin = other.Sin;
            HalfW = other.HalfW; HalfH = other.HalfH; Thickness = other.Thickness;
            A = other.A; B = other.B;
        }

        /// <summary>The same band with its center moved to a new screen position.</summary>
        public BandGeometry At(float cx, float cy) => new BandGeometry(this, cx, cy);

        /// <summary>Half the size of the screen-aligned box that holds the turned box.</summary>
        public (float X, float Y) ExtentsOfTurnedBox() => (
            System.MathF.Abs(HalfW * Cos) + System.MathF.Abs(HalfH * Sin),
            System.MathF.Abs(HalfW * Sin) + System.MathF.Abs(HalfH * Cos));

        /// <summary>The screen point, and its center-relative unrotated local point, at a band position.</summary>
        public void Point(float angleRad, float f, out float sx, out float sy, out float lx, out float ly)
        {
            float c = System.MathF.Cos(angleRad);
            float s = System.MathF.Sin(angleRad);
            if (HalfW == HalfH)
            {
                // A circle: the point is simply at the radius that is `f` of the way in from the box edge.
                float radius = HalfW - Thickness * f;
                lx = c * radius;
                ly = s * radius;
                sx = Cx + lx * Cos + ly * Sin;
                sy = Cy - lx * Sin + ly * Cos;
                return;
            }
            float nx = B * c;
            float ny = A * s;
            float length = System.MathF.Sqrt(nx * nx + ny * ny);
            if (length <= 1e-6f)
            {
                nx = c;
                ny = s;
            }
            else
            {
                nx /= length;
                ny /= length;
            }
            float offset = Thickness * (0.5f - f);
            lx = A * c + nx * offset;
            ly = B * s + ny * offset;
            sx = Cx + lx * Cos + ly * Sin;
            sy = Cy - lx * Sin + ly * Cos;
        }

        /// <summary>The unit screen direction of the centerline as the angle increases.</summary>
        public void Tangent(float angleRad, out float tx, out float ty)
        {
            float localX = -A * System.MathF.Sin(angleRad);
            float localY = B * System.MathF.Cos(angleRad);
            float length = System.MathF.Sqrt(localX * localX + localY * localY);
            if (length <= 1e-6f)
            {
                localX = -System.MathF.Sin(angleRad);
                localY = System.MathF.Cos(angleRad);
                length = 1f;
            }
            localX /= length;
            localY /= length;
            tx = localX * Cos + localY * Sin;
            ty = -localX * Sin + localY * Cos;
        }

        public BandVertex Vertex(float angleRad, float f, GradientSampler? sampler, Color solid)
        {
            Point(angleRad, f, out float sx, out float sy, out float lx, out float ly);
            Color color = sampler.HasValue ? sampler.Value.Sample(lx + HalfW, ly + HalfH) : solid;
            return new BandVertex(sx, sy, color);
        }
    }

    /// <summary>
    /// Interpolates <see cref="Color1"/> → <see cref="Color2"/> for a box-local point. Linear
    /// projects the point onto the axis; radial normalizes the distance from the center against
    /// the <c>[InnerRadius, OuterRadius]</c> band (outer falls back to 100 pixels, like Skia).
    /// </summary>
    private Color ComputeGradientColor(float localX, float localY,
        float gx1, float gy1, float gx2, float gy2)
    {
        float t;
        if (GradientType == GradientType.Linear)
        {
            float dx = gx2 - gx1;
            float dy = gy2 - gy1;
            float lenSq = dx * dx + dy * dy;
            if (lenSq <= 0f)
            {
                return Color2;
            }
            t = ((localX - gx1) * dx + (localY - gy1) * dy) / lenSq;
        }
        else
        {
            float dx = localX - gx1;
            float dy = localY - gy1;
            float dist = System.MathF.Sqrt(dx * dx + dy * dy);
            float effectiveOuter = EffectiveGradientOuterRadius;
            float effectiveInner = EffectiveGradientInnerRadius;
            float outer = effectiveOuter;
            float span = outer - effectiveInner;
            if (span <= 0f)
            {
                return Color2;
            }
            t = (dist - effectiveInner) / span;
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
    /// did). The silhouette is the same band (and rounded caps) the body draws, offset by the
    /// dropshadow offset, so it follows the box's rotation and non-square shape. A circle blurs inside
    /// its own diameter; any other box inside the screen-aligned box that holds the turned box.
    /// </summary>
    private void DrawDropshadow(in BandGeometry band, bool isCircle, float innerRadius, float outerRadius,
        float startAngleDeg, float endAngleDeg, int segments, bool hasCaps, byte bodyAlpha)
    {
        // Like Skia's drop-shadow filter, the offset is in the arc's own (turned) space.
        float shadowCx = band.Cx + DropshadowOffsetX * band.Cos + DropshadowOffsetY * band.Sin;
        float shadowCy = band.Cy - DropshadowOffsetX * band.Sin + DropshadowOffsetY * band.Cos;
        BandGeometry shadowBand = band.At(shadowCx, shadowCy);
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
            // Hard offset silhouette — the nominal band.
            DrawSolid(shadowBand, isCircle, innerRadius, outerRadius, startAngleDeg, endAngleDeg,
                segments, hasCaps, effectiveDropshadowColor);
            return;
        }

        float extentX = outerRadius;
        float extentY = outerRadius;
        if (!isCircle)
        {
            (extentX, extentY) = band.ExtentsOfTurnedBox();
        }
        Color silhouetteColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);
        // The mask is drawn in world pixels, so it depends on the rotation (it turns the arc, and a non-square box).
        float rotation = band.RotationDegrees;
        global::RenderingLibrary.Graphics.Renderer.Self.ShadowBlur.Draw(
            this,
            shadowBand.Cx - extentX,
            shadowBand.Cy - extentY,
            extentX * 2f,
            extentY * 2f,
            blur,
            effectiveDropshadowColor,
            global::RenderingLibrary.Graphics.Renderer.Self.ActiveCamera2D,
            global::RenderingLibrary.Graphics.Renderer.Self.ActiveRenderTexture,
            (px, py) =>
            {
                DrawSolid(shadowBand.At(px + extentX, py + extentY), isCircle, innerRadius, outerRadius,
                    startAngleDeg, endAngleDeg, segments, hasCaps, silhouetteColor);
            },
            isCircle
                ? new ShadowMaskKey(4, false, innerRadius, outerRadius, startAngleDeg, endAngleDeg, segments, hasCaps ? 1f : 0f, rotation)
                : new ShadowMaskKey(5, false, band.HalfW, band.HalfH, band.Thickness, startAngleDeg, endAngleDeg,
                    segments, rotation, hasCaps ? 1f : 0f));
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
