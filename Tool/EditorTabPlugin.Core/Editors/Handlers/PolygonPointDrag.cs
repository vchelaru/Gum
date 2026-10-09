using System.Numerics;
using RenderingLibrary;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using Matrix = System.Numerics.Matrix4x4;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// One drag of a single <see cref="LinePolygon"/> point, from grab to release.
/// </summary>
public class PolygonPointDrag
{
    private readonly LinePolygon _polygon;
    private readonly int _index;
    private readonly bool _snapToGrid;
    private readonly float _gridSize;

    // Where the point would be with no snapping applied. Snapping from the point's current
    // position would read back the previous frame's snap and revert every small movement.
    private Vector2 _unsnappedPoint;

    public PolygonPointDrag(LinePolygon polygon, int index, bool snapToGrid, float gridSize)
    {
        _polygon = polygon;
        _index = index;
        _snapToGrid = snapToGrid;
        _gridSize = gridSize;
        _unsnappedPoint = polygon.PointAt(index);
    }

    /// <summary>
    /// Moves the point by a cursor movement in screen pixels.
    /// </summary>
    public void Apply(float xChange, float yChange, float zoom)
    {
        Matrix.Invert(_polygon.GetAbsoluteRotationMatrix(), out Matrix rotationMatrix);

        var rightVector = new Vector3(rotationMatrix.M11, rotationMatrix.M12, rotationMatrix.M13);
        var upVector = new Vector3(rotationMatrix.M21, rotationMatrix.M22, rotationMatrix.M23);

        var change = new Vector2(
            xChange * rightVector.X + yChange * upVector.X,
            xChange * rightVector.Y + yChange * upVector.Y) / zoom;

        Vector2 point;

        if (_snapToGrid)
        {
            _unsnappedPoint += change;
            point = PolygonPointSnapper.SnapToWorldGrid(_unsnappedPoint,
                new Vector2(_polygon.GetAbsoluteX(), _polygon.GetAbsoluteY()),
                _polygon.GetAbsoluteRotationMatrix(), _gridSize);
        }
        else
        {
            point = _polygon.PointAt(_index) + change;

            // Round to nearest pixel
            var roundMultiple = 1 / zoom;
            point.X = MathFunctions.RoundFloat(point.X, roundMultiple);
            point.Y = MathFunctions.RoundFloat(point.Y, roundMultiple);
        }

        var shouldSetFirstAndLast = (_index == 0 || _index == _polygon.PointCount - 1) &&
            _polygon.PointAt(0) == _polygon.PointAt(_polygon.PointCount - 1);

        if (shouldSetFirstAndLast)
        {
            _polygon.SetPointAt(point, 0);
            _polygon.SetPointAt(point, _polygon.PointCount - 1);
        }
        else
        {
            _polygon.SetPointAt(point, _index);
        }
    }
}
