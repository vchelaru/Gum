using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaGum;
using SkiaGum.Renderables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Tests.GueDeriving;

public class ArcRuntimeTests
{
    public ArcRuntimeTests()
    {
        // Route SetProperty through the production dispatcher so the .gumx-load path (which maps
        // Arc's legacy Red1/Green1/Blue1/Alpha1 onto the primary Color, issue #3009) is exercised.
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    // ---- Dispatcher routing pins (issue #3650) ---------------------------------------------
    // These lock the CURRENT behavior of the Skia CustomSetPropertyOnRenderable dispatcher for the
    // ArcRuntime-intercepted property paths (the `graphicalUiElement is ArcRuntime` arms in the Arc
    // branch of SetPropertyOnRenderableFunc). They drive the STRING property name through the
    // production dispatcher (via SetProperty) and assert the value lands on the runtime — the safety
    // net for the planned runtime-type-first restructure of the dispatcher. The legacy gradient-start
    // channels (Red1/Green1/Blue1/Alpha1 -> primary Color) are already pinned below (issue #3009).

    [Fact]
    public void Dispatch_DropshadowBlur_RoutesToRuntime()
    {
        ArcRuntime sut = new();

        sut.SetProperty("DropshadowBlur", 7f);

        sut.DropshadowBlur.ShouldBe(7f);
    }

    [Fact]
    public void Dispatch_StrokeDashLength_RoutesToRuntime()
    {
        ArcRuntime sut = new();

        sut.SetProperty("StrokeDashLength", 9f);

        sut.StrokeDashLength.ShouldBe(9f);
    }

    [Fact]
    public void Dispatch_StrokeGapLength_RoutesToRuntime()
    {
        ArcRuntime sut = new();

        sut.SetProperty("StrokeGapLength", 5f);

        sut.StrokeGapLength.ShouldBe(5f);
    }

    // StrokeWidth is an obsolete alias for Thickness on ArcRuntime (both route to the same base
    // StrokeWidth backing field). Assert via Thickness so the pin stays warning-free.
    [Fact]
    public void Dispatch_StrokeWidth_RoutesToRuntimeThickness()
    {
        ArcRuntime sut = new();

        sut.SetProperty("StrokeWidth", 3f);

        sut.Thickness.ShouldBe(3f);
    }

    // ---- End dispatcher routing pins -------------------------------------------------------

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

    // Issue #3009 — Arc's gradient start is now its primary Color (the unified "gradient start =
    // body color" model). The renderable's gradient-start channels (Red1/Green1/Blue1/Alpha1) are
    // synced from the primary color each frame in PreRender so the start follows the color
    // regardless of how it was set.
    [Fact]
    public void GradientStart_MirrorsPrimaryColor_AfterPreRender()
    {
        ArcRuntime arcRuntime = new();
        arcRuntime.Red = 10;
        arcRuntime.Green = 20;
        arcRuntime.Blue = 30;
        arcRuntime.Alpha = 40;

        arcRuntime.PreRender();

        Arc containedArc = (Arc)arcRuntime.RenderableComponent;
        containedArc.Red1.ShouldBe(10);
        containedArc.Green1.ShouldBe(20);
        containedArc.Blue1.ShouldBe(30);
        containedArc.Alpha1.ShouldBe(40);
    }

    // Issue #3009 — the obsolete Red1/Green1/Blue1/Alpha1 / Color1 shims map onto the primary
    // Color so old code keeps compiling and behaving sensibly (the standalone gradient start was
    // collapsed onto the body color).
    [Fact]
    public void Red1Shim_MapsToPrimaryColor()
    {
#pragma warning disable CS0618 // exercising the obsolete back-compat shim on purpose
        ArcRuntime arcRuntime = new();

        arcRuntime.Red1 = 77;

        arcRuntime.Red.ShouldBe(77);
        arcRuntime.Red1.ShouldBe(77);
#pragma warning restore CS0618
    }

    // Issue #3009 — Arc back-compat loss case (pinned per the locked design). Old Arc data sets
    // the solid color channels (Color/Red/Green/Blue/Alpha) AND the legacy gradient-start channels
    // (Red1/Green1/Blue1/Alpha1) independently. Both now remap onto the primary Color, and Gum
    // applies variables alphabetically — so the …1 channels apply AFTER the solid ones and win.
    // When UseGradient was false and Color1 was explicitly different from Color, the solid now
    // shows the old Color1 (the documented, accepted lossy outcome).
    [Fact]
    public void Load_Color1WinsOverColor_OnPrimary_DocumentingLossyCase()
    {
        ArcRuntime arcRuntime = new();

        // Solid channels apply first (alphabetical): an opaque blue.
        arcRuntime.SetProperty("Alpha", 255);
        arcRuntime.SetProperty("Blue", 200);
        arcRuntime.SetProperty("Green", 0);
        arcRuntime.SetProperty("Red", 0);
        // …1 channels apply after and win (remapped onto the primary Color): an opaque red.
        arcRuntime.SetProperty("Alpha1", 255);
        arcRuntime.SetProperty("Blue1", 0);
        arcRuntime.SetProperty("Green1", 0);
        arcRuntime.SetProperty("Red1", 255);

        // Primary color reflects the …1 values (Color1 wins) — the accepted lossy outcome.
        arcRuntime.Red.ShouldBe(255);
        arcRuntime.Green.ShouldBe(0);
        arcRuntime.Blue.ShouldBe(0);
    }
}
