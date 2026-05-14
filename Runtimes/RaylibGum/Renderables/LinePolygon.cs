using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

public class LinePolygon : InvisibleRenderable
{
    private readonly List<Vector2> _points = new();

    public Color Color { get; set; } = Color.White;

    public int Alpha
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

    public bool IsDotted { get; set; }

    /// <summary>Length of each dash (and gap) in pixels when <see cref="IsDotted"/>.</summary>
    public float DashLength { get; set; } = 2f;

    public float LinePixelWidth { get; set; } = 1f;

    public IReadOnlyList<Vector2> Points => _points;

    public LinePolygon() : this(null) { }

    public LinePolygon(SystemManagers? _) { }

    public void SetPoints(ICollection<Vector2> points)
    {
        _points.Clear();
        if (points != null)
        {
            _points.AddRange(points);
        }
    }

    public void InsertPointAt(Vector2 point, int index) => _points.Insert(index, point);

    public void RemovePointAtIndex(int index) => _points.RemoveAt(index);

    public void SetPointAt(Vector2 point, int index) => _points[index] = point;

    public Vector2 PointAt(int index) => _points[index];

    public bool IsPointInside(float worldX, float worldY)
    {
        float ox = this.GetAbsoluteLeft();
        float oy = this.GetAbsoluteTop();

        int count = _points.Count;
        bool inside = false;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            float xi = _points[i].X + ox;
            float yi = _points[i].Y + oy;
            float xj = _points[j].X + ox;
            float yj = _points[j].Y + oy;

            if (((yi > worldY) != (yj > worldY)) &&
                (worldX < (xj - xi) * (worldY - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public override void Render(ISystemManagers managers)
    {
        if (!Visible || _points.Count < 2)
        {
            return;
        }

        float ox = this.GetAbsoluteLeft();
        float oy = this.GetAbsoluteTop();

        if (!IsDotted)
        {
            for (int i = 0; i < _points.Count - 1; i++)
            {
                DrawLineEx(
                    new Vector2(ox + _points[i].X, oy + _points[i].Y),
                    new Vector2(ox + _points[i + 1].X, oy + _points[i + 1].Y),
                    LinePixelWidth,
                    Color);
            }
        }
        else
        {
            for (int i = 0; i < _points.Count - 1; i++)
            {
                DrawDashedSegment(
                    ox + _points[i].X, oy + _points[i].Y,
                    ox + _points[i + 1].X, oy + _points[i + 1].Y);
            }
        }
    }

    private void DrawDashedSegment(float ax, float ay, float bx, float by)
    {
        float dx = bx - ax;
        float dy = by - ay;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.5f)
        {
            return;
        }

        float nx = dx / len;
        float ny = dy / len;
        float step = MathF.Max(DashLength, 1f);

        // Pattern: dash, gap, dash, gap — each segment is `step` long.
        for (float t = 0; t < len; t += step * 2)
        {
            float t2 = MathF.Min(t + step, len);
            DrawLineEx(
                new Vector2(ax + nx * t, ay + ny * t),
                new Vector2(ax + nx * t2, ay + ny * t2),
                LinePixelWidth,
                Color);
        }
    }
}
