using System;
using System.Collections.Generic;
using System.Numerics;

namespace Gum.Wireframe;

/// <summary>
/// Computes the rectangle drawn around a selected polygon.
/// </summary>
public static class PolygonOutlineBounds
{
    /// <summary>
    /// Returns the rectangle enclosing the polygon's points and origin. The rectangle's top-left
    /// is the pivot it is drawn rotated around, so rotating it by <paramref name="rotationDegrees"/>
    /// lines it up with the rotated polygon.
    /// </summary>
    /// <param name="points">The polygon's points, relative to its origin and unrotated.</param>
    /// <param name="originX">The polygon origin's absolute X.</param>
    /// <param name="originY">The polygon origin's absolute Y.</param>
    /// <param name="rotationDegrees">The polygon's absolute rotation in degrees.</param>
    public static (float X, float Y, float Width, float Height) Compute(
        IReadOnlyList<Vector2> points, float originX, float originY, float rotationDegrees)
    {
        float left = 0;
        float top = 0;
        float right = 0;
        float bottom = 0;

        foreach (Vector2 point in points)
        {
            left = Math.Min(left, point.X);
            top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X);
            bottom = Math.Max(bottom, point.Y);
        }

        // Same convention as the polygon's own rotation matrix: positive degrees is counterclockwise on screen.
        Vector2 topLeft = Vector2.Transform(
            new Vector2(left, top),
            Matrix3x2.CreateRotation(-rotationDegrees * MathF.PI / 180f));

        return (originX + topLeft.X, originY + topLeft.Y, right - left, bottom - top);
    }
}
