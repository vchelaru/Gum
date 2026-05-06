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

    // Regression for #2629 unification: Thickness must be a true façade for StrokeWidth on the
    // runtime auto-property. If somebody (a) breaks the façade so Thickness gets its own backing
    // field again, or (b) does a careless rename like replace-all "Thickness" -> "StrokeWidth"
    // and produces a self-recursive property, this catches it: the build either fails outright
    // or the round-trip stops agreeing.
    [Fact]
    public void Thickness_AndStrokeWidth_ShouldBeAliased_OnArcRuntime()
    {
        ArcRuntime sut = new ArcRuntime();

        sut.Thickness = 7;
        sut.StrokeWidth.ShouldBe(7);

        sut.StrokeWidth = 12;
        sut.Thickness.ShouldBe(12);
    }

    // Regression for #2629 unification: the legacy default thickness of an Arc is 10. ArcRuntime
    // ctor must seed StrokeWidth=10 so PreRender doesn't push 0 to the renderable. This guards
    // anyone who refactors the ctor (or deletes the seed line) from silently making every default
    // Arc render invisible.
    [Fact]
    public void Default_ArcRuntime_ShouldRenderWithStrokeWidth10_AfterPreRender()
    {
        ArcRuntime sut = new ArcRuntime();
        sut.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        sut.Thickness.ShouldBe(10);
        sut.StrokeWidth.ShouldBe(10);

        var renderable = (IRenderable)sut.RenderableComponent;
        var shape = (RenderableShapeBase)sut.RenderableComponent;

        renderable.PreRender();

        shape.StrokeWidth.ShouldBe(10);
    }

    // Regression for #2629 unification: setting Thickness via SetProperty (the path .gumx default
    // state uses to push the "Thickness" variable onto a loaded ArcRuntime) must survive PreRender.
    // Pre-unification, Thickness lived on its own field and PreRender ignored it - so this test
    // would have passed for the wrong reason. Post-unification, Thickness routes through the
    // runtime's StrokeWidth auto-property and PreRender is the thing that pushes it through.
    [Fact]
    public void Thickness_OnArc_ShouldSurvivePreRender_WhenSetThroughSetProperty()
    {
        ArcRuntime sut = new ArcRuntime();
        sut.SetProperty("Thickness", 6.0f);
        sut.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        var renderable = (IRenderable)sut.RenderableComponent;
        var shape = (RenderableShapeBase)sut.RenderableComponent;

        renderable.PreRender();

        shape.StrokeWidth.ShouldBe(6);
    }

    // Regression for #2629: SetProperty("StrokeWidth", x) on ArcRuntime must survive PreRender.
    // The other shapes (RoundedRectangle, Line, ColoredCircle) special-case StrokeWidth in
    // CustomSetPropertyOnRenderable so the value lands on the runtime's auto-property; PreRender
    // then pushes the same value back to the renderable. Arc was missing that case, so SetProperty
    // wrote 8 directly to the renderable, the runtime's StrokeWidth stayed at 0, and PreRender
    // clobbered the renderable back to 0.
    [Fact]
    public void StrokeWidth_OnArc_ShouldSurvivePreRender_WhenSetThroughSetProperty()
    {
        ArcRuntime sut = new ArcRuntime();
        sut.SetProperty("StrokeWidth", 8.0f);
        sut.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        var renderable = (IRenderable)sut.RenderableComponent;
        var shape = (RenderableShapeBase)sut.RenderableComponent;

        renderable.PreRender();

        shape.StrokeWidth.ShouldBe(8);
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
