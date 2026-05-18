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
    public void StrokeColor_DefaultsToWhite_MatchingSkia()
    {
        // Skia's CircleRuntime ctor seeds StrokeColor = SKColors.White so cells that set only
        // FillColor still render with a visible 1 px white outline (e.g. the gallery's Modes
        // and Alignment rows). Raylib must match or fill-only cells render without the outline
        // Skia draws. Same fix landed for RectangleRuntime in this PR.
        CircleRuntime sut = new();

        sut.StrokeColor.ShouldNotBeNull();
        sut.StrokeColor!.Value.R.ShouldBe((byte)255);
        sut.StrokeColor!.Value.G.ShouldBe((byte)255);
        sut.StrokeColor!.Value.B.ShouldBe((byte)255);
        sut.StrokeColor!.Value.A.ShouldBe((byte)255);
    }

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
    public void DashedStroke_RoundTrips_AndPushesToContainedRenderable()
    {
        // #2757 follow-up #10. Existing #if RAYLIB || SOKOL block previously held the values
        // as backing-field only; raylib now pushes to the renderable so the dashed render
        // path engages.
        CircleRuntime sut = new();

        sut.StrokeDashLength = 6f;
        sut.StrokeGapLength = 4f;

        sut.StrokeDashLength.ShouldBe(6f);
        sut.StrokeGapLength.ShouldBe(4f);
        LineCircle inner = (LineCircle)sut.RenderableComponent!;
        inner.StrokeDashLength.ShouldBe(6f);
        inner.StrokeGapLength.ShouldBe(4f);
    }

    [Fact]
    public void Dropshadow_RoundTrips_AndPushesToContainedRenderable()
    {
        // #2757 follow-up #12. Per-channel setters (Red/Green/Blue/Alpha) compose into
        // DropshadowColor and push to the renderable through its setter.
        CircleRuntime sut = new();

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

        LineCircle inner = (LineCircle)sut.RenderableComponent!;
        inner.HasDropshadow.ShouldBeTrue();
        inner.DropshadowColor.R.ShouldBe((byte)220);
        inner.DropshadowColor.G.ShouldBe((byte)40);
        inner.DropshadowOffsetX.ShouldBe(6f);
        inner.DropshadowBlurY.ShouldBe(4f);
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

    [Fact]
    public void GradientAxis_RoundTrips_AndPushesToContainedRenderable()
    {
        // #2757 follow-ups #8/#9 — six new axis + radius props pushed through to the
        // renderable for the rlgl triangle-fan render path.
        CircleRuntime sut = new();

        sut.GradientX1 = 4f;
        sut.GradientY1 = 8f;
        sut.GradientX2 = 56f;
        sut.GradientY2 = 28f;
        sut.GradientInnerRadius = 4f;
        sut.GradientOuterRadius = 28f;

        LineCircle inner = (LineCircle)sut.RenderableComponent!;
        inner.GradientX1.ShouldBe(4f);
        inner.GradientY1.ShouldBe(8f);
        inner.GradientX2.ShouldBe(56f);
        inner.GradientY2.ShouldBe(28f);
        inner.GradientInnerRadius.ShouldBe(4f);
        inner.GradientOuterRadius.ShouldBe(28f);
    }
}
