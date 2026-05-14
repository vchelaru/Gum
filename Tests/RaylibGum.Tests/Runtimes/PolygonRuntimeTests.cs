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

    [Fact]
    public void IsPointInside_ShouldReturnFalse_WhenPointIsOutsideDefaultPolygon()
    {
        PolygonRuntime sut = new();
        sut.IsPointInside(50, 50).ShouldBeFalse();
    }

    [Fact]
    public void IsPointInside_ShouldReturnTrue_WhenPointIsInsideDefaultPolygon()
    {
        PolygonRuntime sut = new();
        sut.IsPointInside(16, 16).ShouldBeTrue();
    }

    [Fact]
    public void LineWidth_ShouldBe1_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.LineWidth.ShouldBe(1);
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
