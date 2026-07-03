using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;

// CircleRuntime's legacy single-color members (Color/Red/Green/Blue/Alpha) and Radius are now
// [Obsolete] under the unified fill/stroke API, but this file exists to pin their back-compat
// routing on the raylib backend — so CS0618 is silenced for the whole file.
#pragma warning disable CS0618

namespace RaylibGum.Tests.Runtimes;

public class CircleRuntimeTests : BaseTestClass
{
    // Issue #3009 follow-up — a stroke-only circle (IsFilled = false, only StrokeColor set) must
    // render outline-only on raylib. The renderable's fill pass runs when `FillColor.HasValue ||
    // IsFilled`, so the runtime must leave the renderable's FillColor null when the shape isn't
    // filled (it previously pushed the default opaque-white FillColor unconditionally, filling
    // every default circle).
    [Fact]
    public void StrokeOnly_DefaultCircle_DoesNotFill()
    {
        CircleRuntime sut = new();
        sut.StrokeColor = new Color(255, 255, 255, 255);

        LineCircle inner = (LineCircle)sut.RenderableComponent!;
        inner.IsFilled.ShouldBeFalse();
        inner.FillColor.HasValue.ShouldBeFalse();
    }

    [Fact]
    public void IsFilledTrue_PushesFillColorToRenderable()
    {
        CircleRuntime sut = new();
        sut.FillColor = new Color(10, 20, 30, 255);
        sut.IsFilled = true;

        LineCircle inner = (LineCircle)sut.RenderableComponent!;
        inner.FillColor.HasValue.ShouldBeTrue();
        inner.FillColor!.Value.R.ShouldBe((byte)10);
    }

    [Fact]
    public void IsFilledToggledOff_ClearsRenderableFillColor()
    {
        CircleRuntime sut = new();
        sut.FillColor = new Color(10, 20, 30, 255);
        sut.IsFilled = true;
        sut.IsFilled = false;

        LineCircle inner = (LineCircle)sut.RenderableComponent!;
        inner.FillColor.HasValue.ShouldBeFalse();
    }

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

    // Issue #3444 — `circle.Color = X` must render the same outline color on raylib as it does
    // on MonoGame. The raylib LineCircle resolves the visible stroke as `StrokeColor ?? Color`,
    // and a fresh CircleRuntime seeds a non-null white StrokeColor. Writing only the legacy Color
    // slot was therefore silently shadowed (white ring) while MonoGame (single Color slot) drew
    // the assigned color. Assert on the *resolved* visible stroke, not a Color→Color round-trip
    // (which reads the same shadowed slot and passes even with the bug).
    [Fact]
    public void Color_ShouldDriveVisibleStroke_MatchingMonoGame()
    {
        CircleRuntime sut = new();
        Color expected = new Color(80, 200, 120, 255);

        sut.Color = expected;

        LineCircle inner = (LineCircle)sut.RenderableComponent!;
        Color visibleStroke = inner.StrokeColor ?? inner.Color;
        visibleStroke.ShouldBe(expected);
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
        // #2938 — runtime FillColor/StrokeColor are now non-nullable Color (defaulting to
        // white) with IsFilled / StrokeWidth = 0 as the visibility gates.
        CircleRuntime sut = new();

        sut.StrokeColor.R.ShouldBe((byte)255);
        sut.StrokeColor.G.ShouldBe((byte)255);
        sut.StrokeColor.B.ShouldBe((byte)255);
        sut.StrokeColor.A.ShouldBe((byte)255);
    }

    [Fact]
    public void FillColor_RoundTrips_AndPushesToContainedRenderable()
    {
        CircleRuntime sut = new();
        Color expected = new Color(10, 20, 30, 200);

        // Issue #3009 — IsFilled gates the fill (consistent with Apos/Skia), so the fill color
        // reaches the renderable only when the shape is filled.
        sut.IsFilled = true;
        sut.FillColor = expected;

        sut.FillColor.R.ShouldBe((byte)10);
        ((LineCircle)sut.RenderableComponent!).FillColor.ShouldNotBeNull();
        ((LineCircle)sut.RenderableComponent!).FillColor!.Value.R.ShouldBe((byte)10);
    }

    [Fact]
    public void StrokeColor_RoundTrips_AndPushesToContainedRenderable()
    {
        CircleRuntime sut = new();
        Color expected = new Color(40, 50, 60, 255);

        sut.StrokeColor = expected;

        sut.StrokeColor.G.ShouldBe((byte)50);
        ((LineCircle)sut.RenderableComponent!).StrokeColor.ShouldNotBeNull();
        ((LineCircle)sut.RenderableComponent!).StrokeColor!.Value.G.ShouldBe((byte)50);
    }

    [Fact]
    public void PreRender_ShouldPushStrokeWidthToContainedRenderable()
    {
        // Canonical resolve path for StrokeWidth is PreRender (handles StrokeWidthUnits
        // ScreenPixel ↔ camera zoom). The setter intentionally does NOT push immediately —
        // raylib's Renderer.Draw walks the tree calling PreRender before render so this lands
        // in time for the first frame. Symmetric across CircleRuntime, RectangleRuntime, and
        // PolygonRuntime.
        CircleRuntime sut = new();

        sut.StrokeWidth = 5f;
        sut.StrokeWidth.ShouldBe(5f);

        sut.PreRender();
        // Issue #3183 — raylib gets NO AA compensation: the geometric width handed to the
        // renderable IS the visible width (raylib's MSAA adds no width, unlike Apos.Shapes, which
        // subtracts a ~1px geometric contribution then adds it back as an AA band). So PreRender
        // pushes the nominal StrokeWidth straight through. The earlier #3179 subtraction (5 - 1 = 4)
        // collapsed a nominal 1px ring to ~0.01px (invisible) and was reverted. At zoom 1 this is 5.
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
        sut.DropshadowBlur = 4f;

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

    // Issue #3009 — Circle/Rectangle no longer expose a standalone gradient Color1. The gradient
    // start mirrors the active body color (FillColor when filled, StrokeColor otherwise) and is
    // pushed into the contained LineCircle's Color1. Color2 remains the standalone second stop.
    [Fact]
    public void Gradient_StartMirrorsBodyColor_AndPushesToContainedRenderable()
    {
        CircleRuntime sut = new();
        Color fill = new Color(255, 0, 0, 255);
        Color c2 = new Color(0, 0, 255, 255);

        sut.IsFilled = true;
        sut.FillColor = fill;
        sut.UseGradient = true;
        sut.GradientType = GradientType.Radial;
        sut.Color2 = c2;

        sut.UseGradient.ShouldBeTrue();
        sut.GradientType.ShouldBe(GradientType.Radial);
        LineCircle inner = (LineCircle)sut.RenderableComponent!;
        inner.UseGradient.ShouldBeTrue();
        inner.GradientType.ShouldBe(GradientType.Radial);
        inner.Color1.R.ShouldBe((byte)255);
        inner.Color1.A.ShouldBe((byte)255);
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

    // Issue #3175 — parity surface for theme source written against the richer Apos.Shapes gradient
    // feature set. The raylib renderable does not consume these yet, so they round-trip on the
    // runtime as forward compat; this pins that contract so a future renderable hookup is deliberate.
    [Fact]
    public void GradientUnits_RoundTrip()
    {
        CircleRuntime sut = new();

        sut.GradientX1Units = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        sut.GradientY1Units = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        sut.GradientInnerRadiusUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;
        sut.GradientOuterRadiusUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;

        sut.GradientX1Units.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        sut.GradientY1Units.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        sut.GradientInnerRadiusUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.ScreenPixel);
        sut.GradientOuterRadiusUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.ScreenPixel);
    }

    // Issue #3491 — Blend / IsAntialiased raylib parity with the XNALIKE CircleRuntime surface.
    // Blend is real (LineCircle wraps its fill/stroke draws in the counted BeginBlendMode /
    // EndBlendMode pair, mirroring the #3470 Sprite/NineSlice work); IsAntialiased is a round-trip
    // parity surface only — raylib has no per-shape AA primitive, matching the GradientUnits
    // "not yet rendered on raylib" props above and XNALIKE's own no-Apos.Shapes round-trip no-op.

    [Fact]
    public void Blend_DefaultsToNormal()
    {
        // Matches the non-nullable XNALIKE CircleRuntime.Blend (default Blend.Normal) so cross-
        // backend code reading circle.Blend compiles and behaves identically on both backends.
        CircleRuntime sut = new();
        sut.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Normal);
    }

    [Fact]
    public void Blend_Normal_ClearsContainedRenderableBlend()
    {
        // Normal is the no-op default, so it maps to a null renderable Blend — LineCircle.Render
        // then skips the BeginBlendMode/EndBlendMode wrap entirely (its `if (Blend.HasValue)` gate).
        CircleRuntime sut = new();
        sut.Blend = Gum.RenderingLibrary.Blend.Additive;
        sut.Blend = Gum.RenderingLibrary.Blend.Normal;

        ((LineCircle)sut.RenderableComponent!).Blend.ShouldBeNull();
    }

    [Fact]
    public void Blend_RoundTrips_AndPushesToContainedRenderable()
    {
        CircleRuntime sut = new();
        sut.Blend = Gum.RenderingLibrary.Blend.Additive;

        sut.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
        ((LineCircle)sut.RenderableComponent!).Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
    }

    [Fact]
    public void IsAntialiased_DefaultsToTrue()
    {
        // Matches XNALIKE (and Apos.Shapes) — AA on by default.
        CircleRuntime sut = new();
        sut.IsAntialiased.ShouldBeTrue();
    }

    [Fact]
    public void IsAntialiased_RoundTrips()
    {
        CircleRuntime sut = new();
        sut.IsAntialiased = false;
        sut.IsAntialiased.ShouldBeFalse();
    }
}
