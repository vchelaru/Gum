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

        float rotRad = this.GetAbsoluteRotation() * MathF.PI / 180f;
        float cos = MathF.Cos(rotRad);
        float sin = MathF.Sin(rotRad);

        // Inverse-rotate the world point into local space for the ray cast.
        float relX = worldX - ox;
        float relY = worldY - oy;
        float localX = relX * cos - relY * sin;
        float localY = relX * sin + relY * cos;

        int count = _points.Count;
        bool inside = false;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            float xi = _points[i].X;
            float yi = _points[i].Y;
            float xj = _points[j].X;
            float yj = _points[j].Y;

            if (((yi > localY) != (yj > localY)) &&
                (localX < (xj - xi) * (localY - yi) / (yj - yi) + xi))
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

        float rotRad = this.GetAbsoluteRotation() * MathF.PI / 180f;
        float cos = MathF.Cos(rotRad);
        float sin = MathF.Sin(rotRad);

        // Rotate a local point into world space around (ox, oy).
        Vector2 T(Vector2 p) =>
            new Vector2(ox + p.X * cos + p.Y * sin, oy - p.X * sin + p.Y * cos);

        if (!IsDotted)
        {
            for (int i = 0; i < _points.Count - 1; i++)
            {
                DrawLineEx(T(_points[i]), T(_points[i + 1]), LinePixelWidth, Color);
            }
        }
        else
        {
            for (int i = 0; i < _points.Count - 1; i++)
            {
                Vector2 a = T(_points[i]);
                Vector2 b = T(_points[i + 1]);
                DrawDashedSegment(a.X, a.Y, b.X, b.Y);
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
