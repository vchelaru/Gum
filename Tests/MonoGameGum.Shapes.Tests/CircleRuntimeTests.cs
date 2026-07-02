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
        // Issue #3112 — the slot-override factories now gate on ShapeRenderer.IsInitialized.
        // These tests exercise the Apos two-slot model headlessly (no GraphicsDevice), so force
        // the flag on. ShapeRendererGateTests covers the not-initialized fallback path.
        ShapeRenderer.Self.SetIsInitializedForTesting(true);
    }

    // Issue #2937 — the two-slot model draws fill and stroke as separate renderables. The user
    // sets one Blend for the shape, so it must reach BOTH slots: ShapeRenderer.EnsureBlend keys
    // off each renderable's Blend, so if the stroke kept the default the batch would flip back
    // to Normal when drawing it. The runtime forwards Blend to both slots.
    [Fact]
    public void Blend_ForwardsToBothSlots()
    {
        CircleRuntime sut = new();

        sut.SetProperty("Blend", Gum.RenderingLibrary.Blend.Additive);

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        fill.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
        stroke.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
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

        sut.IsFilled = true;
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

        sut.IsFilled = true;
        sut.FillColor = Color.Red;

        Circle fill = (Circle)sut.RenderableComponent;
        fill.IsFilled.ShouldBeTrue();
        fill.Color.ShouldBe(Color.Red);
    }

    // Issue #2834 — when both fill and stroke are visible, the fill is rendered with its
    // radius inset by FillRadiusInset so its outer AA halo sits inside the stroke's opaque
    // band. Without this inset, fill AA and stroke AA composite at the same boundary,
    // producing a visible color bleed (red fringe outside a white stroke on Apos). Inset
    // equals the AA-compensated stroke width: 4 - 1 (AA contribution) = 3 px per side.
    //
    // The inset is pushed via FillRadiusInset rather than mutating fill.Width because the
    // fill renderable IS the runtime's contained sizing object — mutating Width would feed
    // back into layout and accumulate frame-over-frame until the circle vanished.
    [Fact]
    public void FillRadiusInset_AaOn_WhenStrokeVisible_PushedAaCompensatedStrokeWidthInPreRender()
    {
        CircleRuntime sut = new();
        sut.Width = 56;
        sut.Height = 56;
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 4f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.IsAntialiased = true;

        Circle fill = (Circle)sut.RenderableComponent;
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        fill.FillRadiusInset.ShouldBe(3f);
    }

    // At hairline strokes (StrokeWidth = 1, AA on) the AA-compensated stroke width is
    // sub-pixel epsilon, but the AA halo itself is still 1 px. Inset must be at least the AA
    // contribution (1 px) so the fill's outer AA halo doesn't overlap the stroke's AA range.
    [Fact]
    public void FillRadiusInset_AaOn_AtHairlineStroke_FlooredAtAaContribution()
    {
        CircleRuntime sut = new();
        sut.Width = 56;
        sut.Height = 56;
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 1f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.IsAntialiased = true;

        Circle fill = (Circle)sut.RenderableComponent;
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        fill.FillRadiusInset.ShouldBe(1f);
    }

    // When the stroke is invisible (StrokeWidth == 0) the fill should render at full radius —
    // no inset, no background ring where the stroke would have been. This guards against
    // fill-only mode rendering with an unwanted gap.
    [Fact]
    public void FillRadiusInset_WhenStrokeInvisible_StaysZero()
    {
        CircleRuntime sut = new();
        sut.Width = 56;
        sut.Height = 56;
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 0f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;

        Circle fill = (Circle)sut.RenderableComponent;
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        fill.FillRadiusInset.ShouldBe(0f);
    }

    // Regression guard for the bug that triggered the FillRadiusInset approach: mutating
    // fill.Width/Height in PreRender accumulated each frame (because the fill IS the
    // runtime's contained sizing object) and the circle shrank to zero in ~5 frames. Calling
    // PreRender repeatedly must leave fill.Width/Height untouched at the runtime's size.
    [Fact]
    public void PreRender_RepeatedCalls_DoesNotMutateFillWidthOrHeight()
    {
        CircleRuntime sut = new();
        sut.Width = 56;
        sut.Height = 56;
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 4f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.IsAntialiased = true;

        Circle fill = (Circle)sut.RenderableComponent;
        IRenderable asFillRenderable = fill;
        for (int i = 0; i < 10; i++)
        {
            asFillRenderable.PreRender();
        }

        fill.Width.ShouldBe(56f);
        fill.Height.ShouldBe(56f);
    }

    // Load-order contract guard for #2761 / #2768: if any of the four factory registrations
    // moves back inside the _registered guard in AposShapeRuntime, this catches the
    // regression. After Reset + re-call, a new CircleRuntime must still bind Apos Circles.
    // Issue #2791 / #3009: gradient coordinate and Color2 props push through to BOTH Apos Circles
    // so the values round-trip on either slot. The gradient START is per-slot now — each slot
    // mirrors its own body color (see UseGradient_GradientStart_* tests) rather than a shared
    // standalone Color1. The UseGradient GATE routes to the active body slot (fill when IsFilled,
    // else stroke — see UseGradient_When* tests). IsFilled is set true here so the gate lands
    // deterministically on the fill; the shared params are inert on the stroke.
    [Fact]
    public void Gradient_PropertiesPushedToBothFillAndStrokeSlots()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;

        sut.UseGradient = true;
        sut.GradientType = GradientType.Linear;
        sut.Color2 = Color.Blue;
        sut.GradientX2 = 56;
        sut.GradientInnerRadius = 4;
        sut.GradientInnerRadiusUnits = DimensionUnitType.Absolute;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];

        fill.UseGradient.ShouldBeTrue();
        stroke.UseGradient.ShouldBeFalse();
        fill.GradientType.ShouldBe(GradientType.Linear);
        stroke.GradientType.ShouldBe(GradientType.Linear);
        fill.Blue2.ShouldBe(Color.Blue.B);
        stroke.Blue2.ShouldBe(Color.Blue.B);
        fill.GradientX2.ShouldBe(56);
        stroke.GradientX2.ShouldBe(56);
        fill.GradientInnerRadius.ShouldBe(4);
        stroke.GradientInnerRadius.ShouldBe(4);
    }

    // The gradient paints the ACTIVE body: the fill when IsFilled, the stroke when stroke-only.
    // The inactive slot renders solid (its gradient gate stays off) so the two never share the
    // single gradient and composite invisibly.
    //
    // Filled: gradient fills the disk, stroke renders its solid StrokeColor.
    [Fact]
    public void UseGradient_WhenFilled_RoutesGradientToFill_StrokeSolid()
    {
        CircleRuntime sut = new();
        sut.Width = 56;
        sut.Height = 56;
        sut.IsFilled = true;
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 4f;
        sut.Color2 = Color.Green;
        sut.Alpha2 = 255;

        sut.UseGradient = true;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];

        fill.UseGradient.ShouldBeTrue();
        stroke.UseGradient.ShouldBeFalse();
        stroke.ShouldPaintGradient(forcedColor: null).ShouldBeFalse();
        stroke.Color.ShouldBe(Color.White);
    }

    // Stroke-only (IsFilled = false): the gradient paints the stroke; the (invisible) fill slot's
    // gate is off. Without this the gradient rendered on the gated-transparent fill while the
    // stroke showed solid — the reported bug.
    [Fact]
    public void UseGradient_WhenStrokeOnly_RoutesGradientToStroke_FillOff()
    {
        CircleRuntime sut = new();
        sut.Width = 56;
        sut.Height = 56;
        sut.IsFilled = false;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 4f;
        sut.Color2 = Color.Green;
        sut.Alpha2 = 255;

        sut.UseGradient = true;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];

        stroke.UseGradient.ShouldBeTrue();
        stroke.ShouldPaintGradient(forcedColor: null).ShouldBeTrue();
        fill.UseGradient.ShouldBeFalse();
    }

    // Toggling IsFilled re-routes the gradient to the new active slot (mirror of the dropshadow
    // SyncDropshadowToTarget re-routing), so the gate is never stranded on the wrong slot.
    [Fact]
    public void UseGradient_IsFilledToggle_RoutesGradientToActiveSlot()
    {
        CircleRuntime sut = new();
        sut.Width = 56;
        sut.Height = 56;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 4f;
        sut.Color2 = Color.Green;
        sut.Alpha2 = 255;
        sut.UseGradient = true;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];

        sut.IsFilled = true;
        fill.UseGradient.ShouldBeTrue();
        stroke.UseGradient.ShouldBeFalse();

        sut.IsFilled = false;
        fill.UseGradient.ShouldBeFalse();
        stroke.UseGradient.ShouldBeTrue();
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

        // Dropshadow routes to the fill slot only when the shape is filled; IsFilled now
        // defaults to false, so opt the fill in to exercise the fill-slot routing.
        sut.IsFilled = true;
        sut.HasDropshadow = true;
        sut.DropshadowColor = new Color(10, 20, 30, 40);
        sut.DropshadowOffsetX = 5;
        sut.DropshadowOffsetY = 7;
        sut.DropshadowBlur = 2;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];

        fill.HasDropshadow.ShouldBeTrue();
        fill.DropshadowColor.ShouldBe(new Color(10, 20, 30, 40));
        fill.DropshadowOffsetX.ShouldBe(5);
        fill.DropshadowOffsetY.ShouldBe(7);
        fill.GetShadowAntiAliasSize(cameraZoom: 1f).ShouldBe(2);

        // Stroke must stay shadow-free — see XML remarks on IDropshadowRenderable.
        stroke.HasDropshadow.ShouldBeFalse();
    }

    // Issue #2977 — a negative DropshadowBlur is meaningless (blur is a radius) and used to make
    // the shadow vanish on the stroke path: it became a negative Apos aaSize, which the shader
    // won't draw. Negative blur is clamped to 0 so it renders identically to DropshadowBlur = 0.
    // Driven through the user-facing scalar DropshadowBlur and asserted via the rendered shadow
    // halo size (aaSize), so the test never names the per-axis blur fields.
    [Fact]
    public void DropshadowBlur_Negative_RendersAsZero()
    {
        CircleRuntime sut = new();

        sut.DropshadowBlur = -5f;

        Circle fill = (Circle)sut.RenderableComponent;
        fill.GetShadowAntiAliasSize(cameraZoom: 1f).ShouldBe(0);
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
        sut.Radius = 25f;
#pragma warning restore CS0618
        sut.IsFilled = false;
        sut.StrokeWidth = 0;

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

    // Apos.Shapes' DrawCircle adds exactly 1 px of antialiased halo OUTSIDE the nominal
    // stroke thickness (see Runtimes/GumShapes/Renderables/Circle.cs — aaSize passed as 1
    // when IsAntialiased is true). Skia fits AA WITHIN the thickness instead, so the same
    // user-set StrokeWidth would otherwise read 1 px wider on Apos. CircleRuntime subtracts
    // the 1 px before pushing to give visual parity across backends.
    [Fact]
    public void StrokeWidth_AaOn_SubtractsAaContributionBeforePushingToAposStroke()
    {
        CircleRuntime sut = new();
        sut.StrokeColor = Color.Green;
        sut.StrokeWidth = 4f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.IsAntialiased = true;

        Circle stroke = (Circle)((Circle)sut.RenderableComponent).Children[0];
        IRenderable asRenderable = stroke;
        asRenderable.PreRender();

        // 4 - 1 = 3
        stroke.StrokeWidth.ShouldBe(3f);
    }

    // At user StrokeWidth = 1 with AA on, the compensation pushes a tiny epsilon (not 0) to
    // Apos so its shader can't interpret the value as "don't draw". The 1 px AA halo
    // dominates the visible width — exact match for Skia's 1-px-with-AA visible width.
    [Fact]
    public void StrokeWidth_AaOn_AtOnePixel_PushesEpsilonForPureAaHalo()
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
        stroke.StrokeWidth.ShouldBeLessThan(0.1f);
        stroke.IsAntialiased.ShouldBeTrue();
    }

    // Issue #2790 — Clone must re-resolve fresh _fill / _stroke slots from
    // RenderableRegistry so the clone is fully independent of the source. Without the
    // override, MemberwiseClone shallow-copies _fill and _stroke fields and the clone
    // mutates the source's slots. Mirrors the equivalent Skia-side guard.
    [Fact]
    public void Clone_BothSlots_AreFreshFactoryInstances_NotShallowCopiesOfSource()
    {
        CircleRuntime source = new();
        source.FillColor = Color.Red;
        source.StrokeColor = Color.Blue;

        CircleRuntime clone = (CircleRuntime)source.Clone();

        Circle sourceFill = (Circle)source.RenderableComponent;
        Circle cloneFill = (Circle)clone.RenderableComponent;
        Circle sourceStroke = (Circle)sourceFill.Children[0];
        Circle cloneStroke = (Circle)cloneFill.Children[0];

        cloneFill.ShouldNotBeSameAs(sourceFill);
        cloneStroke.ShouldNotBeSameAs(sourceStroke);
    }

    [Fact]
    public void Clone_MutatingClone_DoesNotMutateSource()
    {
        CircleRuntime source = new();
        source.IsFilled = true;
        source.FillColor = Color.Red;
        source.StrokeColor = Color.Blue;

        CircleRuntime clone = (CircleRuntime)source.Clone();
        clone.FillColor = Color.Green;
        clone.StrokeColor = Color.Yellow;

        Circle sourceFill = (Circle)source.RenderableComponent;
        Circle sourceStroke = (Circle)sourceFill.Children[0];
        sourceFill.Color.ShouldBe(Color.Red);
        sourceStroke.Color.ShouldBe(Color.Blue);
    }

    // Boyscout fix (shape-gradient-dropshadow-dedup): neither Gradient nor Dropshadow state was
    // fully re-fired onto the clone's freshly-rebuilt slots. Backing fields survived via
    // MemberwiseClone, but the clone's own fill/stroke are brand-new renderable instances at
    // their factory defaults — a clone with UseGradient / GradientType / Color2 set never
    // reflected that on its own slots until some other property write happened to re-trigger it.
    [Fact]
    public void Clone_PushesGradientAndDropshadowState_ToClonesOwnRenderableSlots()
    {
        CircleRuntime source = new();
        source.IsFilled = true;
        source.FillColor = Color.Red;
        source.UseGradient = true;
        source.GradientType = GradientType.Linear;
        source.Color2 = Color.Blue;
        source.GradientX2 = 56;
        source.HasDropshadow = true;
        source.DropshadowColor = new Color(10, 20, 30, 40);
        source.DropshadowOffsetX = 5;

        CircleRuntime clone = (CircleRuntime)source.Clone();

        Circle cloneFill = (Circle)clone.RenderableComponent;
        Circle cloneStroke = (Circle)cloneFill.Children[0];

        cloneFill.UseGradient.ShouldBeTrue();
        cloneStroke.UseGradient.ShouldBeFalse();
        cloneFill.GradientType.ShouldBe(GradientType.Linear);
        cloneFill.Blue2.ShouldBe(Color.Blue.B);
        cloneStroke.Blue2.ShouldBe(Color.Blue.B);
        cloneFill.GradientX2.ShouldBe(56);
        cloneFill.HasDropshadow.ShouldBeTrue();
        cloneFill.DropshadowColor.ShouldBe(new Color(10, 20, 30, 40));
        cloneFill.DropshadowOffsetX.ShouldBe(5);
        cloneStroke.HasDropshadow.ShouldBeFalse();
    }

    // Issue #2790 — runtime no longer compensates dash/gap. The Apos renderable (Gum.Shapes
    // Circle.RenderDashed) inflates the effective gap by aaSize internally when AA is on, so
    // dashes stay visually distinct without the runtime needing to second-guess the user's
    // values. This test guards that the runtime pushes the raw values through unchanged even
    // with AA on.
    [Fact]
    public void DashedStroke_AaOn_PushesRawValuesUnchanged()
    {
        CircleRuntime sut = new();
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 1;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.StrokeDashLength = 6;
        sut.StrokeGapLength = 4;
        sut.IsAntialiased = true;

        Circle stroke = (Circle)((Circle)sut.RenderableComponent).Children[0];
        IRenderable asRenderable = stroke;
        asRenderable.PreRender();

        stroke.StrokeDashLength.ShouldBe(6f);
        stroke.StrokeGapLength.ShouldBe(4f);
    }

    // Issue #2925 — when MonoGameGumShapes is loaded, CircleRuntime's constructor sets
    // the fill renderable (Apos Circle with IsFilled=true) as mContainedObjectAsIpso.
    // The tool's variable-application path dispatches on the contained renderable via
    // CustomSetPropertyOnRenderable, so the legacy "Color" variable (historically the
    // stroke color on a LineCircle-backed Circle) now writes to the FILL renderable
    // instead of the stroke renderable. Result: a freshly-loaded default Circle renders
    // as a solid white disc instead of a stroke-only outline.
    //
    // The fix must keep "Color" reaching _stroke.Color (matching pre-Apos behavior). After
    // the #2938 regression fix the fill defaults to transparent (alpha 0) — IsFilled gates
    // visibility but the color itself stays alpha 0 until the user lights it up — so the
    // legacy stroke routing must leave fill untouched (still alpha 0).
    [Fact]
    public void SetProperty_Color_RoutesToStroke_NotFill()
    {
        CircleRuntime sut = new();

        sut.SetProperty("Color", System.Drawing.Color.FromArgb(255, 255, 0, 0));

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.Color.ShouldBe(new Color(255, 0, 0, 255));
        fill.Color.ShouldBe(new Color(0, 0, 0, 0));
    }

    [Fact]
    public void SetProperty_Alpha_RoutesToStroke_NotFill()
    {
        CircleRuntime sut = new();

        sut.SetProperty("Alpha", 128);

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.Color.A.ShouldBe((byte)128);
        fill.Color.ShouldBe(new Color(0, 0, 0, 0));
    }

    // Issue #2938 — IsFilled now gates fill visibility. Setting IsFilled = false should push
    // a transparent color into the fill slot so only the stroke draws.
    [Fact]
    public void IsFilled_False_HidesFillSlot()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;

        sut.IsFilled = false;

        Circle fill = (Circle)sut.RenderableComponent;
        fill.Color.A.ShouldBe((byte)0);
    }

    // Setting IsFilled back to true after toggling it off should restore the fill slot's
    // color to the runtime's FillColor (the backing field round-trips through IsFilled
    // changes; the renderable color follows the gate).
    [Fact]
    public void IsFilled_True_AfterFalse_RestoresFillColor()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;
        sut.IsFilled = false;

        sut.IsFilled = true;

        Circle fill = (Circle)sut.RenderableComponent;
        fill.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void FillChannelSetters_PushToFillSlot()
    {
        CircleRuntime sut = new();

        sut.IsFilled = true;
        sut.FillRed = 10;
        sut.FillGreen = 20;
        sut.FillBlue = 30;
        sut.FillAlpha = 200;

        Circle fill = (Circle)sut.RenderableComponent;
        fill.Color.ShouldBe(new Color(10, 20, 30, 200));
    }

    [Fact]
    public void StrokeChannelSetters_PushToStrokeSlot()
    {
        CircleRuntime sut = new();

        sut.StrokeRed = 10;
        sut.StrokeGreen = 20;
        sut.StrokeBlue = 30;
        sut.StrokeAlpha = 200;

        Circle stroke = (Circle)((Circle)sut.RenderableComponent).Children[0];
        stroke.Color.ShouldBe(new Color(10, 20, 30, 200));
    }

    // Issue #2931 — plain CircleRuntime now exposes IsFilled / StrokeWidth /
    // StrokeDashLength / StrokeGapLength in the tool's default state. The shape-side
    // SetProperty dispatcher previously hard-cast to ColoredCircleRuntime for these
    // names, which throws InvalidCastException when the GUE is a plain CircleRuntime.
    [Fact]
    public void SetProperty_StrokeDashLength_RoutesToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("StrokeDashLength", 6f);

        sut.StrokeDashLength.ShouldBe(6f);
    }

    [Fact]
    public void SetProperty_StrokeGapLength_RoutesToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("StrokeGapLength", 4f);

        sut.StrokeGapLength.ShouldBe(4f);
    }

    [Fact]
    public void SetProperty_StrokeWidth_RoutesToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("StrokeWidth", 7f);

        sut.StrokeWidth.ShouldBe(7f);
    }

    // Issue #2931 — IsFilled must route through the runtime's two-slot gate, not the
    // fill renderable's own IsFilled (which would change the Apos shader mode of the fill
    // Circle instead of toggling fill visibility). Reproduces the user-reported "IsFilled
    // checkbox does nothing" symptom.
    [Fact]
    public void SetProperty_IsFilled_True_LightsUpFillSlotWithFillColor()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;
        sut.IsFilled = false;

        sut.SetProperty("IsFilled", true);

        Circle fill = (Circle)sut.RenderableComponent;
        fill.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void SetProperty_IsFilled_False_HidesFillSlot()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;

        sut.SetProperty("IsFilled", false);

        Circle fill = (Circle)sut.RenderableComponent;
        fill.Color.A.ShouldBe((byte)0);
    }

    // Issue #2931 — FillRed / FillGreen / FillBlue / FillAlpha (and the Stroke counterparts)
    // live on the runtime, not on the Apos Circle renderable. Without an explicit route the
    // SetProperty path falls through to SetPropertyThroughReflection on the renderable, finds
    // no matching property, and silently does nothing — leaving the fill at the runtime's
    // ctor default of (0,0,0,0) even though the variable grid shows 255s.
    [Fact]
    public void SetProperty_FillChannels_RouteToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("FillRed", 255);
        sut.SetProperty("FillGreen", 255);
        sut.SetProperty("FillBlue", 255);
        sut.SetProperty("FillAlpha", 255);

        sut.FillColor.ShouldBe(new Color(255, 255, 255, 255));
    }

    [Fact]
    public void SetProperty_StrokeChannels_RouteToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("StrokeRed", 10);
        sut.SetProperty("StrokeGreen", 20);
        sut.SetProperty("StrokeBlue", 30);
        sut.SetProperty("StrokeAlpha", 200);

        sut.StrokeColor.ShouldBe(new Color(10, 20, 30, 200));
    }

    // Issue: with IsFilled = false, DropshadowTarget routed to the fill slot whose color is
    // gated transparent — invisible silhouette can't cast a visible shadow, so no dropshadow
    // appeared. ColoredCircleRuntime (single-slot) didn't hit this because its one shape
    // carries the dropshadow regardless of fill/stroke mode. Fix: route to the stroke slot
    // when IsFilled = false.
    [Fact]
    public void HasDropshadow_True_StrokeOnly_RoutesToStrokeSlot()
    {
        CircleRuntime sut = new();
        sut.IsFilled = false;

        sut.HasDropshadow = true;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.HasDropshadow.ShouldBeTrue();
        fill.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void HasDropshadow_True_Filled_RoutesToFillSlot()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;

        sut.HasDropshadow = true;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        fill.HasDropshadow.ShouldBeTrue();
        stroke.HasDropshadow.ShouldBeFalse();
    }

    // When IsFilled toggles, the dropshadow has to follow the target — and the previous
    // target needs to release its shadow flag, otherwise toggling produces ghost shadows on
    // both slots simultaneously.
    [Fact]
    public void IsFilled_False_AfterHasDropshadow_MovesShadowFromFillToStroke()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.HasDropshadow = true;

        sut.IsFilled = false;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.HasDropshadow.ShouldBeTrue();
        fill.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void IsFilled_True_AfterHasDropshadow_MovesShadowFromStrokeToFill()
    {
        CircleRuntime sut = new();
        sut.IsFilled = false;
        sut.HasDropshadow = true;

        sut.IsFilled = true;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        fill.HasDropshadow.ShouldBeTrue();
        stroke.HasDropshadow.ShouldBeFalse();
    }

    // Issue: state-load from .gusx routes every dropshadow variable through the SetProperty
    // dispatcher. Until #2931's follow-up, that dispatcher's Circle branch had no case for any
    // dropshadow name, so the names fell through to TrySetPropertiesOnRenderableBase which
    // wrote them to the fill Apos Circle directly. The runtime's HasDropshadow setter (and
    // therefore SyncDropshadowToTarget) never fired on .gusx load, leaving the shadow stranded
    // on the fill slot with its gated-transparent color — invisible per EffectiveDropshadowColor.
    [Fact]
    public void SetProperty_HasDropshadow_StrokeOnly_RoutesToStrokeSlot()
    {
        CircleRuntime sut = new();
        sut.IsFilled = false;

        sut.SetProperty("HasDropshadow", true);

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.HasDropshadow.ShouldBeTrue();
        fill.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void SetProperty_DropshadowOffsetAndBlur_RouteToActiveSlot_WhenStrokeOnly()
    {
        CircleRuntime sut = new();
        sut.IsFilled = false;
        sut.HasDropshadow = true;

        sut.SetProperty("DropshadowOffsetX", 19f);
        sut.SetProperty("DropshadowOffsetY", 11f);
        sut.SetProperty("DropshadowBlur", 3f);

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.DropshadowOffsetX.ShouldBe(19f);
        stroke.DropshadowOffsetY.ShouldBe(11f);
        stroke.GetShadowAntiAliasSize(cameraZoom: 1f).ShouldBe(3);
    }

    [Fact]
    public void SetProperty_DropshadowChannels_RouteToActiveSlot_WhenStrokeOnly()
    {
        CircleRuntime sut = new();
        sut.IsFilled = false;
        sut.HasDropshadow = true;

        sut.SetProperty("DropshadowAlpha", 200);
        sut.SetProperty("DropshadowRed", 50);
        sut.SetProperty("DropshadowGreen", 100);
        sut.SetProperty("DropshadowBlue", 150);

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.DropshadowColor.ShouldBe(new Color(50, 100, 150, 200));
    }

    // Issue #2950 follow-up — when the user sets StrokeWidth = 0 (or negative), the runtime's
    // PreRender previously clamped to a 0.01 epsilon floor "to hedge against Apos treating
    // thickness = 0 as don't draw." That hedge defeated the intent: StrokeColor is non-nullable
    // now (#2938) and StrokeWidth = 0 is the canonical hide-stroke gate. With the epsilon push,
    // the stroke renderable still had StrokeWidth > 0 and Apos painted its 1 px AA fringe,
    // showing a hairline of stroke color the user thought they had disabled. Honor a non-
    // positive user value as a literal 0 so the renderable's HasVisibleOutput gate suppresses
    // it entirely.

    [Fact]
    public void PreRender_StrokeWidthZero_PushesZeroToStrokeRenderable()
    {
        CircleRuntime sut = new();
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.StrokeWidth = 0f;

        ((IRenderable)sut.RenderableComponent).PreRender();

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.StrokeWidth.ShouldBe(0f);
        stroke.HasVisibleOutput.ShouldBeFalse();
    }

    [Fact]
    public void PreRender_StrokeWidthNegative_PushesZeroToStrokeRenderable()
    {
        CircleRuntime sut = new();
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.StrokeWidth = -3f;

        ((IRenderable)sut.RenderableComponent).PreRender();

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.StrokeWidth.ShouldBe(0f);
        stroke.HasVisibleOutput.ShouldBeFalse();
    }

    // Issue #2936 — at high camera zoom with a thin ScreenPixel stroke the user saw a visible
    // gap between fill and stroke. Root cause: the aposAaContribution = 1 constant is in
    // SCREEN pixels (Apos's hardcoded 1 px AA halo) but the surrounding math operates in
    // world units. At Zoom > 1, the inset (= aaContribution) was too large relative to the
    // visible 1 px stroke, so the fill receded farther than the stroke extended, opening a
    // ring of background between them. Fix: convert aaContribution to world units (divide by
    // zoom) before using it in either the AA-compensation subtraction or the inset clamp.

    [Fact]
    public void PreRender_StrokeWidthOneScreenPixel_AtZoom4_PushesQuarterWorldInsetToFill()
    {
        float originalZoom = RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;
        try
        {
            CircleRuntime sut = new();
            sut.AddToManagers(RenderingLibrary.SystemManagers.Default, null);
            sut.IsFilled = true;
            sut.StrokeColor = new Color(255, 0, 255, 255);
            sut.StrokeWidth = 1f;
            sut.StrokeWidthUnits = DimensionUnitType.ScreenPixel;
            RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = 4f;

            ((IRenderable)sut.RenderableComponent).PreRender();

            // Visible stroke band = 1 screen pixel = 0.25 world units at Zoom=4. The fill inset
            // must match that world extent — not the unscaled 1 world unit that the original
            // math produced, which left a 3 px gap between fill and stroke at this zoom.
            Circle fill = (Circle)sut.RenderableComponent;
            fill.FillRadiusInset.ShouldBe(0.25f, tolerance: 0.001f);
        }
        finally
        {
            RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = originalZoom;
        }
    }

    [Fact]
    public void PreRender_StrokeWidthOneAbsolute_AtZoom1_PushesUnityWorldInsetToFill()
    {
        // Regression guard for the original #2834 behavior: at Zoom = 1 the fix should be a
        // no-op (aaContribution / 1 = 1), so the original 1-world-unit inset is preserved.
        float originalZoom = RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;
        try
        {
            CircleRuntime sut = new();
            sut.AddToManagers(RenderingLibrary.SystemManagers.Default, null);
            sut.IsFilled = true;
            sut.StrokeColor = new Color(255, 0, 255, 255);
            sut.StrokeWidth = 1f;
            sut.StrokeWidthUnits = DimensionUnitType.Absolute;
            RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = 1f;

            ((IRenderable)sut.RenderableComponent).PreRender();

            Circle fill = (Circle)sut.RenderableComponent;
            fill.FillRadiusInset.ShouldBe(1f, tolerance: 0.001f);
        }
        finally
        {
            RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = originalZoom;
        }
    }

    // Issue #2956 follow-up — the Gum tool's variable grid drives runtime properties via
    // GraphicalUiElement.SetProperty(name, value), which dispatches through
    // CustomSetPropertyOnRenderable. That dispatcher's CircleRuntime-typed branch only
    // special-cases the legacy single-slot properties (Color/Red/Green/Blue/Alpha/Radius);
    // every other property falls through to reflection on the *contained renderable*. For a
    // two-slot CircleRuntime the contained renderable is the fill slot, so two-slot variables
    // (UseGradient, IsFilled, FillColor, StrokeColor, etc.) would only reach the fill slot if they
    // fell through to reflection — bypassing the runtime side effects (IsFilled zeroing the fill
    // alpha, StrokeColor writing the stroke slot, the runtime backing fields used for round-trip).
    //
    // These tests pin the contract that SetProperty routes through the *runtime's* typed
    // setters for two-slot variables. Each test confirms a runtime-level side effect that
    // only happens when the runtime setter is called (not when reflection writes directly to
    // the contained renderable).

    [Fact]
    public void SetProperty_UseGradient_RoutesThroughRuntimeSetter()
    {
        CircleRuntime sut = new();

        sut.SetProperty("UseGradient", true);

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        // Routed through the runtime setter: the backing field is set and the gradient gate routes
        // to the active body slot. IsFilled defaults false (stroke-only), so the gate lands on the
        // stroke. Reflection writing straight to the contained fill renderable would leave the
        // backing field (sut.UseGradient) false and never touch the stroke slot.
        sut.UseGradient.ShouldBeTrue();
        stroke.UseGradient.ShouldBeTrue();
        fill.UseGradient.ShouldBeFalse();
    }

    [Fact]
    public void SetProperty_IsFilled_RoutesThroughRuntimeSetter()
    {
        // The runtime's IsFilled setter zeros _fill.Color.A when IsFilled = false; reflection
        // writing IsFilled directly to the fill renderable would instead flip the renderable's
        // own IsFilled flag (which the factory pins to true). The runtime-level Color.A = 0 is
        // the visible signature that the runtime setter ran.
        CircleRuntime sut = new();
        sut.FillColor = new Color(200, 100, 50, 255);

        sut.SetProperty("IsFilled", false);

        Circle fill = (Circle)sut.RenderableComponent;
        fill.Color.A.ShouldBe((byte)0);
        // Runtime keeps the renderable's IsFilled pinned to true (factory contract); only
        // _fill.Color.A is zeroed when the runtime's IsFilled toggles off.
        fill.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void SetProperty_StrokeColor_WritesToStrokeSlot()
    {
        // Reflection on the contained renderable (fill slot) would write Color on the fill,
        // not the stroke — and there's no "StrokeColor" property on Apos's Circle renderable
        // at all, so reflection would silently no-op. Going through the runtime's StrokeColor
        // setter writes _stroke.Color.
        CircleRuntime sut = new();

        sut.SetProperty("StrokeColor", new Color(10, 20, 30, 255));

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.Color.ShouldBe(new Color(10, 20, 30, 255));
    }

    [Fact]
    public void SetProperty_FillColor_WritesToFillSlot()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;

        sut.SetProperty("FillColor", new Color(60, 70, 80, 255));

        sut.FillColor.ShouldBe(new Color(60, 70, 80, 255));
        Circle fill = (Circle)sut.RenderableComponent;
        fill.Color.ShouldBe(new Color(60, 70, 80, 255));
    }

    // Issue #3009 — Circle/Rectangle no longer store a standalone gradient Color1. The gradient
    // start stop is the ACTIVE body color: FillColor when IsFilled, StrokeColor when stroke-only.
    // This removes the solid↔gradient color jump when toggling UseGradient (the start already
    // equals the solid the shape was showing) and converges the dropshadow alpha (which scales by
    // the renderable's Color.A) onto the gradient start alpha. Each slot mirrors its own solid
    // color into its Red1/Green1/Blue1/Alpha1 start, so both fill and stroke can carry the
    // correct gradient start simultaneously.

    [Fact]
    public void UseGradient_GradientStart_MirrorsFillColor_WhenFilled()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = new Color(10, 20, 30, 200);

        sut.UseGradient = true;

        Circle fill = (Circle)sut.RenderableComponent;
        fill.Red1.ShouldBe(10);
        fill.Green1.ShouldBe(20);
        fill.Blue1.ShouldBe(30);
        fill.Alpha1.ShouldBe(200);
    }

    [Fact]
    public void UseGradient_GradientStart_MirrorsStrokeColor_WhenStrokeOnly()
    {
        CircleRuntime sut = new();
        sut.IsFilled = false;
        sut.StrokeColor = new Color(40, 50, 60, 70);

        sut.UseGradient = true;

        Circle fill = (Circle)sut.RenderableComponent;
        Circle stroke = (Circle)fill.Children[0];
        stroke.Red1.ShouldBe(40);
        stroke.Green1.ShouldBe(50);
        stroke.Blue1.ShouldBe(60);
        stroke.Alpha1.ShouldBe(70);
    }

    // No-jump contract: changing the body color while the gradient is on updates the gradient
    // start in lockstep, so there is never a stale start color left behind.
    [Fact]
    public void FillColor_WhenChangedUnderGradient_UpdatesGradientStart()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.UseGradient = true;

        sut.FillColor = new Color(1, 2, 3, 4);

        Circle fill = (Circle)sut.RenderableComponent;
        fill.Red1.ShouldBe(1);
        fill.Green1.ShouldBe(2);
        fill.Blue1.ShouldBe(3);
        fill.Alpha1.ShouldBe(4);
    }

    // Dropshadow alpha converges onto the gradient start alpha: the renderable's solid Color.A
    // (which EffectiveDropshadowColor scales by) now equals the gradient start alpha, because both
    // are the body color.
    [Fact]
    public void Dropshadow_AlphaTracksGradientStart_WhenFilledGradient()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = new Color(255, 0, 0, 128);
        sut.UseGradient = true;
        sut.HasDropshadow = true;
        sut.DropshadowColor = new Color(0, 0, 0, 255);

        Circle fill = (Circle)sut.RenderableComponent;
        fill.Alpha1.ShouldBe(128);
        // EffectiveDropshadowColor.A = 255 * (Color.A = 128) / 255 = 128
        fill.EffectiveDropshadowColor.A.ShouldBe((byte)128);
    }
}
