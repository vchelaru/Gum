using Gum.GueDeriving;
using Raylib_cs;
using Shouldly;
using System.Numerics;

namespace RaylibGum.Tests.Runtimes;

public class PolygonRuntimeTests : BaseTestClass
{
    [Fact]
    public void Alpha_ShouldDefaultTo255()
    {
        PolygonRuntime sut = new();
        sut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Alpha_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.Alpha = 128;
        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Blue_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.Blue = 64;
        sut.Blue.ShouldBe(64);
    }

    [Fact]
    public void Color_ShouldDefaultToWhite()
    {
        PolygonRuntime sut = new();
        sut.Color.ShouldBe(Color.White);
    }

    [Fact]
    public void Color_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        Color expected = new Color(10, 20, 30, 40);
        sut.Color = expected;
        sut.Color.ShouldBe(expected);
    }

    [Fact]
    public void Green_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.Green = 32;
        sut.Green.ShouldBe(32);
    }

    // IsDotted was obsoleted in #2757 (no cross-backend equivalent on Skia). Preserved on
    // MG/Raylib for back-compat — these tests pin that the legacy property still round-trips.
#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public void IsDotted_ShouldBeFalse_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.IsDotted.ShouldBeFalse();
    }

    [Fact]
    public void IsDotted_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.IsDotted = true;
        sut.IsDotted.ShouldBeTrue();
    }
#pragma warning restore CS0618

    [Fact]
    public void IsPointInside_ShouldReturnFalse_WhenPointIsOutsideDefaultPolygon()
    {
        PolygonRuntime sut = new();
        sut.IsPointInside(50, 50).ShouldBeFalse();
    }

    [Fact]
    public void IsPointInside_ShouldReturnFalse_WhenPointWasInsideBeforeRotation()
    {
        // Default polygon is a 0..32 square. After 90° CCW rotation around (0,0) the
        // occupied region moves to x∈[0,32], y∈[-32,0]. A point that was inside the
        // un-rotated square at (16,16) should now be outside.
        PolygonRuntime sut = new();
        sut.Rotation = 90;
        sut.IsPointInside(16, 16).ShouldBeFalse();
    }

    [Fact]
    public void IsPointInside_ShouldReturnTrue_WhenPointIsInsideDefaultPolygon()
    {
        PolygonRuntime sut = new();
        sut.IsPointInside(16, 16).ShouldBeTrue();
    }

    [Fact]
    public void IsPointInside_ShouldReturnTrue_WhenPointIsInsideRotatedPolygon()
    {
        // After 90° CCW, world (16,-16) maps to local (16,16) which is inside the default square.
        PolygonRuntime sut = new();
        sut.Rotation = 90;
        sut.IsPointInside(16, -16).ShouldBeTrue();
    }

    // LineWidth was obsoleted in #2757 in favor of StrokeWidth + StrokeWidthUnits, but the
    // legacy property is preserved on MG/Raylib for back-compat.
#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public void LineWidth_ShouldBe1_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.LineWidth.ShouldBe(1);
    }

    [Fact]
    public void LineWidth_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.LineWidth = 3f;
        sut.LineWidth.ShouldBe(3f);
    }
#pragma warning restore CS0618

    [Fact]
    public void StrokeWidth_ShouldBe1_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.StrokeWidth.ShouldBe(1);
    }

    [Fact]
    public void StrokeWidth_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.StrokeWidth = 4f;
        sut.StrokeWidth.ShouldBe(4f);
    }

    [Fact]
    public void Red_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.Red = 200;
        sut.Red.ShouldBe(200);
    }

    [Fact]
    public void SetPoints_ShouldUpdatePoints()
    {
        PolygonRuntime sut = new();
        Vector2[] points = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 16),
            new Vector2(16, 16),
            new Vector2(16, 0),
            new Vector2(0, 0),
        };
        sut.SetPoints(points);
        sut.IsPointInside(8, 8).ShouldBeTrue();
        sut.IsPointInside(20, 20).ShouldBeFalse();
    }

    [Fact]
    public void Visible_ShouldBeTrue_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.Visible.ShouldBeTrue();
    }
}
