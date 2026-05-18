using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
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

    // #2757: raylib branch now surfaces the same property names as the XNALIKE/SKIA branches so
    // the shared RectanglesScreen sample compiles across backends. These tests pin the round-trip
    // + push-to-renderable contract.

    [Fact]
    public void FillColor_RoundTrips_AndPushesToContainedRenderable()
    {
        RectangleRuntime sut = new();
        Color expected = new Color(10, 20, 30, 200);

        sut.FillColor = expected;

        sut.FillColor.ShouldNotBeNull();
        sut.FillColor!.Value.R.ShouldBe((byte)10);
        ((LineRectangle)sut.RenderableComponent!).FillColor.ShouldNotBeNull();
        ((LineRectangle)sut.RenderableComponent!).FillColor!.Value.R.ShouldBe((byte)10);
    }

    [Fact]
    public void StrokeColor_RoundTrips_AndPushesToContainedRenderable()
    {
        RectangleRuntime sut = new();
        Color expected = new Color(40, 50, 60, 255);

        sut.StrokeColor = expected;

        sut.StrokeColor.ShouldNotBeNull();
        ((LineRectangle)sut.RenderableComponent!).StrokeColor.ShouldNotBeNull();
        ((LineRectangle)sut.RenderableComponent!).StrokeColor!.Value.G.ShouldBe((byte)50);
    }

    [Fact]
    public void IsFilledFlag_RoundTrips_AndPushesToContainedRenderable()
    {
        RectangleRuntime sut = new();

        sut.IsFilled = true;

        sut.IsFilled.ShouldBeTrue();
        ((LineRectangle)sut.RenderableComponent!).IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void StrokeWidth_PushesToContainedRenderable_AfterPreRender()
    {
        // StrokeWidth flows through PreRender so ScreenPixel scaling resolves against camera zoom.
        // With default Absolute units, value reaches the renderable verbatim.
        RectangleRuntime sut = new();
        sut.StrokeWidth = 5f;

        sut.PreRender();

        sut.StrokeWidth.ShouldBe(5f);
        ((LineRectangle)sut.RenderableComponent!).LinePixelWidth.ShouldBe(5f);
    }

    [Fact]
    public void DashedStroke_RoundTrips_AndPushesToContainedRenderable()
    {
        // #2757 follow-up — previously the raylib branch only flipped IsDotted (binary). Now
        // the actual lengths reach the renderable for the perimeter-walk dashed render path.
        RectangleRuntime sut = new();

        sut.StrokeDashLength = 6f;
        sut.StrokeGapLength = 4f;

        sut.StrokeDashLength.ShouldBe(6f);
        sut.StrokeGapLength.ShouldBe(4f);
        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        inner.StrokeDashLength.ShouldBe(6f);
        inner.StrokeGapLength.ShouldBe(4f);
    }

    [Fact]
    public void Dropshadow_RoundTrips_AndPushesToContainedRenderable()
    {
        RectangleRuntime sut = new();

        sut.HasDropshadow = true;
        sut.DropshadowRed = 220;
        sut.DropshadowGreen = 40;
        sut.DropshadowBlue = 160;
        sut.DropshadowAlpha = 220;
        sut.DropshadowOffsetX = 6f;
        sut.DropshadowOffsetY = 4f;
        sut.DropshadowBlurX = 6f;
        sut.DropshadowBlurY = 4f;

        sut.HasDropshadow.ShouldBeTrue();
        sut.DropshadowColor.R.ShouldBe((byte)220);
        sut.DropshadowColor.A.ShouldBe((byte)220);

        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        inner.HasDropshadow.ShouldBeTrue();
        inner.DropshadowColor.R.ShouldBe((byte)220);
        inner.DropshadowColor.G.ShouldBe((byte)40);
        inner.DropshadowOffsetX.ShouldBe(6f);
        inner.DropshadowBlurY.ShouldBe(4f);
    }

    [Fact]
    public void Gradient_PropertiesRoundTrip_AndPushToContainedRenderable()
    {
        RectangleRuntime sut = new();
        Color c1 = new Color(255, 0, 0, 255);
        Color c2 = new Color(0, 0, 255, 255);

        sut.UseGradient = true;
        sut.GradientType = GradientType.Radial;
        sut.Color1 = c1;
        sut.Color2 = c2;

        sut.UseGradient.ShouldBeTrue();
        sut.GradientType.ShouldBe(GradientType.Radial);
        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        inner.UseGradient.ShouldBeTrue();
        inner.GradientType.ShouldBe(GradientType.Radial);
        inner.Color1.R.ShouldBe((byte)255);
        inner.Color2.B.ShouldBe((byte)255);
    }

    [Fact]
    public void CornerRadius_RoundTrips_AndPushesToContainedRenderable()
    {
        // #2757 follow-up — uniform CornerRadius in pixels, matching Skia's RectangleRuntime
        // surface. raylib renderable handles the pixel→roundness conversion that DrawRectangleRounded
        // requires at draw time.
        RectangleRuntime sut = new();

        sut.CornerRadius = 8f;

        sut.CornerRadius.ShouldBe(8f);
        ((LineRectangle)sut.RenderableComponent!).CornerRadius.ShouldBe(8f);
    }

    [Fact]
    public void GradientAxis_RoundTrips_AndPushesToContainedRenderable()
    {
        RectangleRuntime sut = new();

        sut.GradientX1 = 4f;
        sut.GradientY1 = 8f;
        sut.GradientX2 = 56f;
        sut.GradientY2 = 28f;
        sut.GradientInnerRadius = 4f;
        sut.GradientOuterRadius = 28f;

        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        inner.GradientX1.ShouldBe(4f);
        inner.GradientY1.ShouldBe(8f);
        inner.GradientX2.ShouldBe(56f);
        inner.GradientY2.ShouldBe(28f);
        inner.GradientInnerRadius.ShouldBe(4f);
        inner.GradientOuterRadius.ShouldBe(28f);
    }
}
