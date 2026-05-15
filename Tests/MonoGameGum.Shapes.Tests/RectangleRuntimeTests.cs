using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #2768 two-slot model on RectangleRuntime under MonoGameGumShapes (Apos.Shapes). Both
// slots resolve to Apos RoundedRectangle — fill with IsFilled=true, stroke with IsFilled=false.
// CornerRadius is honored visually on both draws (unlike core's SolidRectangle/LineRectangle
// defaults, which store but don't render it).
public class RectangleRuntimeTests
{
    public RectangleRuntimeTests()
    {
        AposShapeRuntime.RegisterRuntimeTypes();
    }

    [Fact]
    public void Constructor_BindsAposRoundedRectangle_AsBothSlots()
    {
        RectangleRuntime sut = new();

        RoundedRectangle fill = sut.RenderableComponent.ShouldBeOfType<RoundedRectangle>();
        fill.IsFilled.ShouldBeTrue();
        fill.Children.Count.ShouldBe(1);
        RoundedRectangle stroke = fill.Children[0].ShouldBeOfType<RoundedRectangle>();
        stroke.IsFilled.ShouldBeFalse();
    }

    [Fact]
    public void CornerRadius_PushedToBothSlots()
    {
        // Apos.Shapes honors CornerRadius on both fill and stroke draws (rounded fill + rounded
        // outline), so the runtime mirrors the value to both slots. Pre-collapse this was a
        // RoundedRectangleRuntime-only property.
        RectangleRuntime sut = new();

        sut.CornerRadius = 8f;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        fill.CornerRadius.ShouldBe(8f);
        stroke.CornerRadius.ShouldBe(8f);
    }

    [Fact]
    public void FillAndStroke_DrawSimultaneously_BothColorsRoundTrip()
    {
        RectangleRuntime sut = new();

        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.Blue;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        fill.Color.ShouldBe(Color.Red);
        stroke.Color.ShouldBe(Color.Blue);
    }

    // Load-order contract guard: the four factory registrations live OUTSIDE the _registered
    // guard so they survive RenderableRegistry.Reset between Initialize cycles. If any of them
    // moves back inside, a Reset + re-call leaves the rectangle slots null.
    [Fact]
    public void LoadOrderRecovery_AfterRegistryResetAndRecall_StillBindsAposRectangles()
    {
        RenderableRegistry.Reset();
        AposShapeRuntime.RegisterRuntimeTypes();

        RectangleRuntime sut = new();

        RoundedRectangle fill = sut.RenderableComponent.ShouldBeOfType<RoundedRectangle>();
        fill.Children[0].ShouldBeOfType<RoundedRectangle>();
    }

    [Fact]
    public void StrokeWidth_PropagatesViaOnPreRenderHook_OnFillInstance()
    {
        RectangleRuntime sut = new();
        sut.StrokeColor = Color.Green;
        sut.StrokeWidth = 5f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        stroke.StrokeWidth.ShouldBe(5f);
    }

    [Fact]
    public void StrokeWidth_PropagatesViaOnPreRenderHook_OnStrokeInstance()
    {
        RectangleRuntime sut = new();
        sut.StrokeColor = Color.Green;
        sut.StrokeWidth = 7f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;

        RoundedRectangle stroke = (RoundedRectangle)((RoundedRectangle)sut.RenderableComponent).Children[0];
        IRenderable asRenderable = stroke;
        asRenderable.PreRender();

        stroke.StrokeWidth.ShouldBe(7f);
    }
}
