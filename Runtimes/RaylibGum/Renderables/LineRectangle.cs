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
public class LineRectangle : GradientShapeRenderable
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
    /// Blend mode applied to the fill and stroke passes. When set, <see cref="Render"/> wraps
    /// those passes in <c>BeginBlendMode</c>/<c>EndBlendMode</c> (via the batch-counted wrappers),
    /// which reproduce every <see cref="global::Gum.RenderingLibrary.Blend"/> value exactly
    /// (issue #3470). <c>null</c> (the default) leaves raylib on its standard alpha blend. Mirrors
    /// the <c>Blend</c> member on the raylib <c>Sprite</c> / <c>NineSlice</c> renderables
    /// (issue #3458).
    /// </summary>
    public global::Gum.RenderingLibrary.Blend? Blend { get; set; }

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

    /// <summary>Horizontal blur radius of the dropshadow, in pixels: how far the soft falloff visibly extends.</summary>
    /// <remarks>raylib note: treated as isotropic with <see cref="DropshadowBlurY"/> —
    /// rendering collapses anisotropic blur to <c>max(BlurX, BlurY)</c>.</remarks>
    public float DropshadowBlurX { get; set; }

    /// <summary>Vertical blur radius of the dropshadow, in pixels: how far the soft falloff visibly extends.</summary>
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

    /// <summary>
    /// Issue #2998 — see <see cref="LineCircle.ShouldPaintFillGradient"/> for the full
    /// contract. Same gate, same predicate, applied to <see cref="DrawGradientGrid"/>
    /// on the fill pass.
    /// </summary>
    public bool ShouldPaintFillGradient =>
        UseGradient && (FillColor.HasValue || IsFilled) && (Color1.A > 0 || Color2.A > 0);

    /// <summary>
    /// True when the stroke pass will actually render this frame. Mirrors the exact gating
    /// <see cref="Render"/> uses (stroke color set, or no fill so the legacy outline stays
    /// visible; positive <see cref="LinePixelWidth"/>). Exposed so this gating is testable
    /// without a GL context.
    /// </summary>
    /// <remarks>
    /// Does NOT get suppressed by an active fill gradient. The contract (clarified after #2956/
    /// #2757 got this backwards) is: gradient follows the ACTIVE body -- the fill when a fill is
    /// present, the stroke only when there is no fill. A gradient fill never hides an
    /// independently-colored stroke; the stroke keeps rendering as a plain solid outline on top
    /// of the gradient, exactly like it would on top of a solid fill. Mirrors
    /// <see cref="LineCircle.WillRenderStroke"/> and the corrected
    /// <c>SkiaShapeRuntime.RefreshSlotGradients</c> gate.
    /// </remarks>
    public bool WillRenderStroke
    {
        get
        {
            bool runFill = FillColor.HasValue || IsFilled;
            bool runStroke = (StrokeColor.HasValue || !runFill) && LinePixelWidth > 0f;
            return runStroke;
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

            // Issue #2851: scale shadow alpha by the body's effective alpha so fading the
            // rectangle to transparent also fades the shadow. Matches SkiaGum (the Gum
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
                // Issue #2865: render-to-texture + separable Gaussian replaces the 32-concentric-
                // band approximation. The old approach inverted source-over compositing to derive
                // per-band alphas from a target profile, then scaled per-band by the user shadow
                // alpha — but source-over is non-linear under that scale, so shadows at
                // DropshadowAlpha < 255 came out ~2.5× too opaque. See ShadowBlurRenderer for
                // the full rationale; the helper paints the silhouette into an offscreen RT,
                // blurs it H+V with a Gaussian shader, and composites with the tint.
                Color silhouetteColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);
                bool hollow = !(FillColor.HasValue || IsFilled);
                global::RenderingLibrary.Graphics.Renderer.Self.ShadowBlur.Draw(
                    this,
                    shadowX,
                    shadowY,
                    w,
                    h,
                    blur,
                    effectiveDropshadowColor,
                    global::RenderingLibrary.Graphics.Renderer.Self.ActiveCamera2D,
                    global::RenderingLibrary.Graphics.Renderer.Self.ActiveRenderTexture,
                    (px, py) =>
                    {
                        if (hollow)
                        {
                            // Issue #7297: a hollow shape's shadow is the blurred stroke ring only,
                            // as MonoGame and Skia draw it, not the whole silhouette.
                            if (useRounded)
                            {
                                DrawRectangleRoundedLinesEx(new Rectangle(px, py, w, h), roundness,
                                    roundedSegments, LinePixelWidth, silhouetteColor);
                            }
                            else
                            {
                                DrawRectangleLinesEx(new Rectangle(px, py, w, h), LinePixelWidth, silhouetteColor);
                            }
                        }
                        else if (useRounded)
                        {
                            DrawRectangleRounded(
                                new Rectangle(px, py, w, h),
                                roundness,
                                roundedSegments,
                                silhouetteColor);
                        }
                        else
                        {
                            DrawRectangleV(new Vector2(px, py), new Vector2(w, h), silhouetteColor);
                        }
                    },
                    new ShadowMaskKey(useRounded ? 2 : 1, hollow, roundness, roundedSegments, LinePixelWidth));
            }
            else
            {
                // blur = 0: hard offset silhouette, no fade.
                if (useRounded && !rotated)
                {
                    Rectangle shadowRect = new Rectangle(shadowX, shadowY, w, h);
                    DrawRectangleRounded(shadowRect, roundness, roundedSegments, effectiveDropshadowColor);
                }
                else
                {
                    DrawRectangleV(new Vector2(shadowX, shadowY), new Vector2(w, h), effectiveDropshadowColor);
                }
            }
        }

        // Issue #3458 — wrap ONLY the fill + stroke passes in the blend mode. The dropshadow
        // pre-pass above is deliberately left outside: on the blurred path it bakes into its own
        // RenderTexture2D via Renderer.Self.ShadowBlur.Draw (which manages its own blend state),
        // and the hard-offset path already composites the shadow with the correct alpha. Mirrors
        // the raylib Sprite / NineSlice Blend wrap, using the batch-counted BeginBlendMode /
        // EndBlendMode so the draw-call counter stays accurate.
        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.BeginBlendMode(Blend.Value);
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
                //
                // Issue #2998 — gradient visibility comes from the gradient STOP alphas, not the
                // solid fill alpha; DrawGradientGrid
                // does not modulate by fillColor.A. Route through ShouldPaintFillGradient (the single source
                // of truth) so a transparent solid fill with visible stops still paints, and two
                // transparent stops paint nothing.
                if (!ShouldPaintFillGradient)
                {
                    // No visible gradient stop — skip both the gradient and the solid path.
                }
                else if (GradientType == GradientType.Linear)
                {
                    DrawLinearGradient(tl, tr, bl, w, h);
                }
                else
                {
                    // Per-vertex-colored grid clipped to the rectangle (a fan from the centroid gives
                    // "X"-shaped spokes; stacked translucent circles accumulate alpha and ignore the bounds).
                    DrawGradientGrid(R, w, h, RadialColorAt(w, h));
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
        //
        // Issue #3183 — gate the stroke pass on a positive LinePixelWidth. The default StrokeColor
        // is non-null (white), so a fill-only shape (IsFilled = true, StrokeWidth = 0) would
        // otherwise keep runStroke true and raylib's DrawRectangleRoundedLinesEx paints a ~1px
        // white hairline at width 0 (visible on the Dark Pro slider track / scrollbars). Mirrors
        // the Apos RenderableShapeBase.HasVisibleOutput contract (IsFilled || StrokeWidth > 0),
        // which Circle/RoundedRectangle/Arc early-return on so a zero-width stroke draws nothing.
        // A fresh shape (StrokeWidth defaults to 1) keeps its outline. See WillRenderStroke for
        // the full gating contract, including why an active fill gradient does NOT suppress this.
        //
        // Corner case not yet implemented: UseGradient = true with only StrokeColor (no fill).
        // Skia paints a gradient outline; raylib would currently fall through to solid stroke.
        // Not exercised by the sample; tracked as a #2757 follow-up.
        bool runStroke = WillRenderStroke;
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

        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.EndBlendMode();
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
    /// Renders a gradient as a grid of triangles whose per-vertex color is <paramref name="colorAt"/>
    /// sampled at that vertex (used by the radial gradient; the linear one is <see cref="DrawLinearGradient"/>).
    /// The mesh covers exactly the
    /// rectangle (so it never bleeds outside it) and each pixel is painted once, so translucent
    /// stops do not accumulate alpha the way stacked concentric circles did. Colors interpolate
    /// straight (unpremultiplied) like Skia's default gradient shader. Vertices go through
    /// <paramref name="toWorld"/>, so the gradient rotates with the rectangle.
    /// </summary>
    private void DrawGradientGrid(Func<float, float, Vector2> toWorld, float w, float h,
        Func<float, float, Color> colorAt)
    {
        int columns = Math.Clamp((int)MathF.Ceiling(w / 4f), 1, 96);
        int rows = Math.Clamp((int)MathF.Ceiling(h / 4f), 1, 96);

        Rlgl.Begin((int)DrawMode.Triangles);
        for (int row = 0; row < rows; row++)
        {
            float y0 = h * row / rows;
            float y1 = h * (row + 1) / rows;
            for (int column = 0; column < columns; column++)
            {
                float x0 = w * column / columns;
                float x1 = w * (column + 1) / columns;
                Color c00 = colorAt(x0, y0);
                Color c10 = colorAt(x1, y0);
                Color c11 = colorAt(x1, y1);
                Color c01 = colorAt(x0, y1);
                Vector2 p00 = toWorld(x0, y0);
                Vector2 p10 = toWorld(x1, y0);
                Vector2 p11 = toWorld(x1, y1);
                Vector2 p01 = toWorld(x0, y1);
                // CCW front-facing under default culling.
                EmitVertexColored(p00, c00);
                EmitVertexColored(p11, c11);
                EmitVertexColored(p10, c10);
                EmitVertexColored(p00, c00);
                EmitVertexColored(p01, c01);
                EmitVertexColored(p11, c11);
            }
        }
        Rlgl.End();
    }

    private Func<float, float, Color> RadialColorAt(float w, float h)
    {
        float cx = EffectiveGradientX1;
        float cy = EffectiveGradientY1;
        float effectiveOuter = EffectiveGradientOuterRadius;
        float outer = effectiveOuter > 0f
            ? effectiveOuter
            : 0.5f * MathF.Sqrt(w * w + h * h);
        float inner = EffectiveGradientInnerRadius;
        float span = outer - inner;

        // Degenerate band (inner >= outer): whole shape reads as Color2.
        if (span <= 0f)
        {
            return (x, y) => Color2;
        }

        return (x, y) =>
        {
            float dx = x - cx;
            float dy = y - cy;
            return LerpClamped((MathF.Sqrt(dx * dx + dy * dy) - inner) / span);
        };
    }

    /// <summary>Vertices emitted by the last <see cref="DrawLinearGradient"/>; a test seam for the bound.</summary>
    internal int LastLinearGradientVertexCount { get; private set; }

    /// <summary>
    /// Draws the linear gradient as at most three convex pieces, split where the gradient reaches
    /// its start and end (t = 0 and t = 1). The blend is planar inside each piece, so per-vertex colors
    /// are exact and the clamp lands on the endpoints (#7307). The vertex count is at most
    /// 3 pieces x 4 triangles x 3 = 36 whatever the rectangle size, and nothing is allocated.
    /// <paramref name="tl"/>, <paramref name="tr"/> and <paramref name="bl"/> are the rotated
    /// world positions of the rectangle's corners.
    /// </summary>
    private void DrawLinearGradient(Vector2 tl, Vector2 tr, Vector2 bl, float w, float h)
    {
        float x1 = EffectiveGradientX1;
        float y1 = EffectiveGradientY1;
        float dx = EffectiveGradientX2 - x1;
        float dy = EffectiveGradientY2 - y1;
        float lenSq = dx * dx + dy * dy;

        // t = a x + b y + c in rectangle-local coordinates.
        float a = lenSq > 0f ? dx / lenSq : 0f;
        float b = lenSq > 0f ? dy / lenSq : 0f;
        // A zero length gradient is all Color2, like Skia's clamp.
        float c = lenSq > 0f ? -(x1 * dx + y1 * dy) / lenSq : 1f;

        Vector2 axisX = (tr - tl) / (w > 0f ? w : 1f);
        Vector2 axisY = (bl - tl) / (h > 0f ? h : 1f);

        Span<Vector2> source = stackalloc Vector2[8];
        Span<Vector2> first = stackalloc Vector2[8];
        Span<Vector2> second = stackalloc Vector2[8];
        source[0] = new Vector2(0f, 0f);
        source[1] = new Vector2(w, 0f);
        source[2] = new Vector2(w, h);
        source[3] = new Vector2(0f, h);

        int vertices = 0;
        Rlgl.Begin((int)DrawMode.Triangles);
        if (lenSq <= 0f)
        {
            vertices += EmitGradientFan(source, 4, tl, axisX, axisY, a, b, c);
        }
        else
        {
            // t <= 0
            int count = ClipHalfPlane(source, 4, first, -a, -b, -c);
            vertices += EmitGradientFan(first, count, tl, axisX, axisY, a, b, c);
            // 0 <= t <= 1
            count = ClipHalfPlane(source, 4, first, a, b, c);
            count = ClipHalfPlane(first, count, second, -a, -b, 1f - c);
            vertices += EmitGradientFan(second, count, tl, axisX, axisY, a, b, c);
            // t >= 1
            count = ClipHalfPlane(source, 4, first, a, b, c - 1f);
            vertices += EmitGradientFan(first, count, tl, axisX, axisY, a, b, c);
        }
        Rlgl.End();
        LastLinearGradientVertexCount = vertices;
    }

    // Sutherland-Hodgman against the half plane p.x * px + p.y * py + pc >= 0. Keeps the winding.
    private static int ClipHalfPlane(ReadOnlySpan<Vector2> input, int count, Span<Vector2> output,
        float px, float py, float pc)
    {
        int written = 0;
        for (int i = 0; i < count; i++)
        {
            Vector2 current = input[i];
            Vector2 next = input[(i + 1) % count];
            float dCurrent = (current.X * px) + (current.Y * py) + pc;
            float dNext = (next.X * px) + (next.Y * py) + pc;
            if (dCurrent >= 0f)
            {
                output[written++] = current;
            }
            if ((dCurrent >= 0f) != (dNext >= 0f))
            {
                float f = dCurrent / (dCurrent - dNext);
                output[written++] = current + ((next - current) * f);
            }
        }
        return written;
    }

    private int EmitGradientFan(ReadOnlySpan<Vector2> polygon, int count, Vector2 origin,
        Vector2 axisX, Vector2 axisY, float a, float b, float c)
    {
        if (count < 3)
        {
            return 0;
        }
        int emitted = 0;
        for (int i = 1; i < count - 1; i++)
        {
            // Local order (0, i + 1, i) is front-facing under default culling.
            EmitGradientVertex(polygon[0], origin, axisX, axisY, a, b, c);
            EmitGradientVertex(polygon[i + 1], origin, axisX, axisY, a, b, c);
            EmitGradientVertex(polygon[i], origin, axisX, axisY, a, b, c);
            emitted += 3;
        }
        return emitted;
    }

    private void EmitGradientVertex(Vector2 local, Vector2 origin, Vector2 axisX, Vector2 axisY,
        float a, float b, float c)
    {
        Color color = LerpClamped((a * local.X) + (b * local.Y) + c);
        EmitVertexColored(origin + (axisX * local.X) + (axisY * local.Y), color);
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
