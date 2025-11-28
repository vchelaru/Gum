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

        arcRuntime.StrokeWidth = 10;

        arcRuntime.PreRender();

        var containedArc = (Arc)arcRuntime.RenderableComponent;
        containedArc.StrokeWidth.ShouldBe(10);
    }
}
