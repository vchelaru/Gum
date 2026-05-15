using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #2768 two-slot model under MonoGameGumShapes (Apos.Shapes). CircleRuntime now resolves
// both IFilledCircleRenderable AND IStrokedCircleRenderable at construction. The Apos factory
// returns a Circle{IsFilled=true} for the fill slot and a Circle{IsFilled=false} for the
// stroke slot — two Apos Circles per CircleRuntime, both drawn on the same frame. Graceful-
// degradation (no Apos package) lives in MonoGameGum.Tests/Runtimes/CircleRuntimeTests.cs.
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
    public void Constructor_BindsAposCircle_AsFillContainedObject_StrokeAsChild()
    {
        CircleRuntime sut = new();

        // Fill is the contained object so the renderer draws fill first; stroke is its first
        // child and is reached afterward. Both back ends produce Apos Circles, distinguished
        // by IsFilled.
        Circle fill = sut.RenderableComponent.ShouldBeOfType<Circle>();
        fill.IsFilled.ShouldBeTrue();
        fill.Children.Count.ShouldBe(1);
        Circle stroke = fill.Children[0].ShouldBeOfType<Circle>();
        stroke.IsFilled.ShouldBeFalse();
    }

    [Fact]
    public void FillAndStroke_DrawSimultaneously_BothColorsRoundTrip()
    {
        CircleRuntime sut = new();

        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.Blue;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        fill.Color.ShouldBe(Color.Red);
        stroke.Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void FillColor_WhenSet_WritesToFillSlot()
    {
        CircleRuntime sut = new();

        sut.FillColor = Color.Red;

        Circle fill = (Circle)sut.RenderableComponent;
        fill.IsFilled.ShouldBeTrue();
        fill.Color.ShouldBe(Color.Red);
    }

    // Load-order contract guard for #2761 / #2768: if any of the four factory registrations
    // moves back inside the _registered guard in AposShapeRuntime, this catches the
    // regression. After Reset + re-call, a new CircleRuntime must still bind Apos Circles.
    [Fact]
    public void LoadOrderRecovery_AfterRegistryResetAndRecall_StillBindsAposCircles()
    {
        RenderableRegistry.Reset();
        AposShapeRuntime.RegisterRuntimeTypes();

        CircleRuntime sut = new();

        Circle fill = sut.RenderableComponent.ShouldBeOfType<Circle>();
        fill.Children[0].ShouldBeOfType<Circle>();
    }

    [Fact]
    public void Renderable_IsStableAcrossPropertyChanges()
    {
        CircleRuntime sut = new();
        object originalFill = sut.RenderableComponent;
        object originalStroke = ((Circle)sut.RenderableComponent).Children[0];

        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.Blue;
#pragma warning disable CS0618 // exercising deprecated legacy routing on purpose
        sut.Color = Color.Lime;
#pragma warning restore CS0618
        sut.Radius = 25f;
        sut.FillColor = null;
        sut.StrokeColor = null;

        sut.RenderableComponent.ShouldBeSameAs(originalFill);
        ((Circle)sut.RenderableComponent).Children[0].ShouldBeSameAs(originalStroke);
    }

    [Fact]
    public void StrokeColor_WhenSet_WritesToStrokeSlot()
    {
        CircleRuntime sut = new();

        sut.StrokeColor = Color.Green;

        Circle stroke = (Circle)((Circle)sut.RenderableComponent).Children[0];
        stroke.IsFilled.ShouldBeFalse();
        stroke.Color.ShouldBe(Color.Green);
    }

    // Guards the OnPreRender hook the optional assembly wires up in BOTH factory
    // registrations. The runtime keeps StrokeWidth on itself (so ScreenPixel scaling can be
    // re-resolved every frame); PreRender pushes it to the stroke slot. If either factory
    // stops wiring OnPreRender, the renderer's PreRender walk over the renderable won't reach
    // the runtime, and stroke width never propagates.
    [Fact]
    public void StrokeWidth_PropagatesViaOnPreRenderHook_OnStrokeInstance()
    {
        CircleRuntime sut = new();
        sut.StrokeColor = Color.Green;
        sut.StrokeWidth = 7f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;

        Circle stroke = (Circle)((Circle)sut.RenderableComponent).Children[0];
        IRenderable asRenderable = stroke;
        asRenderable.PreRender();

        stroke.StrokeWidth.ShouldBe(7f);
    }

    // Sibling test on the fill instance: its OnPreRender must also fire (the factory wires it
    // identically). The runtime's PreRender is idempotent so being called from both slots per
    // frame is fine; pushing through the fill instance must still update the stroke's width.
    [Fact]
    public void StrokeWidth_PropagatesViaOnPreRenderHook_OnFillInstance()
    {
        CircleRuntime sut = new();
        sut.StrokeColor = Color.Green;
        sut.StrokeWidth = 5f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        stroke.StrokeWidth.ShouldBe(5f);
    }
}
