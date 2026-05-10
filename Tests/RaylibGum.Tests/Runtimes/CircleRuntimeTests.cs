using Gum.GueDeriving;
using Raylib_cs;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

public class CircleRuntimeTests : BaseTestClass
{
    [Fact]
    public void Alpha_ShouldDefaultTo255()
    {
        CircleRuntime sut = new();
        sut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Alpha_ShouldRoundTrip()
    {
        CircleRuntime sut = new();
        sut.Alpha = 128;
        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Blue_ShouldRoundTrip()
    {
        CircleRuntime sut = new();
        sut.Blue = 64;
        sut.Blue.ShouldBe(64);
    }

    [Fact]
    public void Color_ShouldDefaultToWhite()
    {
        CircleRuntime sut = new();
        sut.Color.ShouldBe(Color.White);
    }

    [Fact]
    public void Color_ShouldRoundTrip()
    {
        CircleRuntime sut = new();
        Color expected = new Color(10, 20, 30, 40);
        sut.Color = expected;
        sut.Color.ShouldBe(expected);
    }

    [Fact]
    public void Green_ShouldRoundTrip()
    {
        CircleRuntime sut = new();
        sut.Green = 32;
        sut.Green.ShouldBe(32);
    }

    [Fact]
    public void Radius_ShouldBe16_ByDefault()
    {
        CircleRuntime sut = new();
        sut.Radius.ShouldBe(16);
    }

    [Fact]
    public void Red_ShouldRoundTrip()
    {
        CircleRuntime sut = new();
        sut.Red = 200;
        sut.Red.ShouldBe(200);
    }

    [Fact]
    public void Visible_ShouldBeTrue_ByDefault()
    {
        CircleRuntime sut = new();
        sut.Visible.ShouldBeTrue();
    }
}
