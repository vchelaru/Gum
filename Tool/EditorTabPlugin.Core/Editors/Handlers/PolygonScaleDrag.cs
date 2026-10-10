using System.Numerics;
using RenderingLibrary;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using Matrix = System.Numerics.Matrix4x4;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// One drag of a <see cref="LinePolygon"/> bounding-box handle, from grab to release (#5934).
/// Every <see cref="Apply"/> scales the points captured at the start of the drag, so a drag that
/// shrinks to nothing and grows back restores the original shape.
/// </summary>
public class PolygonScaleDrag
{
    private readonly LinePolygon _polygon;
    private readonly ResizeSide _side;
    private readonly bool _snapToGrid;
    private readonly float _gridSize;

    private readonly Vector2[] _originalPoints;
    private readonly Vector2 _originWorld;
    private readonly Matrix _rotation;
    private readonly Matrix _inverseRotation;

    public PolygonScaleDrag(LinePolygon polygon, ResizeSide side, bool snapToGrid, float gridSize)
    {
        _polygon = polygon;
        _side = side;
        _snapToGrid = snapToGrid;
        _gridSize = gridSize;

        _originalPoints = new Vector2[polygon.PointCount];
        for (int i = 0; i < _originalPoints.Length; i++)
        {
            _originalPoints[i] = polygon.PointAt(i);
        }

        _originWorld = new Vector2(polygon.GetAbsoluteX(), polygon.GetAbsoluteY());
        _rotation = polygon.GetAbsoluteRotationMatrix();
        Matrix.Invert(_rotation, out _inverseRotation);
    }

    /// <summary>
    /// Scales the polygon's points for the cursor having moved <paramref name="totalWorldOffset"/>
    /// since the drag began.
    /// </summary>
    /// <returns>
    /// How far the polygon's position must be from where it was when the drag began, in world
    /// units, so the anchor stays fixed on screen.
    /// </returns>
    public Vector2 Apply(Vector2 totalWorldOffset, bool lockAspectRatio, bool fromCenter)
    {
        Vector2 localOffset = totalWorldOffset.X * new Vector2(_inverseRotation.M11, _inverseRotation.M12) +
            totalWorldOffset.Y * new Vector2(_inverseRotation.M21, _inverseRotation.M22);

        PolygonScaleResult result = PolygonScaler.Scale(_originalPoints, _side, localOffset,
            lockAspectRatio, fromCenter,
            gridSize: _snapToGrid ? _gridSize : 0, _originWorld, _rotation);

        for (int i = 0; i < result.Points.Length; i++)
        {
            _polygon.SetPointAt(result.Points[i], i);
        }

        return result.LocalPositionShift.X * new Vector2(_rotation.M11, _rotation.M12) +
            result.LocalPositionShift.Y * new Vector2(_rotation.M21, _rotation.M22);
    }
}
