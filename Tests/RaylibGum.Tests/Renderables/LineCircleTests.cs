using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #2757 — verifies the property surface added to <see cref="LineCircle"/> so the raylib
/// renderable can render filled disks, thick strokes, and centered radial gradients in addition
/// to its original 1 px outline. These tests cover defaults and property round-trip only; actual
/// rendering paths call native raylib Draw* functions that require a GL context and are validated
/// visually in the raylib sample's CirclesScreen.
/// </summary>
public class LineCircleTests
{
    [Fact]
    public void Defaults_PreserveLegacyOutlineBehavior()
    {
        LineCircle circle = new LineCircle();

        circle.IsFilled.ShouldBeFalse();
        circle.StrokeWidth.ShouldBe(1f);
        circle.FillColor.ShouldBeNull();
        circle.StrokeColor.ShouldBeNull();
        circle.UseGradient.ShouldBeFalse();
        circle.Color.R.ShouldBe((byte)255);
        circle.Color.G.ShouldBe((byte)255);
        circle.Color.B.ShouldBe((byte)255);
        circle.Color.A.ShouldBe((byte)255);
    }

    [Fact]
    public void IsFilled_RoundTrips()
    {
        LineCircle circle = new LineCircle();

        circle.IsFilled = true;

        circle.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void StrokeWidth_RoundTrips()
    {
        LineCircle circle = new LineCircle();

        circle.StrokeWidth = 4.5f;

        circle.StrokeWidth.ShouldBe(4.5f);
    }

    [Fact]
    public void FillColor_RoundTripsNullableColor()
    {
        LineCircle circle = new LineCircle();
        Color expected = new Color(10, 20, 30, 40);

        circle.FillColor = expected;

        circle.FillColor.ShouldNotBeNull();
        circle.FillColor!.Value.R.ShouldBe((byte)10);
        circle.FillColor!.Value.A.ShouldBe((byte)40);

        circle.FillColor = null;
        circle.FillColor.ShouldBeNull();
    }

    [Fact]
    public void StrokeColor_RoundTripsNullableColor()
    {
        LineCircle circle = new LineCircle();
        Color expected = new Color(50, 60, 70, 80);

        circle.StrokeColor = expected;

        circle.StrokeColor.ShouldNotBeNull();
        circle.StrokeColor!.Value.G.ShouldBe((byte)60);

        circle.StrokeColor = null;
        circle.StrokeColor.ShouldBeNull();
    }

    [Fact]
    public void Gradient_PropertiesRoundTrip()
    {
        LineCircle circle = new LineCircle();
        Color c1 = new Color(255, 0, 0, 255);
        Color c2 = new Color(0, 0, 255, 255);

        circle.UseGradient = true;
        circle.GradientType = GradientType.Radial;
        circle.Color1 = c1;
        circle.Color2 = c2;

        circle.UseGradient.ShouldBeTrue();
        circle.GradientType.ShouldBe(GradientType.Radial);
        circle.Color1.R.ShouldBe((byte)255);
        circle.Color2.B.ShouldBe((byte)255);
    }

    [Fact]
    public void LegacyColorChannel_StillRoundTripsViaColor()
    {
        // Back-compat: shared CircleRuntime's raylib branch reads/writes Color, Red, Green,
        // Blue, Alpha — preserve that path unchanged so the runtime keeps working.
        LineCircle circle = new LineCircle();

        circle.Red = 12;
        circle.Green = 34;
        circle.Blue = 56;
        circle.Alpha = 78;

        circle.Color.R.ShouldBe((byte)12);
        circle.Color.G.ShouldBe((byte)34);
        circle.Color.B.ShouldBe((byte)56);
        circle.Color.A.ShouldBe((byte)78);
    }
}
