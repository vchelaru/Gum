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

    // Issue #2998 — UseGradient is a *pattern* flag, not a *visibility* flag. Visibility is driven
    // by the gradient STOP alphas (Color1 / Color2), NOT the slot's solid fill color. A fill slot
    // must exist (FillColor set or IsFilled), but its solid alpha is irrelevant. Replaces the #2956
    // gate, which keyed off `FillColor ?? Color` alpha and went invisible once RectangleRuntime /
    // CircleRuntime defaulted the solid fill transparent (#2938 stroke-only default).

    [Fact]
    public void ShouldPaintFillGradient_BothGradientStopsTransparent_OpaqueFill_False()
    {
        // Both stops invisible → no gradient, even with an opaque solid fill.
        LineCircle circle = new()
        {
            UseGradient = true,
            FillColor = new Color(10, 20, 30, 255),
            Color1 = new Color(1, 2, 3, 0),
            Color2 = new Color(4, 5, 6, 0),
        };

        circle.ShouldPaintFillGradient.ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintFillGradient_GradientOff_False()
    {
        LineCircle circle = new() { UseGradient = false, FillColor = new Color(10, 20, 30, 255) };

        circle.ShouldPaintFillGradient.ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintFillGradient_IsFilled_VisibleStops_True()
    {
        LineCircle circle = new()
        {
            UseGradient = true,
            IsFilled = true,
            Color1 = new Color(10, 20, 30, 255),
            Color2 = new Color(40, 50, 60, 255),
        };

        circle.ShouldPaintFillGradient.ShouldBeTrue();
    }

    [Fact]
    public void ShouldPaintFillGradient_NoFillSlotEnabled_False()
    {
        // Default LineCircle: IsFilled = false, FillColor = null → fill pass doesn't run at all,
        // so the gradient cannot paint regardless of UseGradient or stop alphas.
        LineCircle circle = new()
        {
            UseGradient = true,
            Color1 = new Color(10, 20, 30, 255),
            Color2 = new Color(40, 50, 60, 255),
        };

        circle.ShouldPaintFillGradient.ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintFillGradient_OneGradientStopVisible_True()
    {
        LineCircle circle = new()
        {
            UseGradient = true,
            IsFilled = true,
            Color1 = new Color(10, 20, 30, 255),
            Color2 = new Color(40, 50, 60, 0),
        };

        circle.ShouldPaintFillGradient.ShouldBeTrue();
    }

    [Fact]
    public void ShouldPaintFillGradient_TransparentFill_VisibleStops_True()
    {
        // Core regression repro: solid fill transparent (the new CircleRuntime default), gradient
        // stops opaque → the gradient must still paint.
        LineCircle circle = new()
        {
            UseGradient = true,
            FillColor = new Color(10, 20, 30, 0),
            Color1 = new Color(10, 20, 30, 255),
            Color2 = new Color(40, 50, 60, 255),
        };

        circle.ShouldPaintFillGradient.ShouldBeTrue();
    }

    // Issue #2934 / #2956 — LineCircle.Render previously ignored Rotation entirely. The disc
    // is rotation-symmetric so its position doesn't change, but the gradient axis (defined in
    // object-local bbox coords) must rotate around the bbox center to track the shape — same
    // contract Apos's RenderableShapeBase.GetGradient enforces (PR #2945). The fix rotates
    // GradientX1/Y1 and GradientX2/Y2 around the bbox center (Radius, Radius) before
    // projection in EmitGradientVertex. Rotation convention mirrors LineRectangle's R helper
    // (visual CCW on screen: [cos sin; -sin cos]).

    [Fact]
    public void GetRotatedGradientEndpoints_ZeroRotation_ReturnsOriginalEndpoints()
    {
        LineCircle circle = new()
        {
            Width = 56,
            Height = 56,
            GradientX1 = 0f,
            GradientY1 = 0f,
            GradientX2 = 20f,
            GradientY2 = 0f,
        };

        (float x1, float y1, float x2, float y2) = circle.GetRotatedGradientEndpoints(rotationDegrees: 0f);

        x1.ShouldBe(0f);
        y1.ShouldBe(0f);
        x2.ShouldBe(20f);
        y2.ShouldBe(0f);
    }

    [Fact]
    public void GetRotatedGradientEndpoints_NinetyDegrees_PivotsAroundBboxCenter()
    {
        // bbox is 56x56, center is at (28, 28) in local coords. Original axis runs from
        // (0, 0) to (20, 0). A 90° CCW rotation (visual) around (28, 28) maps:
        //   (0, 0)  → (28 - 28, 28 + 28) = (0, 56)   (delta -28,-28 → cos sin = 0,1 → 0*0 + -28*1 = -28; -(-28)*1 + -28*0 = 28; (28-28, 28+28) = (0, 56))
        // hmm let me recompute by hand using LineRectangle's formula: (cx + dx*cos + dy*sin, cy - dx*sin + dy*cos)
        // (0,0): dx=-28, dy=-28; cos(90°)=0, sin(90°)=1 → (28 + 0 + -28, 28 - -28 + 0) = (0, 56)
        // (20,0): dx=-8, dy=-28 → (28 + 0 + -28, 28 - -8 + 0) = (0, 36)
        LineCircle circle = new()
        {
            Width = 56,
            Height = 56,
            GradientX1 = 0f,
            GradientY1 = 0f,
            GradientX2 = 20f,
            GradientY2 = 0f,
        };

        (float x1, float y1, float x2, float y2) = circle.GetRotatedGradientEndpoints(rotationDegrees: 90f);

        x1.ShouldBe(0f, tolerance: 0.001);
        y1.ShouldBe(56f, tolerance: 0.001);
        x2.ShouldBe(0f, tolerance: 0.001);
        y2.ShouldBe(36f, tolerance: 0.001);
    }

    [Fact]
    public void GetRotatedGradientEndpoints_OneHundredEightyDegrees_FlipsAcrossBboxCenter()
    {
        // 180° flips both endpoints across the bbox center (28, 28).
        // (0, 0)  → (56, 56)
        // (20, 0) → (36, 56)
        LineCircle circle = new()
        {
            Width = 56,
            Height = 56,
            GradientX1 = 0f,
            GradientY1 = 0f,
            GradientX2 = 20f,
            GradientY2 = 0f,
        };

        (float x1, float y1, float x2, float y2) = circle.GetRotatedGradientEndpoints(rotationDegrees: 180f);

        x1.ShouldBe(56f, tolerance: 0.001);
        y1.ShouldBe(56f, tolerance: 0.001);
        x2.ShouldBe(36f, tolerance: 0.001);
        y2.ShouldBe(56f, tolerance: 0.001);
    }
}
