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

    // Same propagation guard as StrokeWidth: dashed-stroke values live on the runtime so the
    // ScreenPixel scaling in AposShapeRuntime.PreRender stays in sync with StrokeWidth, then get
    // pushed to the renderable each frame. If this test starts seeing the dashed values arrive
    // pre-PreRender, somebody simplified the runtime to a passthrough and the screen-pixel
    // scaling will silently stop working at non-1.0 camera zoom.
    [Fact]
    public void StrokeDashAndGap_ShouldPropagateToRenderable_WhenRenderablePreRenderCalled()
    {
        ColoredCircleRuntime sut = new ColoredCircleRuntime();
        sut.SetProperty("StrokeDashLength", 6.0f);
        sut.SetProperty("StrokeGapLength", 4.0f);
        sut.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        var renderable = (IRenderable)sut.RenderableComponent;
        var shape = (RenderableShapeBase)sut.RenderableComponent;

        // Sanity: pristine renderable defaults are 0 - the runtime values have not yet propagated.
        shape.StrokeDashLength.ShouldBe(0);
        shape.StrokeGapLength.ShouldBe(0);

        renderable.PreRender();

        shape.StrokeDashLength.ShouldBe(6);
        shape.StrokeGapLength.ShouldBe(4);
    }
}
