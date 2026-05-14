using Gum.GueDeriving;
using Raylib_cs;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

public class RectangleRuntimeTests : BaseTestClass
{
    [Fact]
    public void Alpha_ShouldDefaultTo255()
    {
        RectangleRuntime sut = new();
        sut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Alpha_ShouldRoundTrip()
    {
        RectangleRuntime sut = new();
        sut.Alpha = 128;
        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Blue_ShouldRoundTrip()
    {
        RectangleRuntime sut = new();
        sut.Blue = 64;
        sut.Blue.ShouldBe(64);
    }

    [Fact]
    public void Color_ShouldDefaultToWhite()
    {
        RectangleRuntime sut = new();
        sut.Color.ShouldBe(Color.White);
    }

    [Fact]
    public void Color_ShouldRoundTrip()
    {
        RectangleRuntime sut = new();
        Color expected = new Color(10, 20, 30, 40);
        sut.Color = expected;
        sut.Color.ShouldBe(expected);
    }

    [Fact]
    public void Green_ShouldRoundTrip()
    {
        RectangleRuntime sut = new();
        sut.Green = 32;
        sut.Green.ShouldBe(32);
    }

    [Fact]
    public void IsDotted_ShouldBeFalse_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.IsDotted.ShouldBeFalse();
    }

    [Fact]
    public void IsDotted_ShouldRoundTrip()
    {
        RectangleRuntime sut = new();
        sut.IsDotted = true;
        sut.IsDotted.ShouldBeTrue();
    }

    [Fact]
    public void LineWidth_ShouldBe1_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.LineWidth.ShouldBe(1);
    }

    [Fact]
    public void Red_ShouldRoundTrip()
    {
        RectangleRuntime sut = new();
        sut.Red = 200;
        sut.Red.ShouldBe(200);
    }

    [Fact]
    public void Visible_ShouldBeTrue_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.Visible.ShouldBeTrue();
    }
}
