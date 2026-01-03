using Shouldly;
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Tests.GueDeriving;

public class ArcRuntimeTests
{
    [Fact]
    public void ArcRuntime_ShouldCreateArc()
    {
        ArcRuntime arcRuntime = new();

        arcRuntime.RenderableComponent.ShouldNotBeNull();
        (arcRuntime.RenderableComponent is Arc).ShouldBeTrue();
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
