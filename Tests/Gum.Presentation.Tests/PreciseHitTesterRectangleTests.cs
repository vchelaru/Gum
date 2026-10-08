using System.Numerics;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;

namespace Gum.Presentation.Tests;

public class PreciseHitTesterRectangleTests : BaseTestClass
{
    private readonly SystemManagers? _previousDefault;

    public PreciseHitTesterRectangleTests()
    {
        _previousDefault = SystemManagers.Default;
        SystemManagers.Default = new SystemManagers
        {
            Renderer = new Renderer(),
            ShapeManager = new ShapeManager(),
            TextManager = new TextManager()
        };
    }

    public override void Dispose()
    {
        SystemManagers.Default = _previousDefault;
        base.Dispose();
    }

    // L shape: a 20px-thick arm along the top and another down the left, with an empty notch
    // in the lower right of its 100x100 bounding box.
    private static GraphicalUiElement CreateLShapedPolygon()
    {
        LinePolygon polygon = new LinePolygon();
        polygon.SetPoints(new[]
        {
            new Vector2(0, 0),
            new Vector2(100, 0),
            new Vector2(100, 20),
            new Vector2(20, 20),
            new Vector2(20, 100),
            new Vector2(0, 100),
        });
        return new GraphicalUiElement(polygon);
    }

    [Fact]
    public void IntersectsRectangle_Polygon_ReturnsFalse_WhenRectangleOnlyOverlapsBoundingBox()
    {
        GraphicalUiElement element = CreateLShapedPolygon();

        bool result = new PreciseHitTester().IntersectsRectangle(element, 50, 50, 90, 90);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IntersectsRectangle_Polygon_ReturnsTrue_WhenRectangleCrossesAnEdge()
    {
        GraphicalUiElement element = CreateLShapedPolygon();

        bool result = new PreciseHitTester().IntersectsRectangle(element, 10, 10, 30, 30);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IntersectsRectangle_Polygon_ReturnsTrue_WhenRectangleIsInsideTheShape()
    {
        GraphicalUiElement element = CreateLShapedPolygon();

        bool result = new PreciseHitTester().IntersectsRectangle(element, 2, 2, 8, 8);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IntersectsRectangle_Polygon_ReturnsTrue_WhenRectangleContainsTheShape()
    {
        GraphicalUiElement element = CreateLShapedPolygon();

        bool result = new PreciseHitTester().IntersectsRectangle(element, -10, -10, 200, 200);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IntersectsRectangle_Polygon_ReturnsFalse_WhenRectangleIsOutsideBoundingBox()
    {
        GraphicalUiElement element = CreateLShapedPolygon();

        bool result = new PreciseHitTester().IntersectsRectangle(element, 200, 200, 300, 300);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IntersectsRectangle_NonPolygon_UsesBounds()
    {
        GraphicalUiElement element = new GraphicalUiElement(new InvisibleRenderable())
        {
            X = 0,
            Y = 0,
            Width = 100,
            Height = 100,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
        };
        PreciseHitTester tester = new PreciseHitTester();

        tester.IntersectsRectangle(element, 50, 50, 150, 150).ShouldBeTrue();
        tester.IntersectsRectangle(element, 150, 150, 200, 200).ShouldBeFalse();
    }
}
