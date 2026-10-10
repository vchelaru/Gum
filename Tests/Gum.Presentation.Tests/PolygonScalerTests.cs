using System;
using System.Numerics;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Handlers;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// #5934: dragging a bounding-box handle scales every polygon point and shifts the polygon's
/// position so the anchor edge stays fixed on screen.
/// </summary>
public class PolygonScalerTests
{
    // Local x runs -1..3 (4 wide) and y runs 0..2 (2 tall), as in the design on the issue.
    private static readonly Vector2[] _points =
    {
        new Vector2(-1, 0), new Vector2(3, 0), new Vector2(3, 2), new Vector2(-1, 2), new Vector2(-1, 0)
    };

    [Fact]
    public void Scale_ShouldScalePointsAndShiftPosition_WhenRightEdgeIsDraggedOut()
    {
        PolygonScaleResult result = PolygonScaler.Scale(_points, ResizeSide.Right, new Vector2(4, 0));

        result.Points[0].ShouldBe(new Vector2(-2, 0));
        result.Points[1].ShouldBe(new Vector2(6, 0));
        result.Points[2].ShouldBe(new Vector2(6, 2));
        result.LocalPositionShift.ShouldBe(new Vector2(1, 0));
    }

    [Fact]
    public void Scale_ShouldKeepRightEdgeFixed_WhenLeftEdgeIsDraggedOut()
    {
        PolygonScaleResult result = PolygonScaler.Scale(_points, ResizeSide.Left, new Vector2(-4, 0));

        // Right edge was at local 3; after the shift it must still be at 3 + shift - shift.
        result.Points[1].X.ShouldBe(6);
        result.LocalPositionShift.X.ShouldBe(-3);
        (result.Points[1].X + result.LocalPositionShift.X).ShouldBe(3);
        (result.Points[0].X + result.LocalPositionShift.X).ShouldBe(-5);
    }

    [Fact]
    public void Scale_ShouldScaleBothAxesIndependently_WhenCornerIsDraggedWithoutLock()
    {
        PolygonScaleResult result = PolygonScaler.Scale(_points, ResizeSide.BottomRight, new Vector2(4, 1));

        result.Points[2].ShouldBe(new Vector2(6, 3));
    }

    [Fact]
    public void Scale_ShouldUseTheDominantAxisForBoth_WhenAspectRatioIsLocked()
    {
        // x asks for 2x, y asks for 1.5x. The larger change wins.
        PolygonScaleResult result = PolygonScaler.Scale(_points, ResizeSide.BottomRight, new Vector2(4, 1),
            lockAspectRatio: true);

        result.Points[2].ShouldBe(new Vector2(6, 4));
    }

    [Fact]
    public void Scale_ShouldScaleTheOtherAxisAboutItsCenter_WhenAspectRatioIsLockedOnAnEdge()
    {
        PolygonScaleResult result = PolygonScaler.Scale(_points, ResizeSide.Right, new Vector2(4, 0),
            lockAspectRatio: true);

        // Height doubles from 2 to 4; its center (y = 1) stays put in local space after the shift.
        result.Points[2].Y.ShouldBe(4);
        (result.Points[2].Y + result.LocalPositionShift.Y).ShouldBe(3);
        (result.Points[0].Y + result.LocalPositionShift.Y).ShouldBe(-1);
    }

    [Fact]
    public void Scale_ShouldScaleAboutTheCenter_WhenFromCenter()
    {
        // Box center is x = 1. Dragging the right edge out by 2 doubles the width.
        PolygonScaleResult result = PolygonScaler.Scale(_points, ResizeSide.Right, new Vector2(2, 0),
            fromCenter: true);

        result.Points[0].X.ShouldBe(-2);
        result.Points[1].X.ShouldBe(6);
        // Left edge moves from -1 to -3 and right edge from 3 to 5, about the center at 1.
        (result.Points[0].X + result.LocalPositionShift.X).ShouldBe(-3);
        (result.Points[1].X + result.LocalPositionShift.X).ShouldBe(5);
    }

    [Fact]
    public void Scale_ShouldCollapseToTheAnchor_WhenDraggedPastTheOppositeEdge()
    {
        PolygonScaleResult result = PolygonScaler.Scale(_points, ResizeSide.Right, new Vector2(-100, 0));

        foreach (Vector2 point in result.Points)
        {
            point.X.ShouldBe(0);
        }
        // Anchor was the left edge at -1; it stays there after the shift.
        result.LocalPositionShift.X.ShouldBe(-1);
    }

    [Fact]
    public void Scale_ShouldLeaveAxisUnchanged_WhenPointsHaveNoExtentOnThatAxis()
    {
        Vector2[] flat = { new Vector2(0, 5), new Vector2(0, 9) };

        PolygonScaleResult result = PolygonScaler.Scale(flat, ResizeSide.Right, new Vector2(10, 0));

        result.Points[0].ShouldBe(new Vector2(0, 5));
        result.Points[1].ShouldBe(new Vector2(0, 9));
        result.LocalPositionShift.ShouldBe(Vector2.Zero);
    }

    [Fact]
    public void Scale_ShouldNotModifyTheOriginalPoints()
    {
        PolygonScaler.Scale(_points, ResizeSide.Right, new Vector2(4, 0));

        _points[1].ShouldBe(new Vector2(3, 0));
    }

    [Fact]
    public void Scale_ShouldSnapDraggedEdgeToWorldGrid_WhenGridSizeIsSet()
    {
        // Origin is at world x = 1, so the right edge starts at world 4. Dragging 3.2 puts it at
        // world 7.2, which snaps to 8: local 7.
        PolygonScaleResult result = PolygonScaler.Scale(_points, ResizeSide.Right, new Vector2(3.2f, 0),
            gridSize: 4, originWorld: new Vector2(1, 0), rotation: Matrix4x4.Identity);

        result.Points[1].X.ShouldBe(6, 0.0001f);
        (result.Points[1].X + result.LocalPositionShift.X + 1).ShouldBe(8, 0.0001f);
    }

    [Fact]
    public void Scale_ShouldSnapAlongWorldYAxis_WhenRotatedNinetyDegrees()
    {
        // Rotated 90 degrees, local +x points along world +y. Right edge is at world y = 3.
        Matrix4x4 rotation = Matrix4x4.CreateRotationZ(MathF.PI / 2);

        PolygonScaleResult result = PolygonScaler.Scale(_points, ResizeSide.Right, new Vector2(3.2f, 0),
            gridSize: 4, originWorld: Vector2.Zero, rotation: rotation);

        // World y 6.2 snaps to 8, so local x 8: scale = (8 - -1) / 4 = 2.25.
        result.Points[1].X.ShouldBe(3 * 2.25f, 0.001f);
    }

    [Fact]
    public void Scale_ShouldNotSnap_WhenRotationIsNotAMultipleOfNinetyDegrees()
    {
        Matrix4x4 rotation = Matrix4x4.CreateRotationZ(MathF.PI / 6);

        PolygonScaleResult result = PolygonScaler.Scale(_points, ResizeSide.Right, new Vector2(3.2f, 0),
            gridSize: 4, originWorld: Vector2.Zero, rotation: rotation);

        // Unsnapped: scale = (3 + 3.2 + 1) / 4 = 1.8.
        result.Points[1].X.ShouldBe(3 * 1.8f, 0.001f);
    }
}
