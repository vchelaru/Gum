using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
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

    // #2757: the raylib branch now surfaces the same property names as the XNALIKE/SKIA
    // branches so the shared CirclesScreen samples compile across backends. These tests pin
    // the round-trip + push-to-renderable contract.

    [Fact]
    public void FillColor_RoundTrips_AndPushesToContainedRenderable()
    {
        CircleRuntime sut = new();
        Color expected = new Color(10, 20, 30, 200);

        sut.FillColor = expected;

        sut.FillColor.ShouldNotBeNull();
        sut.FillColor!.Value.R.ShouldBe((byte)10);
        ((LineCircle)sut.RenderableComponent!).FillColor.ShouldNotBeNull();
        ((LineCircle)sut.RenderableComponent!).FillColor!.Value.R.ShouldBe((byte)10);
    }

    [Fact]
    public void StrokeColor_RoundTrips_AndPushesToContainedRenderable()
    {
        CircleRuntime sut = new();
        Color expected = new Color(40, 50, 60, 255);

        sut.StrokeColor = expected;

        sut.StrokeColor.ShouldNotBeNull();
        ((LineCircle)sut.RenderableComponent!).StrokeColor.ShouldNotBeNull();
        ((LineCircle)sut.RenderableComponent!).StrokeColor!.Value.G.ShouldBe((byte)50);
    }

    [Fact]
    public void StrokeWidth_RoundTrips_AndPushesToContainedRenderable()
    {
        CircleRuntime sut = new();

        sut.StrokeWidth = 5f;

        sut.StrokeWidth.ShouldBe(5f);
        ((LineCircle)sut.RenderableComponent!).StrokeWidth.ShouldBe(5f);
    }

    [Fact]
    public void Gradient_PropertiesRoundTrip_AndPushToContainedRenderable()
    {
        CircleRuntime sut = new();
        Color c1 = new Color(255, 0, 0, 255);
        Color c2 = new Color(0, 0, 255, 255);

        sut.UseGradient = true;
        sut.GradientType = GradientType.Radial;
        sut.Color1 = c1;
        sut.Color2 = c2;

        sut.UseGradient.ShouldBeTrue();
        sut.GradientType.ShouldBe(GradientType.Radial);
        LineCircle inner = (LineCircle)sut.RenderableComponent!;
        inner.UseGradient.ShouldBeTrue();
        inner.GradientType.ShouldBe(GradientType.Radial);
        inner.Color1.R.ShouldBe((byte)255);
        inner.Color2.B.ShouldBe((byte)255);
    }
}
