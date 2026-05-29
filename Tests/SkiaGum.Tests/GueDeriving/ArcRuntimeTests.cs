using Gum.GueDeriving;
using Shouldly;
using SkiaGum.Renderables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Tests.GueDeriving;

public class ArcRuntimeTests
{
    // #2949: ArcRuntime exposes a single isotropic DropshadowBlur (mirroring CSS box-shadow /
    // Figma / Photoshop). Setting the scalar fans the value out to both axes; asserted on the
    // renderable's per-axis blur (which stays a real API at the renderable layer), not the
    // deprecated per-axis runtime members. Skia pushes blur to the renderable in PreRender
    // (ApplyDropshadow).
    [Fact]
    public void ArcRuntime_DropshadowBlur_ShouldSetBothAxesOnRenderable()
    {
        ArcRuntime arcRuntime = new();

        arcRuntime.DropshadowBlur = 5;
        arcRuntime.PreRender();

        RenderableShapeBase shape = (RenderableShapeBase)arcRuntime.RenderableComponent;
        shape.DropshadowBlurX.ShouldBe(5f);
        shape.DropshadowBlurY.ShouldBe(5f);
        arcRuntime.DropshadowBlur.ShouldBe(5);
    }

    // Locked in unification (issue #2728): dropshadow defaults are seeded on both backends so
    // ctor state is consistent. Values are inert until HasDropshadow is set true. Apos seeded
    // these before unification; Skia did not.
    [Fact]
    public void ArcRuntime_DropshadowOffsetY_DefaultShouldBeThree()
    {
        ArcRuntime arcRuntime = new();
        arcRuntime.DropshadowOffsetY.ShouldBe(3);
    }

    // Locked in unification (issue #2728): regression guard. Skia previously left this at the
    // renderable's false default; the unified ctor still defaults to false (matching Skia and
    // graphics-convention flat caps). If somebody flips this back to Apos's prior `true`
    // default, this catches it.
    [Fact]
    public void ArcRuntime_IsEndRounded_DefaultShouldBeFalse()
    {
        ArcRuntime arcRuntime = new();
        arcRuntime.IsEndRounded.ShouldBeFalse();
    }

    [Fact]
    public void ArcRuntime_ShouldCreateArc()
    {
        ArcRuntime arcRuntime = new();

        arcRuntime.RenderableComponent.ShouldNotBeNull();
        (arcRuntime.RenderableComponent is Arc).ShouldBeTrue();
    }

    // Locked in unification (issue #2728): Arc's legacy default stroke width is 10. Apos seeded
    // this before unification; Skia inherited the base default. The unified ctor seeds 10 on
    // both so a freshly-constructed ArcRuntime renders visibly without consumer setup.
    [Fact]
    public void ArcRuntime_Thickness_DefaultShouldBe10()
    {
        ArcRuntime arcRuntime = new();
        arcRuntime.Thickness.ShouldBe(10);
    }

    // Regression for the silent gradient suppression introduced by #2790 (commit 5c34a77).
    // Before #2790, SkiaShapeRuntime.UseGradient passed straight through to the contained
    // renderable; after #2790, RefreshSlotGradients gated it on `_fillColor != null`, which
    // turned off gradient for every single-slot shape (Arc, RoundedRectangle, Polygon, etc.)
    // used via the legacy `Color` API. Two-slot runtimes (Circle, Rectangle) were unaffected
    // because their gate is per-slot and they always set FillColor/StrokeColor explicitly.
    [Fact]
    public void ArcRuntime_UseGradient_ShouldEnableGradientOnSingleSlotWithoutFillColor()
    {
        ArcRuntime arcRuntime = new();
        Arc containedArc = (Arc)arcRuntime.RenderableComponent;

        arcRuntime.UseGradient = true;

        containedArc.UseGradient.ShouldBeTrue(
            "single-slot Skia shapes must honor UseGradient even when FillColor is null " +
            "(legacy Color API path) — regression from #2790's per-slot gate.");
    }

    [Fact]
    public void ArcRuntime_Thickness_ShouldSetStrokeWithInPreRender()
    {
        ArcRuntime arcRuntime = new();

        var containedArc = (Arc)arcRuntime.RenderableComponent;
        arcRuntime.Thickness = 10;
        containedArc.StrokeWidth.ShouldNotBe(10, "because the thickness isn't applied until PreRender where the units are also considered");

        arcRuntime.PreRender();

        containedArc.StrokeWidth.ShouldBe(10);
    }
}
