using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Gum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

#pragma warning disable CS0618 // legacy Color routing on RectangleRuntime is intentionally [Obsolete]

namespace MonoGameGum.Tests.Runtimes;

// Issue #2768 two-slot model on RectangleRuntime. Unlike CircleRuntime, core MonoGameGum
// ships defaults for BOTH slots — DefaultFilledRectangleRenderable (wraps SolidRectangle)
// and DefaultStrokedRectangleRenderable (wraps LineRectangle) — so fill and stroke both
// work without MonoGameGumShapes installed. CornerRadius is stored on both but rendered
// only on the Apos backend; that branch is covered in Tests/MonoGameGum.Shapes.Tests.
public class RectangleRuntimeTests : BaseTestClass
{
    [Fact]
    public void Constructor_BindsBothDefaults_FillContained_StrokeAsChildOfFill()
    {
        RectangleRuntime sut = new();

        // Fill is the contained object so the renderer draws it first; stroke is its first
        // child so the renderer reaches stroke after fill. User-added children append into
        // the same collection and draw on top of both.
        sut.RenderableComponent.ShouldBeOfType<DefaultFilledRectangleRenderable>();
        DefaultFilledRectangleRenderable fill = (DefaultFilledRectangleRenderable)sut.RenderableComponent;
        fill.Children.Count.ShouldBe(1);
        fill.Children[0].ShouldBeOfType<DefaultStrokedRectangleRenderable>();
    }

    [Fact]
    public void CornerRadius_StoredOnBothSlots_ButNotRendered()
    {
        // Per #2768 graceful-degradation contract: core defaults store CornerRadius for
        // round-tripping but the underlying SolidRectangle / LineRectangle draw hard
        // corners. MonoGameGumShapes is what makes corners actually round.
        RectangleRuntime sut = new();

        sut.CornerRadius = 8f;

        sut.CornerRadius.ShouldBe(8f);
        DefaultFilledRectangleRenderable fill = (DefaultFilledRectangleRenderable)sut.RenderableComponent;
        DefaultStrokedRectangleRenderable stroke = (DefaultStrokedRectangleRenderable)fill.Children[0];
        ((IFilledRectangleRenderable)fill).CornerRadius.ShouldBe(8f);
        ((IStrokedRectangleRenderable)stroke).CornerRadius.ShouldBe(8f);
    }

    [Fact]
    public void FillAndStroke_DrawSimultaneously_BothColorsRoundTrip()
    {
        // The point of the two-slot model — bordered panel / button / card with both visible.
        RectangleRuntime sut = new();

        sut.IsFilled = true;
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.Blue;

        DefaultFilledRectangleRenderable fill = (DefaultFilledRectangleRenderable)sut.RenderableComponent;
        DefaultStrokedRectangleRenderable stroke = (DefaultStrokedRectangleRenderable)fill.Children[0];
        ((IFilledRectangleRenderable)fill).Color.ShouldBe(Color.Red);
        ((IStrokedRectangleRenderable)stroke).Color.ShouldBe(Color.Blue);
    }

    [Fact]
    public void LegacyColor_RoutesToStroke()
    {
        RectangleRuntime sut = new();

        sut.Color = Color.Yellow;

        DefaultFilledRectangleRenderable fill = (DefaultFilledRectangleRenderable)sut.RenderableComponent;
        DefaultStrokedRectangleRenderable stroke = (DefaultStrokedRectangleRenderable)fill.Children[0];
        ((IStrokedRectangleRenderable)stroke).Color.ShouldBe(Color.Yellow);
    }

    [Fact]
    public void LegacyChannelSetters_ComposeColor()
    {
        RectangleRuntime sut = new();

        sut.Red = 11;
        sut.Green = 22;
        sut.Blue = 33;
        sut.Alpha = 44;

        sut.Color.ShouldBe(new Color(11, 22, 33, 44));

        DefaultFilledRectangleRenderable fill = (DefaultFilledRectangleRenderable)sut.RenderableComponent;
        DefaultStrokedRectangleRenderable stroke = (DefaultStrokedRectangleRenderable)fill.Children[0];
        ((IStrokedRectangleRenderable)stroke).Color.ShouldBe(new Color(11, 22, 33, 44));
    }

    [Fact]
    public void LegacyIsDotted_RoutesToLineRectangle()
    {
        RectangleRuntime sut = new();

        sut.IsDotted = true;

        DefaultFilledRectangleRenderable fill = (DefaultFilledRectangleRenderable)sut.RenderableComponent;
        DefaultStrokedRectangleRenderable stroke = (DefaultStrokedRectangleRenderable)fill.Children[0];
        stroke.IsDotted.ShouldBeTrue();
    }

    [Fact]
    public void LegacyLineWidth_RoutesToStrokeWidth_BypassingUnits()
    {
        RectangleRuntime sut = new();

        sut.LineWidth = 4f;

        sut.LineWidth.ShouldBe(4f);
        DefaultFilledRectangleRenderable fill = (DefaultFilledRectangleRenderable)sut.RenderableComponent;
        DefaultStrokedRectangleRenderable stroke = (DefaultStrokedRectangleRenderable)fill.Children[0];
        ((IStrokedRectangleRenderable)stroke).StrokeWidth.ShouldBe(4f);
    }

    [Fact]
    public void Renderable_IsStableAcrossPropertyChanges()
    {
        RectangleRuntime sut = new();
        object original = sut.RenderableComponent;
        object originalStroke = ((DefaultFilledRectangleRenderable)sut.RenderableComponent).Children[0];

        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.Blue;
        sut.Color = Color.Lime;
        sut.CornerRadius = 12f;
        sut.Width = 100;
        sut.Height = 60;
        // Issue #2938 — FillColor/StrokeColor are non-nullable; visibility is gated via
        // IsFilled (fill) and StrokeWidth = 0 (stroke).
        sut.IsFilled = false;
        sut.StrokeWidth = 0;

        sut.RenderableComponent.ShouldBeSameAs(original);
        ((DefaultFilledRectangleRenderable)sut.RenderableComponent).Children[0].ShouldBeSameAs(originalStroke);
    }

    // FillColor defaults to opaque white and IsFilled defaults to false, so a freshly-
    // constructed runtime renders as a stroke-only outline (the white fill is gated off).
    // Flipping IsFilled = true fills the shape white without needing to also assign FillColor.
    [Fact]
    public void FillColor_DefaultsToWhite()
    {
        RectangleRuntime sut = new();

        sut.FillColor.ShouldBe(Color.White);
    }

    [Fact]
    public void StrokeColor_DefaultsToWhite()
    {
        RectangleRuntime sut = new();

        sut.StrokeColor.ShouldBe(Color.White);
    }

    [Fact]
    public void IsFilled_DefaultsToFalse()
    {
        RectangleRuntime sut = new();

        sut.IsFilled.ShouldBeFalse();
    }

    [Fact]
    public void IsFilled_RoundTripsBackingField()
    {
        RectangleRuntime sut = new();

        sut.IsFilled = false;
        sut.IsFilled.ShouldBeFalse();

        sut.IsFilled = true;
        sut.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void FillChannelSetters_ComposeFillColor()
    {
        RectangleRuntime sut = new();

        sut.FillRed = 11;
        sut.FillGreen = 22;
        sut.FillBlue = 33;
        sut.FillAlpha = 44;

        sut.FillColor.ShouldBe(new Color(11, 22, 33, 44));
    }

    [Fact]
    public void StrokeChannelSetters_ComposeStrokeColor()
    {
        RectangleRuntime sut = new();

        sut.StrokeRed = 11;
        sut.StrokeGreen = 22;
        sut.StrokeBlue = 33;
        sut.StrokeAlpha = 44;

        sut.StrokeColor.ShouldBe(new Color(11, 22, 33, 44));
    }
}
