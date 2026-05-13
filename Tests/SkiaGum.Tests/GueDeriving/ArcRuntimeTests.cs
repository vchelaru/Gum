using Gum.GueDeriving;
using Shouldly;
using SkiaGum.Renderables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Tests.GueDeriving;

public class ArcRuntimeTests
{
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
    public void ArcRuntime_StrokeWidth_DefaultShouldBe10()
    {
        ArcRuntime arcRuntime = new();
        arcRuntime.StrokeWidth.ShouldBe(10);
    }

    [Fact]
    public void ArcRuntime_Thickness_ShouldSetStrokeWithInPreRender()
    {
        ArcRuntime arcRuntime = new();

        var containedArc = (Arc)arcRuntime.RenderableComponent;
        arcRuntime.StrokeWidth = 10;
        containedArc.StrokeWidth.ShouldNotBe(10, "because the stroke width isn't applied until PreRender where the units are also considered");

        arcRuntime.PreRender();

        containedArc.StrokeWidth.ShouldBe(10);
    }
}
