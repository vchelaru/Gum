using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;


/// <summary>
/// Raylib rectangle renderable. Originally a 1 px stroke-only outline via four
/// <c>DrawLineEx</c> calls; extended in issue #2757 to also paint a filled rectangle, a
/// variable-width stroke (via <c>DrawRectangleLinesEx</c>), perimeter-walked dashed strokes,
/// linear + radial gradients (via an <c>rlgl</c> triangle mesh), and an approximated
/// dropshadow (concentric semi-transparent rectangles with raised-cosine alpha falloff). The
/// legacy <see cref="Color"/> / <see cref="Red"/> / <see cref="Green"/> / <see cref="Blue"/> /
/// <see cref="Alpha"/> surface is preserved so the shared <c>RectangleRuntime</c>'s raylib
/// branch keeps working unchanged — when <see cref="FillColor"/> and <see cref="StrokeColor"/>
/// are both <c>null</c> the render path collapses to the original behavior.
/// </summary>
public class LineRectangle : InvisibleRenderable
{
    /// <summary>
    /// Legacy binary-dotted flag. When <c>true</c>, the stroke renders as <see cref="DashLength"/>-px
    /// dashes with equal-length gaps. Superseded by the <see cref="StrokeDashLength"/> /
    /// <see cref="StrokeGapLength"/> pair (#2757) which support independent dash/gap lengths;
    /// kept for back-compat with callers that only know about <c>IsDotted</c>.
    /// </summary>
    public bool IsDotted { get; set; }

    /// <summary>Length of each dash (and gap) in pixels when <see cref="IsDotted"/>.</summary>
    public float DashLength { get; set; } = 2f;

    /// <summary>
    /// Stroke width in world-space pixels. Drives <c>DrawRectangleLinesEx</c> when no fill is
    /// painted, and the four <c>DrawLineEx</c> calls on the rotated path. Default 1.
    /// </summary>
    public float LinePixelWidth { get; set; } = 1f;

    /// <summary>
    /// Legacy single-color slot used as the stroke color when <see cref="StrokeColor"/> is
    /// <c>null</c>. Defaults to white so the pre-#2757 outline path renders the same as before.
    /// </summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// When <c>true</c>, draws a filled rectangle using <see cref="Color"/> (or
    /// <see cref="FillColor"/> when set). Independent of the stroke pass — both fill and stroke
    /// may render in the same <see cref="Render"/> call.
    /// </summary>
    public bool IsFilled { get; set; }

    /// <summary>
    /// Uniform corner radius in world-space pixels — same unit as the rest of Gum (Skia's
    /// <c>RectangleRuntime.CornerRadius</c>, MG's Apos.Shapes <c>RoundedRectangle</c>). A
    /// value of 0 (the default) draws hard corners via the existing
    /// <c>DrawRectangle*</c> paths. When &gt; 0, fill and stroke route through raylib's
    /// <c>DrawRectangleRounded</c> / <c>DrawRectangleRoundedLinesEx</c>. raylib's API takes
    /// roundness as a 0..1 fraction of <c>min(Width, Height) / 2</c>, so <see cref="Render"/>
    /// converts pixels → fraction at draw time. Rounded corners are skipped on the rotated and
    /// dashed paths (raylib's rounded primitives don't accept either) — those paths fall back
    /// to the hard-cornered render.
    /// </summary>
    public float CornerRadius { get; set; }

    /// <summary>
    /// Explicit fill-pass color. When set, the fill pass runs regardless of <see cref="IsFilled"/>
    /// — the raylib analog of Skia's two-slot composition (#2790). When <c>null</c> the fill
    /// pass only runs if <see cref="IsFilled"/> is <c>true</c>, in which case <see cref="Color"/>
    /// is used.
    /// </summary>
    public Color? FillColor { get; set; }

    /// <summary>
    /// Explicit stroke-pass color. When <c>null</c>, the stroke pass uses <see cref="Color"/>
    /// (legacy behavior) — but only if no fill is rendered. When non-<c>null</c>, the stroke
    /// always renders regardless of fill, enabling a filled rectangle with a contrasting outline.
    /// </summary>
    public Color? StrokeColor { get; set; }

    /// <summary>
    /// When <c>true</c>, the fill pass paints a gradient from <see cref="Color1"/> to
    /// <see cref="Color2"/> rather than a solid color. Both <see cref="GradientType.Linear"/>
    /// and <see cref="GradientType.Radial"/> are supported (#2757); rendering goes through
    /// <c>rlgl</c> immediate mode with per-vertex colors computed from
    /// <see cref="GradientX1"/>/<see cref="GradientY1"/>, <see cref="GradientX2"/>/<see cref="GradientY2"/>,
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
    /// relative to the rectangle's top-left. Default 0.
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
    /// When 0 (the default), half the diagonal of the rectangle is used so a default-configured
    /// radial gradient covers the whole shape.
    /// </summary>
    public float GradientOuterRadius { get; set; }

    /// <summary>
    /// Length in world-space pixels of each dash segment around the perimeter.
    /// A value of 0 (the default) draws a solid stroke. Both <see cref="StrokeDashLength"/>
    /// and <see cref="StrokeGapLength"/> must be &gt; 0 for dashed rendering to engage.
    /// Issue #2757 — implemented via a perimeter-walking loop that emits per-dash
    /// <c>DrawLineEx</c> calls.
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
    /// with concentric semi-transparent rectangles (raised-cosine falloff). Anisotropic blur
    /// (BlurX ≠ BlurY) collapses to the larger of the two on raylib.
    /// </summary>
    public bool HasDropshadow { get; set; }

    /// <summary>Color of the dropshadow rectangle (alpha channel scales the falloff).</summary>
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

    public LineRectangle() : this(null) { }

    public LineRectangle(SystemManagers? _) { }

    public override void Render(ISystemManagers managers)
    {
        if (!Visible)
        {
            return;
        }

        float ox = this.GetAbsoluteLeft();
        float oy = this.GetAbsoluteTop();
        float w = this.Width;
        float h = this.Height;

        float rotDeg = this.GetAbsoluteRotation();
        bool rotated = MathF.Abs(rotDeg) > 0.0001f;

        // Raylib's DrawRectangleRounded takes "roundness" as a 0..1 fraction of min(w,h)/2;
        // Gum exposes CornerRadius in pixels. Convert here so callers stay in the same unit
        // system as Skia / Apos.Shapes. min(w,h)/2 is the largest corner radius that fits
        // without the four arcs overlapping — values past that clamp to 1.0 the same way
        // raylib's own renderer would.
        float roundness = 0f;
        bool useRounded = false;
        if (CornerRadius > 0f && w > 0f && h > 0f)
        {
            float halfMinDim = MathF.Min(w, h) * 0.5f;
            if (halfMinDim > 0f)
            {
                roundness = MathF.Min(1f, CornerRadius / halfMinDim);
                useRounded = true;
            }
        }
        // Segment count for the corner arcs — 8 per quarter is raylib's default and stays
        // smooth at the radii UI typically uses. Bumped proportionally with CornerRadius so
        // large radii don't show faceting.
        int roundedSegments = useRounded
            ? Math.Max(8, (int)(CornerRadius * 0.75f))
            : 0;
        float rotRad = rotDeg * MathF.PI / 180f;
        float cos = MathF.Cos(rotRad);
        float sin = MathF.Sin(rotRad);

        // Rotate a local offset around (ox, oy). Gum rotation is CCW, matching visual CCW on screen.
        Vector2 R(float dx, float dy) =>
            new Vector2(ox + dx * cos + dy * sin, oy - dx * sin + dy * cos);

        Vector2 tl = R(0, 0);
        Vector2 tr = R(w, 0);
        Vector2 br = R(w, h);
        Vector2 bl = R(0, h);

        // Dropshadow pre-pass — runs first so the shape draws over it. raylib has no
        // SKImageFilter.CreateDropShadow equivalent and no shader-free blur primitive, so the
        // blurred edge is approximated by N concentric rectangles of decreasing alpha around a
        // solid core. Anisotropic blur collapses to max(BlurX, BlurY). Rotation is not applied
        // to the shadow — same limitation the circle dropshadow has, kept consistent.
        if (HasDropshadow && w > 0f && h > 0f)
        {
            float shadowX = ox + DropshadowOffsetX;
            float shadowY = oy + DropshadowOffsetY;
            float blur = MathF.Max(DropshadowBlurX, DropshadowBlurY);

            // Solid core — rounded if CornerRadius > 0 so the shadow silhouette matches the
            // rounded shape that draws over it. Skipped for rotated shapes (raylib's rounded
            // primitives have no rotation parameter, matching the existing shadow's
            // "no rotation" limitation).
            if (useRounded && !rotated)
            {
                Rectangle shadowRect = new Rectangle(shadowX, shadowY, w, h);
                DrawRectangleRounded(shadowRect, roundness, roundedSegments, DropshadowColor);
            }
            else
            {
                DrawRectangleV(new Vector2(shadowX, shadowY), new Vector2(w, h), DropshadowColor);
            }

            if (blur > 0f)
            {
                // Approximate Skia's SKImageFilter.CreateDropShadow output. Skia treats user-set
                // BlurX/BlurY as the VISIBLE blur radius — internally it divides by 3 to get
                // Gaussian sigma (RenderableShapeBase.cs lines 778-779: "BlurX / 3.0f"), and the
                // Gaussian's visible falloff extends ~3σ which works back out to the user-set
                // blur value. So the visible fade extent in raylib is just `blur`, NOT 3*blur as
                // an earlier iteration of this code assumed (that version made the dropshadow
                // bleed ~3× further than Skia's).
                //
                // Band layout:
                //   - falloffExtent = blur (where blur = max(BlurX, BlurY)).
                //   - blurRings non-overlapping outlines, each bandThickness px thick,
                //     positioned at increasing distances outside the solid core.
                //   - Gaussian-ish alpha profile exp(-t²*3) sampled at the band centerline.
                //     exp(-3) ≈ 0.05 at the outer edge — soft tail without going invisible so
                //     fast that the rings stairstep visibly.
                const int blurRings = 32;
                float falloffExtent = blur;
                float bandThickness = falloffExtent / blurRings;
                for (int i = 0; i < blurRings; i++)
                {
                    float bandCenter = (i + 0.5f) * bandThickness;
                    float tCenter = bandCenter / falloffExtent;
                    float alphaScale = MathF.Exp(-tCenter * tCenter * 3f);
                    byte ringAlpha = (byte)(DropshadowColor.A * alphaScale);
                    if (ringAlpha == 0)
                    {
                        continue;
                    }
                    Color ringColor = new Color(DropshadowColor.R, DropshadowColor.G,
                        DropshadowColor.B, ringAlpha);
                    Rectangle outerRect = new Rectangle(
                        shadowX - bandCenter,
                        shadowY - bandCenter,
                        w + 2f * bandCenter,
                        h + 2f * bandCenter);
                    if (useRounded && !rotated)
                    {
                        // Outer corner radius grows by bandCenter so the rounded silhouette
                        // tracks the shape outward — pixel-space corner radius at the band's
                        // outer edge stays (CornerRadius + bandCenter) instead of shrinking
                        // toward 0 as the rect inflates.
                        float bandMinDim = MathF.Min(outerRect.Width, outerRect.Height);
                        float bandRoundness = bandMinDim > 0f
                            ? MathF.Min(1f, (CornerRadius + bandCenter) * 2f / bandMinDim)
                            : 0f;
                        DrawRectangleRoundedLinesEx(outerRect, bandRoundness, roundedSegments,
                            bandThickness, ringColor);
                    }
                    else
                    {
                        DrawRectangleLinesEx(outerRect, bandThickness, ringColor);
                    }
                }
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
                // #2757 follow-up — rectangle gradient via rlgl. Linear uses 2 triangles over
                // the four corners with per-vertex colors (exact for a planar gradient). Radial
                // uses a triangle fan from the centroid → many perimeter samples for smooth
                // falloff. Both paths read GradientX1/Y1/X2/Y2/InnerRadius/OuterRadius in
                // rectangle-local coords (origin = top-left, +X right, +Y down). Rotation
                // applied via the same R() helper used by the outline path.
                if (GradientType == GradientType.Linear)
                {
                    DrawLinearGradientQuad(tl, tr, br, bl, w, h);
                }
                else
                {
                    DrawRadialGradientFan(R, w, h);
                }
            }
            else if (useRounded && !rotated)
            {
                DrawRectangleRounded(new Rectangle(ox, oy, w, h), roundness, roundedSegments,
                    fillColor);
            }
            else
            {
                if (rotated)
                {
                    // Rotated solid fill — split the rotated quad into two triangles. Vertex
                    // order: tl → tr → br and tl → br → bl. raylib uses CCW front-facing under
                    // default backface culling, and Gum rotation is CCW (sin sign in R()
                    // mirrors that), so this winding stays visible regardless of rotation
                    // angle.
                    DrawTriangle(tl, tr, br, fillColor);
                    DrawTriangle(tl, br, bl, fillColor);
                }
                else
                {
                    DrawRectangleV(new Vector2(ox, oy), new Vector2(w, h), fillColor);
                }
            }
        }

        // Stroke pass — runs when StrokeColor is explicitly set (paired with fill), or when
        // no fill ran (so the legacy outline path stays the default visible behavior). For the
        // unrotated, solid case we use DrawRectangleLinesEx which gives a proper inset thick
        // outline. Rotated or dashed paths fall back to per-edge DrawLineEx so the rotation
        // composition and dash perimeter-walk apply cleanly.
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
        //
        // Corner case not yet implemented: UseGradient = true with only StrokeColor (no fill).
        // Skia paints a gradient outline; raylib would currently fall through to solid stroke.
        // Not exercised by the sample; tracked as a #2757 follow-up.
        if (runStroke && UseGradient && runFill)
        {
            runStroke = false;
        }
        if (runStroke)
        {
            Color strokeColor = StrokeColor ?? Color;
            bool dashed =
                (StrokeDashLength > 0f && StrokeGapLength > 0f) ||
                IsDotted;

            // Stroke is inset entirely inside the nominal bounds — outer edge sits at the
            // (ox, oy, w, h) rectangle. Two different inset strategies depending on which raylib
            // primitive renders the stroke:
            //
            //   - DrawRectangleLinesEx and DrawRectangleRoundedLinesEx already inset internally
            //     (they compose four inward-drawn rectangles whose outer edges sit at the rect's
            //     edge). Pass the FULL nominal rect — no manual inset, or the stroke gets
            //     visibly insetting twice and the cell shrinks as StrokeWidth grows.
            //   - DrawLineEx (rotated and dashed paths) centers the line on its endpoints, so
            //     a stroke=8 corner-to-corner line would bleed 4 px outside the nominal bounds.
            //     Inset the corners by halfStroke before drawing so the outer edge of the stroke
            //     sits at the nominal bounds.
            //
            // Both strategies produce the same visual contract: outer edge at the nominal rect,
            // matching Skia's RenderableShapeBase.IsOffsetAppliedForStroke (#2814).

            if (!rotated && !dashed && useRounded)
            {
                DrawRectangleRoundedLinesEx(new Rectangle(ox, oy, w, h), roundness,
                    roundedSegments, LinePixelWidth, strokeColor);
            }
            else if (!rotated && !dashed)
            {
                DrawRectangleLinesEx(new Rectangle(ox, oy, w, h), LinePixelWidth, strokeColor);
            }
            else if (!dashed)
            {
                // Rotated solid stroke — DrawLineEx centers on endpoints, so use halfStroke-inset
                // corners rotated into world space.
                float halfStroke = LinePixelWidth * 0.5f;
                Vector2 stl = R(halfStroke, halfStroke);
                Vector2 str = R(w - halfStroke, halfStroke);
                Vector2 sbr = R(w - halfStroke, h - halfStroke);
                Vector2 sbl = R(halfStroke, h - halfStroke);
                DrawLineEx(stl, str, LinePixelWidth, strokeColor);
                DrawLineEx(str, sbr, LinePixelWidth, strokeColor);
                DrawLineEx(sbr, sbl, LinePixelWidth, strokeColor);
                DrawLineEx(sbl, stl, LinePixelWidth, strokeColor);
            }
            else
            {
                // Dashed perimeter — pixel dash/gap lengths translate into segment lengths along
                // each edge. The new StrokeDashLength + StrokeGapLength pair (#2757) takes
                // precedence over the legacy IsDotted flag, which collapses to a "DashLength /
                // DashLength" pattern. Inset corners again because DrawDashedSegment uses
                // DrawLineEx internally.
                float dashLen;
                float gapLen;
                if (StrokeDashLength > 0f && StrokeGapLength > 0f)
                {
                    dashLen = StrokeDashLength;
                    gapLen = StrokeGapLength;
                }
                else
                {
                    dashLen = MathF.Max(DashLength, 1f);
                    gapLen = dashLen;
                }
                float halfStroke = LinePixelWidth * 0.5f;
                Vector2 stl = R(halfStroke, halfStroke);
                Vector2 str = R(w - halfStroke, halfStroke);
                Vector2 sbr = R(w - halfStroke, h - halfStroke);
                Vector2 sbl = R(halfStroke, h - halfStroke);
                DrawDashedSegment(stl, str, dashLen, gapLen, strokeColor);
                DrawDashedSegment(str, sbr, dashLen, gapLen, strokeColor);
                DrawDashedSegment(sbr, sbl, dashLen, gapLen, strokeColor);
                DrawDashedSegment(sbl, stl, dashLen, gapLen, strokeColor);
            }
        }
    }

    private void DrawDashedSegment(Vector2 a, Vector2 b, float dashLen, float gapLen, Color color)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.5f) return;

        float nx = dx / len;
        float ny = dy / len;
        float pattern = dashLen + gapLen;

        for (float t = 0; t < len; t += pattern)
        {
            float t2 = MathF.Min(t + dashLen, len);
            DrawLineEx(
                new Vector2(a.X + nx * t, a.Y + ny * t),
                new Vector2(a.X + nx * t2, a.Y + ny * t2),
                LinePixelWidth,
                color);
        }
    }

    /// <summary>
    /// Emits two triangles covering the rotated quad with per-vertex colors computed from the
    /// linear gradient axis. Exact for a planar gradient — barycentric interpolation across each
    /// triangle reproduces the gradient correctly across the diagonal seam.
    /// </summary>
    private void DrawLinearGradientQuad(Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl,
        float w, float h)
    {
        // Coords are rectangle-local (0,0 = top-left). Cache the four corner t values then
        // emit two triangles. Same vertex order rule as raylib's DrawTriangle / the circle's
        // gradient fan: CCW front-facing.
        Color cTl = SampleLinearAt(0f, 0f);
        Color cTr = SampleLinearAt(w, 0f);
        Color cBr = SampleLinearAt(w, h);
        Color cBl = SampleLinearAt(0f, h);

        Rlgl.Begin((int)DrawMode.Triangles);
        EmitVertexColored(tl, cTl);
        EmitVertexColored(br, cBr);
        EmitVertexColored(tr, cTr);

        EmitVertexColored(tl, cTl);
        EmitVertexColored(bl, cBl);
        EmitVertexColored(br, cBr);
        Rlgl.End();
    }

    /// <summary>
    /// Triangle fan from the rectangle's centroid out to many perimeter samples. Each fan
    /// triangle gets per-vertex colors sampled from the radial gradient function — many small
    /// triangles approximate the smooth color change a true shader would produce.
    /// </summary>
    private void DrawRadialGradientFan(Func<float, float, Vector2> R, float w, float h)
    {
        // 64 samples around the perimeter is plenty for typical UI rectangle sizes — same
        // smoothness budget as the circle's segment-count rule (~4° resolution at typical
        // radii). The four corners are always included so the rectangle edges stay sharp.
        const int perimeterSamples = 64;
        Vector2 center = R(w * 0.5f, h * 0.5f);
        Color centerColor = SampleRadialAt(w * 0.5f, h * 0.5f, w, h);

        // Walk the perimeter clockwise in local space: top edge → right edge → bottom edge →
        // left edge. Sample at evenly-spaced perimeter distances. Then close the fan by
        // re-emitting the first perimeter point.
        float perimeter = 2f * (w + h);
        float step = perimeter / perimeterSamples;

        Vector2 firstWorld = default;
        Color firstColor = default;
        Vector2 prevWorld = default;
        Color prevColor = default;
        bool havePrev = false;

        Rlgl.Begin((int)DrawMode.Triangles);
        for (int i = 0; i < perimeterSamples; i++)
        {
            float d = i * step;
            (float lx, float ly) = PerimeterLocalCoord(d, w, h);
            Vector2 worldP = R(lx, ly);
            Color cP = SampleRadialAt(lx, ly, w, h);

            if (!havePrev)
            {
                firstWorld = worldP;
                firstColor = cP;
                prevWorld = worldP;
                prevColor = cP;
                havePrev = true;
                continue;
            }

            // Vertex order: center → current → previous. The perimeter walk runs clockwise
            // in screen-down coords (top-left → top-right → bottom-right → bottom-left), so
            // center→prev→curr is CW, which raylib's default backface cull drops. Swapping
            // the last two vertices flips winding to CCW (front-facing) and the radial fan
            // renders. Same rule applied in LineCircle's DrawGradientFan
            // ("center → later-angle → earlier-angle").
            EmitVertexColored(center, centerColor);
            EmitVertexColored(worldP, cP);
            EmitVertexColored(prevWorld, prevColor);

            prevWorld = worldP;
            prevColor = cP;
        }
        // Close the fan.
        EmitVertexColored(center, centerColor);
        EmitVertexColored(firstWorld, firstColor);
        EmitVertexColored(prevWorld, prevColor);
        Rlgl.End();
    }

    /// <summary>
    /// Maps a clockwise perimeter distance to a rectangle-local coordinate. Used by the radial
    /// fan to walk the rectangle's edge.
    /// </summary>
    private static (float x, float y) PerimeterLocalCoord(float d, float w, float h)
    {
        if (d < w) return (d, 0f);
        d -= w;
        if (d < h) return (w, d);
        d -= h;
        if (d < w) return (w - d, h);
        d -= w;
        return (0f, h - d);
    }

    private Color SampleLinearAt(float localX, float localY)
    {
        float dx = GradientX2 - GradientX1;
        float dy = GradientY2 - GradientY1;
        float lenSq = dx * dx + dy * dy;
        if (lenSq <= 0f)
        {
            return Color1;
        }
        float t = ((localX - GradientX1) * dx + (localY - GradientY1) * dy) / lenSq;
        return LerpClamped(t);
    }

    private Color SampleRadialAt(float localX, float localY, float w, float h)
    {
        float dx = localX - GradientX1;
        float dy = localY - GradientY1;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        // OuterRadius = 0 (default) collapses to half-diagonal so a no-config radial covers
        // the whole rectangle. Matches the spirit of the circle's "outer defaults to Radius"
        // rule for the rectangle's shape.
        float outer = GradientOuterRadius > 0f
            ? GradientOuterRadius
            : 0.5f * MathF.Sqrt(w * w + h * h);
        float span = outer - GradientInnerRadius;
        if (span <= 0f)
        {
            return Color2;
        }
        float t = (dist - GradientInnerRadius) / span;
        return LerpClamped(t);
    }

    private Color LerpClamped(float t)
    {
        if (t < 0f) t = 0f;
        else if (t > 1f) t = 1f;
        byte r = (byte)(Color1.R + (Color2.R - Color1.R) * t);
        byte g = (byte)(Color1.G + (Color2.G - Color1.G) * t);
        byte b = (byte)(Color1.B + (Color2.B - Color1.B) * t);
        byte a = (byte)(Color1.A + (Color2.A - Color1.A) * t);
        return new Color(r, g, b, a);
    }

    private static void EmitVertexColored(Vector2 p, Color c)
    {
        Rlgl.Color4ub(c.R, c.G, c.B, c.A);
        Rlgl.Vertex2f(p.X, p.Y);
    }
}
