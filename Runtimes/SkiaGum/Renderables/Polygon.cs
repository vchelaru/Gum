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

    GeneralUnitType _pointXUnits = GeneralUnitType.PixelsFromSmall;
    GeneralUnitType _pointYUnits = GeneralUnitType.PixelsFromSmall;

    /// <summary>
    /// How point X values are measured against the polygon's width: PixelsFromSmall (from the left), PixelsFromLarge
    /// (from the right), PixelsFromMiddle or Percentage. Any other unit is reported through
    /// <see cref="CustomSetPropertyOnRenderable.PropertyAssignmentError"/> and the current unit is kept.
    /// </summary>
    public GeneralUnitType PointXUnits
    {
        get => _pointXUnits;
        set
        {
            if (IsSupportedPointUnit(value, nameof(PointXUnits), _pointXUnits))
            {
                _pointXUnits = value;
            }
        }
    }

    /// <summary>
    /// How point Y values are measured against the polygon's height: PixelsFromSmall (from the top), PixelsFromLarge
    /// (from the bottom), PixelsFromMiddle or Percentage. Any other unit is reported through
    /// <see cref="CustomSetPropertyOnRenderable.PropertyAssignmentError"/> and the current unit is kept.
    /// </summary>
    public GeneralUnitType PointYUnits
    {
        get => _pointYUnits;
        set
        {
            if (IsSupportedPointUnit(value, nameof(PointYUnits), _pointYUnits))
            {
                _pointYUnits = value;
            }
        }
    }

    public Polygon()
    {
        // set up some default triangle:

        Points = new List<SKPoint>();
        Points.Add(new SKPoint(0, -4));
        Points.Add(new SKPoint(16, 0));
        Points.Add(new SKPoint(0, 4));

    }

    /// <inheritdoc/>
    public override object Clone()
    {
        Polygon clone = (Polygon)base.Clone();
        clone.Points = new List<SKPoint>(Points);
        return clone;
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

    public int PointCount => Points.Count;

    public Vector2 PointAt(int index)
    {
        SKPoint point = Points[index];
        return new Vector2(point.X, point.Y);
    }

    public void RemovePointAtIndex(int index) => Points.RemoveAt(index);

    public void SetPointAt(Vector2 point, int index) =>
        Points[index] = new SKPoint(point.X, point.Y);

    /// <summary>
    /// Ray-cast hit test in world space. Mirrors the same-named method on the MG/Raylib
    /// <c>LinePolygon</c> renderables so <see cref="Gum.GueDeriving.PolygonRuntime"/>
    /// can override <see cref="Gum.Wireframe.GraphicalUiElement.IsPointInside"/> uniformly.
    /// Inverse-rotates the world point into local space, applies the point unit
    /// conversion used by <see cref="DrawBound"/>, then runs the
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

    // Points are measured against the polygon's own size, the way a position is measured against its parent.
    void ScalePoint(SKPoint point, float width, float height, out float x, out float y) =>
        UnitConverter.Self.ConvertToPixelCoordinates(point.X, point.Y, PointXUnits, PointYUnits, width, height, 0, 0, out x, out y);

    // Units that compare a point to a file, a baseline or the other coordinate have no meaning for a polygon point.
    bool IsSupportedPointUnit(GeneralUnitType unit, string propertyName, GeneralUnitType current)
    {
        switch (unit)
        {
            case GeneralUnitType.PixelsFromSmall:
            case GeneralUnitType.PixelsFromLarge:
            case GeneralUnitType.PixelsFromMiddle:
            case GeneralUnitType.Percentage:
                return true;
            default:
                CustomSetPropertyOnRenderable.RaisePropertyAssignmentError(
                    $"Polygon {Name} cannot use {propertyName} {unit}; it keeps {current}.");
                return false;
        }
    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        using var builder = new SKPathBuilder();

        var thisPosition = new SKPoint(boundingRect.Left, boundingRect.Top);

        ScalePoint(Points[0], boundingRect.Width, boundingRect.Height, out float firstX, out float firstY);
        SKPoint first = new SKPoint(firstX, firstY) + thisPosition;

        builder.MoveTo(first);

        for (int i = 1; i < Points.Count; i++)
        {
            var point = Points[i];
            ScalePoint(point, boundingRect.Width, boundingRect.Height, out float x, out float y);

            builder.LineTo(new SKPoint(x, y) + thisPosition);
        }

        if (IsClosed)
        {
            builder.LineTo(first);
            builder.Close();
        }

        using SKPath path = builder.Detach();
        canvas.DrawPath(path, paint);
    }
}
