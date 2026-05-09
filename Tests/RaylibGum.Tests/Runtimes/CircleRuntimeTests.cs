using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

public class CircleRuntimeTests : BaseTestClass
{
    [Fact]
    public void Alpha_ShouldDefaultTo255()
    {
        CircleRuntime cut = new();
        cut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Alpha_ShouldRoundTrip()
    {
        CircleRuntime cut = new();
        cut.Alpha = 128;
        cut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Radius_ShouldBe16_ByDefault()
    {
        CircleRuntime cut = new();
        cut.Radius.ShouldBe(16);
    }

    [Fact]
    public void Visible_ShouldBeTrue_ByDefault()
    {
        CircleRuntime cut = new();
        cut.Visible.ShouldBeTrue();
    }
}