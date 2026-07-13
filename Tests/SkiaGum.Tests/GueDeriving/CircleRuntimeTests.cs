using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Linq;

namespace SkiaGum.Tests.GueDeriving;

// These tests pin down the post-unification defaults of CircleRuntime on the Skia backend
// (issue #2785). After #2785 lands, the canonical CircleRuntime source lives in
// MonoGameGum/GueDeriving/CircleRuntime.cs and is file-linked into SkiaGum.csproj; Skia's
// previously-divergent 100x100 default is realigned to 32x32 to match MonoGame/Raylib.
// Stroke/fill/dropshadow defaults are still preserved under #if SKIA so existing Skia
// rendering behavior is unchanged for users who instantiate and configure beyond the size.
public class CircleRuntimeTests
{
    public CircleRuntimeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    // ---- Dispatcher routing pins (issue #3650) ---------------------------------------------
    // These lock the CURRENT behavior of the Skia CustomSetPropertyOnRenderable dispatcher for the
    // CircleRuntime-intercepted property paths (the `graphicalUiElement is CircleRuntime` arms in
    // the Circle branch of SetPropertyOnRenderableFunc). Unlike the tests below — which set the
    // runtime's typed properties directly — these drive the STRING property name through the
    // production dispatcher (via SetProperty) and assert the value lands on the runtime. They are
    // the safety net for the planned runtime-type-first restructure of the dispatcher: that refactor
    // must keep routing each name to the same runtime setter.

    [Fact]
    public void Dispatch_DropshadowBlur_RoutesToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("DropshadowBlur", 7f);

        sut.DropshadowBlur.ShouldBe(7f);
    }

    [Fact]
    public void Dispatch_DropshadowChannels_ComposeDropshadowColor()
    {
        CircleRuntime sut = new();

        sut.SetProperty("DropshadowRed", 10);
        sut.SetProperty("DropshadowGreen", 20);
        sut.SetProperty("DropshadowBlue", 30);
        sut.SetProperty("DropshadowAlpha", 40);

        sut.DropshadowColor.ShouldBe(new SKColor(10, 20, 30, 40));
    }

    [Fact]
    public void Dispatch_DropshadowOffset_RoutesToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("DropshadowOffsetX", 4f);
        sut.SetProperty("DropshadowOffsetY", 6f);

        sut.DropshadowOffsetX.ShouldBe(4f);
        sut.DropshadowOffsetY.ShouldBe(6f);
    }

    [Fact]
    public void Dispatch_FillChannels_ComposeFillColor()
    {
        CircleRuntime sut = new();

        sut.SetProperty("FillRed", 10);
        sut.SetProperty("FillGreen", 20);
        sut.SetProperty("FillBlue", 30);
        sut.SetProperty("FillAlpha", 40);

        sut.FillColor.ShouldBe(new SKColor(10, 20, 30, 40));
    }

    [Fact]
    public void Dispatch_HasDropshadow_RoutesToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("HasDropshadow", true);

        sut.HasDropshadow.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_IsFilled_RoutesToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("IsFilled", true);

        sut.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_Radius_SetsWidthAndHeight()
    {
        CircleRuntime sut = new();

        sut.SetProperty("Radius", 20f);

        sut.Width.ShouldBe(40f);
        sut.Height.ShouldBe(40f);
    }

    [Fact]
    public void Dispatch_StrokeChannels_ComposeStrokeColor()
    {
        CircleRuntime sut = new();

        sut.SetProperty("StrokeRed", 11);
        sut.SetProperty("StrokeGreen", 22);
        sut.SetProperty("StrokeBlue", 33);
        sut.SetProperty("StrokeAlpha", 44);

        sut.StrokeColor.ShouldBe(new SKColor(11, 22, 33, 44));
    }

    [Fact]
    public void Dispatch_StrokeDashLength_RoutesToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("StrokeDashLength", 9f);

        sut.StrokeDashLength.ShouldBe(9f);
    }

    [Fact]
    public void Dispatch_StrokeGapLength_RoutesToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("StrokeGapLength", 5f);

        sut.StrokeGapLength.ShouldBe(5f);
    }

    [Fact]
    public void Dispatch_StrokeWidth_RoutesToRuntime()
    {
        CircleRuntime sut = new();

        sut.SetProperty("StrokeWidth", 3f);

        sut.StrokeWidth.ShouldBe(3f);
    }

    // ---- End dispatcher routing pins -------------------------------------------------------

    [Fact]
    public void ContainedRenderable_ShouldBeCircle()
    {
        CircleRuntime sut = new();
        sut.RenderableComponent.ShouldBeOfType<Circle>();
    }

    [Fact]
    public void Height_ShouldBe32_ByDefault()
    {
        CircleRuntime sut = new();
        sut.Height.ShouldBe(32);
    }

    // FillColor / StrokeColor are non-nullable on Skia, mirroring the XNALIKE CircleRuntime.
    // FillColor defaults to opaque white and IsFilled defaults to false, so a fresh runtime
    // renders as a stroke-only outline (the white fill is gated off). Flipping IsFilled = true
    // fills the shape white without needing to also assign FillColor.
    [Fact]
    public void FillColor_ShouldBeWhite_ByDefault()
    {
        CircleRuntime sut = new();
        sut.FillColor.ShouldBe(new SKColor(255, 255, 255, 255));
    }

    [Fact]
    public void IsFilled_ShouldBeFalse_ByDefault()
    {
        CircleRuntime sut = new();
        sut.IsFilled.ShouldBeFalse();
    }

    [Fact]
    public void IsFilled_False_ZeroesFillSlotAlpha()
    {
        CircleRuntime sut = new();
        sut.FillColor = SKColors.Red;

        sut.IsFilled = false;

        Circle fillSlot = (Circle)sut.RenderableComponent;
        fillSlot.Color.Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void FillChannelSetters_ComposeFillColor()
    {
        CircleRuntime sut = new();
        sut.FillRed = 10;
        sut.FillGreen = 20;
        sut.FillBlue = 30;
        sut.FillAlpha = 40;

        sut.FillColor.ShouldBe(new SKColor(10, 20, 30, 40));
    }

    [Fact]
    public void StrokeChannelSetters_ComposeStrokeColor()
    {
        CircleRuntime sut = new();
        sut.StrokeRed = 11;
        sut.StrokeGreen = 22;
        sut.StrokeBlue = 33;
        sut.StrokeAlpha = 44;

        sut.StrokeColor.ShouldBe(new SKColor(11, 22, 33, 44));
    }

    [Fact]
    public void StrokeColor_ShouldBeWhite_ByDefault()
    {
        CircleRuntime sut = new();
        sut.StrokeColor.ShouldBe(SKColors.White);
    }

    [Fact]
    public void Radius_ShouldBe16_ByDefault()
    {
        CircleRuntime sut = new();
#pragma warning disable CS0618 // Radius is obsolete (use Width/Height); pinning the default here
        sut.Radius.ShouldBe(16);
#pragma warning restore CS0618
    }

    [Fact]
    public void StrokeWidth_ShouldBe1_ByDefault()
    {
        CircleRuntime sut = new();
        sut.StrokeWidth.ShouldBe(1);
    }

    [Fact]
    public void Width_ShouldBe32_ByDefault()
    {
        CircleRuntime sut = new();
        sut.Width.ShouldBe(32);
    }

    // Two-slot composition (#2790) — the fill renderable is the contained object and the stroke
    // renderable is its first child. The renderer draws parent before children so the visual
    // order is fill under stroke.
    [Fact]
    public void StrokeSlot_ShouldExistAsChildOfFillSlot_ByDefault()
    {
        CircleRuntime sut = new();

        Circle fillSlot = (Circle)sut.RenderableComponent;
        fillSlot.Children.Count.ShouldBe(1);

        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        strokeSlot.IsFilled.ShouldBeFalse();
        fillSlot.IsFilled.ShouldBeTrue();
    }

    // #2790 acceptance: setting both FillColor and StrokeColor non-null paints the fill slot
    // with the fill color (filled) and the stroke slot with the stroke color (outline). No
    // last-write-wins clobber.
    [Fact]
    public void FillColorAndStrokeColor_BothSet_PaintsEachSlotIndependently()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = SKColors.Crimson;
        sut.StrokeColor = SKColors.Cyan;

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();

        fillSlot.Color.ShouldBe(SKColors.Crimson);
        fillSlot.IsFilled.ShouldBeTrue();
        strokeSlot.Color.ShouldBe(SKColors.Cyan);
        strokeSlot.IsFilled.ShouldBeFalse();
    }

    [Fact]
    public void FillColorAndStrokeColor_SetInReverseOrder_StillPaintsBothSlots()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.StrokeColor = SKColors.Magenta;
        sut.FillColor = SKColors.Gold;

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();

        fillSlot.Color.ShouldBe(SKColors.Gold);
        strokeSlot.Color.ShouldBe(SKColors.Magenta);
    }

    // PreRender mirrors the runtime's Width/Height onto the stroke slot. The stroke renderable
    // honors IsOffsetAppliedForStroke so the drawn ring stays inscribed inside those bounds
    // rather than spilling past — that's the "stroke is contained inside the bounds" check
    // called out in #2790.
    [Fact]
    public void PreRender_ShouldMirrorWidthAndHeight_OntoStrokeSlot()
    {
        CircleRuntime sut = new();
        sut.Width = 80;
        sut.Height = 60;
        sut.FillColor = SKColors.Crimson;
        sut.StrokeColor = SKColors.Cyan;
        sut.StrokeWidth = 4;

        sut.PreRender();

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        strokeSlot.Width.ShouldBe(fillSlot.Width);
        strokeSlot.Height.ShouldBe(fillSlot.Height);
        strokeSlot.IsOffsetAppliedForStroke.ShouldBeTrue();
    }

    // Issue #2834 — when both fill and stroke are visible, the fill is rendered with its
    // radius inset so its outer AA halo sits inside the stroke's opaque band. Skia's stroke
    // is inscribed inside the runtime bounds (IsOffsetAppliedForStroke shifts the stroke
    // inward by StrokeWidth/2 so the ring spans R-sw to R). Without the inset, the stroke's
    // inner AA edge fades from opaque white to transparent atop the still-opaque fill at
    // that radius, producing a visible pink halo on the inside of the stroke. Inset = full
    // stroke width (Skia fits AA within the stroke thickness, so no AA-bloom adjustment).
    //
    // Pushed via FillRadiusInset rather than mutating fill.Width because the fill is the
    // runtime's contained sizing object — mutating Width would feed back into layout and
    // accumulate frame-over-frame until the circle vanished (same trap caught on Apos).
    [Fact]
    public void FillRadiusInset_WhenStrokeVisible_PushedStrokeWidthInPreRender()
    {
        CircleRuntime sut = new();
        sut.Width = 56;
        sut.Height = 56;
        sut.FillColor = SKColors.Red;
        sut.StrokeColor = SKColors.White;
        sut.StrokeWidth = 4;
        sut.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        sut.PreRender();

        Circle fillSlot = (Circle)sut.RenderableComponent;
        fillSlot.FillRadiusInset.ShouldBe(4f);
    }

    // Issue #2938 — stroke is hidden via StrokeWidth = 0 (StrokeColor is non-nullable now).
    // No stroke means no halo overlap to inset against.
    [Fact]
    public void FillRadiusInset_WhenStrokeInvisible_StaysZero()
    {
        CircleRuntime sut = new();
        sut.Width = 56;
        sut.Height = 56;
        sut.FillColor = SKColors.Red;
        sut.StrokeWidth = 0;
        sut.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        sut.PreRender();

        Circle fillSlot = (Circle)sut.RenderableComponent;
        fillSlot.FillRadiusInset.ShouldBe(0f);
    }

    // Regression guard matching the Apos-side test — repeated PreRender calls must not
    // accumulate mutations on the fill slot's Width/Height. The Apos attempt that mutated
    // Width directly shrank circles to zero in ~5 frames; the FillRadiusInset approach keeps
    // Width layout-owned.
    [Fact]
    public void PreRender_RepeatedCalls_DoesNotMutateFillWidthOrHeight()
    {
        CircleRuntime sut = new();
        sut.Width = 56;
        sut.Height = 56;
        sut.FillColor = SKColors.Red;
        sut.StrokeColor = SKColors.White;
        sut.StrokeWidth = 4;
        sut.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        Circle fillSlot = (Circle)sut.RenderableComponent;
        for (int i = 0; i < 10; i++)
        {
            sut.PreRender();
        }

        fillSlot.Width.ShouldBe(56f);
        fillSlot.Height.ShouldBe(56f);
    }

    [Fact]
    public void StrokeWidth_AfterPreRender_AppliesToStrokeSlotNotFillSlot()
    {
        CircleRuntime sut = new();
        sut.StrokeWidth = 5;
        sut.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        sut.PreRender();

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        strokeSlot.StrokeWidth.ShouldBe(5);
    }

    // #2790: UseGradient is a single user knob that applies to whichever slots are active.
    // Issue #2938 — fill activity is gated by IsFilled (FillColor is non-nullable), stroke
    // activity by StrokeWidth > 0; SKPaint.Shader otherwise overrides the alpha-0 fill color
    // and the gradient would render anyway.
    [Fact]
    public void UseGradient_IsFilledFalse_FillSlotStaysOff()
    {
        CircleRuntime sut = new();
        sut.IsFilled = false;
        sut.UseGradient = true;

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.UseGradient.ShouldBeFalse();
        strokeSlot.UseGradient.ShouldBeTrue();
    }

    [Fact]
    public void UseGradient_StrokeWidthZero_StrokeSlotStaysOff()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.StrokeWidth = 0;
        sut.UseGradient = true;

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.UseGradient.ShouldBeTrue();
        strokeSlot.UseGradient.ShouldBeFalse();
    }

    [Fact]
    public void UseGradient_DefaultsBothSlotsOn()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.UseGradient = true;

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.UseGradient.ShouldBeTrue();
        strokeSlot.UseGradient.ShouldBeTrue();
    }

    [Fact]
    public void SettingIsFilledTrue_AfterUseGradientTrue_LightsUpFillSlotGradient()
    {
        CircleRuntime sut = new();
        sut.IsFilled = false;
        sut.UseGradient = true;
        Circle fillSlot = (Circle)sut.RenderableComponent;
        fillSlot.UseGradient.ShouldBeFalse();

        sut.IsFilled = true;

        fillSlot.UseGradient.ShouldBeTrue();
    }

    // Issue #3009 — Circle/Rectangle no longer carry a standalone gradient Color1. Each slot's
    // gradient start mirrors its own solid body color: the fill slot follows FillColor, the stroke
    // slot follows StrokeColor. This removes the solid↔gradient jump on UseGradient toggle and
    // converges the dropshadow alpha onto the gradient start.
    [Fact]
    public void GradientStart_MirrorsFillColor_OnFillSlot()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = new SKColor(10, 20, 30, 40);

        Circle fillSlot = (Circle)sut.RenderableComponent;
        fillSlot.Red1.ShouldBe(10);
        fillSlot.Green1.ShouldBe(20);
        fillSlot.Blue1.ShouldBe(30);
        fillSlot.Alpha1.ShouldBe(40);
    }

    [Fact]
    public void GradientStart_MirrorsStrokeColor_OnStrokeSlot()
    {
        CircleRuntime sut = new();
        sut.StrokeColor = new SKColor(40, 50, 60, 70);

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        strokeSlot.Red1.ShouldBe(40);
        strokeSlot.Green1.ShouldBe(50);
        strokeSlot.Blue1.ShouldBe(60);
        strokeSlot.Alpha1.ShouldBe(70);
    }

    // #2790: dropshadow routes to fill when FillColor is set (shadow underneath the disk reads
    // through any stroke layered on top), otherwise stroke (a stroke-only ring still casts a
    // shadow). Live-routed in PreRender so toggling FillColor moves the shadow.
    [Fact]
    public void Dropshadow_FillColorSet_AppliesToFillSlot()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = SKColors.Red;
        sut.HasDropshadow = true;

        sut.PreRender();

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.HasDropshadow.ShouldBeTrue();
        strokeSlot.HasDropshadow.ShouldBeFalse();
    }

    // Issue #2938 — when IsFilled = false, the shadow routes to the stroke slot.
    [Fact]
    public void Dropshadow_IsFilledFalse_AppliesToStrokeSlot()
    {
        CircleRuntime sut = new();
        sut.IsFilled = false;
        sut.HasDropshadow = true;

        sut.PreRender();

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        strokeSlot.HasDropshadow.ShouldBeTrue();
        fillSlot.HasDropshadow.ShouldBeFalse();
    }

    // #2790: IsAntialiased writes to BOTH slots in two-slot mode so flipping it actually
    // reaches the slot that's drawing the stroke. Without this, setting IsAntialiased = false
    // on a stroke-only CircleRuntime would silently no-op (the stroke slot is the runtime's
    // only visible renderable yet the pass-through only wrote to the fill slot).
    [Fact]
    public void IsAntialiased_MirrorsToStrokeSlot_InTwoSlotMode()
    {
        CircleRuntime sut = new();

        sut.IsAntialiased = false;

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.IsAntialiased.ShouldBeFalse();
        strokeSlot.IsAntialiased.ShouldBeFalse();
    }

    // Issue #2790 — Clone must rebuild a fresh stroke slot so the clone's slot isn't a
    // shallow-copied pointer to the source's. CircleRuntime.Clone (Skia branch) does this via
    // ClearStrokeRenderable + SetStrokeRenderable(new Circle()).
    [Fact]
    public void Clone_StrokeSlot_IsFreshInstance_NotShallowCopyOfSource()
    {
        CircleRuntime source = new();
        source.FillColor = SKColors.Red;
        source.StrokeColor = SKColors.Blue;

        CircleRuntime clone = (CircleRuntime)source.Clone();

        Circle sourceFill = (Circle)source.RenderableComponent;
        Circle cloneFill = (Circle)clone.RenderableComponent;
        Circle sourceStroke = (Circle)sourceFill.Children.Single();
        Circle cloneStroke = (Circle)cloneFill.Children.Single();

        cloneFill.ShouldNotBeSameAs(sourceFill);
        cloneStroke.ShouldNotBeSameAs(sourceStroke);
    }

    [Fact]
    public void Clone_MutatingClone_DoesNotMutateSource()
    {
        CircleRuntime source = new();
        source.IsFilled = true;
        source.FillColor = SKColors.Red;
        source.StrokeColor = SKColors.Blue;

        CircleRuntime clone = (CircleRuntime)source.Clone();
        clone.FillColor = SKColors.Green;
        clone.StrokeColor = SKColors.Yellow;

        Circle sourceFill = (Circle)source.RenderableComponent;
        Circle sourceStroke = (Circle)sourceFill.Children.Single();
        sourceFill.Color.ShouldBe(SKColors.Red);
        sourceStroke.Color.ShouldBe(SKColors.Blue);
    }

    // Composite DropshadowColor mirrors the MG runtime so cross-backend sample code can set
    // the color in one call instead of four per-channel writes.
    [Fact]
    public void DropshadowColor_RoundTrips()
    {
        CircleRuntime sut = new();
        SKColor color = new(10, 20, 30, 40);

        sut.DropshadowColor = color;

        sut.DropshadowColor.ShouldBe(color);
        sut.DropshadowRed.ShouldBe(10);
        sut.DropshadowGreen.ShouldBe(20);
        sut.DropshadowBlue.ShouldBe(30);
        sut.DropshadowAlpha.ShouldBe(40);
    }

    // Scalar-blur collapse: the plain Circle exposes a single isotropic DropshadowBlur. The Skia
    // ctor seeds it to 3 so a freshly-constructed runtime matches MonoGame/raylib (where the
    // scalar already defaults to 3). Previously the Skia ctor seeded only the Y axis, leaving the
    // scalar getter (which reads the X axis) reporting 0 — a cross-backend inconsistency.
    [Fact]
    public void DropshadowBlur_ShouldBe3_ByDefault()
    {
        CircleRuntime sut = new();
        sut.DropshadowBlur.ShouldBe(3);
    }

    [Fact]
    public void Dropshadow_TargetSwitch_ClearsPreviousSlot()
    {
        CircleRuntime sut = new();
        sut.IsFilled = true;
        sut.FillColor = SKColors.Red;
        sut.HasDropshadow = true;
        sut.PreRender();
        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.HasDropshadow.ShouldBeTrue();

        sut.IsFilled = false;
        sut.PreRender();

        fillSlot.HasDropshadow.ShouldBeFalse();
        strokeSlot.HasDropshadow.ShouldBeTrue();
    }
}
