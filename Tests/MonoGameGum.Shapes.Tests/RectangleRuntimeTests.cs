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

    // Issue #2818: gradient props on RectangleRuntime push through to both Apos RoundedRectangles
    // so a single gradient can paint fill and stroke at once (mirror of CircleRuntime #2791).
    [Fact]
    public void Gradient_PropertiesPushedToBothFillAndStrokeSlots()
    {
        RectangleRuntime sut = new();

        sut.UseGradient = true;
        sut.GradientType = GradientType.Linear;
        sut.Color1 = Color.Red;
        sut.Color2 = Color.Blue;
        sut.GradientX2 = 56;
        sut.GradientInnerRadius = 4;
        sut.GradientInnerRadiusUnits = DimensionUnitType.Absolute;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];

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

    // Issue #2818: dropshadow pushes to the fill slot only (with fallback to stroke when fill
    // is null) — pushing to BOTH slots would render the shadow twice and visibly double up.
    [Fact]
    public void Dropshadow_PropertiesPushedToFillSlotOnly_NotStroke()
    {
        RectangleRuntime sut = new();

        sut.HasDropshadow = true;
        sut.DropshadowColor = new Color(10, 20, 30, 40);
        sut.DropshadowOffsetX = 5;
        sut.DropshadowOffsetY = 7;
        sut.DropshadowBlurX = 2;
        sut.DropshadowBlurY = 4;

        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle stroke = (RoundedRectangle)fill.Children[0];

        fill.HasDropshadow.ShouldBeTrue();
        fill.DropshadowColor.ShouldBe(new Color(10, 20, 30, 40));
        fill.DropshadowOffsetX.ShouldBe(5);
        fill.DropshadowOffsetY.ShouldBe(7);
        fill.DropshadowBlurX.ShouldBe(2);
        fill.DropshadowBlurY.ShouldBe(4);

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
}
