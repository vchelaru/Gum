using Gum.Converters;
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
    // Issue #2791: gradient props on CircleRuntime push through to both Apos Circles so a single
    // gradient can paint fill and stroke at once (matches Skia's single-renderable behavior).
    [Fact]
    public void Gradient_PropertiesPushedToBothFillAndStrokeSlots()
    {
        CircleRuntime sut = new();

        sut.UseGradient = true;
        sut.GradientType = GradientType.Linear;
        sut.Color1 = Color.Red;
        sut.Color2 = Color.Blue;
        sut.GradientX2 = 56;
        sut.GradientInnerRadius = 4;
        sut.GradientInnerRadiusUnits = DimensionUnitType.Absolute;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];

        fill.UseGradient.ShouldBeTrue();
        stroke.UseGradient.ShouldBeTrue();
        fill.GradientType.ShouldBe(GradientType.Linear);
        stroke.GradientType.ShouldBe(GradientType.Linear);
        fill.Red1.ShouldBe(Color.Red.R);
        stroke.Red1.ShouldBe(Color.Red.R);
        fill.Blue2.ShouldBe(Color.Blue.B);
        stroke.Blue2.ShouldBe(Color.Blue.B);
        fill.GradientX2.ShouldBe(56);
        stroke.GradientX2.ShouldBe(56);
        fill.GradientInnerRadius.ShouldBe(4);
        stroke.GradientInnerRadius.ShouldBe(4);
    }

    // Issue #2797: dropshadow pushes to the fill slot only (with fallback to stroke when fill
    // is null) because Apos draws one shadow per renderable — pushing to BOTH slots would
    // render the shadow twice and visibly double up. This deliberately differs from gradient
    // (#2791) and AA (#2798), which DO push to both slots. With both slots Apos here, the
    // shadow lands on fill and stroke stays clean.
    [Fact]
    public void Dropshadow_PropertiesPushedToFillSlotOnly_NotStroke()
    {
        CircleRuntime sut = new();

        sut.HasDropshadow = true;
        sut.DropshadowColor = new Color(10, 20, 30, 40);
        sut.DropshadowOffsetX = 5;
        sut.DropshadowOffsetY = 7;
        sut.DropshadowBlurX = 2;
        sut.DropshadowBlurY = 4;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];

        fill.HasDropshadow.ShouldBeTrue();
        fill.DropshadowColor.ShouldBe(new Color(10, 20, 30, 40));
        fill.DropshadowOffsetX.ShouldBe(5);
        fill.DropshadowOffsetY.ShouldBe(7);
        fill.DropshadowBlurX.ShouldBe(2);
        fill.DropshadowBlurY.ShouldBe(4);

        // Stroke must stay shadow-free — see XML remarks on IDropshadowRenderable.
        stroke.HasDropshadow.ShouldBeFalse();
    }

    // Issue #2796: dashed-stroke props on CircleRuntime push to the stroke slot only (not
    // fill). Dashing is a stroke-mode operation — the Apos Circle's RenderDashed path is
    // guarded by !IsFilled — so pushing to fill would be ignored. Routed through PreRender
    // so ScreenPixel-scaled dash/gap stays in sync with camera zoom alongside StrokeWidth.
    [Fact]
    public void DashedStroke_PushedViaPreRender_ToStrokeSlotOnly()
    {
        CircleRuntime sut = new();

        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 1;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.StrokeDashLength = 6;
        sut.StrokeGapLength = 4;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        IRenderable asStrokeRenderable = stroke;
        asStrokeRenderable.PreRender();

        stroke.StrokeDashLength.ShouldBe(6f);
        stroke.StrokeGapLength.ShouldBe(4f);
        // Fill is the wrong place for dashing (Apos guards RenderDashed on !IsFilled) — the
        // runtime intentionally skips pushing dash/gap to the fill slot.
        fill.StrokeDashLength.ShouldBe(0f);
        fill.StrokeGapLength.ShouldBe(0f);
    }

    // Issue #2798: IsAntialiased pushes through to both slots so a single setter flips AA on
    // fill and stroke together. Default true matches Apos.Shapes' own default. The push
    // happens via PreRender (matching how StrokeWidth/StrokeDashLength flow); fire the hook
    // explicitly here to mirror the renderer's PreRender walk.
    [Fact]
    public void IsAntialiased_PushedViaPreRender_ToBothFillAndStrokeSlots()
    {
        CircleRuntime sut = new();

        sut.IsAntialiased = false;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        fill.IsAntialiased.ShouldBeFalse();
        stroke.IsAntialiased.ShouldBeFalse();
    }

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
        // IsAntialiased = false skips the Apos AA-bloom compensation so this test asserts
        // exact propagation. Compensation math is covered in StrokeWidth_AaOn_* below.
        sut.IsAntialiased = false;

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
        sut.IsAntialiased = false;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        stroke.StrokeWidth.ShouldBe(5f);
    }

    // Apos.Shapes renders ~0.5 px of antialiased bloom on each side OF the nominal stroke,
    // while Skia's SKPaint fits AA WITHIN the stroke. To give cross-backend visual parity for
    // the same user-set StrokeWidth (the contract the user reasonably expects), CircleRuntime
    // subtracts ~1 px (2 * 0.5) from the value pushed to the Apos stroke renderable when
    // IsAntialiased = true. Without compensation a "1 px" ring on MG reads ~2 px while Skia
    // reads ~1 px — visible asymmetry across the CirclesScreen sample.
    [Fact]
    public void StrokeWidth_AaOn_SubtractsBloomBeforePushingToAposStroke()
    {
        CircleRuntime sut = new();
        sut.StrokeColor = Color.Green;
        sut.StrokeWidth = 4f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.IsAntialiased = true;

        Circle stroke = (Circle)((Circle)sut.RenderableComponent).Children[0];
        IRenderable asRenderable = stroke;
        asRenderable.PreRender();

        // 4 - (2 * 0.5) = 3
        stroke.StrokeWidth.ShouldBe(3f);
    }

    // Special case: at user StrokeWidth = 1 with AA on, naive subtraction would push 0 to Apos
    // and the stroke disappears. The runtime floors the pushed value so a thin ring still
    // renders, accepting a small (~0.5 px) Skia/Apos visual mismatch at this size in exchange
    // for not vanishing.
    [Fact]
    public void StrokeWidth_AaOn_AtOnePixel_FloorsInsteadOfPushingZero()
    {
        CircleRuntime sut = new();
        sut.StrokeColor = Color.Green;
        sut.StrokeWidth = 1f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.IsAntialiased = true;

        Circle stroke = (Circle)((Circle)sut.RenderableComponent).Children[0];
        IRenderable asRenderable = stroke;
        asRenderable.PreRender();

        stroke.StrokeWidth.ShouldBeGreaterThan(0f);
        stroke.StrokeWidth.ShouldBeLessThanOrEqualTo(1f);
    }
}
