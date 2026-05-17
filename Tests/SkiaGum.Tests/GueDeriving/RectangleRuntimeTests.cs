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
        source.FillColor = SKColors.Red;
        source.StrokeColor = SKColors.Blue;

        RectangleRuntime clone = (RectangleRuntime)source.Clone();
        clone.FillColor = SKColors.Green;
        clone.StrokeColor = SKColors.Yellow;

        LineRectangle sourceFill = (LineRectangle)source.RenderableComponent;
        LineRectangle sourceStroke = (LineRectangle)sourceFill.Children.Single();
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

        LineRectangle sourceFill = (LineRectangle)source.RenderableComponent;
        LineRectangle cloneFill = (LineRectangle)clone.RenderableComponent;
        LineRectangle sourceStroke = (LineRectangle)sourceFill.Children.Single();
        LineRectangle cloneStroke = (LineRectangle)cloneFill.Children.Single();

        cloneFill.ShouldNotBeSameAs(sourceFill);
        cloneStroke.ShouldNotBeSameAs(sourceStroke);
    }

    [Fact]
    public void Color1_MirrorsToBothSlots()
    {
        RectangleRuntime sut = new();
        sut.Color1 = new SKColor(10, 20, 30, 40);

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();
        fillSlot.Red1.ShouldBe(10);
        strokeSlot.Red1.ShouldBe(10);
        strokeSlot.Alpha1.ShouldBe(40);
    }

    [Fact]
    public void ContainedRenderable_ShouldBeLineRectangle()
    {
        RectangleRuntime sut = new();
        sut.RenderableComponent.ShouldBeOfType<LineRectangle>();
    }

    [Fact]
    public void Dropshadow_FillColorNull_AppliesToStrokeSlot()
    {
        RectangleRuntime sut = new();
        sut.HasDropshadow = true;

        sut.PreRender();

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();
        strokeSlot.HasDropshadow.ShouldBeTrue();
        fillSlot.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void Dropshadow_FillColorSet_AppliesToFillSlot()
    {
        RectangleRuntime sut = new();
        sut.FillColor = SKColors.Red;
        sut.HasDropshadow = true;

        sut.PreRender();

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();
        fillSlot.HasDropshadow.ShouldBeTrue();
        strokeSlot.HasDropshadow.ShouldBeFalse();
    }

    [Fact]
    public void Dropshadow_TargetSwitch_ClearsPreviousSlot()
    {
        RectangleRuntime sut = new();
        sut.FillColor = SKColors.Red;
        sut.HasDropshadow = true;
        sut.PreRender();
        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();
        fillSlot.HasDropshadow.ShouldBeTrue();

        sut.FillColor = null;
        sut.PreRender();

        fillSlot.HasDropshadow.ShouldBeFalse();
        strokeSlot.HasDropshadow.ShouldBeTrue();
    }

    [Fact]
    public void FillColor_ShouldBeNull_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.FillColor.ShouldBeNull();
    }

    [Fact]
    public void FillColorAndStrokeColor_BothSet_PaintsEachSlotIndependently()
    {
        RectangleRuntime sut = new();
        sut.FillColor = SKColors.Crimson;
        sut.StrokeColor = SKColors.Cyan;

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();

        fillSlot.Color.ShouldBe(SKColors.Crimson);
        fillSlot.IsFilled.ShouldBeTrue();
        strokeSlot.Color.ShouldBe(SKColors.Cyan);
        strokeSlot.IsFilled.ShouldBeFalse();
    }

    [Fact]
    public void FillColorAndStrokeColor_SetInReverseOrder_StillPaintsBothSlots()
    {
        RectangleRuntime sut = new();
        sut.StrokeColor = SKColors.Magenta;
        sut.FillColor = SKColors.Gold;

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();

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

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();
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

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();
        strokeSlot.Width.ShouldBe(fillSlot.Width);
        strokeSlot.Height.ShouldBe(fillSlot.Height);
        strokeSlot.IsOffsetAppliedForStroke.ShouldBeTrue();
    }

    [Fact]
    public void SettingFillColor_AfterUseGradientTrue_LightsUpFillSlotGradient()
    {
        RectangleRuntime sut = new();
        sut.UseGradient = true;
        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        fillSlot.UseGradient.ShouldBeFalse();

        sut.FillColor = SKColors.White;

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

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        fillSlot.Children.Count.ShouldBe(1);

        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();
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

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();
        strokeSlot.StrokeWidth.ShouldBe(5);
    }

    [Fact]
    public void StrokeWidth_ShouldBe1_ByDefault()
    {
        RectangleRuntime sut = new();
        sut.StrokeWidth.ShouldBe(1);
    }

    [Fact]
    public void UseGradient_BothColorsSet_BothSlotsOn()
    {
        RectangleRuntime sut = new();
        sut.FillColor = SKColors.White;
        sut.UseGradient = true;

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();
        fillSlot.UseGradient.ShouldBeTrue();
        strokeSlot.UseGradient.ShouldBeTrue();
    }

    [Fact]
    public void UseGradient_FillColorNull_FillSlotStaysOff()
    {
        RectangleRuntime sut = new();
        sut.UseGradient = true;

        LineRectangle fillSlot = (LineRectangle)sut.RenderableComponent;
        LineRectangle strokeSlot = (LineRectangle)fillSlot.Children.Single();
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
