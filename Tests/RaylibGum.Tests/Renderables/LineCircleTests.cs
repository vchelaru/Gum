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

    // Issue #2852 — Radius is computed from min(Width, Height) / 2 so the raylib renderable
    // renders a non-square bounding box as a circle that fits the smaller dimension, matching
    // the Skia and Apos.Shapes renderables. The layout system sets Width/Height directly when
    // a circle is sized by its parent, so Radius cannot be an independent field.
    [Fact]
    public void Radius_WhenWidthGreaterThanHeight_UsesSmallerDimension()
    {
        LineCircle circle = new() { Width = 200, Height = 50 };

        circle.Radius.ShouldBe(25f);
    }

    [Fact]
    public void Radius_WhenHeightGreaterThanWidth_UsesSmallerDimension()
    {
        LineCircle circle = new() { Width = 50, Height = 200 };

        circle.Radius.ShouldBe(25f);
    }

    [Fact]
    public void Radius_Setter_KeepsWidthAndHeightSquare()
    {
        LineCircle circle = new();

        circle.Radius = 30;

        circle.Width.ShouldBe(60f);
        circle.Height.ShouldBe(60f);
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
    public void GradientAxis_PropertiesRoundTrip()
    {
        // #2757 follow-ups #8 (linear axis) and #9 (offset-center / inner-outer radius).
        // The renderable holds bbox-local pixel coords; rlgl triangle fan reads them per
        // vertex.
        LineCircle circle = new LineCircle();

        circle.GradientX1.ShouldBe(0f);
        circle.GradientY1.ShouldBe(0f);
        circle.GradientX2.ShouldBe(0f);
        circle.GradientY2.ShouldBe(0f);
        circle.GradientInnerRadius.ShouldBe(0f);
        circle.GradientOuterRadius.ShouldBe(0f);

        circle.GradientX1 = 4f;
        circle.GradientY1 = 8f;
        circle.GradientX2 = 56f;
        circle.GradientY2 = 28f;
        circle.GradientInnerRadius = 4f;
        circle.GradientOuterRadius = 28f;

        circle.GradientX1.ShouldBe(4f);
        circle.GradientY1.ShouldBe(8f);
        circle.GradientX2.ShouldBe(56f);
        circle.GradientY2.ShouldBe(28f);
        circle.GradientInnerRadius.ShouldBe(4f);
        circle.GradientOuterRadius.ShouldBe(28f);
    }

    [Fact]
    public void DashedStroke_PropertiesRoundTrip()
    {
        // #2757 follow-up #10 — dashed stroke uses StrokeDashLength + StrokeGapLength;
        // both default to 0 (solid stroke). Both must be > 0 for the render path to engage.
        LineCircle circle = new LineCircle();

        circle.StrokeDashLength.ShouldBe(0f);
        circle.StrokeGapLength.ShouldBe(0f);

        circle.StrokeDashLength = 6f;
        circle.StrokeGapLength = 4f;

        circle.StrokeDashLength.ShouldBe(6f);
        circle.StrokeGapLength.ShouldBe(4f);
    }

    [Fact]
    public void Dropshadow_PropertiesRoundTrip()
    {
        // #2757 follow-up #12 — dropshadow defaults to off (HasDropshadow == false) with
        // opaque-black color, zero offset/blur. All props round-trip independently.
        LineCircle circle = new LineCircle();

        circle.HasDropshadow.ShouldBeFalse();
        circle.DropshadowColor.A.ShouldBe((byte)255);
        circle.DropshadowOffsetX.ShouldBe(0f);
        circle.DropshadowBlurY.ShouldBe(0f);

        circle.HasDropshadow = true;
        circle.DropshadowColor = new Color(220, 40, 160, 220);
        circle.DropshadowOffsetX = 6f;
        circle.DropshadowOffsetY = 4f;
        circle.DropshadowBlurX = 6f;
        circle.DropshadowBlurY = 4f;

        circle.HasDropshadow.ShouldBeTrue();
        circle.DropshadowColor.R.ShouldBe((byte)220);
        circle.DropshadowColor.A.ShouldBe((byte)220);
        circle.DropshadowOffsetX.ShouldBe(6f);
        circle.DropshadowOffsetY.ShouldBe(4f);
        circle.DropshadowBlurX.ShouldBe(6f);
        circle.DropshadowBlurY.ShouldBe(4f);
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

    // Issue #2956 — UseGradient is a *pattern* flag, not a *visibility* flag. The effective
    // fill color is `FillColor ?? Color`; when its alpha is 0 the slot is invisible and the
    // gradient must not paint over it. Mirrors the SkPaint contract (paint.Color.alpha
    // modulates shader output) that the SkiaGum backend enforces naturally. Apos and raylib
    // both used to leak — see fix in EmitGradientVertex / DrawGradientFan gating.

    [Fact]
    public void ShouldPaintFillGradient_FillColorOpaque_True()
    {
        LineCircle circle = new() { UseGradient = true, FillColor = new Color(10, 20, 30, 255) };

        circle.ShouldPaintFillGradient.ShouldBeTrue();
    }

    [Fact]
    public void ShouldPaintFillGradient_FillColorTransparent_False()
    {
        LineCircle circle = new() { UseGradient = true, FillColor = new Color(10, 20, 30, 0) };

        circle.ShouldPaintFillGradient.ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintFillGradient_GradientOff_False()
    {
        LineCircle circle = new() { UseGradient = false, FillColor = new Color(10, 20, 30, 255) };

        circle.ShouldPaintFillGradient.ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintFillGradient_LegacyFillViaIsFilled_OpaqueColor_True()
    {
        LineCircle circle = new() { UseGradient = true, IsFilled = true };

        // Default Color is opaque white, so the legacy fill path lights up.
        circle.ShouldPaintFillGradient.ShouldBeTrue();
    }

    [Fact]
    public void ShouldPaintFillGradient_LegacyFillViaIsFilled_TransparentColor_False()
    {
        LineCircle circle = new() { UseGradient = true, IsFilled = true, Alpha = 0 };

        circle.ShouldPaintFillGradient.ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintFillGradient_NoFillSlotEnabled_False()
    {
        // Default LineCircle: IsFilled = false, FillColor = null → fill pass doesn't run at all,
        // so the gradient cannot paint regardless of UseGradient.
        LineCircle circle = new() { UseGradient = true };

        circle.ShouldPaintFillGradient.ShouldBeFalse();
    }
}
