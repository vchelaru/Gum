using Gum.GueDeriving;
using Raylib_cs;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

public class NineSliceRuntimeTests : BaseTestClass
{
    [Fact]
    public void Alpha_ShouldDefaultTo255()
    {
        NineSliceRuntime sut = new();
        sut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Alpha_ShouldRoundTrip()
    {
        NineSliceRuntime sut = new();
        sut.Alpha = 128;
        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Blue_ShouldRoundTrip()
    {
        NineSliceRuntime sut = new();
        sut.Blue = 64;
        sut.Blue.ShouldBe(64);
    }

    [Fact]
    public void Color_ShouldDefaultToWhite()
    {
        NineSliceRuntime sut = new();
        sut.Color.ShouldBe(Color.White);
    }

    [Fact]
    public void Color_ShouldRoundTrip()
    {
        NineSliceRuntime sut = new();
        Color expected = new Color(10, 20, 30, 40);
        sut.Color = expected;
        sut.Color.ShouldBe(expected);
    }

    [Fact]
    public void ExposeChildrenEvents_ShouldBeTrue()
    {
        NineSliceRuntime sut = new();
        sut.ExposeChildrenEvents.ShouldBeTrue();
    }

    [Fact]
    public void Green_ShouldRoundTrip()
    {
        NineSliceRuntime sut = new();
        sut.Green = 32;
        sut.Green.ShouldBe(32);
    }

    [Fact]
    public void HasEvents_ShouldDefaultToFalse()
    {
        NineSliceRuntime sut = new();
        sut.HasEvents.ShouldBeFalse();
    }

    [Fact]
    public void Height_ShouldDefaultTo100()
    {
        NineSliceRuntime sut = new();
        sut.Height.ShouldBe(100);
    }

    [Fact]
    public void Red_ShouldRoundTrip()
    {
        NineSliceRuntime sut = new();
        sut.Red = 200;
        sut.Red.ShouldBe(200);
    }

    [Fact]
    public void Visible_ShouldBeTrue_ByDefault()
    {
        NineSliceRuntime sut = new();
        sut.Visible.ShouldBeTrue();
    }

    [Fact]
    public void Width_ShouldDefaultTo100()
    {
        NineSliceRuntime sut = new();
        sut.Width.ShouldBe(100);
    }
}
