using Gum.Converters;
using RenderingLibrary;
using SkiaSharp;
using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;

namespace SkiaGum.Renderables;

public class Polygon : RenderableShapeBase
{
    public bool IsClosed { get; set; } = true;

    public List<SKPoint> Points
    {
        get; set;
    }

    public GeneralUnitType PointXUnits { get; set; } = GeneralUnitType.PixelsFromSmall;
    public GeneralUnitType PointYUnits { get; set; } = GeneralUnitType.PixelsFromSmall;

    public Polygon()
    {
        // set up some default triangle:

        Points = new List<SKPoint>();
        Points.Add(new SKPoint(0, -4));
        Points.Add(new SKPoint(16, 0));
        Points.Add(new SKPoint(0, 4));

    }

    /// <summary>
    /// Replaces the current points with the supplied collection. Vector2-based overload
    /// added in issue #2757 so the shared <see cref="Gum.GueDeriving.PolygonRuntime"/>
    /// can call a uniform API across Skia / MonoGame / Raylib.
    /// </summary>
    public void SetPoints(ICollection<Vector2> points)
    {
        Points.Clear();
        if (points != null)
        {
            foreach (var p in points)
            {
                Points.Add(new SKPoint(p.X, p.Y));
            }
        }
    }

    public void InsertPointAt(Vector2 point, int index) =>
        Points.Insert(index, new SKPoint(point.X, point.Y));

    public void RemovePointAtIndex(int index) => Points.RemoveAt(index);

    public void SetPointAt(Vector2 point, int index) =>
        Points[index] = new SKPoint(point.X, point.Y);

    /// <summary>
    /// Ray-cast hit test in world space. Mirrors the same-named method on the MG/Raylib
    /// <c>LinePolygon</c> renderables so <see cref="Gum.GueDeriving.PolygonRuntime"/>
    /// can override <see cref="Gum.Wireframe.GraphicalUiElement.IsPointInside"/> uniformly.
    /// Inverse-rotates the world point into local space, applies the Percentage /
    /// PixelsFromSmall unit conversion used by <see cref="DrawBound"/>, then runs the
    /// standard even-odd ray cast against the polygon's edges.
    /// </summary>
    public bool IsPointInside(float worldX, float worldY)
    {
        if (Points == null || Points.Count < 3)
        {
            return false;
        }

        float ox = this.GetAbsoluteLeft();
        float oy = this.GetAbsoluteTop();

        float rotRad = this.GetAbsoluteRotation() * MathF.PI / 180f;
        float cos = MathF.Cos(rotRad);
        float sin = MathF.Sin(rotRad);

        // Inverse-rotate the world point into local space.
        float relX = worldX - ox;
        float relY = worldY - oy;
        float localX = relX * cos - relY * sin;
        float localY = relX * sin + relY * cos;

        float width = this.Width;
        float height = this.Height;

        int count = Points.Count;
        bool inside = false;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            ScalePoint(Points[i], width, height, out float xi, out float yi);
            ScalePoint(Points[j], width, height, out float xj, out float yj);

            if (((yi > localY) != (yj > localY)) &&
                (localX < (xj - xi) * (localY - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    void ScalePoint(SKPoint point, float width, float height, out float x, out float y)
    {
        x = PointXUnits == GeneralUnitType.Percentage ? point.X * width / 100f : point.X;
        y = PointYUnits == GeneralUnitType.Percentage ? point.Y * height / 100f : point.Y;
    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        SKPath path = new SKPath();

        var thisPosition = new SKPoint(boundingRect.Left, boundingRect.Top);

        GetXY(boundingRect, Points[0], out float x, out float y);

        path.MoveTo(new SKPoint(x, y) + thisPosition);

        for (int i = 1; i < Points.Count; i++)
        {
            var point = Points[i];
            GetXY(boundingRect, point, out x, out y);

            path.LineTo(new SKPoint(x, y) + thisPosition);
        }

        if (IsClosed)
        {
            path.LineTo(Points[0] + thisPosition);
            path.Close();
        }

        canvas.DrawPath(path, paint);
    }

    private void GetXY(SKRect boundingRect, SKPoint point, out float x, out float y)
    {
        x = point.X;
        y = point.Y;
        switch (PointXUnits)
        {
            case GeneralUnitType.PixelsFromSmall:
                break;
            case GeneralUnitType.Percentage:
                x = x * boundingRect.Width / 100.0f;
                break;
            default:
                throw new NotImplementedException($"Need to implement {PointXUnits}");
        }

        switch (PointYUnits)
        {
            case GeneralUnitType.PixelsFromSmall:
                break;
            case GeneralUnitType.Percentage:
                y = y * boundingRect.Height / 100.0f;
                break;
            default:
                throw new NotImplementedException($"Need to implement {PointYUnits}");

        }

    }
}
