using MonoGameAndGum.Renderables;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Shapes.Tests;

public class ArcRuntimeTests
{
    [Fact]
    public void Alpha_ShouldAssign_ThroughSetProperty()
    {
        ArcRuntime sut = new ArcRuntime();
        sut.SetProperty("Alpha", 128);
        sut.Alpha.ShouldBe(128);
    }

    // Renderer adds the inner Apos.Shapes renderable to the layer (not the runtime/GUE wrapper),
    // so the renderer's PreRender walk only invokes PreRender on the renderable. The runtime's
    // override is reached only because RenderableShapeBase.PreRender now calls back into it via
    // the OnPreRender hook wired in SetContainedShape. This test guards that hook so the
    // unit-resolution logic in AposShapeRuntime.PreRender (e.g. ScreenPixel stroke scaling) does
    // not silently become dead code again.
    [Fact]
    public void StrokeWidth_ShouldPropagateToRenderable_WhenRenderablePreRenderCalled()
    {
        ColoredCircleRuntime sut = new ColoredCircleRuntime();
        sut.SetProperty("StrokeWidth", 8.0f);
        sut.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        var renderable = (IRenderable)sut.RenderableComponent;
        var shape = (RenderableShapeBase)sut.RenderableComponent;

        // Sanity: pristine renderable still has its hardcoded default of 2 - the runtime
        // value has not yet propagated.
        shape.StrokeWidth.ShouldBe(2);

        // Simulate the renderer's PreRender walk over layer.Renderables, which dispatches
        // on the renderable, not the GUE.
        renderable.PreRender();

        shape.StrokeWidth.ShouldBe(8);
    }
}
