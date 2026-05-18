using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #2757 — verifies the property surface added to <see cref="LineRectangle"/> so the raylib
/// renderable can render filled rectangles, thick strokes, dashed perimeters, dropshadows, and
/// linear/radial gradients in addition to its original 1 px outline. These tests cover defaults
/// and property round-trip only; actual rendering paths call native raylib Draw* functions that
/// require a GL context and are validated visually in the raylib sample's RectanglesScreen.
/// </summary>
public class LineRectangleTests
{
    [Fact]
    public void Defaults_PreserveLegacyOutlineBehavior()
    {
        LineRectangle rectangle = new LineRectangle();

        rectangle.IsFilled.ShouldBeFalse();
        rectangle.LinePixelWidth.ShouldBe(1f);
        rectangle.FillColor.ShouldBeNull();
        rectangle.StrokeColor.ShouldBeNull();
        rectangle.UseGradient.ShouldBeFalse();
        rectangle.IsDotted.ShouldBeFalse();
        rectangle.Color.R.ShouldBe((byte)255);
        rectangle.Color.G.ShouldBe((byte)255);
        rectangle.Color.B.ShouldBe((byte)255);
        rectangle.Color.A.ShouldBe((byte)255);
    }

    [Fact]
    public void IsFilled_RoundTrips()
    {
        LineRectangle rectangle = new LineRectangle();

        rectangle.IsFilled = true;

        rectangle.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void FillColor_RoundTripsNullableColor()
    {
        LineRectangle rectangle = new LineRectangle();
        Color expected = new Color(10, 20, 30, 40);

        rectangle.FillColor = expected;

        rectangle.FillColor.ShouldNotBeNull();
        rectangle.FillColor!.Value.R.ShouldBe((byte)10);
        rectangle.FillColor!.Value.A.ShouldBe((byte)40);

        rectangle.FillColor = null;
        rectangle.FillColor.ShouldBeNull();
    }

    [Fact]
    public void StrokeColor_RoundTripsNullableColor()
    {
        LineRectangle rectangle = new LineRectangle();
        Color expected = new Color(50, 60, 70, 80);

        rectangle.StrokeColor = expected;

        rectangle.StrokeColor.ShouldNotBeNull();
        rectangle.StrokeColor!.Value.G.ShouldBe((byte)60);

        rectangle.StrokeColor = null;
        rectangle.StrokeColor.ShouldBeNull();
    }

    [Fact]
    public void Gradient_PropertiesRoundTrip()
    {
        LineRectangle rectangle = new LineRectangle();
        Color c1 = new Color(255, 0, 0, 255);
        Color c2 = new Color(0, 0, 255, 255);

        rectangle.UseGradient = true;
        rectangle.GradientType = GradientType.Radial;
        rectangle.Color1 = c1;
        rectangle.Color2 = c2;

        rectangle.UseGradient.ShouldBeTrue();
        rectangle.GradientType.ShouldBe(GradientType.Radial);
        rectangle.Color1.R.ShouldBe((byte)255);
        rectangle.Color2.B.ShouldBe((byte)255);
    }

    [Fact]
    public void GradientAxis_PropertiesRoundTrip()
    {
        LineRectangle rectangle = new LineRectangle();

        rectangle.GradientX1.ShouldBe(0f);
        rectangle.GradientY1.ShouldBe(0f);
        rectangle.GradientX2.ShouldBe(0f);
        rectangle.GradientY2.ShouldBe(0f);
        rectangle.GradientInnerRadius.ShouldBe(0f);
        rectangle.GradientOuterRadius.ShouldBe(0f);

        rectangle.GradientX1 = 4f;
        rectangle.GradientY1 = 8f;
        rectangle.GradientX2 = 56f;
        rectangle.GradientY2 = 28f;
        rectangle.GradientInnerRadius = 4f;
        rectangle.GradientOuterRadius = 28f;

        rectangle.GradientX1.ShouldBe(4f);
        rectangle.GradientY1.ShouldBe(8f);
        rectangle.GradientX2.ShouldBe(56f);
        rectangle.GradientY2.ShouldBe(28f);
        rectangle.GradientInnerRadius.ShouldBe(4f);
        rectangle.GradientOuterRadius.ShouldBe(28f);
    }

    [Fact]
    public void DashedStroke_PropertiesRoundTrip()
    {
        // #2757 follow-up — dashed stroke uses StrokeDashLength + StrokeGapLength; both default
        // to 0 (solid stroke). Both must be > 0 for the render path to engage. Preferred over
        // the binary IsDotted flag for cross-backend parity.
        LineRectangle rectangle = new LineRectangle();

        rectangle.StrokeDashLength.ShouldBe(0f);
        rectangle.StrokeGapLength.ShouldBe(0f);

        rectangle.StrokeDashLength = 6f;
        rectangle.StrokeGapLength = 4f;

        rectangle.StrokeDashLength.ShouldBe(6f);
        rectangle.StrokeGapLength.ShouldBe(4f);
    }

    [Fact]
    public void Dropshadow_PropertiesRoundTrip()
    {
        // #2757 follow-up — dropshadow defaults to off (HasDropshadow == false) with opaque-black
        // color, zero offset/blur. All props round-trip independently.
        LineRectangle rectangle = new LineRectangle();

        rectangle.HasDropshadow.ShouldBeFalse();
        rectangle.DropshadowColor.A.ShouldBe((byte)255);
        rectangle.DropshadowOffsetX.ShouldBe(0f);
        rectangle.DropshadowBlurY.ShouldBe(0f);

        rectangle.HasDropshadow = true;
        rectangle.DropshadowColor = new Color(220, 40, 160, 220);
        rectangle.DropshadowOffsetX = 6f;
        rectangle.DropshadowOffsetY = 4f;
        rectangle.DropshadowBlurX = 6f;
        rectangle.DropshadowBlurY = 4f;

        rectangle.HasDropshadow.ShouldBeTrue();
        rectangle.DropshadowColor.R.ShouldBe((byte)220);
        rectangle.DropshadowColor.A.ShouldBe((byte)220);
        rectangle.DropshadowOffsetX.ShouldBe(6f);
        rectangle.DropshadowOffsetY.ShouldBe(4f);
        rectangle.DropshadowBlurX.ShouldBe(6f);
        rectangle.DropshadowBlurY.ShouldBe(4f);
    }

    [Fact]
    public void LegacyColorChannel_StillRoundTripsViaColor()
    {
        LineRectangle rectangle = new LineRectangle();

        rectangle.Red = 12;
        rectangle.Green = 34;
        rectangle.Blue = 56;
        rectangle.Alpha = 78;

        rectangle.Color.R.ShouldBe((byte)12);
        rectangle.Color.G.ShouldBe((byte)34);
        rectangle.Color.B.ShouldBe((byte)56);
        rectangle.Color.A.ShouldBe((byte)78);
    }
}
