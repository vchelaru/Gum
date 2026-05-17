using Gum.Wireframe;
using Shouldly;
using SkiaGum.GueDeriving;
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

    [Fact]
    public void FillColor_ShouldBeNull_ByDefault()
    {
        CircleRuntime sut = new();
        sut.FillColor.ShouldBeNull();
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
        sut.Radius.ShouldBe(16);
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
    // FillColor null => fill slot stays gradient-off even when UseGradient = true; otherwise
    // SKPaint.Shader would override the alpha-0 fill color and the gradient would render anyway.
    [Fact]
    public void UseGradient_FillColorNull_FillSlotStaysOff()
    {
        CircleRuntime sut = new();
        sut.UseGradient = true;

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.UseGradient.ShouldBeFalse();
        strokeSlot.UseGradient.ShouldBeTrue();
    }

    [Fact]
    public void UseGradient_BothColorsSet_BothSlotsOn()
    {
        CircleRuntime sut = new();
        sut.FillColor = SKColors.White;
        sut.UseGradient = true;

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.UseGradient.ShouldBeTrue();
        strokeSlot.UseGradient.ShouldBeTrue();
    }

    [Fact]
    public void SettingFillColor_AfterUseGradientTrue_LightsUpFillSlotGradient()
    {
        CircleRuntime sut = new();
        sut.UseGradient = true;
        Circle fillSlot = (Circle)sut.RenderableComponent;
        fillSlot.UseGradient.ShouldBeFalse();

        sut.FillColor = SKColors.White;

        fillSlot.UseGradient.ShouldBeTrue();
    }

    [Fact]
    public void Color1_MirrorsToBothSlots()
    {
        CircleRuntime sut = new();
        sut.Color1 = new SKColor(10, 20, 30, 40);

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.Red1.ShouldBe(10);
        strokeSlot.Red1.ShouldBe(10);
        strokeSlot.Alpha1.ShouldBe(40);
    }

    // #2790: dropshadow routes to fill when FillColor is set (shadow underneath the disk reads
    // through any stroke layered on top), otherwise stroke (a stroke-only ring still casts a
    // shadow). Live-routed in PreRender so toggling FillColor moves the shadow.
    [Fact]
    public void Dropshadow_FillColorSet_AppliesToFillSlot()
    {
        CircleRuntime sut = new();
        sut.FillColor = SKColors.Red;
        sut.HasDropshadow = true;

        sut.PreRender();

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.HasDropshadow.ShouldBeTrue();
        strokeSlot.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void Dropshadow_FillColorNull_AppliesToStrokeSlot()
    {
        CircleRuntime sut = new();
        // FillColor stays null by default; StrokeColor stays white by default.
        sut.HasDropshadow = true;

        sut.PreRender();

        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        strokeSlot.HasDropshadow.ShouldBeTrue();
        fillSlot.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void Dropshadow_TargetSwitch_ClearsPreviousSlot()
    {
        CircleRuntime sut = new();
        sut.FillColor = SKColors.Red;
        sut.HasDropshadow = true;
        sut.PreRender();
        Circle fillSlot = (Circle)sut.RenderableComponent;
        Circle strokeSlot = (Circle)fillSlot.Children.Single();
        fillSlot.HasDropshadow.ShouldBeTrue();

        sut.FillColor = null;
        sut.PreRender();

        fillSlot.HasDropshadow.ShouldBeFalse();
        strokeSlot.HasDropshadow.ShouldBeTrue();
    }
}
