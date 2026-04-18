using RenderingLibrary;
using RenderingLibrary.Graphics;
using static Sokol.SGP;

namespace SokolGum.Renderables;

/// <summary>
/// Stroked rectangle outline. Emits four line segments via sgp_draw_line.
/// When <see cref="IsDotted"/> is true the outline is rendered as a dashed
/// pattern by subdividing each edge into short on/off segments.
/// </summary>
public sealed class LineRectangle : RenderableBase
{
    public Color Color = Color.White;

    public int Red   { get => Color.R; set => Color.R = (byte)value; }
    public int Green { get => Color.G; set => Color.G = (byte)value; }
    public int Blue  { get => Color.B; set => Color.B = (byte)value; }
    public new int Alpha { get => Color.A; set => Color.A = (byte)value; }
    int IRenderableIpso.Alpha => Color.A;

    public bool IsDotted { get; set; }

    /// <summary>Length of each dash (and gap) in pixels when <see cref="IsDotted"/>.</summary>
    public float DashLength { get; set; } = 2f;

    public LineRectangle() { }
    public LineRectangle(SystemManagers? _) { }

    public override void Render(ISystemManagers? managers)
    {
        if (!Visible) return;

        var x = this.GetAbsoluteLeft();
        var y = this.GetAbsoluteTop();
        var w = this.Width;
        var h = this.Height;

        sgp_set_color(Color.R / 255f, Color.G / 255f, Color.B / 255f, Color.A / 255f);

        if (!IsDotted)
        {
            sgp_draw_line(x,     y,     x + w, y    );
            sgp_draw_line(x + w, y,     x + w, y + h);
            sgp_draw_line(x + w, y + h, x,     y + h);
            sgp_draw_line(x,     y + h, x,     y    );
        }
        else
        {
            DrawDashedSegment(x,     y,     x + w, y    );
            DrawDashedSegment(x + w, y,     x + w, y + h);
            DrawDashedSegment(x + w, y + h, x,     y + h);
            DrawDashedSegment(x,     y + h, x,     y    );
        }

        sgp_reset_color();
    }

    private void DrawDashedSegment(float ax, float ay, float bx, float by)
    {
        var dx = bx - ax;
        var dy = by - ay;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.5f) return;

        var nx = dx / len;
        var ny = dy / len;
        var step = MathF.Max(DashLength, 1f);

        // Pattern: dash, gap, dash, gap — each segment is `step` long.
        for (float t = 0; t < len; t += step * 2)
        {
            var t2 = MathF.Min(t + step, len);
            sgp_draw_line(ax + nx * t, ay + ny * t,
                          ax + nx * t2, ay + ny * t2);
        }
    }
}
