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

    /// <summary>Radius in world-space pixels.</summary>
    public float Radius { get; set; }

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
    /// When <c>true</c>, the fill pass paints a centered radial gradient from <see cref="Color1"/>
    /// at the center to <see cref="Color2"/> at the rim, via raylib's <c>DrawCircleGradient</c>.
    /// Only <see cref="GradientType.Radial"/> is supported on raylib today — linear and
    /// offset-center radial require a custom rlgl triangle fan (see issue #2757 follow-ups).
    /// </summary>
    public bool UseGradient { get; set; }

    /// <summary>
    /// Gradient mode. Only <see cref="GradientType.Radial"/> renders on raylib in this pass;
    /// other values are accepted (the property round-trips) but fall back to a solid fill.
    /// </summary>
    public GradientType GradientType { get; set; }

    /// <summary>Inner color for a radial gradient (painted at the circle's center).</summary>
    public Color Color1 { get; set; } = Color.White;

    /// <summary>Outer color for a radial gradient (painted at the rim).</summary>
    public Color Color2 { get; set; } = Color.White;

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

        // Fill pass — runs when FillColor is set, or when IsFilled is true with no explicit
        // FillColor (legacy Color slot supplies the fill color). Mirrors Skia's two-slot
        // composition (#2790) where setting FillColor alone, StrokeColor alone, or both lights
        // up the appropriate layers.
        bool runFill = FillColor.HasValue || IsFilled;
        if (runFill)
        {
            Color fillColor = FillColor ?? Color;
            if (UseGradient && GradientType == GradientType.Radial)
            {
                // raylib's DrawCircleGradient interpolates Color1 at the center to Color2 at
                // the rim — no offset-center, no inner radius. Offset and inner-radius variants
                // need a custom rlgl triangle fan; tracked as a #2757 follow-up.
                DrawCircleGradient((int)cx, (int)cy, Radius, Color1, Color2);
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
        if (runStroke)
        {
            Color strokeColor = StrokeColor ?? Color;
            // Segment count scales with radius to keep the ring smooth at larger sizes; 36
            // is the floor for small circles to avoid visible faceting. DrawRing is used for
            // every stroke width (including 1 px) so the inset behavior stays uniform — the
            // legacy DrawCircleLines path centered the line on Radius, which bled outward.
            int segments = System.Math.Max(36, (int)(Radius * 2));
            float innerRadius = System.Math.Max(0f, Radius - StrokeWidth);
            DrawRing(new Vector2(cx, cy), innerRadius, Radius,
                startAngle: 0f, endAngle: 360f, segments, strokeColor);
        }
    }
}
