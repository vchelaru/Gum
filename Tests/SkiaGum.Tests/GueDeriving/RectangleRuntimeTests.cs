using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Linq;

namespace SkiaGum.Tests.GueDeriving;

// Issue #2814 - RectangleRuntime on Skia gains two-slot fill+stroke composition (mirror of
// CircleRuntime / #2790). The fill renderable is the contained object and the stroke
// renderable is its first child. The renderer draws parent before children so the visual
// order is fill under stroke.
public class RectangleRuntimeTests
{
    public RectangleRuntimeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void Clone_MutatingClone_DoesNotMutateSource()
    {
        RectangleRuntime source = new();
        source.IsFilled = true;
        source.FillColor = SKColors.Red;
        source.StrokeColor = SKColors.Blue;

        RectangleRuntime clone = (RectangleRuntime)source.Clone();
        clone.FillColor = SKColors.Green;
        clone.StrokeColor = SKColors.Yellow;

        RoundedRectangle sourceFill = (RoundedRectangle)source.RenderableComponent;
        RoundedRectangle sourceStroke = (RoundedRectangle)sourceFill.Children.Single();
        sourceFill.Color.ShouldBe(SKColors.Red);
        sourceStroke.Color.ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void Clone_StrokeSlot_IsFreshInstance_NotShallowCopyOfSource()
    {
        RectangleRuntime source = new();
        source.FillColor = SKColors.Red;
        source.StrokeColor = SKColors.Blue;

        RectangleRuntime clone = (RectangleRuntime)source.Clone();

        RoundedRectangle sourceFill = (RoundedRectangle)source.RenderableComponent;
        RoundedRectangle cloneFill = (RoundedRectangle)clone.RenderableComponent;
        RoundedRectangle sourceStroke = (RoundedRectangle)sourceFill.Children.Single();
        RoundedRectangle cloneStroke = (RoundedRectangle)cloneFill.Children.Single();

        cloneFill.ShouldNotBeSameAs(sourceFill);
        cloneStroke.ShouldNotBeSameAs(sourceStroke);
    }

    [Fact]
    public void Color1_MirrorsToBothSlots()
    {
        RectangleRuntime sut = new();
        sut.Color1 = new SKColor(10, 20, 30, 40);

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        fillSlot.Red1.ShouldBe(10);
        strokeSlot.Red1.ShouldBe(10);
        strokeSlot.Alpha1.ShouldBe(40);
    }

    [Fact]
    public void ContainedRenderable_ShouldBeRoundedRectangle()
    {
        RectangleRuntime sut = new();
        sut.RenderableComponent.ShouldBeOfType<RoundedRectangle>();
    }

    // Issue #2818: default CornerRadius = 0 keeps the historical hard-cornered visual even
    // though the contained type is now RoundedRectangle (whose own ctor defaults to 5).
    [Fact]
    public void CornerRadius_ShouldBe0_ByDefault()
    {
        RectangleRuntime sut = new();

        sut.CornerRadius.ShouldBe(0f);
        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        fillSlot.CornerRadius.ShouldBe(0f);
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        strokeSlot.CornerRadius.ShouldBe(0f);
    }

    // Issue #2818: CornerRadius mirrors onto both slots each frame in PreRender so the outline
    // traces the same rounded corners as the fill.
    [Fact]
    public void CornerRadius_PushedToBothSlots_InPreRender()
    {
        RectangleRuntime sut = new();
        sut.CornerRadius = 8f;

        sut.PreRender();

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        fillSlot.CornerRadius.ShouldBe(8f);
        strokeSlot.CornerRadius.ShouldBe(8f);
    }

    // Issue #2720: per-corner radii set via the string path (SetProperty) must land on the
    // runtime, not on the renderable. The runtime's setter mirrors to fill+stroke, and PreRender
    // pushes the runtime's stored value to the renderable each frame, so a string-path write
    // that landed on the renderable directly would be silently clobbered.
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

    [Fact]
    public void CustomRadiusTopLeft_OnRectangleRuntime_ShouldAcceptNull_WhenSetThroughSetProperty()
    {
        RectangleRuntime sut = new();
        sut.CustomRadiusTopLeft = 5f;

        sut.SetProperty("CustomRadiusTopLeft", null);

        sut.CustomRadiusTopLeft.ShouldBeNull();
    }

    [Fact]
    public void PerCornerRadii_PushedToBothSlots_InPreRender()
    {
        RectangleRuntime sut = new();
        sut.CustomRadiusTopLeft = 1f;
        sut.CustomRadiusTopRight = 2f;
        sut.CustomRadiusBottomLeft = 3f;
        sut.CustomRadiusBottomRight = 4f;

        sut.PreRender();

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        fillSlot.CustomRadiusTopLeft.ShouldBe(1f);
        fillSlot.CustomRadiusTopRight.ShouldBe(2f);
        fillSlot.CustomRadiusBottomLeft.ShouldBe(3f);
        fillSlot.CustomRadiusBottomRight.ShouldBe(4f);
        strokeSlot.CustomRadiusTopLeft.ShouldBe(1f);
        strokeSlot.CustomRadiusTopRight.ShouldBe(2f);
        strokeSlot.CustomRadiusBottomLeft.ShouldBe(3f);
        strokeSlot.CustomRadiusBottomRight.ShouldBe(4f);
    }

    // Issue #2938 — IsFilled gates dropshadow routing. When IsFilled = false, the shadow lands
    // on the stroke slot (a stroke-only ring still casts a shadow).
    [Fact]
    public void Dropshadow_IsFilledFalse_AppliesToStrokeSlot()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = false;
        sut.HasDropshadow = true;

        sut.PreRender();

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        strokeSlot.HasDropshadow.ShouldBeTrue();
        fillSlot.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void Dropshadow_IsFilledTrue_AppliesToFillSlot()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = SKColors.Red;
        sut.HasDropshadow = true;

        sut.PreRender();

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        fillSlot.HasDropshadow.ShouldBeTrue();
        strokeSlot.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void Dropshadow_TargetSwitch_ClearsPreviousSlot()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = SKColors.Red;
        sut.HasDropshadow = true;
        sut.PreRender();
        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        fillSlot.HasDropshadow.ShouldBeTrue();

        sut.IsFilled = false;
        sut.PreRender();

        fillSlot.HasDropshadow.ShouldBeFalse();
        strokeSlot.HasDropshadow.ShouldBeTrue();
    }

    // Issue #2938 (regression fix) — RectangleRuntime preserves the historical Skia default of
    // an invisible fill via FillColor = transparent (alpha 0). IsFilled is true by default
    // (base behavior), so the gate is open — assigning FillColor to a visible color lights the
    // fill up without needing to flip IsFilled.
    [Fact]
    public void FillColor_ShouldBeTransparent_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.FillColor.ShouldBe(new SKColor(0, 0, 0, 0));
    }

    // Regression guard for the gallery breakage caught after PR #2939's first fix attempt:
    // SkiaShapeRuntime.PushFillColorToSlot only runs from the FillColor / IsFilled setters,
    // never from field init. If the ctor relies on the field default and skips the explicit
    // FillColor assignment, the Skia RoundedRectangle renderable retains its own
    // constructor default (SKColors.White at RoundedRectangle.cs:23) and the rectangle
    // renders as a solid white block. The runtime property reports the transparent default
    // — so the existing default test passes — while the actual visual is wrong. This test
    // asserts the renderable's Color directly so the bug can't reappear silently.
    [Fact]
    public void FillRenderableColor_ShouldBeTransparent_ByDefault()
    {
        RectangleRuntime sut = new();
        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        fill.Color.ShouldBe(new SKColor(0, 0, 0, 0));
    }

    [Fact]
    public void IsFilled_ShouldBeTrue_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.IsFilled.ShouldBeTrue();
    }

    // Issue #2938 — per-channel ints compose into FillColor via the same setter pipeline
    // that round-trips through the fill slot.
    [Fact]
    public void FillChannelSetters_ComposeFillColor()
    {
        RectangleRuntime sut = new();
        sut.FillRed = 10;
        sut.FillGreen = 20;
        sut.FillBlue = 30;
        sut.FillAlpha = 40;

        sut.FillColor.ShouldBe(new SKColor(10, 20, 30, 40));
    }

    // Issue #2938 — per-channel ints compose into StrokeColor via the same setter pipeline
    // that round-trips through the stroke slot.
    [Fact]
    public void StrokeChannelSetters_ComposeStrokeColor()
    {
        RectangleRuntime sut = new();
        sut.StrokeRed = 11;
        sut.StrokeGreen = 22;
        sut.StrokeBlue = 33;
        sut.StrokeAlpha = 44;

        sut.StrokeColor.ShouldBe(new SKColor(11, 22, 33, 44));
    }

    [Fact]
    public void FillColorAndStrokeColor_BothSet_PaintsEachSlotIndependently()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = SKColors.Crimson;
        sut.StrokeColor = SKColors.Cyan;

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();

        fillSlot.Color.ShouldBe(SKColors.Crimson);
        fillSlot.IsFilled.ShouldBeTrue();
        strokeSlot.Color.ShouldBe(SKColors.Cyan);
        strokeSlot.IsFilled.ShouldBeFalse();
    }

    [Fact]
    public void FillColorAndStrokeColor_SetInReverseOrder_StillPaintsBothSlots()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.StrokeColor = SKColors.Magenta;
        sut.FillColor = SKColors.Gold;

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();

        fillSlot.Color.ShouldBe(SKColors.Gold);
        strokeSlot.Color.ShouldBe(SKColors.Magenta);
    }

    [Fact]
    public void Height_ShouldBe50_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.Height.ShouldBe(50);
    }

    [Fact]
    public void IsAntialiased_MirrorsToStrokeSlot_InTwoSlotMode()
    {
        RectangleRuntime sut = new();

        sut.IsAntialiased = false;

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        fillSlot.IsAntialiased.ShouldBeFalse();
        strokeSlot.IsAntialiased.ShouldBeFalse();
    }

    [Fact]
    public void PreRender_ShouldMirrorWidthAndHeight_OntoStrokeSlot()
    {
        RectangleRuntime sut = new();
        sut.Width = 80;
        sut.Height = 60;
        sut.FillColor = SKColors.Crimson;
        sut.StrokeColor = SKColors.Cyan;
        sut.StrokeWidth = 4;

        sut.PreRender();

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        strokeSlot.Width.ShouldBe(fillSlot.Width);
        strokeSlot.Height.ShouldBe(fillSlot.Height);
        strokeSlot.IsOffsetAppliedForStroke.ShouldBeTrue();
    }

    // Issue #2938 — IsFilled gates the fill-slot gradient. With IsFilled = false the fill-slot
    // gradient stays off even when UseGradient = true; toggling IsFilled = true lights it up.
    [Fact]
    public void SettingIsFilledTrue_AfterUseGradientTrue_LightsUpFillSlotGradient()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = false;
        sut.UseGradient = true;
        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        fillSlot.UseGradient.ShouldBeFalse();

        sut.IsFilled = true;

        fillSlot.UseGradient.ShouldBeTrue();
    }

    [Fact]
    public void StrokeColor_ShouldBeWhite_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.StrokeColor.ShouldBe(SKColors.White);
    }

    [Fact]
    public void StrokeSlot_ShouldExistAsChildOfFillSlot_ByDefault()
    {
        RectangleRuntime sut = new();

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        fillSlot.Children.Count.ShouldBe(1);

        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        strokeSlot.IsFilled.ShouldBeFalse();
        fillSlot.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void StrokeWidth_AfterPreRender_AppliesToStrokeSlotNotFillSlot()
    {
        RectangleRuntime sut = new();
        sut.StrokeWidth = 5;
        sut.StrokeWidthUnits = DimensionUnitType.Absolute;

        sut.PreRender();

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        strokeSlot.StrokeWidth.ShouldBe(5);
    }

    [Fact]
    public void StrokeWidth_ShouldBe1_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.StrokeWidth.ShouldBe(1);
    }

    [Fact]
    public void UseGradient_BothSlotsActive_BothSlotsOn()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.UseGradient = true;

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        fillSlot.UseGradient.ShouldBeTrue();
        strokeSlot.UseGradient.ShouldBeTrue();
    }

    // Issue #2938 — IsFilled gates the fill-slot gradient. With IsFilled = false the fill slot
    // stays off; the stroke slot lights up because the default StrokeWidth is 1.
    [Fact]
    public void UseGradient_IsFilledFalse_FillSlotStaysOff()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = false;
        sut.UseGradient = true;

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        fillSlot.UseGradient.ShouldBeFalse();
        strokeSlot.UseGradient.ShouldBeTrue();
    }

    [Fact]
    public void Width_ShouldBe50_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.Width.ShouldBe(50);
    }
}
