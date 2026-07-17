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

    // ---- Dispatcher routing pins (issue #3650) ---------------------------------------------
    // These lock the CURRENT behavior of the Skia CustomSetPropertyOnRenderable dispatcher for the
    // RectangleRuntime-intercepted property paths (the `graphicalUiElement is RectangleRuntime` arms
    // in the RoundedRectangle branch of SetPropertyOnRenderableFunc). They drive the STRING property
    // name through the production dispatcher (via SetProperty) and assert the value lands on the
    // runtime — the safety net for the planned runtime-type-first restructure of the dispatcher.
    // CornerRadius / per-corner radii are already pinned this way below (issue #2720).

    [Fact]
    public void Dispatch_DropshadowBlur_RoutesToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("DropshadowBlur", 7f);

        sut.DropshadowBlur.ShouldBe(7f);
    }

    [Fact]
    public void Dispatch_DropshadowChannels_ComposeDropshadowColor()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("DropshadowRed", 10);
        sut.SetProperty("DropshadowGreen", 20);
        sut.SetProperty("DropshadowBlue", 30);
        sut.SetProperty("DropshadowAlpha", 40);

        sut.DropshadowColor.ShouldBe(new SKColor(10, 20, 30, 40));
    }

    [Fact]
    public void Dispatch_DropshadowOffset_RoutesToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("DropshadowOffsetX", 4f);
        sut.SetProperty("DropshadowOffsetY", 6f);

        sut.DropshadowOffsetX.ShouldBe(4f);
        sut.DropshadowOffsetY.ShouldBe(6f);
    }

    [Fact]
    public void Dispatch_FillChannels_ComposeFillColor()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("FillRed", 10);
        sut.SetProperty("FillGreen", 20);
        sut.SetProperty("FillBlue", 30);
        sut.SetProperty("FillAlpha", 40);

        sut.FillColor.ShouldBe(new SKColor(10, 20, 30, 40));
    }

    [Fact]
    public void Dispatch_HasDropshadow_RoutesToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("HasDropshadow", true);

        sut.HasDropshadow.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_IsFilled_RoutesToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("IsFilled", true);

        sut.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_StrokeChannels_ComposeStrokeColor()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("StrokeRed", 11);
        sut.SetProperty("StrokeGreen", 22);
        sut.SetProperty("StrokeBlue", 33);
        sut.SetProperty("StrokeAlpha", 44);

        sut.StrokeColor.ShouldBe(new SKColor(11, 22, 33, 44));
    }

    [Fact]
    public void Dispatch_StrokeDashLength_RoutesToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("StrokeDashLength", 9f);

        sut.StrokeDashLength.ShouldBe(9f);
    }

    [Fact]
    public void Dispatch_StrokeGapLength_RoutesToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("StrokeGapLength", 5f);

        sut.StrokeGapLength.ShouldBe(5f);
    }

    [Fact]
    public void Dispatch_StrokeWidth_RoutesToRuntime()
    {
        RectangleRuntime sut = new();

        sut.SetProperty("StrokeWidth", 3f);

        sut.StrokeWidth.ShouldBe(3f);
    }

    // ---- End dispatcher routing pins -------------------------------------------------------

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

    // Issue #3009 — Circle/Rectangle no longer carry a standalone gradient Color1. Each slot's
    // gradient start mirrors its own solid body color: the fill slot follows FillColor, the stroke
    // slot follows StrokeColor.
    [Fact]
    public void GradientStart_MirrorsFillColor_OnFillSlot()
    {
        RectangleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = new SKColor(10, 20, 30, 40);

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        fillSlot.Red1.ShouldBe(10);
        fillSlot.Green1.ShouldBe(20);
        fillSlot.Blue1.ShouldBe(30);
        fillSlot.Alpha1.ShouldBe(40);
    }

    [Fact]
    public void GradientStart_MirrorsStrokeColor_OnStrokeSlot()
    {
        RectangleRuntime sut = new();
        sut.StrokeColor = new SKColor(40, 50, 60, 70);

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        strokeSlot.Red1.ShouldBe(40);
        strokeSlot.Green1.ShouldBe(50);
        strokeSlot.Blue1.ShouldBe(60);
        strokeSlot.Alpha1.ShouldBe(70);
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

    // Scalar-blur collapse: the plain Rectangle exposes a single isotropic DropshadowBlur. The
    // Skia ctor seeds it to 3 so a freshly-constructed runtime matches MonoGame/raylib (where the
    // scalar already defaults to 3). Previously the Skia ctor seeded only the Y axis, leaving the
    // scalar getter (which reads the X axis) reporting 0 — a cross-backend inconsistency.
    [Fact]
    public void DropshadowBlur_ShouldBe3_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.DropshadowBlur.ShouldBe(3);
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

    // FillColor defaults to opaque white and IsFilled defaults to false, so a fresh runtime
    // renders as a stroke-only outline (the white fill is gated off). Flipping IsFilled = true
    // fills the shape white without needing to also assign FillColor.
    [Fact]
    public void FillColor_ShouldBeWhite_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.FillColor.ShouldBe(new SKColor(255, 255, 255, 255));
    }

    // Regression guard for the gallery breakage caught after PR #2939's first fix attempt:
    // SkiaShapeRuntime.PushFillColorToSlot only runs from the FillColor / IsFilled setters,
    // never from field init. With IsFilled = false by default the gate forces the fill slot's
    // renderable Color to transparent regardless of the (now white) FillColor, so a fresh
    // rectangle still renders stroke-only rather than as a solid white block. This test asserts
    // the renderable's Color directly so the bug can't reappear silently.
    [Fact]
    public void FillRenderableColor_ShouldBeTransparent_ByDefault()
    {
        RectangleRuntime sut = new();
        RoundedRectangle fill = (RoundedRectangle)sut.RenderableComponent;
        fill.Color.ShouldBe(new SKColor(0, 0, 0, 0));
    }

    [Fact]
    public void IsFilled_ShouldBeFalse_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.IsFilled.ShouldBeFalse();
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

    // Issue #3671 — StrokeWidth = 0 must render as no stroke at all, matching Apos.Shapes/raylib
    // semantics. SkiaSharp's SKPaint.StrokeWidth = 0 means "hairline" (always a visible
    // 1-device-pixel line), so a theme that leaves the ctor's default white StrokeColor in place
    // and only zeroes StrokeWidth (a common pattern for a fill-only shape) got a stray white
    // outline on Skia despite MonoGame/raylib showing nothing, because Render() drew the stroke
    // slot unconditionally instead of skipping it when the width is non-positive.
    [Fact]
    public void Render_StrokeWidthZero_DrawsNoVisibleStroke()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        Gum.GumService.Default.Initialize(surface.Canvas, 64, 64);
        surface.Canvas.Clear(SKColors.Black);

        RectangleRuntime sut = new()
        {
            X = 8,
            Y = 8,
            Width = 48,
            Height = 48,
            IsFilled = true,
            FillColor = SKColors.Blue,
            StrokeWidth = 0,
            // Disable anti-aliasing so a stray hairline draws as a crisp, deterministic pixel
            // instead of blending into a hard-to-assert gray at the edge.
            IsAntialiased = false,
        };
        Gum.GumService.Default.Root.Children.Add(sut);
        sut.PreRender();

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);

        // Sample every pixel along the rectangle's outer edge (where a hairline stroke, centered
        // on the path, would land) plus a 1px margin on each side. A stray hairline shows up as a
        // white (or white-blended) pixel; the fill's blue or the black background should not have
        // any green/red contribution from a white stroke.
        for (int x = 7; x <= 56; x++)
        {
            bitmap.GetPixel(x, 7).Red.ShouldBe((byte)0);
            bitmap.GetPixel(x, 56).Red.ShouldBe((byte)0);
        }
        for (int y = 7; y <= 56; y++)
        {
            bitmap.GetPixel(7, y).Red.ShouldBe((byte)0);
            bitmap.GetPixel(56, y).Red.ShouldBe((byte)0);
        }
    }
}
