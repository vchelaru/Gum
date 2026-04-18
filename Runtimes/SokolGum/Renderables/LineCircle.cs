using RenderingLibrary;
using RenderingLibrary.Graphics;
using static Sokol.SGP;

namespace SokolGum.Renderables;

/// <summary>Where the (X, Y) coordinate refers to on a <see cref="LineCircle"/>.</summary>
public enum CircleOrigin
{
    /// <summary>(X, Y) is the centre point.</summary>
    Center,
    /// <summary>(X, Y) is the top-left of the circle's bounding box — centre is (X + Radius, Y + Radius).</summary>
    TopLeft,
}

/// <summary>
/// Stroked circle outline, approximated as an N-segment polygon and drawn
/// via sgp_draw_lines_strip. Segment count scales with radius so large
/// circles don't look faceted but tiny ones don't waste vertices.
/// </summary>
public sealed class LineCircle : RenderableBase, IRenderableIpso
{
    public float Radius { get; set; }
    public CircleOrigin CircleOrigin { get; set; } = CircleOrigin.Center;

    public Color Color = Color.White;

    public int Red   { get => Color.R; set => Color.R = (byte)value; }
    public int Green { get => Color.G; set => Color.G = (byte)value; }
    public int Blue  { get => Color.B; set => Color.B = (byte)value; }
    public new int Alpha { get => Color.A; set => Color.A = (byte)value; }
    int IRenderableIpso.Alpha => Color.A;

    public LineCircle() { }
    public LineCircle(SystemManagers? _) { }

    public override unsafe void Render(ISystemManagers? managers)
    {
        if (!Visible || Radius <= 0f) return;

        float cx = this.GetAbsoluteLeft();
        float cy = this.GetAbsoluteTop();
        if (CircleOrigin == CircleOrigin.TopLeft)
        {
            cx += Radius;
            cy += Radius;
        }

        // Scale segment count with radius. 16 minimum, 128 maximum — keeps
        // small circles cheap and large ones smooth.
        int segments = Math.Clamp((int)(Radius * 0.7f) + 12, 16, 128);

        // Use a points strip that closes the loop (segments + 1 points,
        // last == first). sgp_draw_lines_strip takes a span of points.
        Span<sgp_point> points = stackalloc sgp_point[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * MathF.Tau;
            points[i] = new sgp_point
            {
                x = cx + MathF.Cos(angle) * Radius,
                y = cy + MathF.Sin(angle) * Radius,
            };
        }

        sgp_set_color(Color.R / 255f, Color.G / 255f, Color.B / 255f, Color.A / 255f);
        fixed (sgp_point* p = points)
        {
            sgp_draw_lines_strip(in *p, (uint)points.Length);
        }
        sgp_reset_color();
    }
}
