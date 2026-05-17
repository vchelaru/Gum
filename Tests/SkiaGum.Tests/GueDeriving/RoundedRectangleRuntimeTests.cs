using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Linq;

namespace SkiaGum.Tests.GueDeriving;

// Issue #2814 - RoundedRectangleRuntime on Skia gains two-slot fill+stroke composition
// (mirror of CircleRuntime / #2790). The fill RoundedRectangle is the contained object and
// the stroke RoundedRectangle is its first child. CornerRadius (and per-corner overrides)
// is mirrored onto the stroke slot in PreRender so the outline traces the same rounded
// corners as the fill.
public class RoundedRectangleRuntimeTests
{
    public RoundedRectangleRuntimeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void Clone_MutatingClone_DoesNotMutateSource()
    {
        RoundedRectangleRuntime source = new();
        source.FillColor = SKColors.Red;
        source.StrokeColor = SKColors.Blue;

        RoundedRectangleRuntime clone = (RoundedRectangleRuntime)source.Clone();
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
        RoundedRectangleRuntime source = new();
        source.FillColor = SKColors.Red;
        source.StrokeColor = SKColors.Blue;

        RoundedRectangleRuntime clone = (RoundedRectangleRuntime)source.Clone();

        RoundedRectangle sourceFill = (RoundedRectangle)source.RenderableComponent;
        RoundedRectangle cloneFill = (RoundedRectangle)clone.RenderableComponent;
        RoundedRectangle sourceStroke = (RoundedRectangle)sourceFill.Children.Single();
        RoundedRectangle cloneStroke = (RoundedRectangle)cloneFill.Children.Single();

        cloneFill.ShouldNotBeSameAs(sourceFill);
        cloneStroke.ShouldNotBeSameAs(sourceStroke);
    }

    [Fact]
    public void CornerRadius_MirrorsToStrokeSlot_InPreRender()
    {
        RoundedRectangleRuntime sut = new();
        sut.CornerRadius = 12;

        sut.PreRender();

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        strokeSlot.CornerRadius.ShouldBe(12);
    }

    [Fact]
    public void CustomCornerRadii_MirrorToStrokeSlot_InPreRender()
    {
        RoundedRectangleRuntime sut = new();
        sut.CustomRadiusTopLeft = 1;
        sut.CustomRadiusTopRight = 2;
        sut.CustomRadiusBottomLeft = 3;
        sut.CustomRadiusBottomRight = 4;

        sut.PreRender();

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        strokeSlot.CustomRadiusTopLeft.ShouldBe(1);
        strokeSlot.CustomRadiusTopRight.ShouldBe(2);
        strokeSlot.CustomRadiusBottomLeft.ShouldBe(3);
        strokeSlot.CustomRadiusBottomRight.ShouldBe(4);
    }

    [Fact]
    public void FillColorAndStrokeColor_BothSet_PaintsEachSlotIndependently()
    {
        RoundedRectangleRuntime sut = new();
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
    public void StrokeSlot_ShouldExistAsChildOfFillSlot_ByDefault()
    {
        RoundedRectangleRuntime sut = new();

        RoundedRectangle fillSlot = (RoundedRectangle)sut.RenderableComponent;
        fillSlot.Children.Count.ShouldBe(1);

        RoundedRectangle strokeSlot = (RoundedRectangle)fillSlot.Children.Single();
        strokeSlot.IsFilled.ShouldBeFalse();
        fillSlot.IsFilled.ShouldBeTrue();
    }
}
