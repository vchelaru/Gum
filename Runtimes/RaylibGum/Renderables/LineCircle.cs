using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Numerics;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

/// <summary>Where the (X, Y) coordinate refers to on a <see cref="LineCircle"/>.</summary>
public enum CircleOrigin
{
    /// <summary>(X, Y) is the centre point.</summary>
    Center,
    /// <summary>(X, Y) is the top-left of the bounding box — centre is derived from Width and Height.</summary>
    TopLeft,
}

/// <summary>
/// Raylib circle renderable. Originally a 1 px stroke-only outline via <c>DrawCircleLines</c>;
/// extended in issue #2757 to also paint a filled disk, a variable-width stroke ring via
/// <c>DrawRing</c>, and a centered radial gradient via <c>DrawCircleGradient</c>. The legacy
/// <see cref="Color"/> / <see cref="Red"/> / <see cref="Green"/> / <see cref="Blue"/> /
/// <see cref="Alpha"/> surface is preserved so the shared <c>CircleRuntime</c>'s raylib branch
/// keeps working unchanged — when <see cref="FillColor"/> and <see cref="StrokeColor"/> are
/// both <c>null</c> the render path collapses to the original behavior.
/// </summary>
public class LineCircle : InvisibleRenderable
{
    /// <inheritdoc cref="CircleOrigin"/>
    public CircleOrigin CircleOrigin { get; set; }

    /// <summary>Radius in world-space pixels. Computed from <see cref="Width"/> and
    /// <see cref="Height"/> as <c>min(Width, Height) / 2</c> so a non-square bounding box
    /// renders a circle that fits inside the smaller dimension, centered (#2852). The setter
    /// keeps Width and Height in lockstep so direct Radius assignments still yield a square
    /// shape. Mirrors the Skia and Apos.Shapes renderables.</summary>
    public float Radius
    {
        get => System.Math.Min(Width, Height) / 2f;
        set
        {
            Width = value * 2;
            Height = value * 2;
        }
    }

    /// <summary>
    /// Legacy single-color slot used as the stroke color when <see cref="StrokeColor"/> is
    /// <c>null</c>. Defaults to white so the pre-#2757 outline path renders the same as before.
    /// </summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// When <c>true</c>, draws a filled disk using <see cref="Color"/> (or <see cref="FillColor"/>
    /// when set). Independent of the stroke pass — both fill and stroke may render in the same
    /// <see cref="Render"/> call.
    /// </summary>
    public bool IsFilled { get; set; }

    /// <summary>
    /// Width of the stroke ring in world-space pixels. A value of 1 (the default) uses the
    /// crisp <c>DrawCircleLines</c> path; larger values draw a ring via <c>DrawRing</c>.
    /// </summary>
    public float StrokeWidth { get; set; } = 1f;

    /// <summary>
    /// Explicit fill-pass color. When set, the fill pass runs regardless of <see cref="IsFilled"/>
    /// — this is the raylib analog of Skia's two-slot composition (#2790). When <c>null</c>
    /// the fill pass only runs if <see cref="IsFilled"/> is <c>true</c>, in which case
    /// <see cref="Color"/> is used.
    /// </summary>
    public Color? FillColor { get; set; }

    /// <summary>
    /// Explicit stroke-pass color. When <c>null</c>, the stroke pass uses <see cref="Color"/>
    /// (legacy behavior) — but only if no fill is rendered. When non-<c>null</c>, the stroke
    /// always renders regardless of fill, enabling a filled disk with a contrasting outline.
    /// </summary>
    public Color? StrokeColor { get; set; }

    /// <summary>
    /// When <c>true</c>, the fill pass paints a gradient from <see cref="Color1"/> to
    /// <see cref="Color2"/> rather than a solid color. Both <see cref="GradientType.Linear"/>
    /// and <see cref="GradientType.Radial"/> are supported (#2757 follow-ups #8 and #9);
    /// rendering goes through an <c>rlgl</c> triangle fan with per-vertex colors computed
    /// from <see cref="GradientX1"/>/<see cref="GradientY1"/>, <see cref="GradientX2"/>/<see cref="GradientY2"/>,
    /// and (for radial) <see cref="GradientInnerRadius"/>/<see cref="GradientOuterRadius"/>.
    /// </summary>
    public bool UseGradient { get; set; }

    /// <summary>
    /// Gradient mode. <see cref="GradientType.Linear"/> uses (X1,Y1)→(X2,Y2) as the gradient
    /// axis; <see cref="GradientType.Radial"/> uses (X1,Y1) as the center with InnerRadius/
    /// OuterRadius as the falloff band.
    /// </summary>
    public GradientType GradientType { get; set; }

    /// <summary>Start color (linear: at the axis start; radial: at the inner radius).</summary>
    public Color Color1 { get; set; } = Color.White;

    /// <summary>End color (linear: at the axis end; radial: at the outer radius).</summary>
    public Color Color2 { get; set; } = Color.White;

    /// <summary>
    /// X coordinate of the gradient axis start (linear) or radial gradient center, in pixels
    /// relative to the circle's bounding-box top-left. Default 0.
    /// </summary>
    public float GradientX1 { get; set; }

    /// <summary>Y coordinate of <see cref="GradientX1"/>, same coord space.</summary>
    public float GradientY1 { get; set; }

    /// <summary>
    /// X coordinate of the gradient axis end (linear only — ignored for radial).
    /// </summary>
    public float GradientX2 { get; set; }

    /// <summary>Y coordinate of <see cref="GradientX2"/>.</summary>
    public float GradientY2 { get; set; }

    /// <summary>
    /// Inner radius for a radial gradient — at this radius the color is <see cref="Color1"/>.
    /// Default 0 (solid inner color at the gradient center).
    /// </summary>
    public float GradientInnerRadius { get; set; }

    /// <summary>
    /// Outer radius for a radial gradient — at this radius the color is <see cref="Color2"/>.
    /// When 0 (the default), the circle's <see cref="Radius"/> is used so a default-configured
    /// radial gradient covers the whole disk.
    /// </summary>
    public float GradientOuterRadius { get; set; }

    /// <summary>
    /// Length in world-space pixels of each dash segment around the ring's circumference.
    /// A value of 0 (the default) draws a solid stroke. Both <see cref="StrokeDashLength"/>
    /// and <see cref="StrokeGapLength"/> must be &gt; 0 for dashed rendering to engage.
    /// Issue #2757 — implemented via a per-dash <c>DrawRing</c> arc loop.
    /// </summary>
    public float StrokeDashLength { get; set; }

    /// <summary>
    /// Length in world-space pixels of each gap between dashes. Ignored when
    /// <see cref="StrokeDashLength"/> is 0.
    /// </summary>
    public float StrokeGapLength { get; set; }

    /// <summary>
    /// When <c>true</c>, a dropshadow pass renders behind the fill/stroke passes using
    /// <see cref="DropshadowColor"/>, offset by <see cref="DropshadowOffsetX"/>/<see cref="DropshadowOffsetY"/>
    /// with an isotropic blur radius of <c>max(DropshadowBlurX, DropshadowBlurY)</c>.
    /// Issue #2757 — raylib has no shader-free blur primitive, so the blur is approximated
    /// with concentric semi-transparent rings (raised-cosine falloff). Anisotropic blur
    /// (BlurX ≠ BlurY) collapses to the larger of the two on raylib.
    /// </summary>
    public bool HasDropshadow { get; set; }

    /// <summary>Color of the dropshadow disk (alpha channel scales the falloff).</summary>
    public Color DropshadowColor { get; set; } = new Color((byte)0, (byte)0, (byte)0, (byte)255);

    /// <summary>X offset of the dropshadow center in world-space pixels.</summary>
    public float DropshadowOffsetX { get; set; }

    /// <summary>Y offset of the dropshadow center in world-space pixels.</summary>
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

    /// <summary>
    /// Issue #2956 — true when the fill pass should paint its gradient. Mirrors the SkPaint
    /// contract enforced by SkiaGum: <see cref="UseGradient"/> is a *pattern* flag, not a
    /// *visibility* flag, so a slot whose effective fill alpha is 0 (e.g. the default-
    /// transparent fill on a stroke-only plain Circle) must NOT paint its gradient. Without
    /// this gate <see cref="DrawGradientFan"/> would emit opaque triangles regardless of
    /// <see cref="FillColor"/>'s alpha. The fill slot is enabled when an explicit
    /// <see cref="FillColor"/> is set OR <see cref="IsFilled"/> is true (legacy
    /// <see cref="Color"/> path); the effective fill color is then <c>FillColor ?? Color</c>.
    /// </summary>
    public bool ShouldPaintFillGradient =>
        UseGradient && (FillColor.HasValue || IsFilled) && (FillColor ?? Color).A > 0;

    /// <summary>
    /// Issue #2934 / #2956 — returns the linear gradient axis endpoints rotated around the
    /// bbox center by <paramref name="rotationDegrees"/>. The disc itself is rotation-
    /// symmetric so its position is unchanged by rotation, but the gradient axis is defined
    /// in object-local bbox coords and must rotate with the shape — otherwise the gradient
    /// pattern stays world-axis-aligned while the user rotates the circle. Mirrors the
    /// approach Apos's <c>RenderableShapeBase.GetGradient</c> took in PR #2945. Rotation
    /// convention matches <see cref="LineRectangle"/>'s R helper (visual CCW on screen, since
    /// screen Y points down): <c>[cos sin; -sin cos]</c>.
    /// </summary>
    public (float x1, float y1, float x2, float y2) GetRotatedGradientEndpoints(float rotationDegrees)
    {
        if (rotationDegrees == 0f)
        {
            return (GradientX1, GradientY1, GradientX2, GradientY2);
        }
        float rotRad = rotationDegrees * System.MathF.PI / 180f;
        float cos = System.MathF.Cos(rotRad);
        float sin = System.MathF.Sin(rotRad);
        float pivotX = Radius;
        float pivotY = Radius;
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

    /// <inheritdoc cref="LineCircle"/>
    public LineCircle() : this(null) { }

    /// <inheritdoc cref="LineCircle"/>
    public LineCircle(SystemManagers? _) { }

    /// <inheritdoc/>
    public override void Render(ISystemManagers managers)
    {
        if (!Visible)
        {
            return;
        }

        float cx;
        float cy;
        if (CircleOrigin == CircleOrigin.TopLeft)
        {
            cx = this.GetAbsoluteLeft() + this.Width * 0.5f;
            cy = this.GetAbsoluteTop() + this.Height * 0.5f;
        }
        else
        {
            cx = this.GetAbsoluteLeft();
            cy = this.GetAbsoluteTop();
        }

        // Dropshadow pre-pass — runs first so the shape draws over it. raylib has no
        // SKImageFilter.CreateDropShadow equivalent and no shader-free blur primitive, so the
        // blurred edge is approximated by N concentric rings of decreasing alpha around a
        // solid core disk. Anisotropic blur collapses to max(BlurX, BlurY).
        if (HasDropshadow && Radius > 0f)
        {
            float shadowCx = cx + DropshadowOffsetX;
            float shadowCy = cy + DropshadowOffsetY;
            float blur = System.MathF.Max(DropshadowBlurX, DropshadowBlurY);

            // Issue #2851: scale shadow alpha by the body's effective alpha so fading the
            // circle to transparent also fades the shadow. Matches SkiaGum (the Gum
            // tool/viewport) and Apos.Shapes (RenderableShapeBase.EffectiveDropshadowColor).
            // In the two-slot model the user picks the body alpha via FillColor / StrokeColor
            // / legacy Color; we read the same precedence the fill pass below uses
            // (FillColor ?? Color), falling back to StrokeColor when no fill is set so an
            // outline-only shape's shadow still fades with the outline.
            byte bodyAlpha = FillColor?.A ?? StrokeColor?.A ?? Color.A;
            Color effectiveDropshadowColor = new Color(
                DropshadowColor.R,
                DropshadowColor.G,
                DropshadowColor.B,
                (byte)(DropshadowColor.A * bodyAlpha / 255));

            if (blur > 0f)
            {
                // Issue #2865: render-to-texture + separable Gaussian replaces the band stack.
                // See LineRectangle / ShadowBlurRenderer for the full rationale — band approach
                // overshot effective alpha by ~2.5× because per-band scaling broke the
                // source-over inversion the band alphas were derived from.
                float r = Radius;
                float diameter = r * 2f;
                Color silhouetteColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);
                global::RenderingLibrary.Graphics.Renderer.Self.ShadowBlur.Draw(
                    this,
                    shadowCx - r,
                    shadowCy - r,
                    diameter,
                    diameter,
                    blur,
                    effectiveDropshadowColor,
                    (px, py) =>
                    {
                        DrawCircleV(new Vector2(px + r, py + r), r, silhouetteColor);
                    });
            }
            else
            {
                // blur = 0: hard offset silhouette, no fade.
                DrawCircle((int)shadowCx, (int)shadowCy, Radius, effectiveDropshadowColor);
            }
        }

        // Fill pass — runs when FillColor is set, or when IsFilled is true with no explicit
        // FillColor (legacy Color slot supplies the fill color). Mirrors Skia's two-slot
        // composition (#2790) where setting FillColor alone, StrokeColor alone, or both lights
        // up the appropriate layers.
        bool runFill = FillColor.HasValue || IsFilled;
        if (runFill)
        {
            Color fillColor = FillColor ?? Color;
            if (UseGradient)
            {
                // #2757 follow-ups #8 (linear) and #9 (offset/inner-radius radial). Both go
                // through an rlgl triangle fan with per-vertex colors — raylib's public
                // DrawCircleGradient handles only centered, inner=0, outer=Radius radial, so
                // we drop to immediate-mode for full coverage. Centered radial would still
                // render correctly through DrawCircleGradient, but unifying the path keeps
                // one tested branch instead of a fragile "is this the centered default?"
                // detector. The fan structure matches raylib's own DrawCircle implementation
                // in rshapes.c — center vertex + N rim vertices fan into N triangles.
                //
                // Issue #2956 — suppress the fan when the slot's effective fill alpha is 0.
                // DrawGradientFan emits per-vertex Color1/Color2 directly with no modulation
                // by fillColor.A, so without this gate a default-transparent fill would still
                // paint an opaque gradient (Skia naturally avoids this via paint.Color.alpha
                // modulating the shader; we replicate it explicitly). See ShouldPaintFillGradient.
                if (fillColor.A > 0)
                {
                    DrawGradientFan(cx, cy, Radius);
                }
            }
            else
            {
                DrawCircle((int)cx, (int)cy, Radius, fillColor);
            }
        }

        // Stroke pass — runs when StrokeColor is explicitly set (paired with fill), or when
        // no fill ran (so the legacy outline path stays the default visible behavior). Stroke
        // is inset entirely inside Radius (outer edge at Radius, inner edge at Radius -
        // StrokeWidth) so the ring never bleeds past the nominal bounds — mirrors Skia's
        // RenderableShapeBase.IsOffsetAppliedForStroke contract, which the #2790 gallery's
        // "inscribed in 64x64 frame" row treats as the visual acceptance.
        bool runStroke = StrokeColor.HasValue || !runFill;

        // Skia parity (#2757): SkiaShapeRuntime.RefreshSlotGradients auto-gates each slot's
        // UseGradient flag by whether that slot has a non-null color, so a cell with both
        // FillColor and StrokeColor set + UseGradient = true paints the gradient as BOTH the
        // fill and the stroke. The stroke's gradient samples match the fill's gradient samples
        // at the boundary pixels, so the stroke is visually indistinguishable from the fill
        // underneath — no visible outline. raylib has one UseGradient flag per renderable and
        // would otherwise paint the stroke as solid strokeColor over the gradient fill, which
        // shows up as a visible outline that Skia doesn't draw. Suppressing the stroke here
        // matches Skia's rendered output without needing a separate gradient-stroke draw path.
        // Same gate landed on LineRectangle in commit 7f1e3b55b.
        //
        // Issue #2956 — gate on ShouldPaintFillGradient (which checks effective fill alpha)
        // not on the looser `UseGradient && runFill`: with a transparent fill the gradient
        // pass is suppressed, so the solid stroke is the only visible output and should NOT
        // be suppressed. Before this tightening, IsFilled = false + UseGradient = true
        // produced no fill (alpha 0) AND no stroke (suppressed by the old gate) — a
        // completely invisible Circle. The outline gallery row exercises exactly this case.
        if (runStroke && ShouldPaintFillGradient)
        {
            runStroke = false;
        }
        if (runStroke)
        {
            Color strokeColor = StrokeColor ?? Color;
            // Segment count scales with radius to keep the ring smooth at larger sizes; 36
            // is the floor for small circles to avoid visible faceting. DrawRing is used for
            // every stroke width (including 1 px) so the inset behavior stays uniform — the
            // legacy DrawCircleLines path centered the line on Radius, which bled outward.
            int segments = System.Math.Max(36, (int)(Radius * 2));
            float innerRadius = System.Math.Max(0f, Radius - StrokeWidth);
            Vector2 center = new Vector2(cx, cy);
            bool dashed = StrokeDashLength > 0f && StrokeGapLength > 0f && Radius > 0f;
            if (dashed)
            {
                // Translate user-space dash/gap pixel lengths into arc angles around the
                // ring's circumference. raylib has no built-in dash path effect (Skia's
                // SKPathEffect.CreateDash, MG/Apos's RenderDashed); we emit each dash as a
                // separate DrawRing arc instead.
                float circumference = 2f * System.MathF.PI * Radius;
                float dashAngleDeg = (StrokeDashLength / circumference) * 360f;
                float gapAngleDeg = (StrokeGapLength / circumference) * 360f;
                float patternAngleDeg = dashAngleDeg + gapAngleDeg;
                // Per-dash segment count proportional to the dash arc — ~4° per segment is
                // smooth at typical radii. Floor at 1 so tiny dashes (e.g. 2 px dotted) still
                // render as at least one triangle pair.
                int segmentsPerDash = System.Math.Max(1, (int)(dashAngleDeg / 4f));
                float currentAngle = 0f;
                while (currentAngle < 360f)
                {
                    float dashEnd = System.MathF.Min(currentAngle + dashAngleDeg, 360f);
                    DrawRing(center, innerRadius, Radius,
                        currentAngle, dashEnd, segmentsPerDash, strokeColor);
                    currentAngle += patternAngleDeg;
                }
            }
            else
            {
                DrawRing(center, innerRadius, Radius,
                    startAngle: 0f, endAngle: 360f, segments, strokeColor);
            }
        }
    }

    /// <summary>
    /// Emits a triangle fan around (<paramref name="cx"/>, <paramref name="cy"/>) with per-vertex
    /// colors computed from the user's gradient configuration. Mirrors the immediate-mode
    /// pattern raylib itself uses for <c>DrawCircleGradient</c> in <c>rshapes.c</c> — N
    /// segments × 3 vertices per triangle, color set via <see cref="Rlgl.Color4ub"/> before
    /// each <see cref="Rlgl.Vertex2f"/>. The GPU rasterizes the per-vertex colors via
    /// barycentric interpolation, giving smooth gradients across each triangle.
    /// </summary>
    /// <remarks>
    /// Quality note: the center vertex contributes a single t-value to every fan triangle,
    /// so very high-contrast linear gradients can show a faint "star" near the center where
    /// adjacent triangles meet. Same artifact <c>DrawCircleGradient</c> has on raylib itself.
    /// 36+ segments at typical radii is enough that the effect is invisible.
    /// </remarks>
    private void DrawGradientFan(float cx, float cy, float radius)
    {
        int segments = System.Math.Max(36, (int)(radius * 2));
        // Gradient coords are circle-local (origin = bbox top-left). Cache the world-space
        // bbox origin once so per-vertex projection only has to subtract.
        float bboxLeft = cx - radius;
        float bboxTop = cy - radius;

        // Issue #2934 / #2956 — pre-rotate the gradient endpoints (linear) or center (radial)
        // around the bbox center so the gradient axis tracks the shape under self-rotation.
        // Same approach Apos's RenderableShapeBase.GetGradient takes (PR #2945). Computed
        // once here rather than per-vertex.
        float rotDeg = this.GetAbsoluteRotation();
        (float gx1, float gy1, float gx2, float gy2) = GetRotatedGradientEndpoints(rotDeg);

        Rlgl.Begin((int)DrawMode.Triangles);
        for (int i = 0; i < segments; i++)
        {
            float a0 = (i / (float)segments) * System.MathF.PI * 2f;
            float a1 = ((i + 1) / (float)segments) * System.MathF.PI * 2f;
            float v1x = cx + System.MathF.Cos(a0) * radius;
            float v1y = cy + System.MathF.Sin(a0) * radius;
            float v2x = cx + System.MathF.Cos(a1) * radius;
            float v2y = cy + System.MathF.Sin(a1) * radius;

            // Vertex order: center → later-angle → earlier-angle. Matches raylib's own
            // DrawCircleGradient winding (src/rshapes.c). Reversing the two rim vertices
            // produces back-facing triangles for raylib's default front-face setting, which
            // get culled and render as nothing.
            EmitGradientVertex(cx, cy, bboxLeft, bboxTop, gx1, gy1, gx2, gy2);
            EmitGradientVertex(v2x, v2y, bboxLeft, bboxTop, gx1, gy1, gx2, gy2);
            EmitGradientVertex(v1x, v1y, bboxLeft, bboxTop, gx1, gy1, gx2, gy2);
        }
        Rlgl.End();
    }

    private void EmitGradientVertex(float worldX, float worldY, float bboxLeft, float bboxTop,
        float gx1, float gy1, float gx2, float gy2)
    {
        float localX = worldX - bboxLeft;
        float localY = worldY - bboxTop;

        float t;
        if (GradientType == GradientType.Linear)
        {
            // Project the vertex onto the (rotation-adjusted) gradient axis. Degenerate
            // (zero-length) axis collapses to a solid Color1 — same fallback Skia's
            // CreateLinearGradient takes.
            float dx = gx2 - gx1;
            float dy = gy2 - gy1;
            float lenSq = dx * dx + dy * dy;
            if (lenSq <= 0f)
            {
                Rlgl.Color4ub(Color1.R, Color1.G, Color1.B, Color1.A);
                Rlgl.Vertex2f(worldX, worldY);
                return;
            }
            t = ((localX - gx1) * dx + (localY - gy1) * dy) / lenSq;
        }
        else
        {
            // Radial: distance from (gx1, gy1) — the rotation-adjusted radial center —
            // normalized against the [InnerRadius, OuterRadius] band. OuterRadius = 0
            // (default) collapses to the full circle radius so a no-config radial covers
            // the whole disk.
            float dx = localX - gx1;
            float dy = localY - gy1;
            float dist = System.MathF.Sqrt(dx * dx + dy * dy);
            float outer = GradientOuterRadius > 0f ? GradientOuterRadius : Radius;
            float span = outer - GradientInnerRadius;
            if (span <= 0f)
            {
                Rlgl.Color4ub(Color2.R, Color2.G, Color2.B, Color2.A);
                Rlgl.Vertex2f(worldX, worldY);
                return;
            }
            t = (dist - GradientInnerRadius) / span;
        }
        if (t < 0f) t = 0f;
        else if (t > 1f) t = 1f;

        byte r = (byte)(Color1.R + (Color2.R - Color1.R) * t);
        byte g = (byte)(Color1.G + (Color2.G - Color1.G) * t);
        byte b = (byte)(Color1.B + (Color2.B - Color1.B) * t);
        byte a = (byte)(Color1.A + (Color2.A - Color1.A) * t);
        Rlgl.Color4ub(r, g, b, a);
        Rlgl.Vertex2f(worldX, worldY);
    }
}
