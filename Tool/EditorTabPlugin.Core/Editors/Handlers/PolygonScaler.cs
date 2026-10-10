using System;
using System.Collections.Generic;
using System.Numerics;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// The scaled points of a <see cref="PolygonScaler.Scale"/> call, and how far the polygon's
/// position moves (in polygon-local axes) to keep the anchor fixed on screen.
/// </summary>
public readonly record struct PolygonScaleResult(Vector2[] Points, Vector2 LocalPositionShift);

/// <summary>
/// Scales a polygon's points from a bounding-box handle drag (#5934). Points scale about the
/// polygon's origin and the position shifts, so the anchor edge or corner stays put on screen
/// and the origin keeps its proportional place in the shape.
/// </summary>
public static class PolygonScaler
{
    private const float AxisAlignmentTolerance = 0.001f;

    /// <summary>The decimal places that scaled points and position shifts are rounded to.</summary>
    public const int DecimalPlaces = 3;

    /// <summary>
    /// Scales <paramref name="originalPoints"/> for a drag of the <paramref name="side"/> handle.
    /// Always computed from the points at the start of the drag, so a drag that shrinks to zero
    /// and grows back restores the original shape.
    /// </summary>
    /// <param name="originalPoints">The points when the drag began, relative to the polygon's position.</param>
    /// <param name="side">The grabbed handle.</param>
    /// <param name="draggedLocalOffset">Total cursor movement since the drag began, in polygon-local axes.</param>
    /// <param name="lockAspectRatio">If true, both axes scale by the larger of the two requested scales.</param>
    /// <param name="fromCenter">If true, scales about the bounding box center instead of the opposite edge or corner.</param>
    /// <param name="gridSize">If positive, the dragged edge snaps to the world grid. Only applies along
    /// axes that line up with a world axis (rotation is a multiple of 90 degrees).</param>
    /// <param name="originWorld">The polygon's world position when the drag began. Used for snapping.</param>
    /// <param name="rotation">The polygon's absolute rotation matrix. Used for snapping. Identity if null.</param>
    public static PolygonScaleResult Scale(IReadOnlyList<Vector2> originalPoints, ResizeSide side,
        Vector2 draggedLocalOffset, bool lockAspectRatio = false, bool fromCenter = false,
        float gridSize = 0, Vector2 originWorld = default, Matrix4x4? rotation = null)
    {
        Matrix4x4 rotationMatrix = rotation ?? Matrix4x4.Identity;

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        foreach (Vector2 point in originalPoints)
        {
            minX = MathF.Min(minX, point.X);
            minY = MathF.Min(minY, point.Y);
            maxX = MathF.Max(maxX, point.X);
            maxY = MathF.Max(maxY, point.Y);
        }

        bool movesLeft = side is ResizeSide.Left or ResizeSide.TopLeft or ResizeSide.BottomLeft;
        bool movesRight = side is ResizeSide.Right or ResizeSide.TopRight or ResizeSide.BottomRight;
        bool movesTop = side is ResizeSide.Top or ResizeSide.TopLeft or ResizeSide.TopRight;
        bool movesBottom = side is ResizeSide.Bottom or ResizeSide.BottomLeft or ResizeSide.BottomRight;

        AxisPlan x = CreateAxisPlan(minX, maxX, movesLeft, movesRight, fromCenter);
        AxisPlan y = CreateAxisPlan(minY, maxY, movesTop, movesBottom, fromCenter);

        float scaleX = 1;
        float scaleY = 1;

        if (x.IsDragged)
        {
            scaleX = GetScale(x, draggedLocalOffset.X);
        }
        if (y.IsDragged)
        {
            scaleY = GetScale(y, draggedLocalOffset.Y);
        }

        if (lockAspectRatio && (x.IsDragged || y.IsDragged))
        {
            bool useX = x.IsDragged && (!y.IsDragged || MathF.Abs(scaleX - 1) >= MathF.Abs(scaleY - 1));
            AxisPlan dominantPlan = useX ? x : y;
            float scale = useX ? scaleX : scaleY;

            scale = Snap(dominantPlan, scale, useX ? 0 : 1, gridSize, originWorld, rotationMatrix);
            scaleX = scale;
            scaleY = scale;

            // An axis that isn't being dragged scales about its center.
            if (!x.IsDragged)
            {
                x = x.WithAnchor((minX + maxX) / 2);
            }
            if (!y.IsDragged)
            {
                y = y.WithAnchor((minY + maxY) / 2);
            }
        }
        else
        {
            if (x.IsDragged)
            {
                scaleX = Snap(x, scaleX, 0, gridSize, originWorld, rotationMatrix);
            }
            if (y.IsDragged)
            {
                scaleY = Snap(y, scaleY, 1, gridSize, originWorld, rotationMatrix);
            }
        }

        Vector2[] scaledPoints = new Vector2[originalPoints.Count];
        for (int i = 0; i < scaledPoints.Length; i++)
        {
            scaledPoints[i] = new Vector2(
                RemoveFloatNoise(originalPoints[i].X * scaleX),
                RemoveFloatNoise(originalPoints[i].Y * scaleY));
        }

        Vector2 localPositionShift = new Vector2(
            RemoveFloatNoise((1 - scaleX) * x.Anchor),
            RemoveFloatNoise((1 - scaleY) * y.Anchor));

        return new PolygonScaleResult(scaledPoints, localPositionShift);
    }

    /// <summary>
    /// Rounds to <see cref="DecimalPlaces"/> places. A scale that is not exactly representable in
    /// a float leaves results like 30.999998 where the real answer is 31.
    /// </summary>
    public static float RemoveFloatNoise(float value) => MathF.Round(value, DecimalPlaces);

    private static AxisPlan CreateAxisPlan(float min, float max, bool movesMin, bool movesMax, bool fromCenter)
    {
        float center = (min + max) / 2;

        if (movesMin)
        {
            return new AxisPlan(IsDragged: true, Dragged: min, Anchor: fromCenter ? center : max);
        }
        if (movesMax)
        {
            return new AxisPlan(IsDragged: true, Dragged: max, Anchor: fromCenter ? center : min);
        }
        return new AxisPlan(IsDragged: false, Dragged: center, Anchor: center);
    }

    private static float GetScale(AxisPlan axis, float offset)
    {
        float extent = axis.Dragged - axis.Anchor;
        if (extent == 0)
        {
            return 1;
        }

        return MathF.Max(0, (axis.Dragged + offset - axis.Anchor) / extent);
    }

    /// <summary>
    /// Returns the scale that puts the dragged edge on the world grid, or <paramref name="scale"/>
    /// unchanged when snapping is off or the axis doesn't line up with a world axis.
    /// </summary>
    private static float Snap(AxisPlan axis, float scale, int localAxis, float gridSize,
        Vector2 originWorld, Matrix4x4 rotation)
    {
        float extent = axis.Dragged - axis.Anchor;
        if (gridSize <= 0 || extent == 0)
        {
            return scale;
        }

        Vector2 direction = localAxis == 0
            ? new Vector2(rotation.M11, rotation.M12)
            : new Vector2(rotation.M21, rotation.M22);

        int worldAxis;
        if (MathF.Abs(direction.X) > 1 - AxisAlignmentTolerance && MathF.Abs(direction.Y) < AxisAlignmentTolerance)
        {
            worldAxis = 0;
        }
        else if (MathF.Abs(direction.Y) > 1 - AxisAlignmentTolerance && MathF.Abs(direction.X) < AxisAlignmentTolerance)
        {
            worldAxis = 1;
        }
        else
        {
            return scale;
        }

        float directionComponent = worldAxis == 0 ? direction.X : direction.Y;
        float originComponent = worldAxis == 0 ? originWorld.X : originWorld.Y;

        float edgeLocal = axis.Anchor + scale * extent;
        float edgeWorld = originComponent + directionComponent * edgeLocal;
        float snappedWorld = GridSnapper.SnapRound(edgeWorld, gridSize);
        float snappedLocal = (snappedWorld - originComponent) / directionComponent;

        return MathF.Max(0, (snappedLocal - axis.Anchor) / extent);
    }

    private readonly record struct AxisPlan(bool IsDragged, float Dragged, float Anchor)
    {
        public AxisPlan WithAnchor(float anchor) => this with { Anchor = anchor };
    }
}
