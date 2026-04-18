using System.Numerics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using static Sokol.SGP;

namespace SokolGum.Renderables;

/// <summary>
/// Stroked polyline from a caller-supplied list of points, rendered via
/// sgp_draw_lines_strip. Points are in the renderable's local space and
/// are offset by (X, Y) at draw time.
/// </summary>
public sealed class LinePolygon : RenderableBase
{
    private readonly List<Vector2> _points = new();

    public Color Color = Color.White;

    public int Red   { get => Color.R; set => Color.R = (byte)value; }
    public int Green { get => Color.G; set => Color.G = (byte)value; }
    public int Blue  { get => Color.B; set => Color.B = (byte)value; }
    public new int Alpha { get => Color.A; set => Color.A = (byte)value; }
    int IRenderableIpso.Alpha => Color.A;

    public LinePolygon() { }
    public LinePolygon(SystemManagers? _) { }

    public IReadOnlyList<Vector2> Points => _points;

    public void SetPoints(IEnumerable<Vector2> points)
    {
        _points.Clear();
        _points.AddRange(points);
    }

    public void AddPoint(Vector2 point) => _points.Add(point);
    public void ClearPoints() => _points.Clear();

    public override unsafe void Render(ISystemManagers? managers)
    {
        if (!Visible || _points.Count < 2) return;

        float ox = this.GetAbsoluteLeft();
        float oy = this.GetAbsoluteTop();

        Span<sgp_point> points = _points.Count <= 256
            ? stackalloc sgp_point[_points.Count]
            : new sgp_point[_points.Count];

        for (int i = 0; i < _points.Count; i++)
        {
            points[i] = new sgp_point
            {
                x = ox + _points[i].X,
                y = oy + _points[i].Y,
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
