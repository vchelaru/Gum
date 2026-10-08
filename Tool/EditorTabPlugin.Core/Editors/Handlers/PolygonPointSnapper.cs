using System.Numerics;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Snaps a polygon point to the world-space grid.
/// </summary>
public static class PolygonPointSnapper
{
    /// <summary>
    /// Returns <paramref name="localPoint"/> moved so its world position is the nearest grid
    /// point. Snapping the local value instead would miss the grid whenever the polygon is
    /// off-grid or rotated.
    /// </summary>
    /// <param name="localPoint">The point relative to the polygon's origin.</param>
    /// <param name="polygonAbsolutePosition">The polygon's world position.</param>
    /// <param name="absoluteRotation">The polygon's absolute rotation matrix.</param>
    /// <param name="gridSize">The grid cell size. A non-positive size leaves the point unchanged.</param>
    public static Vector2 SnapToWorldGrid(Vector2 localPoint, Vector2 polygonAbsolutePosition,
        Matrix4x4 absoluteRotation, float gridSize)
    {
        // Same transform as LinePolygon.AbsolutePointAt.
        Vector2 world = localPoint.X * new Vector2(absoluteRotation.M11, absoluteRotation.M12) +
            localPoint.Y * new Vector2(absoluteRotation.M21, absoluteRotation.M22) +
            polygonAbsolutePosition;

        Vector2 snappedWorld = new Vector2(
            GridSnapper.SnapRound(world.X, gridSize),
            GridSnapper.SnapRound(world.Y, gridSize));

        if (!Matrix4x4.Invert(absoluteRotation, out Matrix4x4 inverse))
        {
            return localPoint;
        }

        Vector2 offset = snappedWorld - polygonAbsolutePosition;
        return offset.X * new Vector2(inverse.M11, inverse.M12) +
            offset.Y * new Vector2(inverse.M21, inverse.M22);
    }
}
