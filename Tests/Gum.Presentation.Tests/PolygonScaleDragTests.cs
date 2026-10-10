using System;
using System.Numerics;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Handlers;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// #5934: one scale drag of a <see cref="LinePolygon"/> from a bounding-box handle.
/// </summary>
public class PolygonScaleDragTests : BaseTestClass
{
    private readonly SystemManagers? _previousDefault;

    public PolygonScaleDragTests()
    {
        _previousDefault = SystemManagers.Default;
        SystemManagers.Default = new SystemManagers
        {
            Renderer = new Renderer(),
            ShapeManager = new ShapeManager()
        };
        SystemManagers.Default.ShapeManager.Managers = SystemManagers.Default;
        SystemManagers.Default.Renderer.AddLayer();
    }

    public override void Dispose()
    {
        SystemManagers.Default = _previousDefault!;
        base.Dispose();
    }

    private static LinePolygon CreatePolygon(float x, float y, float rotation = 0)
    {
        LinePolygon polygon = new LinePolygon();
        polygon.SetPoints(new[]
        {
            new Vector2(0, 0), new Vector2(40, 0), new Vector2(40, 40), new Vector2(0, 40), new Vector2(0, 0)
        });
        polygon.X = x;
        polygon.Y = y;
        polygon.Rotation = rotation;
        return polygon;
    }

    [Fact]
    public void Apply_ShouldScalePointsAndReturnZeroShift_WhenRightEdgeIsDragged()
    {
        LinePolygon polygon = CreatePolygon(x: 3, y: 5);
        PolygonScaleDrag drag = new PolygonScaleDrag(polygon, ResizeSide.Right, snapToGrid: false, gridSize: 16);

        Vector2 shift = drag.Apply(new Vector2(40, 0), lockAspectRatio: false, fromCenter: false);

        polygon.PointAt(1).ShouldBe(new Vector2(80, 0));
        polygon.PointAt(2).ShouldBe(new Vector2(80, 40));
        shift.ShouldBe(Vector2.Zero);
    }

    [Fact]
    public void Apply_ShouldReturnWorldShift_WhenLeftEdgeIsDragged()
    {
        LinePolygon polygon = CreatePolygon(x: 3, y: 5);
        PolygonScaleDrag drag = new PolygonScaleDrag(polygon, ResizeSide.Left, snapToGrid: false, gridSize: 16);

        Vector2 shift = drag.Apply(new Vector2(-40, 0), lockAspectRatio: false, fromCenter: false);

        polygon.PointAt(1).ShouldBe(new Vector2(80, 0));
        shift.ShouldBe(new Vector2(-40, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(-45)]
    public void Apply_ShouldKeepAnchorFixedAndLetDraggedEdgeFollowCursor_WhenRotated(float rotation)
    {
        LinePolygon polygon = CreatePolygon(x: 7, y: -3, rotation);
        Vector2 anchorBefore = polygon.AbsolutePointAt(1);
        Vector2 draggedBefore = polygon.AbsolutePointAt(0);
        PolygonScaleDrag drag = new PolygonScaleDrag(polygon, ResizeSide.Left, snapToGrid: false, gridSize: 16);

        // Cursor moves 20 units along the polygon's own -x axis, expressed in world axes.
        Vector2 localAxis = new Vector2(polygon.GetAbsoluteRotationMatrix().M11, polygon.GetAbsoluteRotationMatrix().M12);
        Vector2 worldOffset = -20 * localAxis;

        Vector2 shift = drag.Apply(worldOffset, lockAspectRatio: false, fromCenter: false);
        polygon.X += shift.X;
        polygon.Y += shift.Y;

        // The right edge (anchor) did not move. After scaling, the old right edge is point 1.
        polygon.AbsolutePointAt(1).X.ShouldBe(anchorBefore.X, 0.001f);
        polygon.AbsolutePointAt(1).Y.ShouldBe(anchorBefore.Y, 0.001f);
        // The left edge is exactly where the cursor went.
        polygon.AbsolutePointAt(0).X.ShouldBe(draggedBefore.X + worldOffset.X, 0.001f);
        polygon.AbsolutePointAt(0).Y.ShouldBe(draggedBefore.Y + worldOffset.Y, 0.001f);
    }

    [Fact]
    public void Apply_ShouldRestoreOriginalShape_WhenShrunkToNothingAndGrownBack()
    {
        LinePolygon polygon = CreatePolygon(x: 0, y: 0);
        PolygonScaleDrag drag = new PolygonScaleDrag(polygon, ResizeSide.BottomRight, snapToGrid: false, gridSize: 16);

        drag.Apply(new Vector2(-500, -500), lockAspectRatio: false, fromCenter: false);
        polygon.PointAt(2).ShouldBe(Vector2.Zero);

        drag.Apply(new Vector2(0, 0), lockAspectRatio: false, fromCenter: false);
        polygon.PointAt(2).ShouldBe(new Vector2(40, 40));
    }

    [Fact]
    public void Apply_WithSnapToGrid_ShouldPutDraggedEdgeOnTheWorldGrid_WhenPolygonIsOffGrid()
    {
        LinePolygon polygon = CreatePolygon(x: 3, y: 5);
        PolygonScaleDrag drag = new PolygonScaleDrag(polygon, ResizeSide.Right, snapToGrid: true, gridSize: 16);

        // Right edge starts at world x = 43; 43 + 10 = 53 snaps to 48, so local 45.
        drag.Apply(new Vector2(10, 0), lockAspectRatio: false, fromCenter: false);

        polygon.PointAt(1).X.ShouldBe(45, 0.001f);
    }
}
