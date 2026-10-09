using System;
using System.Numerics;
using Gum.Wireframe.Editors.Handlers;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// #5889: dragging a polygon point snaps its absolute (world) position to the grid, not its
/// offset from the polygon.
/// </summary>
public class PolygonPointDragTests : BaseTestClass
{
    private readonly SystemManagers? _previousDefault;

    public PolygonPointDragTests()
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
        SystemManagers.Default = _previousDefault;
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
    public void Apply_WithSnapToGrid_ShouldSnapAbsolutePointToGrid_WhenPolygonIsOffGrid()
    {
        LinePolygon polygon = CreatePolygon(x: 3, y: 5);
        PolygonPointDrag drag = new PolygonPointDrag(polygon, 1, snapToGrid: true, gridSize: 16);

        drag.Apply(10, 10, zoom: 1);

        // Local (50, 10) is world (53, 15), which snaps to (48, 16), so local (45, 11).
        polygon.PointAt(1).ShouldBe(new Vector2(45, 11));
    }

    [Fact]
    public void Apply_WithSnapToGrid_ShouldKeepAccumulatingTheUnsnappedDrag_WhenEachFrameIsSmallerThanAGridCell()
    {
        LinePolygon polygon = CreatePolygon(x: 3, y: 0);
        PolygonPointDrag drag = new PolygonPointDrag(polygon, 1, snapToGrid: true, gridSize: 16);

        drag.Apply(10, 0, zoom: 1);
        polygon.PointAt(1).X.ShouldBe(45f); // world 53 -> 48
        drag.Apply(7, 0, zoom: 1);
        polygon.PointAt(1).X.ShouldBe(61f); // world 60 -> 64, not stuck at 48
        drag.Apply(7, 0, zoom: 1);
        polygon.PointAt(1).X.ShouldBe(61f); // world 67 -> 64
    }

    [Theory]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(-45)]
    public void Apply_WithSnapToGrid_ShouldSnapAbsolutePointToGrid_WhenPolygonIsRotatedAndOffGrid(float rotation)
    {
        LinePolygon polygon = CreatePolygon(x: 7.5f, y: -3, rotation);
        PolygonPointDrag drag = new PolygonPointDrag(polygon, 2, snapToGrid: true, gridSize: 16);

        drag.Apply(13, 9, zoom: 1);

        Vector2 world = polygon.AbsolutePointAt(2);
        (world.X / 16f).ShouldBe(MathF.Round(world.X / 16f), 0.001f);
        (world.Y / 16f).ShouldBe(MathF.Round(world.Y / 16f), 0.001f);
    }

    [Fact]
    public void Apply_WithoutSnapToGrid_ShouldMoveByTheCursorChange()
    {
        LinePolygon polygon = CreatePolygon(x: 3, y: 5);
        PolygonPointDrag drag = new PolygonPointDrag(polygon, 1, snapToGrid: false, gridSize: 16);

        drag.Apply(10, 10, zoom: 1);

        polygon.PointAt(1).ShouldBe(new Vector2(50, 10));
    }

    [Fact]
    public void Apply_WithSnapToGrid_ShouldMoveFirstAndLastTogether_WhenPolygonIsClosed()
    {
        LinePolygon polygon = CreatePolygon(x: 3, y: 5);
        PolygonPointDrag drag = new PolygonPointDrag(polygon, 0, snapToGrid: true, gridSize: 16);

        drag.Apply(10, 10, zoom: 1);

        // Local (10, 10) is world (13, 15), which snaps to (16, 16), so local (13, 11).
        polygon.PointAt(0).ShouldBe(new Vector2(13, 11));
        polygon.PointAt(4).ShouldBe(new Vector2(13, 11));
    }

    [Fact]
    public void Apply_ShouldDivideTheCursorChangeByZoom()
    {
        LinePolygon polygon = CreatePolygon(x: 0, y: 0);
        PolygonPointDrag drag = new PolygonPointDrag(polygon, 1, snapToGrid: false, gridSize: 16);

        drag.Apply(20, 0, zoom: 2);

        polygon.PointAt(1).ShouldBe(new Vector2(50, 0));
    }
}
