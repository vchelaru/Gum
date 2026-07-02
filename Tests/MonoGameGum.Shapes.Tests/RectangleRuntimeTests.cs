using Gum.Converters;
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
        // Issue #3112 — the slot-override factories now gate on ShapeRenderer.IsInitialized.
        // These tests exercise the Apos two-slot model headlessly (no GraphicsDevice), so force
        // the flag on. ShapeRendererGateTests covers the not-initialized fallback path.
        ShapeRenderer.Self.SetIsInitializedForTesting(true);
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

    // Issue #2925 (Phase 0 parity) — under Absolute units the post-PreRender corner radii on both
    // slots match the raw runtime values. Skia already does this (RoundedRectangleRuntime.cs
    // PreRender SKIA branch); the Apos side previously pushed in the setter only and never
    // re-resolved in PreRender, so this guards the new resolution path.
    [Fact]
    public void CornerRadiusUnits_Absolute_PreRender_PushesRawValueToBothSlots()
    {
        RectangleRuntime sut = new();
        sut.CornerRadius = 8f;
        sut.CustomRadiusTopLeft = 1f;
        sut.CustomRadiusTopRight = 2f;
        sut.CustomRadiusBottomLeft = 3f;
        sut.CustomRadiusBottomRight = 4f;
        sut.CornerRadiusUnits = DimensionUnitType.Absolute;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        fill.CornerRadius.ShouldBe(8f);
        stroke.CornerRadius.ShouldBe(8f);
        fill.CustomRadiusTopLeft.ShouldBe(1f);
        stroke.CustomRadiusTopLeft.ShouldBe(1f);
        fill.CustomRadiusTopRight.ShouldBe(2f);
        stroke.CustomRadiusTopRight.ShouldBe(2f);
        fill.CustomRadiusBottomLeft.ShouldBe(3f);
        stroke.CustomRadiusBottomLeft.ShouldBe(3f);
        fill.CustomRadiusBottomRight.ShouldBe(4f);
        stroke.CustomRadiusBottomRight.ShouldBe(4f);
    }

    // FillInset is the rectangle analog of CircleRuntime's FillRadiusInset (#2834): when both
    // fill and stroke are visible, PreRender pushes an inset onto the fill slot so the fill's
    // outer edge sits inside the stroke's band. Without it a filled rectangle with a
    // semi-transparent stroke shows the FILL through the stroke instead of the background —
    // the reported bug. Inset equals the AA-compensated stroke width: 4 - 1 (Apos AA halo) = 3.
    [Fact]
    public void FillInset_AaOn_WhenStrokeVisible_PushedAaCompensatedStrokeWidthInPreRender()
    {
        RectangleRuntime sut = new();
        sut.Width = 100;
        sut.Height = 60;
        sut.IsFilled = true;
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 4f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.IsAntialiased = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        fill.FillInset.ShouldBe(3f);
    }

    // At hairline strokes (StrokeWidth = 1, AA on) the AA-compensated stroke width is sub-pixel
    // epsilon, but the AA halo is still 1 px. Inset must be floored at the AA contribution (1 px)
    // so the fill's outer AA halo doesn't overlap the stroke's AA range.
    [Fact]
    public void FillInset_AaOn_AtHairlineStroke_FlooredAtAaContribution()
    {
        RectangleRuntime sut = new();
        sut.Width = 100;
        sut.Height = 60;
        sut.IsFilled = true;
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 1f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.IsAntialiased = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        fill.FillInset.ShouldBe(1f);
    }

    // When the stroke is invisible (StrokeWidth == 0) the fill renders at full size — no inset,
    // no background ring where the stroke would have been.
    [Fact]
    public void FillInset_WhenStrokeInvisible_StaysZero()
    {
        RectangleRuntime sut = new();
        sut.Width = 100;
        sut.Height = 60;
        sut.IsFilled = true;
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 0f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        fill.FillInset.ShouldBe(0f);
    }

    // The gradient paints the ACTIVE body: the fill when IsFilled, the stroke when stroke-only.
    // Whichever slot is not the active body renders solid (its gradient gate stays off) so the two
    // never share the single gradient and composite invisibly.
    //
    // Filled: gradient fills the body, stroke renders its solid StrokeColor.
    [Fact]
    public void UseGradient_WhenFilled_RoutesGradientToFill_StrokeSolid()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 4f;
        sut.Color2 = Color.Green;
        sut.Alpha2 = 255;

        sut.UseGradient = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];

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
        RectangleRuntime sut = new();
        sut.IsFilled = false;
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 4f;
        sut.Color2 = Color.Green;
        sut.Alpha2 = 255;

        sut.UseGradient = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];

        stroke.UseGradient.ShouldBeTrue();
        stroke.ShouldPaintGradient(forcedColor: null).ShouldBeTrue();
        fill.UseGradient.ShouldBeFalse();
    }

    // Toggling IsFilled re-routes the gradient to the new active slot (mirror of the dropshadow
    // SyncDropshadowToTarget re-routing), so the gate is never stranded on the wrong slot.
    [Fact]
    public void UseGradient_IsFilledToggle_RoutesGradientToActiveSlot()
    {
        RectangleRuntime sut = new();
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 4f;
        sut.Color2 = Color.Green;
        sut.Alpha2 = 255;
        sut.UseGradient = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];

        sut.IsFilled = true;
        fill.UseGradient.ShouldBeTrue();
        stroke.UseGradient.ShouldBeFalse();

        sut.IsFilled = false;
        fill.UseGradient.ShouldBeFalse();
        stroke.UseGradient.ShouldBeTrue();
    }

    // Issue #2925 (Phase 0 parity) — ScreenPixel was previously a Skia-only honor (Apos parity gap
    // noted in RoundedRectangleRuntime.cs:80, 124). At Camera.Zoom = 2 the resolved radius is
    // raw / zoom so the rounded corner holds a constant on-screen pixel size as the camera zooms,
    // matching the existing StrokeWidthUnits.ScreenPixel behavior in AposShapeRuntime.PreRender.
    // Skia's RoundedRectangleRuntime.PreRender already does this; bringing the Apos branch into
    // parity is the whole of Phase 0's RectangleRuntime work.
    [Fact]
    public void CornerRadiusUnits_ScreenPixel_DividesByCameraZoom_OnBothSlots()
    {
        float originalZoom = RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;
        try
        {
            RectangleRuntime sut = new();
            sut.AddToManagers(RenderingLibrary.SystemManagers.Default, null);
            sut.CornerRadius = 8f;
            sut.CustomRadiusTopLeft = 1f;
            sut.CustomRadiusTopRight = 2f;
            sut.CustomRadiusBottomLeft = 3f;
            sut.CustomRadiusBottomRight = 4f;
            sut.CornerRadiusUnits = DimensionUnitType.ScreenPixel;
            RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = 2f;

            RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
            RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
            IRenderable asFillRenderable = fill;
            asFillRenderable.PreRender();

            fill.CornerRadius.ShouldBe(4f);
            stroke.CornerRadius.ShouldBe(4f);
            fill.CustomRadiusTopLeft.ShouldBe(0.5f);
            stroke.CustomRadiusTopLeft.ShouldBe(0.5f);
            fill.CustomRadiusTopRight.ShouldBe(1f);
            stroke.CustomRadiusTopRight.ShouldBe(1f);
            fill.CustomRadiusBottomLeft.ShouldBe(1.5f);
            stroke.CustomRadiusBottomLeft.ShouldBe(1.5f);
            fill.CustomRadiusBottomRight.ShouldBe(2f);
            stroke.CustomRadiusBottomRight.ShouldBe(2f);
        }
        finally
        {
            RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = originalZoom;
        }
    }

    [Fact]
    public void FillAndStroke_DrawSimultaneously_BothColorsRoundTrip()
    {
        RectangleRuntime sut = new();

        sut.IsFilled = true;
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
        sut.IsAntialiased = false;

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
        // IsAntialiased = false skips the Apos AA-bloom compensation so this test asserts
        // exact propagation. Compensation math is covered in StrokeWidth_AaOn_* below.
        sut.IsAntialiased = false;

        RoundedRectangle stroke = (RoundedRectangle)((RoundedRectangle)sut.RenderableComponent).Children[0];
        IRenderable asRenderable = stroke;
        asRenderable.PreRender();

        stroke.StrokeWidth.ShouldBe(7f);
    }

    // Issue #2818 (mirror of CircleRuntime #2790) — Apos.Shapes' DrawRectangle adds aaSize
    // pixels of halo OUTSIDE the nominal stroke thickness, same as DrawCircle. Skia fits AA
    // WITHIN the thickness, so RectangleRuntime subtracts the 1 px AA contribution before
    // pushing for visual parity on axis-aligned 1-px borders.
    [Fact]
    public void StrokeWidth_AaOn_SubtractsAaContributionBeforePushingToAposStroke()
    {
        RectangleRuntime sut = new();
        sut.StrokeColor = Color.Green;
        sut.StrokeWidth = 4f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.IsAntialiased = true;

        RoundedRectangle stroke = (RoundedRectangle)((RoundedRectangle)sut.RenderableComponent).Children[0];
        IRenderable asRenderable = stroke;
        asRenderable.PreRender();

        stroke.StrokeWidth.ShouldBe(3f);
    }

    [Fact]
    public void StrokeWidth_AaOn_AtOnePixel_PushesEpsilonForPureAaHalo()
    {
        RectangleRuntime sut = new();
        sut.StrokeColor = Color.Green;
        sut.StrokeWidth = 1f;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.IsAntialiased = true;

        RoundedRectangle stroke = (RoundedRectangle)((RoundedRectangle)sut.RenderableComponent).Children[0];
        IRenderable asRenderable = stroke;
        asRenderable.PreRender();

        stroke.StrokeWidth.ShouldBeGreaterThan(0f);
        stroke.StrokeWidth.ShouldBeLessThan(0.1f);
        stroke.IsAntialiased.ShouldBeTrue();
    }

    // Issue #2818 / #3009: gradient coordinate and Color2 props push through to BOTH Apos
    // RoundedRectangles so the values round-trip on either slot. The gradient START is per-slot now
    // — each slot mirrors its own body color (see UseGradient_GradientStart_* tests) rather than a
    // shared standalone Color1. The UseGradient GATE routes to the active body slot (fill when
    // IsFilled, else stroke — see UseGradient_When* tests). IsFilled is set true here so the gate
    // lands deterministically on the fill; the shared params are inert on the stroke while its
    // gate is off.
    [Fact]
    public void Gradient_PropertiesPushedToBothFillAndStrokeSlots()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;

        sut.UseGradient = true;
        sut.GradientType = GradientType.Linear;
        sut.Color2 = Color.Blue;
        sut.GradientX2 = 56;
        sut.GradientInnerRadius = 4;
        sut.GradientInnerRadiusUnits = DimensionUnitType.Absolute;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];

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

    // Issue #3009 — gradient start mirrors the active body color (parallel of the CircleRuntime
    // tests). FillColor when filled, StrokeColor when stroke-only; dropshadow alpha converges onto
    // the start because both equal the body color.
    [Fact]
    public void UseGradient_GradientStart_MirrorsFillColor_WhenFilled()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = new Color(10, 20, 30, 200);

        sut.UseGradient = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        fill.Red1.ShouldBe(10);
        fill.Green1.ShouldBe(20);
        fill.Blue1.ShouldBe(30);
        fill.Alpha1.ShouldBe(200);
    }

    [Fact]
    public void UseGradient_GradientStart_MirrorsStrokeColor_WhenStrokeOnly()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = false;
        sut.StrokeColor = new Color(40, 50, 60, 70);

        sut.UseGradient = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        stroke.Red1.ShouldBe(40);
        stroke.Green1.ShouldBe(50);
        stroke.Blue1.ShouldBe(60);
        stroke.Alpha1.ShouldBe(70);
    }

    [Fact]
    public void FillColor_WhenChangedUnderGradient_UpdatesGradientStart()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.UseGradient = true;

        sut.FillColor = new Color(1, 2, 3, 4);

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        fill.Red1.ShouldBe(1);
        fill.Green1.ShouldBe(2);
        fill.Blue1.ShouldBe(3);
        fill.Alpha1.ShouldBe(4);
    }

    [Fact]
    public void Dropshadow_AlphaTracksGradientStart_WhenFilledGradient()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = new Color(255, 0, 0, 128);
        sut.UseGradient = true;
        sut.HasDropshadow = true;
        sut.DropshadowColor = new Color(0, 0, 0, 255);

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        fill.Alpha1.ShouldBe(128);
        fill.EffectiveDropshadowColor.A.ShouldBe((byte)128);
    }

    // Issue #2818: dropshadow pushes to the fill slot only (with fallback to stroke when fill
    // is null) — pushing to BOTH slots would render the shadow twice and visibly double up.
    [Fact]
    public void Dropshadow_PropertiesPushedToFillSlotOnly_NotStroke()
    {
        RectangleRuntime sut = new();

        // Dropshadow routes to the fill slot only when the shape is filled; IsFilled now
        // defaults to false, so opt the fill in to exercise the fill-slot routing.
        sut.IsFilled = true;
        sut.HasDropshadow = true;
        sut.DropshadowColor = new Color(10, 20, 30, 40);
        sut.DropshadowOffsetX = 5;
        sut.DropshadowOffsetY = 7;
        sut.DropshadowBlur = 2;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];

        fill.HasDropshadow.ShouldBeTrue();
        fill.DropshadowColor.ShouldBe(new Color(10, 20, 30, 40));
        fill.DropshadowOffsetX.ShouldBe(5);
        fill.DropshadowOffsetY.ShouldBe(7);
        fill.GetShadowAntiAliasSize(cameraZoom: 1f).ShouldBe(2);

        stroke.HasDropshadow.ShouldBeFalse();
    }

    // Issue #2818: dashed-stroke props push to stroke slot only via PreRender (mirror of
    // CircleRuntime #2796). Fill slot must stay 0 because RenderDashed is guarded by !IsFilled.
    [Fact]
    public void DashedStroke_PushedViaPreRender_ToStrokeSlotOnly()
    {
        RectangleRuntime sut = new();
        sut.StrokeColor = Color.White;
        sut.StrokeWidth = 1;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;
        sut.StrokeDashLength = 6;
        sut.StrokeGapLength = 4;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        IRenderable asStrokeRenderable = stroke;
        asStrokeRenderable.PreRender();

        stroke.StrokeDashLength.ShouldBe(6f);
        stroke.StrokeGapLength.ShouldBe(4f);
        fill.StrokeDashLength.ShouldBe(0f);
        fill.StrokeGapLength.ShouldBe(0f);
    }

    // Issue #2818: IsAntialiased pushes through to both slots via PreRender (mirror of
    // CircleRuntime #2798).
    [Fact]
    public void IsAntialiased_PushedViaPreRender_ToBothFillAndStrokeSlots()
    {
        RectangleRuntime sut = new();

        sut.IsAntialiased = false;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        IRenderable asFillRenderable = fill;
        asFillRenderable.PreRender();

        fill.IsAntialiased.ShouldBeFalse();
        stroke.IsAntialiased.ShouldBeFalse();
    }

    // Issue #2818: per-corner radii push to both slots so the outline matches the fill.
    [Fact]
    public void PerCornerRadii_PushedToBothSlots()
    {
        RectangleRuntime sut = new();

        sut.CustomRadiusTopLeft = 1f;
        sut.CustomRadiusTopRight = 2f;
        sut.CustomRadiusBottomLeft = 3f;
        sut.CustomRadiusBottomRight = 4f;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];

        fill.CustomRadiusTopLeft.ShouldBe(1f);
        fill.CustomRadiusTopRight.ShouldBe(2f);
        fill.CustomRadiusBottomLeft.ShouldBe(3f);
        fill.CustomRadiusBottomRight.ShouldBe(4f);
        stroke.CustomRadiusTopLeft.ShouldBe(1f);
        stroke.CustomRadiusTopRight.ShouldBe(2f);
        stroke.CustomRadiusBottomLeft.ShouldBe(3f);
        stroke.CustomRadiusBottomRight.ShouldBe(4f);
    }

    // Issue #2720: per-corner radii set via the string path (SetProperty) must land on the
    // runtime, not on the renderable. The runtime's setter immediately mirrors to fill+stroke,
    // and (more importantly) any later setter on the runtime — or PreRender on Skia — pushes
    // the runtime's stored value onto the renderable, so a string-path write that landed on
    // the renderable directly would be silently clobbered. Mirrors the StrokeWidth regression
    // pattern from #2629 (ArcRuntime).
    [Fact]
    public void CornerRadius_OnRectangleRuntime_ShouldLandOnRuntime_WhenSetThroughSetProperty()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("CornerRadius", 8f);

        sut.CornerRadius.ShouldBe(8f);
    }

    [Fact]
    public void CustomRadiusTopLeft_OnRectangleRuntime_ShouldLandOnRuntime_WhenSetThroughSetProperty()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("CustomRadiusTopLeft", (float?)9f);

        sut.CustomRadiusTopLeft.ShouldBe(9f);
    }

    [Fact]
    public void CustomRadiusTopRight_OnRectangleRuntime_ShouldLandOnRuntime_WhenSetThroughSetProperty()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("CustomRadiusTopRight", (float?)10f);

        sut.CustomRadiusTopRight.ShouldBe(10f);
    }

    [Fact]
    public void CustomRadiusBottomLeft_OnRectangleRuntime_ShouldLandOnRuntime_WhenSetThroughSetProperty()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("CustomRadiusBottomLeft", (float?)11f);

        sut.CustomRadiusBottomLeft.ShouldBe(11f);
    }

    [Fact]
    public void CustomRadiusBottomRight_OnRectangleRuntime_ShouldLandOnRuntime_WhenSetThroughSetProperty()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("CustomRadiusBottomRight", (float?)12f);

        sut.CustomRadiusBottomRight.ShouldBe(12f);
    }

    // Null = inherit from CornerRadius. The string path must accept null without throwing and
    // must clear the override on the runtime.
    [Fact]
    public void CustomRadiusTopLeft_OnRectangleRuntime_ShouldAcceptNull_WhenSetThroughSetProperty()
    {
        RectangleRuntime sut = new();
        sut.CustomRadiusTopLeft = 5f;

        sut.SetProperty("CustomRadiusTopLeft", null);

        sut.CustomRadiusTopLeft.ShouldBeNull();
    }

    // Issue #2818: Clone re-resolves fresh fill/stroke slots so the clone is independent.
    [Fact]
    public void Clone_BothSlots_AreFreshFactoryInstances_NotShallowCopiesOfSource()
    {
        RectangleRuntime source = new();
        source.FillColor = Color.Red;
        source.StrokeColor = Color.Blue;

        RectangleRuntime clone = (RectangleRuntime)source.Clone();

        RoundedRectangle sourceFill = (RoundedRectangle)source.RenderableComponent;
        RoundedRectangle cloneFill = (RoundedRectangle)clone.RenderableComponent;
        RoundedRectangle sourceStroke = (RoundedRectangle)sourceFill.Children[0];
        RoundedRectangle cloneStroke = (RoundedRectangle)cloneFill.Children[0];

        cloneFill.ShouldNotBeSameAs(sourceFill);
        cloneStroke.ShouldNotBeSameAs(sourceStroke);
    }

    [Fact]
    public void Clone_MutatingClone_DoesNotMutateSource()
    {
        RectangleRuntime source = new();
        source.IsFilled = true;
        source.FillColor = Color.Red;
        source.StrokeColor = Color.Blue;

        RectangleRuntime clone = (RectangleRuntime)source.Clone();
        clone.FillColor = Color.Green;
        clone.StrokeColor = Color.Yellow;

        RoundedRectangle sourceFill = (RoundedRectangle)source.RenderableComponent;
        RoundedRectangle sourceStroke = (RoundedRectangle)sourceFill.Children[0];
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
        RectangleRuntime source = new();
        source.IsFilled = true;
        source.FillColor = Color.Red;
        source.UseGradient = true;
        source.GradientType = GradientType.Linear;
        source.Color2 = Color.Blue;
        source.GradientX2 = 56;
        source.HasDropshadow = true;
        source.DropshadowColor = new Color(10, 20, 30, 40);
        source.DropshadowOffsetX = 5;

        RectangleRuntime clone = (RectangleRuntime)source.Clone();

        RoundedRectangle cloneFill = (RoundedRectangle)clone.RenderableComponent;
        RoundedRectangle cloneStroke = (RoundedRectangle)cloneFill.Children[0];

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

    // Issue #2925 — same bug as CircleRuntime: constructor sets the fill renderable as
    // mContainedObjectAsIpso, so the tool's variable-application path routes the legacy
    // "Color" / "Alpha" variables to the FILL renderable instead of stroke. Result: a
    // default Rectangle renders as a solid white box instead of a stroke-only outline.
    [Fact]
    public void SetProperty_Color_RoutesToStroke_NotFill()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("Color", System.Drawing.Color.FromArgb(255, 255, 0, 0));

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        stroke.Color.ShouldBe(new Color(255, 0, 0, 255));
        // Issue #2938 (regression fix) — fill defaults to transparent (alpha 0); the legacy
        // Color setter only touches stroke, so fill stays at its default rather than being
        // recolored.
        fill.Color.ShouldBe(new Color(0, 0, 0, 0));
    }

    [Fact]
    public void SetProperty_Alpha_RoutesToStroke_NotFill()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("Alpha", 128);

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        stroke.Color.A.ShouldBe((byte)128);
        // Issue #2938 (regression fix) — fill defaults to transparent (alpha 0); the legacy
        // Alpha setter only touches stroke, so fill stays at its default rather than being
        // recolored.
        fill.Color.ShouldBe(new Color(0, 0, 0, 0));
    }

    // Issue #2938 — IsFilled now gates fill visibility (mirror of CircleRuntime Pass 1).
    // Setting IsFilled = false pushes a transparent color into the fill slot so only the
    // stroke draws; toggling back to true restores the runtime's FillColor.
    [Fact]
    public void IsFilled_False_HidesFillSlot()
    {
        RectangleRuntime sut = new();
        sut.FillColor = Color.Red;

        sut.IsFilled = false;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        fill.Color.A.ShouldBe((byte)0);
    }

    [Fact]
    public void IsFilled_True_AfterFalse_RestoresFillColor()
    {
        RectangleRuntime sut = new();
        sut.FillColor = Color.Red;
        sut.IsFilled = false;

        sut.IsFilled = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        fill.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void FillChannelSetters_PushToFillSlot()
    {
        RectangleRuntime sut = new();

        sut.IsFilled = true;
        sut.FillRed = 10;
        sut.FillGreen = 20;
        sut.FillBlue = 30;
        sut.FillAlpha = 200;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        fill.Color.ShouldBe(new Color(10, 20, 30, 200));
    }

    [Fact]
    public void StrokeChannelSetters_PushToStrokeSlot()
    {
        RectangleRuntime sut = new();

        sut.StrokeRed = 10;
        sut.StrokeGreen = 20;
        sut.StrokeBlue = 30;
        sut.StrokeAlpha = 200;

        RoundedRectangle stroke = (RoundedRectangle)((RoundedRectangle)sut.RenderableComponent).Children[0];
        stroke.Color.ShouldBe(new Color(10, 20, 30, 200));
    }

    // Issue #2931 — plain RectangleRuntime now exposes IsFilled / StrokeWidth /
    // StrokeDashLength / StrokeGapLength in the tool's default state. The shape-side
    // SetProperty dispatcher previously fell back to the renderable for non-RoundedRectangleRuntime
    // GUEs, bypassing the runtime's PreRender ScreenPixel-zoom scaling.
    [Fact]
    public void SetProperty_StrokeDashLength_RoutesToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("StrokeDashLength", 6f);

        sut.StrokeDashLength.ShouldBe(6f);
    }

    [Fact]
    public void SetProperty_StrokeGapLength_RoutesToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("StrokeGapLength", 4f);

        sut.StrokeGapLength.ShouldBe(4f);
    }

    [Fact]
    public void SetProperty_StrokeWidth_RoutesToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("StrokeWidth", 7f);

        sut.StrokeWidth.ShouldBe(7f);
    }

    [Fact]
    public void SetProperty_IsFilled_True_LightsUpFillSlotWithFillColor()
    {
        RectangleRuntime sut = new();
        sut.FillColor = Color.Red;
        sut.IsFilled = false;

        sut.SetProperty("IsFilled", true);

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        fill.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void SetProperty_IsFilled_False_HidesFillSlot()
    {
        RectangleRuntime sut = new();
        sut.FillColor = Color.Red;

        sut.SetProperty("IsFilled", false);

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        fill.Color.A.ShouldBe((byte)0);
    }

    [Fact]
    public void SetProperty_FillChannels_RouteToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("FillRed", 255);
        sut.SetProperty("FillGreen", 255);
        sut.SetProperty("FillBlue", 255);
        sut.SetProperty("FillAlpha", 255);

        sut.FillColor.ShouldBe(new Color(255, 255, 255, 255));
    }

    [Fact]
    public void SetProperty_StrokeChannels_RouteToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("StrokeRed", 10);
        sut.SetProperty("StrokeGreen", 20);
        sut.SetProperty("StrokeBlue", 30);
        sut.SetProperty("StrokeAlpha", 200);

        sut.StrokeColor.ShouldBe(new Color(10, 20, 30, 200));
    }

    [Fact]
    public void HasDropshadow_True_StrokeOnly_RoutesToStrokeSlot()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = false;

        sut.HasDropshadow = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        stroke.HasDropshadow.ShouldBeTrue();
        fill.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void HasDropshadow_True_Filled_RoutesToFillSlot()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;

        sut.HasDropshadow = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        fill.HasDropshadow.ShouldBeTrue();
        stroke.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void IsFilled_False_AfterHasDropshadow_MovesShadowFromFillToStroke()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.HasDropshadow = true;

        sut.IsFilled = false;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        stroke.HasDropshadow.ShouldBeTrue();
        fill.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void IsFilled_True_AfterHasDropshadow_MovesShadowFromStrokeToFill()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = false;
        sut.HasDropshadow = true;

        sut.IsFilled = true;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        fill.HasDropshadow.ShouldBeTrue();
        stroke.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void SetProperty_HasDropshadow_StrokeOnly_RoutesToStrokeSlot()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = false;

        sut.SetProperty("HasDropshadow", true);

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        stroke.HasDropshadow.ShouldBeTrue();
        fill.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void SetProperty_DropshadowOffsetAndBlur_RouteToActiveSlot_WhenStrokeOnly()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = false;
        sut.HasDropshadow = true;

        sut.SetProperty("DropshadowOffsetX", 19f);
        sut.SetProperty("DropshadowOffsetY", 11f);
        sut.SetProperty("DropshadowBlur", 3f);

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        stroke.DropshadowOffsetX.ShouldBe(19f);
        stroke.DropshadowOffsetY.ShouldBe(11f);
        stroke.GetShadowAntiAliasSize(cameraZoom: 1f).ShouldBe(3);
    }

    [Fact]
    public void SetProperty_DropshadowChannels_RouteToActiveSlot_WhenStrokeOnly()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = false;
        sut.HasDropshadow = true;

        sut.SetProperty("DropshadowAlpha", 200);
        sut.SetProperty("DropshadowRed", 50);
        sut.SetProperty("DropshadowGreen", 100);
        sut.SetProperty("DropshadowBlue", 150);

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];
        stroke.DropshadowColor.ShouldBe(new Color(50, 100, 150, 200));
    }
}
