using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Phase 2 (rewrite) of #2761: CircleRuntime binds a single ICircleRenderable at construction
// from RenderableRegistry. The MonoGameGumShapes (Apos.Shapes) package overrides the core
// default by registering a factory that produces an Apos Circle implementing ICircleRenderable
// — so once the optional package is loaded, every new CircleRuntime is Apos-backed for life.
// No swap-per-property. The graceful-degradation (no factory registered) cases live in
// MonoGameGum.Tests/Runtimes/CircleRuntimeTests.cs.
//
// CircleRuntime lives in core MonoGameGum, so newing one does not load the MonoGameGumShapes
// assembly or fire its [ModuleInitializer]. The constructor below touches AposShapeRuntime
// explicitly so the registry is populated. The call is idempotent.
public class CircleRuntimeTests
{
    public CircleRuntimeTests()
    {
        AposShapeRuntime.RegisterRuntimeTypes();
    }

    [Fact]
    public void Constructor_BindsAposCircle_WhenShapesPackageRegistered()
    {
        CircleRuntime sut = new();

        sut.RenderableComponent.ShouldBeOfType<Circle>();
    }

    [Fact]
    public void FillColor_WhenSet_RendersFilled()
    {
        CircleRuntime sut = new();

        sut.FillColor = Color.Red;

        Circle renderable = sut.RenderableComponent.ShouldBeOfType<Circle>();
        renderable.IsFilled.ShouldBeTrue();
        renderable.Color.ShouldBe(Color.Red);
    }

    // Load-order contract guard for #2761: if somebody pulls the registration back inside the
    // _registered guard in AposShapeRuntime, this test catches the regression — after Reset +
    // re-call, a new CircleRuntime must still bind the Apos Circle.
    [Fact]
    public void LoadOrderRecovery_AfterRegistryResetAndRecall_StillBindsAposCircle()
    {
        RenderableRegistry.Reset();
        AposShapeRuntime.RegisterRuntimeTypes();

        CircleRuntime sut = new();

        sut.RenderableComponent.ShouldBeOfType<Circle>();
    }

    [Fact]
    public void Renderable_IsStableAcrossPropertyChanges()
    {
        CircleRuntime sut = new();
        object original = sut.RenderableComponent;

        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.Blue;
#pragma warning disable CS0618 // exercising deprecated legacy routing on purpose
        sut.Color = Color.Lime;
#pragma warning restore CS0618
        sut.Radius = 25f;
        sut.FillColor = null;
        sut.StrokeColor = null;

        sut.RenderableComponent.ShouldBeSameAs(original);
    }

    [Fact]
    public void StrokeColor_WhenSet_RendersOutline()
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
    public void StrokeWidth_PropagatesViaOnPreRenderHook()
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
