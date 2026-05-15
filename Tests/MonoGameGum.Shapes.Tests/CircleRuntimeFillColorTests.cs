using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Phase 2 of #2761: full collapse of ColoredCircleRuntime into CircleRuntime. The Apos.Shapes
// side of the test split — this assembly DOES reference MonoGameGumShapes, so the
// RenderableRegistry factory for IFilledShapeRenderable is populated and CircleRuntime.FillColor
// drives a real renderable swap. The graceful-degradation cases (no factory registered) live in
// MonoGameGum.Tests/Runtimes/CircleRuntimeTests.cs.
//
// CircleRuntime lives in core MonoGameGum, so newing one does not load the MonoGameGumShapes
// assembly or fire its [ModuleInitializer]. The constructor below touches AposShapeRuntime
// explicitly so the registry is populated. The call is idempotent.
public class CircleRuntimeFillColorTests
{
    public CircleRuntimeFillColorTests()
    {
        AposShapeRuntime.RegisterRuntimeTypes();
    }

    [Fact]
    public void FillColor_AndStrokeColor_BothSet_PrefersFill()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.Blue;

        Circle renderable = sut.RenderableComponent.ShouldBeOfType<Circle>();
        renderable.IsFilled.ShouldBeTrue();
        renderable.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void FillColor_LegacyColorProperty_RoutesThroughFillRenderableAfterSwap()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;

        // Pre-#2761 the legacy `Color` setter wrote through the cached LineCircle reference,
        // which goes stale post-swap. Phase 2 routes it through whichever renderable is
        // currently contained, so this round-trip stays valid.
#pragma warning disable CS0618 // exercising deprecated routing on purpose
        sut.Color = Color.Blue;
        sut.Color.ShouldBe(Color.Blue);
#pragma warning restore CS0618

        Circle renderable = sut.RenderableComponent.ShouldBeOfType<Circle>();
        renderable.Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void FillColor_WhenClearedToNull_SwapsRenderableBackToLineCircle()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;
        sut.FillColor = null;

        sut.RenderableComponent.ShouldBeOfType<LineCircle>();
    }

    [Fact]
    public void FillColor_WhenSet_ForwardsColorToFilledRenderable()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;

        Circle renderable = sut.RenderableComponent.ShouldBeOfType<Circle>();
        renderable.IsFilled.ShouldBeTrue();
        renderable.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void FillColor_WhenSet_SwapsRenderableToFilledShape()
    {
        CircleRuntime sut = new();
        sut.RenderableComponent.ShouldBeOfType<LineCircle>();

        sut.FillColor = Color.Red;

        sut.RenderableComponent.ShouldBeOfType<Circle>();
    }

    // Load-order contract guard for #2761: BaseTestClass.Dispose calls RenderableRegistry.Reset()
    // between tests, which clears the IFilledShapeRenderable factory. AposShapeRuntime's
    // RegisterRuntimeTypes is idempotent for the ElementSave registrations (guarded by
    // _registered) but the RenderableRegistry call sits OUTSIDE that guard precisely so it can
    // be re-applied after a Reset. If somebody pulls that line back inside the guard, this test
    // catches the regression: after Reset + re-call, FillColor must still drive the swap.
    [Fact]
    public void Registration_IsRecovered_AfterRegistryResetAndRecallOfRegisterRuntimeTypes()
    {
        RenderableRegistry.Reset();
        AposShapeRuntime.RegisterRuntimeTypes();

        CircleRuntime sut = new();
        sut.FillColor = Color.Red;

        sut.RenderableComponent.ShouldBeOfType<Circle>();
    }

    [Fact]
    public void StrokeColor_WhenSet_SwapsToFillRenderableWithIsFilledFalse()
    {
        CircleRuntime sut = new();
        sut.StrokeColor = Color.Green;

        Circle renderable = sut.RenderableComponent.ShouldBeOfType<Circle>();
        renderable.IsFilled.ShouldBeFalse();
        renderable.Color.ShouldBe(Color.Green);
    }

    // Guards the OnPreRender hook the optional assembly wires up in the registered factory.
    // The runtime keeps StrokeWidth on itself (so ScreenPixel scaling can be re-resolved every
    // frame); PreRender pushes it to the renderable. If the factory stops wiring OnPreRender,
    // the renderer's PreRender walk over the renderable won't reach the runtime, and stroke
    // width never propagates.
    [Fact]
    public void StrokeWidth_PropagatesToRenderable_WhenRenderablePreRenderCalled()
    {
        CircleRuntime sut = new();
        sut.StrokeColor = Color.Green;
        sut.StrokeWidth = 7f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;

        Circle renderable = sut.RenderableComponent.ShouldBeOfType<Circle>();
        IRenderable asRenderable = renderable;

        // Renderer dispatches PreRender on the renderable, not the GUE. If the factory's
        // OnPreRender hook is intact, this propagates back to CircleRuntime.PreRender and the
        // StrokeWidth is pushed onto the renderable. If the hook is missing, the renderable's
        // StrokeWidth stays at its default (2).
        asRenderable.PreRender();

        renderable.StrokeWidth.ShouldBe(7f);
    }
}
