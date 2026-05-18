using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

/// <summary>
/// Line-join style for <see cref="LinePolygon"/>. Controls how the renderer fills the corner
/// gap where two adjacent thick-stroke segments meet — raylib's <c>DrawLineEx</c> produces a
/// rectangle per segment, so without an explicit join the outside of every vertex shows a
/// triangular wedge gap at thick stroke widths.
/// </summary>
public enum LineJoinStyle
{
    /// <summary>No join — segments end as raylib draws them (visible wedge gaps at thick strokes).</summary>
    None,
    /// <summary>
    /// Round join — a filled circle of radius <c>StrokeWidth / 2</c> is drawn at each vertex,
    /// closing the wedge gap regardless of bend angle. Handles convex, concave, and acute
    /// corners uniformly with no miter-spike degenerate case. Default.
    /// </summary>
    Round,
}

/// <summary>
/// Raylib polygon renderable. Originally a 1 px stroke-only polyline via per-segment
/// <c>DrawLineEx</c> calls; extended in issue #2757 to honor an <see cref="IsClosed"/> flag
/// (auto-draws the final edge back to the first point), an optional explicit
/// <see cref="StrokeColor"/> slot, and per-segment dashed strokes driven by
/// <see cref="StrokeDashLength"/> + <see cref="StrokeGapLength"/> (Skia's
/// <c>SKPathEffect.CreateDash</c> analog) that walk a continuous arc-length across vertices so
/// the dash pattern doesn't restart at every corner.
/// </summary>
public class LinePolygon : InvisibleRenderable
{
    private readonly List<Vector2> _points = new();

    /// <summary>
    /// Legacy single-color slot used as the stroke color when <see cref="StrokeColor"/> is
    /// <c>null</c>. Defaults to white so the pre-#2757 outline path renders the same as before.
    /// </summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// Explicit stroke-pass color. When <c>null</c>, the stroke pass falls back to
    /// <see cref="Color"/>. The shared <c>PolygonRuntime</c>'s raylib branch pushes its
    /// <c>StrokeColor</c> through here so the cross-backend API matches Skia
    /// (#2757 — same two-slot pattern used by <see cref="LineCircle"/> and
    /// <see cref="LineRectangle"/>).
    /// </summary>
    public Color? StrokeColor { get; set; }

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
    /// Legacy binary-dotted flag. When <c>true</c>, the stroke renders as <see cref="DashLength"/>-px
    /// dashes with equal-length gaps. Superseded by the <see cref="StrokeDashLength"/> /
    /// <see cref="StrokeGapLength"/> pair (#2757) which support independent dash/gap lengths;
    /// kept for back-compat with callers that only know about <c>IsDotted</c>.
    /// </summary>
    public bool IsDotted { get; set; }

    /// <summary>Length of each dash (and gap) in pixels when <see cref="IsDotted"/>.</summary>
    public float DashLength { get; set; } = 2f;

    /// <summary>
    /// Length in world-space pixels of each dash segment. A value of 0 (the default) draws a
    /// solid stroke. Both <see cref="StrokeDashLength"/> and <see cref="StrokeGapLength"/> must
    /// be &gt; 0 for dashed rendering to engage. Issue #2757 — the dash walk carries an
    /// arc-length cursor across vertices so a corner doesn't reset the pattern (matches Skia's
    /// path-effect dashing).
    /// </summary>
    public float StrokeDashLength { get; set; }

    /// <summary>
    /// Length in world-space pixels of each gap between dashes. Ignored when
    /// <see cref="StrokeDashLength"/> is 0.
    /// </summary>
    public float StrokeGapLength { get; set; }

    public float LinePixelWidth { get; set; } = 1f;

    /// <summary>
    /// How vertices are filled where two adjacent thick-stroke segments meet. See
    /// <see cref="LineJoinStyle"/>. Defaults to <see cref="LineJoinStyle.Round"/>.
    /// </summary>
    public LineJoinStyle JoinStyle { get; set; } = LineJoinStyle.Round;

    /// <summary>
    /// When <c>true</c>, an extra segment is drawn from the last point back to the first so
    /// the polygon outline closes. Default <c>true</c> (matches Skia's <c>Polygon.IsClosed</c>);
    /// set <c>false</c> for open polylines. Callers do not need to repeat the first point at
    /// the end of <see cref="SetPoints"/> when <see cref="IsClosed"/> is <c>true</c>.
    /// </summary>
    public bool IsClosed { get; set; } = true;

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

        Color strokeColor = StrokeColor ?? Color;

        bool perSegmentDash = StrokeDashLength > 0f && StrokeGapLength > 0f;
        bool dashed = perSegmentDash || IsDotted;

        int segmentCount = IsClosed ? _points.Count : _points.Count - 1;

        if (!dashed)
        {
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 a = T(_points[i]);
                Vector2 b = T(_points[(i + 1) % _points.Count]);
                DrawLineEx(a, b, LinePixelWidth, strokeColor);
            }

            // Round line-join: fill the wedge gap at each vertex where two adjacent
            // DrawLineEx rectangles meet. Skipped for sub-pixel strokes (nothing to fill) and
            // when the user explicitly opts out via JoinStyle.None. For an open polyline the
            // two endpoints are not joined — that would visually be a round cap, which raylib
            // doesn't render today (DrawLineEx is butt-capped). Inner vertices i in [1,
            // _points.Count - 2] always need a join; the first/last vertex of a closed
            // polygon is the join between the last segment (last → 0) and the first segment
            // (0 → 1), so it's included only when IsClosed.
            if (JoinStyle == LineJoinStyle.Round && LinePixelWidth > 1.5f)
            {
                float joinRadius = LinePixelWidth * 0.5f;
                int joinStart = IsClosed ? 0 : 1;
                int joinEnd = IsClosed ? _points.Count : _points.Count - 1;
                for (int i = joinStart; i < joinEnd; i++)
                {
                    DrawCircleV(T(_points[i]), joinRadius, strokeColor);
                }
            }
        }
        else if (perSegmentDash)
        {
            // Carry an arc-length cursor across segments so the dash pattern is continuous at
            // each vertex. Skia's SKPathEffect.CreateDash does the same — without carry-over,
            // a hexagon corner whose two edges meet mid-dash would visibly restart the pattern.
            float dashLen = StrokeDashLength;
            float gapLen = StrokeGapLength;
            float pattern = dashLen + gapLen;
            float cursor = 0f;
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 a = T(_points[i]);
                Vector2 b = T(_points[(i + 1) % _points.Count]);
                cursor = DrawDashedSegmentContinuous(a, b, dashLen, pattern, cursor, strokeColor);
            }
        }
        else
        {
            // Legacy IsDotted path — equal-length dash/gap of DashLength px. Pattern restarts
            // at each segment (back-compat with pre-#2757 behavior).
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 a = T(_points[i]);
                Vector2 b = T(_points[(i + 1) % _points.Count]);
                DrawDashedSegment(a.X, a.Y, b.X, b.Y, strokeColor);
            }
        }
    }

    // Continuous-cursor variant for the per-segment dash path. Returns the cursor position
    // (mod pattern) for the next segment so the dash pattern carries through corners.
    private float DrawDashedSegmentContinuous(Vector2 a, Vector2 b, float dashLen, float pattern,
        float startCursor, Color color)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.0001f)
        {
            return startCursor;
        }

        float nx = dx / len;
        float ny = dy / len;

        // Walk t from 0..len in pattern-sized steps. Account for the partial dash that may
        // already be in progress when this segment begins (startCursor in [0, pattern)).
        float cursor = startCursor;
        float t = 0f;
        while (t < len)
        {
            // Position within the current pattern instance.
            float inPattern = cursor;
            float remainingInPattern = pattern - inPattern;

            if (inPattern < dashLen)
            {
                // Mid-dash: draw from t to min(t + (dashLen - inPattern), len).
                float dashRemaining = dashLen - inPattern;
                float drawEnd = MathF.Min(t + dashRemaining, len);
                DrawLineEx(
                    new Vector2(a.X + nx * t, a.Y + ny * t),
                    new Vector2(a.X + nx * drawEnd, a.Y + ny * drawEnd),
                    LinePixelWidth,
                    color);
            }
            // Advance t and cursor by the remaining pattern (or the rest of the segment,
            // whichever is shorter).
            float advance = MathF.Min(remainingInPattern, len - t);
            t += advance;
            cursor = (cursor + advance) % pattern;
        }
        return cursor;
    }

    private void DrawDashedSegment(float ax, float ay, float bx, float by, Color color)
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
                color);
        }
    }
}
