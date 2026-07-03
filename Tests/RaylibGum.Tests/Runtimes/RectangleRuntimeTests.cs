using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;

// RectangleRuntime's legacy single-color members (Color/Red/Green/Blue/Alpha) are now
// [Obsolete] under the unified fill/stroke API, but this file exists to pin their back-compat
// routing on the raylib backend — so CS0618 is silenced for the whole file.
#pragma warning disable CS0618

namespace RaylibGum.Tests.Runtimes;

public class RectangleRuntimeTests : BaseTestClass
{
    // Issue #3009 follow-up — a stroke-only rectangle (IsFilled = false, only StrokeColor set) must
    // render outline-only on raylib. The renderable's fill pass runs when `FillColor.HasValue ||
    // IsFilled`, so the runtime must leave the renderable's FillColor null when the shape isn't
    // filled. It previously pushed the default opaque-white FillColor unconditionally, which made
    // every default rectangle fill white (mirrors how Apos/Skia push a transparent fill when not
    // filled).
    [Fact]
    public void StrokeOnly_DefaultRectangle_DoesNotFill()
    {
        RectangleRuntime sut = new();
        sut.StrokeColor = new Color(255, 255, 255, 255);

        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        inner.IsFilled.ShouldBeFalse();
        inner.FillColor.HasValue.ShouldBeFalse();
    }

    [Fact]
    public void IsFilledTrue_PushesFillColorToRenderable()
    {
        RectangleRuntime sut = new();
        sut.FillColor = new Color(10, 20, 30, 255);
        sut.IsFilled = true;

        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        inner.FillColor.HasValue.ShouldBeTrue();
        inner.FillColor!.Value.R.ShouldBe((byte)10);
    }

    [Fact]
    public void IsFilledToggledOff_ClearsRenderableFillColor()
    {
        RectangleRuntime sut = new();
        sut.FillColor = new Color(10, 20, 30, 255);
        sut.IsFilled = true;
        sut.IsFilled = false;

        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        inner.FillColor.HasValue.ShouldBeFalse();
    }

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

    // Issue #3458 — raylib Blend surface. The runtime exposes a non-nullable Blend (default Normal,
    // matching the XNALIKE public signature) that forwards to the contained LineRectangle's nullable
    // blend member, whose Render wraps the fill/stroke passes in BeginBlendMode when set.
    [Fact]
    public void Blend_DefaultsToNormal()
    {
        RectangleRuntime sut = new();
        sut.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Normal);
    }

    [Fact]
    public void Blend_RoundTrips_AndPushesToContainedRenderable()
    {
        RectangleRuntime sut = new();

        sut.Blend = Gum.RenderingLibrary.Blend.Additive;

        sut.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        inner.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
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

    // Editor-theme outlines (ListBox/TextBox/Button/CheckBox/ComboBox) set their RectangleRuntime
    // outline via the legacy Color property, e.g. `rectangle.Color = new Color(60, 60, 60)`. The
    // raylib renderer draws `StrokeColor ?? Color`, and the ctor seeds StrokeColor opaque-white, so
    // a legacy Color write landed in the de-prioritized Color slot and the outline rendered white
    // instead of the themed gray (MonoGame was correct because there Color and StrokeColor are the
    // same field). Legacy Color must drive the rendered stroke slot. Round-trip alone (above) did not
    // catch this — it read back the same dead slot it wrote.
    [Fact]
    public void Color_Setter_ShouldDriveRenderedStrokeColor_NotBeShadowedBySeededStrokeColor()
    {
        RectangleRuntime sut = new();
        sut.Color = new Color(60, 60, 60, 255);

        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        // The renderer draws StrokeColor ?? Color, so the legacy write must reach the rendered slot.
        Color renderedStroke = inner.StrokeColor ?? inner.Color;
        renderedStroke.R.ShouldBe((byte)60);
        renderedStroke.G.ShouldBe((byte)60);
        renderedStroke.B.ShouldBe((byte)60);
    }

    // Same shadowing bug as Color, via the per-channel legacy member: a legacy channel write must
    // reach the rendered stroke slot, not the de-prioritized Color slot.
    [Fact]
    public void Red_Setter_ShouldDriveRenderedStrokeColor_NotBeShadowedBySeededStrokeColor()
    {
        RectangleRuntime sut = new();
        sut.Red = 60;

        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        Color renderedStroke = inner.StrokeColor ?? inner.Color;
        renderedStroke.R.ShouldBe((byte)60);
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

        // Issue #3009 — IsFilled gates the fill (consistent with Apos/Skia), so the fill color
        // reaches the renderable only when the shape is filled.
        sut.IsFilled = true;
        sut.FillColor = expected;

        sut.FillColor.R.ShouldBe((byte)10);
        ((LineRectangle)sut.RenderableComponent!).FillColor.ShouldNotBeNull();
        ((LineRectangle)sut.RenderableComponent!).FillColor!.Value.R.ShouldBe((byte)10);
    }

    [Fact]
    public void StrokeColor_RoundTrips_AndPushesToContainedRenderable()
    {
        RectangleRuntime sut = new();
        Color expected = new Color(40, 50, 60, 255);

        sut.StrokeColor = expected;

        sut.StrokeColor.G.ShouldBe((byte)50);
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
    public void PreRender_ShouldPushStrokeWidthToContainedRenderable()
    {
        // Canonical resolve path for StrokeWidth is PreRender (handles StrokeWidthUnits
        // ScreenPixel ↔ camera zoom). The setter intentionally does NOT push immediately —
        // raylib's Renderer.Draw walks the tree calling PreRender before render so this lands
        // in time for the first frame. Symmetric across CircleRuntime, RectangleRuntime, and
        // PolygonRuntime.
        RectangleRuntime sut = new();

        sut.StrokeWidth = 5f;
        sut.StrokeWidth.ShouldBe(5f);

        sut.PreRender();
        // Issue #3183 — raylib gets NO AA compensation: the geometric width handed to the
        // renderable IS the visible width (raylib's MSAA adds no width, unlike Apos.Shapes, which
        // subtracts a ~1px geometric contribution then adds it back as an AA band). So PreRender
        // pushes the nominal StrokeWidth straight through. The earlier #3179 subtraction (5 - 1 = 4)
        // collapsed a nominal 1px outline to ~0.01px (invisible) and was reverted. At zoom 1 this is 5.
        ((LineRectangle)sut.RenderableComponent!).LinePixelWidth.ShouldBe(5f);
    }

    [Fact]
    public void StrokeColor_DefaultsToWhite_MatchingSkia()
    {
        // #2757 follow-up — Skia's RectangleRuntime ctor sets StrokeColor = SKColors.White
        // so cells that set only FillColor still render with a visible 1 px white outline
        // (e.g. the gallery's Modes row first cell). Raylib must match or fill-only cells
        // render without the outline Skia draws.
        // #2938 — runtime FillColor/StrokeColor are now non-nullable Color (defaulting to
        // white) with IsFilled / StrokeWidth = 0 as the visibility gates.
        RectangleRuntime sut = new();

        sut.StrokeColor.R.ShouldBe((byte)255);
        sut.StrokeColor.G.ShouldBe((byte)255);
        sut.StrokeColor.B.ShouldBe((byte)255);
        sut.StrokeColor.A.ShouldBe((byte)255);
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
        sut.DropshadowBlur = 4f;

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

    // Issue #3009 — Circle/Rectangle no longer expose a standalone gradient Color1. The gradient
    // start mirrors the active body color (FillColor when filled, StrokeColor otherwise) and is
    // pushed into the contained LineRectangle's Color1. Color2 remains the standalone second stop.
    [Fact]
    public void Gradient_StartMirrorsBodyColor_AndPushesToContainedRenderable()
    {
        RectangleRuntime sut = new();
        Color fill = new Color(255, 0, 0, 255);
        Color c2 = new Color(0, 0, 255, 255);

        sut.IsFilled = true;
        sut.FillColor = fill;
        sut.UseGradient = true;
        sut.GradientType = GradientType.Radial;
        sut.Color2 = c2;

        sut.UseGradient.ShouldBeTrue();
        sut.GradientType.ShouldBe(GradientType.Radial);
        LineRectangle inner = (LineRectangle)sut.RenderableComponent!;
        inner.UseGradient.ShouldBeTrue();
        inner.GradientType.ShouldBe(GradientType.Radial);
        inner.Color1.R.ShouldBe((byte)255);
        inner.Color1.A.ShouldBe((byte)255);
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

    // Issue #3175 — parity surface for theme source written against the richer Apos.Shapes feature
    // set. The raylib renderable does not consume these yet, so they round-trip on the runtime as
    // forward compat; this pins that contract so a future renderable hookup is a deliberate change.
    [Fact]
    public void CustomCornerRadii_RoundTrip()
    {
        RectangleRuntime sut = new();

        sut.CustomRadiusTopLeft = 1f;
        sut.CustomRadiusTopRight = 2f;
        sut.CustomRadiusBottomRight = 3f;
        sut.CustomRadiusBottomLeft = 4f;

        sut.CustomRadiusTopLeft.ShouldBe(1f);
        sut.CustomRadiusTopRight.ShouldBe(2f);
        sut.CustomRadiusBottomRight.ShouldBe(3f);
        sut.CustomRadiusBottomLeft.ShouldBe(4f);
    }

    [Fact]
    public void GradientUnits_RoundTrip()
    {
        RectangleRuntime sut = new();

        sut.GradientX1Units = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        sut.GradientY1Units = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        sut.GradientX2Units = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        sut.GradientY2Units = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        sut.GradientInnerRadiusUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;
        sut.GradientOuterRadiusUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;

        sut.GradientX1Units.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.GradientY1Units.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        sut.GradientX2Units.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        sut.GradientY2Units.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.GradientInnerRadiusUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.ScreenPixel);
        sut.GradientOuterRadiusUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.ScreenPixel);
    }

    [Fact]
    public void IsAntialiased_RoundTrips_AndDefaultsTrue()
    {
        RectangleRuntime sut = new();

        sut.IsAntialiased.ShouldBeTrue();

        sut.IsAntialiased = false;
        sut.IsAntialiased.ShouldBeFalse();
    }
}
