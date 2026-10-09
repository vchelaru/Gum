using System;
using System.Numerics;
using RenderingLibrary;
using RenderingLibrary.Math.Geometry;

namespace Gum.Wireframe;

/// <inheritdoc cref="IPreciseHitTester"/>
public class PreciseHitTester : IPreciseHitTester
{
    public bool HasCursorOver(GraphicalUiElement element, float x, float y)
    {
        if (element.RenderableComponent is LinePolygon linePolygon)
        {
            return linePolygon.IsPointInside(x, y);
        }

        return element.HasCursorOver(x, y);
    }

    public bool IntersectsRectangle(GraphicalUiElement element, float left, float top, float right, float bottom)
    {
        if (element.RenderableComponent is LinePolygon linePolygon)
        {
            return linePolygon.IntersectsRectangle(left, top, right, bottom);
        }

        float rotation = element.GetAbsoluteRotation();
        if (rotation != 0)
        {
            return RotatedBoxIntersectsRectangle(element, rotation, left, top, right, bottom);
        }

        return !(element.GetAbsoluteRight() < left ||
                 element.GetAbsoluteLeft() > right ||
                 element.GetAbsoluteBottom() < top ||
                 element.GetAbsoluteTop() > bottom);
    }

    // Separating-axis test between the element's rotated box (rotated about its top-left, the
    // same convention as HasCursorOver) and the axis-aligned rectangle.
    private static bool RotatedBoxIntersectsRectangle(GraphicalUiElement element, float rotationDegrees, float left, float top, float right, float bottom)
    {
        float radians = -rotationDegrees * MathF.PI / 180f;
        Vector2 across = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
        Vector2 down = new Vector2(-across.Y, across.X);

        Vector2 origin = new Vector2(element.GetAbsoluteX(), element.GetAbsoluteY());
        Vector2 widthStep = across * element.Width;
        Vector2 heightStep = down * element.Height;
        Span<Vector2> boxCorners = stackalloc Vector2[]
        {
            origin,
            origin + widthStep,
            origin + widthStep + heightStep,
            origin + heightStep,
        };
        Span<Vector2> rectangleCorners = stackalloc Vector2[]
        {
            new Vector2(left, top),
            new Vector2(right, top),
            new Vector2(right, bottom),
            new Vector2(left, bottom),
        };
        Span<Vector2> axes = stackalloc Vector2[]
        {
            Vector2.UnitX,
            Vector2.UnitY,
            across,
            down,
        };

        foreach (Vector2 axis in axes)
        {
            GetProjectedRange(boxCorners, axis, out float boxMin, out float boxMax);
            GetProjectedRange(rectangleCorners, axis, out float rectangleMin, out float rectangleMax);
            if (boxMax < rectangleMin || rectangleMax < boxMin)
            {
                return false;
            }
        }

        return true;
    }

    private static void GetProjectedRange(ReadOnlySpan<Vector2> points, Vector2 axis, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;
        foreach (Vector2 point in points)
        {
            float projected = Vector2.Dot(point, axis);
            min = MathF.Min(min, projected);
            max = MathF.Max(max, projected);
        }
    }
}
