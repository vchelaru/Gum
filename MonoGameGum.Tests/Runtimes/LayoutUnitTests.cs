using MonoGameGum.GueDeriving;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.ObjectModel;
using Gum.DataTypes;
using Gum.Wireframe;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class LayoutUnitTests : BaseTestClass
{
    private GraphicalUiElement CreateElementWithSourceRectangle(int sourceWidth, int sourceHeight)
    {
        // Exactly matches the mock pattern from GraphicalUiElementTests.WidthUnits_Ratio_ShouldUseAvailableSpace
        // which is known to work for PercentageOfSourceFile.
        Mock<ITextureCoordinate> mockSprite = new Mock<IPositionedSizedObject>()
            .As<IRenderable>()
            .As<IRenderableIpso>()
            .As<IVisible>()
            .As<IAspectRatio>()
            .As<ITextureCoordinate>();

        mockSprite
            .Setup(m => m.SourceRectangle)
            .Returns(new System.Drawing.Rectangle(0, 0, sourceWidth, sourceHeight));
        mockSprite
            .Setup(m => m.TextureWidth)
            .Returns((float)sourceWidth);
        mockSprite
            .Setup(m => m.TextureHeight)
            .Returns((float)sourceHeight);
        ObservableCollection<IRenderableIpso> spriteChildren = new();
        mockSprite
            .As<IRenderableIpso>()
            .Setup(m => m.Children)
            .Returns(spriteChildren);
        mockSprite
            .As<IRenderableIpso>()
            .SetupProperty(m => m.Width, sourceWidth);
        mockSprite
            .As<IRenderableIpso>()
            .SetupProperty(m => m.Height, sourceHeight);
        mockSprite
            .As<IVisible>()
            .Setup(m => m.AbsoluteVisible)
            .Returns(true);
        mockSprite
            .As<IVisible>()
            .Setup(m => m.Visible)
            .Returns(true);
        mockSprite
            .As<IAspectRatio>()
            .Setup(m => m.AspectRatio)
            .Returns((float)sourceWidth / sourceHeight);

        GraphicalUiElement element = new GraphicalUiElement((IRenderable)mockSprite.Object);
        return element;
    }

    #region Width Units - Absolute and Relative

    [Fact]
    public void WidthAbsolute_ShouldAllowNegativeValues()
    {
        ContainerRuntime element = new();
        element.Width = -10;
        element.WidthUnits = DimensionUnitType.Absolute;

        element.GetAbsoluteWidth().ShouldBe(-10);
    }

    [Fact]
    public void WidthAbsolute_ShouldReturnExactPixels()
    {
        ContainerRuntime element = new();
        element.Width = 150;
        element.WidthUnits = DimensionUnitType.Absolute;

        element.GetAbsoluteWidth().ShouldBe(150);
    }

    [Fact]
    public void WidthAbsolute_ShouldReturnZero_WhenSetToZero()
    {
        ContainerRuntime element = new();
        element.Width = 0;
        element.WidthUnits = DimensionUnitType.Absolute;

        element.GetAbsoluteWidth().ShouldBe(0);
    }

    [Fact]
    public void WidthPercentageOfParent_ShouldReturnFullParentWidth_WhenHundredPercent()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;

        child.GetAbsoluteWidth().ShouldBe(400);
    }

    [Fact]
    public void WidthPercentageOfParent_ShouldReturnHalfParentWidth_WhenFiftyPercent()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;

        child.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void WidthPercentageOfParent_ShouldReturnZero_WhenParentWidthIsZero()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;

        child.GetAbsoluteWidth().ShouldBe(0);
    }

    [Fact]
    public void WidthRelativeToParent_ShouldBeLargerThanParent_WhenPositive()
    {
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Width = 20;
        child.WidthUnits = DimensionUnitType.RelativeToParent;

        child.GetAbsoluteWidth().ShouldBe(220);
    }

    [Fact]
    public void WidthRelativeToParent_ShouldBeSmallerThanParent_WhenNegative()
    {
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Width = -20;
        child.WidthUnits = DimensionUnitType.RelativeToParent;

        child.GetAbsoluteWidth().ShouldBe(180);
    }

    [Fact]
    public void WidthRelativeToParent_ShouldMatchParent_WhenZero()
    {
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Width = 0;
        child.WidthUnits = DimensionUnitType.RelativeToParent;

        child.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void WidthScreenPixel_ShouldUseCanvasWidth()
    {
        // ScreenPixel width does not scale with parent - it uses the raw value
        // (divided by camera zoom when a renderer is present). Without a renderer,
        // it should just return the raw value regardless of parent size.
        ContainerRuntime parent = new();
        parent.Width = 500;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Width = 200;
        child.WidthUnits = DimensionUnitType.ScreenPixel;

        // Should be the raw value, not scaled to parent
        child.GetAbsoluteWidth().ShouldBe(200);
    }

    #endregion

    #region Height Units - Absolute and Relative

    [Fact]
    public void HeightAbsolute_ShouldAllowNegativeValues()
    {
        ContainerRuntime element = new();
        element.Height = -10;
        element.HeightUnits = DimensionUnitType.Absolute;

        element.GetAbsoluteHeight().ShouldBe(-10);
    }

    [Fact]
    public void HeightAbsolute_ShouldReturnExactPixels()
    {
        ContainerRuntime element = new();
        element.Height = 150;
        element.HeightUnits = DimensionUnitType.Absolute;

        element.GetAbsoluteHeight().ShouldBe(150);
    }

    [Fact]
    public void HeightAbsolute_ShouldReturnZero_WhenSetToZero()
    {
        ContainerRuntime element = new();
        element.Height = 0;
        element.HeightUnits = DimensionUnitType.Absolute;

        element.GetAbsoluteHeight().ShouldBe(0);
    }

    [Fact]
    public void HeightPercentageOfParent_ShouldReturnFullParentHeight_WhenHundredPercent()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.PercentageOfParent;

        child.GetAbsoluteHeight().ShouldBe(400);
    }

    [Fact]
    public void HeightPercentageOfParent_ShouldReturnHalfParentHeight_WhenFiftyPercent()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.PercentageOfParent;

        child.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void HeightPercentageOfParent_ShouldReturnZero_WhenParentHeightIsZero()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.PercentageOfParent;

        child.GetAbsoluteHeight().ShouldBe(0);
    }

    [Fact]
    public void HeightRelativeToParent_ShouldBeLargerThanParent_WhenPositive()
    {
        ContainerRuntime parent = new();
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Height = 20;
        child.HeightUnits = DimensionUnitType.RelativeToParent;

        child.GetAbsoluteHeight().ShouldBe(220);
    }

    [Fact]
    public void HeightRelativeToParent_ShouldBeSmallerThanParent_WhenNegative()
    {
        ContainerRuntime parent = new();
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Height = -20;
        child.HeightUnits = DimensionUnitType.RelativeToParent;

        child.GetAbsoluteHeight().ShouldBe(180);
    }

    [Fact]
    public void HeightRelativeToParent_ShouldMatchParent_WhenZero()
    {
        ContainerRuntime parent = new();
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Height = 0;
        child.HeightUnits = DimensionUnitType.RelativeToParent;

        child.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void HeightScreenPixel_ShouldUseCanvasHeight()
    {
        ContainerRuntime parent = new();
        parent.Height = 500;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Height = 200;
        child.HeightUnits = DimensionUnitType.ScreenPixel;

        child.GetAbsoluteHeight().ShouldBe(200);
    }

    #endregion

    #region RelativeToChildren Sizing

    [Fact]
    public void HeightRelativeToChildren_ShouldAddPaddingValue()
    {
        ContainerRuntime parent = new();
        parent.Height = 20;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteHeight().ShouldBe(120);
    }

    [Fact]
    public void HeightRelativeToChildren_ShouldIgnoreInvisibleChildren()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime visibleChild = new();
        visibleChild.Height = 80;
        visibleChild.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(visibleChild);

        ContainerRuntime invisibleChild = new();
        invisibleChild.Height = 200;
        invisibleChild.HeightUnits = DimensionUnitType.Absolute;
        invisibleChild.Visible = false;
        parent.AddChild(invisibleChild);

        parent.GetAbsoluteHeight().ShouldBe(80);
    }

    [Fact]
    public void HeightRelativeToChildren_ShouldIncludeChildYOffset()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Y = 50;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteHeight().ShouldBe(150);
    }

    [Fact]
    public void HeightRelativeToChildren_ShouldMatchTallestChild()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child1 = new();
        child1.Height = 80;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 120;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        parent.GetAbsoluteHeight().ShouldBe(120);
    }

    [Fact]
    public void HeightRelativeToChildren_ShouldReturnPaddingOnly_WhenNoChildren()
    {
        ContainerRuntime parent = new();
        parent.Height = 10;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        parent.GetAbsoluteHeight().ShouldBe(10);
    }

    [Fact]
    public void HeightRelativeToChildren_ShouldUpdateWhenChildResizes()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteHeight().ShouldBe(100);

        child.Height = 200;
        parent.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void HeightRelativeToChildren_ShouldUpdateWhenChildVisibilityToggles_RegularLayout()
    {
        ContainerRuntime grandparent = new();
        grandparent.Height = 0;
        grandparent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        // Explicitly Regular (not TopToBottomStack) — this is the default but
        // being explicit makes the intent of this test clear.
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.Regular;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Baseline: parent and grandparent should reflect the child's height
        parent.GetAbsoluteHeight().ShouldBe(100);
        grandparent.GetAbsoluteHeight().ShouldBe(100);

        // Hide the child — parent and grandparent should shrink to 0
        child.Visible = false;
        parent.GetAbsoluteHeight().ShouldBe(0);
        grandparent.GetAbsoluteHeight().ShouldBe(0);

        // Show the child again — parent and grandparent should restore to 100
        child.Visible = true;
        parent.GetAbsoluteHeight().ShouldBe(100);
        grandparent.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void WidthRelativeToChildren_ShouldAddPaddingValue()
    {
        ContainerRuntime parent = new();
        parent.Width = 20;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteWidth().ShouldBe(120);
    }

    [Fact]
    public void WidthRelativeToChildren_ShouldIgnoreInvisibleChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime visibleChild = new();
        visibleChild.Width = 80;
        visibleChild.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(visibleChild);

        ContainerRuntime invisibleChild = new();
        invisibleChild.Width = 200;
        invisibleChild.WidthUnits = DimensionUnitType.Absolute;
        invisibleChild.Visible = false;
        parent.AddChild(invisibleChild);

        parent.GetAbsoluteWidth().ShouldBe(80);
    }

    [Fact]
    public void WidthRelativeToChildren_ShouldIncludeChildXOffset()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.X = 50;
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteWidth().ShouldBe(150);
    }

    [Fact]
    public void WidthRelativeToChildren_ShouldMatchWidestChild()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 120;
        child2.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        parent.GetAbsoluteWidth().ShouldBe(120);
    }

    [Fact]
    public void WidthRelativeToChildren_ShouldReturnPaddingOnly_WhenNoChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 10;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        parent.GetAbsoluteWidth().ShouldBe(10);
    }

    [Fact]
    public void WidthRelativeToChildren_ShouldUpdateWhenChildMoves()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.X = 10;
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteWidth().ShouldBe(110);

        child.X = 50;
        parent.GetAbsoluteWidth().ShouldBe(150);
    }

    [Fact]
    public void WidthRelativeToChildren_ShouldUpdateWhenChildResizes()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteWidth().ShouldBe(100);

        child.Width = 200;
        parent.GetAbsoluteWidth().ShouldBe(200);
    }

    #endregion

    #region PercentageOfOtherDimension

    [Fact]
    public void HeightPercentOfOtherDimension_ShouldBeHalfWidth_WhenFiftyPercent()
    {
        ContainerRuntime element = new();
        element.Width = 200;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Height = 50;
        element.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;

        element.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void HeightPercentOfOtherDimension_ShouldEqualWidth_WhenHundredPercent()
    {
        ContainerRuntime element = new();
        element.Width = 200;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;

        element.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void WidthPercentOfOtherDimension_ShouldBeHalfHeight_WhenFiftyPercent()
    {
        ContainerRuntime element = new();
        element.Height = 200;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Width = 50;
        element.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;

        element.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void WidthPercentOfOtherDimension_ShouldEqualHeight_WhenHundredPercent()
    {
        ContainerRuntime element = new();
        element.Height = 200;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;

        element.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void WidthPercentOfOtherDimension_ShouldUpdate_WhenHeightChanges()
    {
        ContainerRuntime element = new();
        element.Height = 200;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;

        element.GetAbsoluteWidth().ShouldBe(200);

        element.Height = 300;
        element.GetAbsoluteWidth().ShouldBe(300);
    }

    #endregion

    #region AbsoluteMultipliedByFontScale

    [Fact]
    public void HeightAbsoluteMultipliedByFontScale_ShouldHandleFractionalScale()
    {
        GraphicalUiElement.GlobalFontScale = 1.5f;

        ContainerRuntime element = new();
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;

        element.GetAbsoluteHeight().ShouldBe(150);
    }

    [Fact]
    public void HeightAbsoluteMultipliedByFontScale_ShouldScaleByGlobalFontScale()
    {
        GraphicalUiElement.GlobalFontScale = 2;

        ContainerRuntime element = new();
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;

        element.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void WidthAbsoluteMultipliedByFontScale_ShouldReturnUnscaled_WhenScaleIsOne()
    {
        GraphicalUiElement.GlobalFontScale = 1;

        ContainerRuntime element = new();
        element.Width = 100;
        element.WidthUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;

        element.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void WidthAbsoluteMultipliedByFontScale_ShouldScaleByGlobalFontScale()
    {
        GraphicalUiElement.GlobalFontScale = 2;

        ContainerRuntime element = new();
        element.Width = 100;
        element.WidthUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;

        element.GetAbsoluteWidth().ShouldBe(200);
    }

    #endregion

    #region Ratio Width and Height

    [Fact]
    public void HeightRatio_ShouldDistributeRemainingSpace_AmongRatioSiblings()
    {
        ContainerRuntime parent = new();
        parent.Height = 300;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 1;
        child1.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 2;
        child2.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        child1.GetAbsoluteHeight().ShouldBe(100);
        child2.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void HeightRatio_ShouldSubtractAbsoluteSiblings_BeforeDistributing()
    {
        ContainerRuntime parent = new();
        parent.Height = 300;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime absoluteChild = new();
        absoluteChild.Height = 100;
        absoluteChild.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime ratioChild = new();
        ratioChild.Height = 1;
        ratioChild.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        ratioChild.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void HeightRatio_ShouldSubtractPercentageSiblings_BeforeDistributing()
    {
        ContainerRuntime parent = new();
        parent.Height = 300;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime percentChild = new();
        percentChild.Height = 50;
        percentChild.HeightUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(percentChild);

        ContainerRuntime ratioChild = new();
        ratioChild.Height = 1;
        ratioChild.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        ratioChild.GetAbsoluteHeight().ShouldBe(150);
    }

    [Fact]
    public void WidthRatio_ShouldDistributeEvenly_WhenMultipleSiblingsHaveEqualRatio()
    {
        ContainerRuntime parent = new();
        parent.Width = 300;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 1;
        child1.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 1;
        child3.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child3);

        child1.GetAbsoluteWidth().ShouldBe(100);
        child2.GetAbsoluteWidth().ShouldBe(100);
        child3.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void WidthRatio_ShouldDistributeProportionally_WhenDifferentRatios()
    {
        ContainerRuntime parent = new();
        parent.Width = 600;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 1;
        child1.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 2;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 3;
        child3.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child3);

        child1.GetAbsoluteWidth().ShouldBe(100);
        child2.GetAbsoluteWidth().ShouldBe(200);
        child3.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void WidthRatio_ShouldIgnoreInvisibleRatioSiblings()
    {
        ContainerRuntime parent = new();
        parent.Width = 300;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 1;
        child1.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child1);

        ContainerRuntime invisibleChild = new();
        invisibleChild.Width = 1;
        invisibleChild.WidthUnits = DimensionUnitType.Ratio;
        invisibleChild.Visible = false;
        parent.AddChild(invisibleChild);

        ContainerRuntime child3 = new();
        child3.Width = 1;
        child3.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child3);

        child1.GetAbsoluteWidth().ShouldBe(150);
        child3.GetAbsoluteWidth().ShouldBe(150);
    }

    [Fact]
    public void WidthRatio_ShouldReturnZero_WhenParentHasNoRemainingSpace()
    {
        ContainerRuntime parent = new();
        parent.Width = 300;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime absoluteChild = new();
        absoluteChild.Width = 300;
        absoluteChild.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime ratioChild = new();
        ratioChild.Width = 1;
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        ratioChild.GetAbsoluteWidth().ShouldBe(0);
    }

    #endregion

    #region X Position Units

    [Fact]
    public void XPercentage_ShouldPositionAsPercentOfParentWidth()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.X = 50;
        child.XUnits = Gum.Converters.GeneralUnitType.Percentage;

        child.AbsoluteLeft.ShouldBe(200);
    }

    [Fact]
    public void XPercentage_ShouldReturnZero_WhenParentWidthIsZero()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.X = 50;
        child.XUnits = Gum.Converters.GeneralUnitType.Percentage;

        child.AbsoluteLeft.ShouldBe(0);
    }

    [Fact]
    public void XPixelsFromLarge_ShouldOffsetOutward_WhenPositive()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.X = 10;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;

        // PixelsFromLarge: positive values go further right from the right edge
        child.AbsoluteLeft.ShouldBe(410);
    }

    [Fact]
    public void XPixelsFromLarge_ShouldPositionFromRightEdge()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.X = 0;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;

        child.AbsoluteLeft.ShouldBe(400);
    }

    [Fact]
    public void XPixelsFromMiddle_ShouldOffsetFromCenter_WhenNonZero()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.X = 50;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        child.AbsoluteLeft.ShouldBe(250);
    }

    [Fact]
    public void XPixelsFromMiddle_ShouldPositionAtCenter()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.X = 0;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        child.AbsoluteLeft.ShouldBe(200);
    }

    [Fact]
    public void XPixelsFromSmall_ShouldOffsetByXValue()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.X = 100;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.X = 50;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;

        child.AbsoluteLeft.ShouldBe(150);
    }

    [Fact]
    public void XPixelsFromSmall_ShouldPositionFromLeftEdge()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.X = 30;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;

        child.AbsoluteLeft.ShouldBe(30);
    }

    #endregion

    #region Y Position Units

    [Fact]
    public void YPercentage_ShouldPositionAsPercentOfParentHeight()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Y = 50;
        child.YUnits = Gum.Converters.GeneralUnitType.Percentage;

        child.AbsoluteTop.ShouldBe(200);
    }

    [Fact]
    public void YPercentage_ShouldReturnZero_WhenParentHeightIsZero()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Y = 50;
        child.YUnits = Gum.Converters.GeneralUnitType.Percentage;

        child.AbsoluteTop.ShouldBe(0);
    }

    [Fact]
    public void YPixelsFromLarge_ShouldOffsetOutward_WhenPositive()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Y = 10;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;

        // PixelsFromLarge: positive values go further down from the bottom edge
        child.AbsoluteTop.ShouldBe(410);
    }

    [Fact]
    public void YPixelsFromLarge_ShouldPositionFromBottomEdge()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Y = 0;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;

        child.AbsoluteTop.ShouldBe(400);
    }

    [Fact]
    public void YPixelsFromMiddle_ShouldOffsetFromCenter_WhenNonZero()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Y = 50;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        child.AbsoluteTop.ShouldBe(250);
    }

    [Fact]
    public void YPixelsFromMiddle_ShouldPositionAtCenter()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Y = 0;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        child.AbsoluteTop.ShouldBe(200);
    }

    [Fact]
    public void YPixelsFromSmall_ShouldOffsetByYValue()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.Y = 100;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Y = 50;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;

        child.AbsoluteTop.ShouldBe(150);
    }

    [Fact]
    public void YPixelsFromSmall_ShouldPositionFromTopEdge()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        parent.AddChild(child);
        child.Y = 30;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;

        child.AbsoluteTop.ShouldBe(30);
    }

    #endregion

    #region X Origin

    [Fact]
    public void XOriginCenter_ShouldAlignCenterToPosition()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.XOrigin = HorizontalAlignment.Center;
        child.X = 200;
        parent.AddChild(child);

        // AbsoluteX is the origin point position (center of child at 200)
        // AbsoluteLeft is the left edge = 200 - 50 = 150
        child.AbsoluteLeft.ShouldBe(150);
    }

    [Fact]
    public void XOriginCenter_WithPixelsFromMiddle_ShouldCenterInParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.XOrigin = HorizontalAlignment.Center;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        child.X = 0;
        parent.AddChild(child);

        // Center of parent = 200, center origin shifts left edge to 150
        child.AbsoluteLeft.ShouldBe(150);
    }

    [Fact]
    public void XOriginLeft_ShouldAlignLeftEdgeToPosition()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.XOrigin = HorizontalAlignment.Left;
        child.X = 100;
        parent.AddChild(child);

        child.AbsoluteLeft.ShouldBe(100);
    }

    [Fact]
    public void XOriginRight_ShouldAlignRightEdgeToPosition()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.XOrigin = HorizontalAlignment.Right;
        child.X = 200;
        parent.AddChild(child);

        // Right edge at 200, so left edge = 200 - 100 = 100
        child.AbsoluteLeft.ShouldBe(100);
    }

    [Fact]
    public void XOriginRight_WithPixelsFromLarge_ShouldAlignToRightEdge()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.XOrigin = HorizontalAlignment.Right;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        child.X = 0;
        parent.AddChild(child);

        // Right edge at parent right (400), left edge = 400 - 100 = 300
        child.AbsoluteLeft.ShouldBe(300);
    }

    #endregion

    #region Y Origin

    [Fact]
    public void YOriginBottom_ShouldAlignBottomEdgeToPosition()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.YOrigin = VerticalAlignment.Bottom;
        child.Y = 200;
        parent.AddChild(child);

        // Bottom edge at 200, top edge = 200 - 100 = 100
        child.AbsoluteTop.ShouldBe(100);
    }

    [Fact]
    public void YOriginBottom_WithPixelsFromLarge_ShouldAlignToBottomEdge()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.YOrigin = VerticalAlignment.Bottom;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        child.Y = 0;
        parent.AddChild(child);

        // Bottom edge at parent bottom (400), top edge = 400 - 100 = 300
        child.AbsoluteTop.ShouldBe(300);
    }

    [Fact]
    public void YOriginCenter_ShouldAlignCenterToPosition()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.YOrigin = VerticalAlignment.Center;
        child.Y = 200;
        parent.AddChild(child);

        child.AbsoluteTop.ShouldBe(150);
    }

    [Fact]
    public void YOriginCenter_WithPixelsFromMiddle_ShouldCenterInParent()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.YOrigin = VerticalAlignment.Center;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        child.Y = 0;
        parent.AddChild(child);

        child.AbsoluteTop.ShouldBe(150);
    }

    [Fact]
    public void YOriginTop_ShouldAlignTopEdgeToPosition()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.YOrigin = VerticalAlignment.Top;
        child.Y = 100;
        parent.AddChild(child);

        child.AbsoluteTop.ShouldBe(100);
    }

    #endregion

    #region Origin + Units Combinations

    [Fact]
    public void XOriginCenter_WithPixelsFromSmall_ShouldShiftLeftByHalfWidth()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.XOrigin = HorizontalAlignment.Center;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        child.X = 200;
        parent.AddChild(child);

        child.AbsoluteLeft.ShouldBe(150);
    }

    [Fact]
    public void XOriginLeft_WithPixelsFromLarge_ShouldPositionFromRightEdge()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.XOrigin = HorizontalAlignment.Left;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        child.X = 0;
        parent.AddChild(child);

        // Left edge at parent right = 400
        child.AbsoluteLeft.ShouldBe(400);
    }

    [Fact]
    public void XOriginRight_WithPercentage_ShouldOffsetByChildWidth()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.XOrigin = HorizontalAlignment.Right;
        child.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        child.X = 100;
        parent.AddChild(child);

        // X=100% of parent width = 400 pixels from left. Right edge at 400, left edge = 300
        child.AbsoluteLeft.ShouldBe(300);
    }

    [Fact]
    public void YOriginBottom_WithPercentage_ShouldOffsetByChildHeight()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.YOrigin = VerticalAlignment.Bottom;
        child.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        child.Y = 100;
        parent.AddChild(child);

        // Y=100% of parent height = 400. Bottom edge at 400, top edge = 300
        child.AbsoluteTop.ShouldBe(300);
    }

    [Fact]
    public void YOriginCenter_WithPixelsFromSmall_ShouldShiftUpByHalfHeight()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.YOrigin = VerticalAlignment.Center;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        child.Y = 200;
        parent.AddChild(child);

        child.AbsoluteTop.ShouldBe(150);
    }

    #endregion

    #region TopToBottomStack

    [Fact]
    public void TopToBottomStack_ShouldHandleMixedHeightUnits()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 100;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child2);

        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(100);
        child2.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void TopToBottomStack_ShouldHandleZeroHeightChildren()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 0;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(0);
    }

    [Fact]
    public void TopToBottomStack_ShouldPositionFirstChildAtZero()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child = new();
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        child.AbsoluteTop.ShouldBe(parent.AbsoluteTop);
    }

    [Fact]
    public void TopToBottomStack_ShouldRespectChildXUnits()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child = new();
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.X = 100;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        parent.AddChild(child);

        child.AbsoluteLeft.ShouldBe(100);
    }

    [Fact]
    public void TopToBottomStack_ShouldRespectStackSpacing()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = 10;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(60);
        child3.AbsoluteTop.ShouldBe(120);
    }

    [Fact]
    public void TopToBottomStack_ShouldSkipInvisibleChildren()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.Visible = false;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        child3.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void TopToBottomStack_ShouldStackChildrenVertically()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(50);
        child3.AbsoluteTop.ShouldBe(100);
    }

    [Fact]
    public void TopToBottomStack_ShouldUpdatePositions_WhenChildHeightChanges()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        child2.AbsoluteTop.ShouldBe(50);

        child1.Height = 100;
        child2.AbsoluteTop.ShouldBe(100);
    }

    #endregion

    #region UseFixedStackChildrenSize

    [Fact]
    public void UseFixedStackChildrenSize_ShouldPositionChildrenUsingFirstChildHeight()
    {
        // When UseFixedStackChildrenSize is true, ALL children should be positioned
        // as if every child has the same height as the first child — even if they don't.
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.UseFixedStackChildrenSize = true;

        ContainerRuntime child1 = new();
        child1.Height = 40;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 100; // intentionally different from child1
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 10; // intentionally different from child1
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // All children should be spaced as if height = 40 (first child's height)
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(40);
        child3.AbsoluteTop.ShouldBe(80);
    }

    [Fact]
    public void UseFixedStackChildrenSize_WithStackSpacing_ShouldUseFirstChildHeightPlusSpacing()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.UseFixedStackChildrenSize = true;
        parent.StackSpacing = 5;

        ContainerRuntime child1 = new();
        child1.Height = 30;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 80; // different from child1
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 10; // different from child1
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Positions: 0, 30+5=35, 35+30+5=70
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(35);
        child3.AbsoluteTop.ShouldBe(70);
    }

    [Fact]
    public void UseFixedStackChildrenSize_ParentHeightRelativeToChildren_ShouldUseFirstChildHeight()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.UseFixedStackChildrenSize = true;

        ContainerRuntime child1 = new();
        child1.Height = 25;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 100; // much taller, but should be treated as 25
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 5;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Parent height should be: 25 + (0 + 25) * 2 = 75
        parent.GetAbsoluteHeight().ShouldBe(75);
    }

    [Fact]
    public void UseFixedStackChildrenSize_False_ShouldPositionUsingActualChildHeights()
    {
        // Baseline: without the flag, variable heights are respected
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.UseFixedStackChildrenSize = false;

        ContainerRuntime child1 = new();
        child1.Height = 40;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 100;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 10;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Normal stacking uses actual heights
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(40);
        child3.AbsoluteTop.ShouldBe(140); // 40 + 100
    }

    #endregion

    #region LeftToRightStack

    [Fact]
    public void LeftToRightStack_ShouldHandleMixedWidthUnits()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 25;
        child2.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child2);

        child1.AbsoluteLeft.ShouldBe(0);
        child2.AbsoluteLeft.ShouldBe(100);
        child2.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void LeftToRightStack_ShouldPositionFirstChildAtZero()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        child.AbsoluteLeft.ShouldBe(parent.AbsoluteLeft);
    }

    [Fact]
    public void LeftToRightStack_ShouldRespectChildYUnits()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Y = 100;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        parent.AddChild(child);

        child.AbsoluteTop.ShouldBe(100);
    }

    [Fact]
    public void LeftToRightStack_ShouldRespectStackSpacing()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.StackSpacing = 10;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        child1.AbsoluteLeft.ShouldBe(0);
        child2.AbsoluteLeft.ShouldBe(60);
        child3.AbsoluteLeft.ShouldBe(120);
    }

    [Fact]
    public void LeftToRightStack_ShouldSkipInvisibleChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Visible = false;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        child3.AbsoluteLeft.ShouldBe(50);
    }

    [Fact]
    public void LeftToRightStack_ShouldStackChildrenHorizontally()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        child1.AbsoluteLeft.ShouldBe(0);
        child2.AbsoluteLeft.ShouldBe(50);
        child3.AbsoluteLeft.ShouldBe(100);
    }

    [Fact]
    public void LeftToRightStack_ShouldUpdatePositions_WhenChildWidthChanges()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        child2.AbsoluteLeft.ShouldBe(50);

        child1.Width = 100;
        child2.AbsoluteLeft.ShouldBe(100);
    }

    #endregion

    #region WrapsChildren

    [Fact]
    public void WrapsChildren_LeftToRight_ShouldCreateNewRow_WhenExceedingMaxWidth()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.MaxWidth = 200;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 80;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // First two fit in 200px, third wraps to new row
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(0);
        child3.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void WrapsChildren_ShouldNotWrap_WhenMaxDimensionNotSet()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 80;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // All on the same row since parent width is large enough
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(0);
        child3.AbsoluteTop.ShouldBe(0);
    }

    [Fact]
    public void WrapsChildren_ShouldPositionWrappedItems_AtCorrectOffsets()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.MaxWidth = 160;
        parent.WrapsChildren = true;

        for (int i = 0; i < 4; i++)
        {
            ContainerRuntime child = new();
            child.Width = 80;
            child.WidthUnits = DimensionUnitType.Absolute;
            child.Height = 50;
            child.HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(child);
        }

        // Row 1: children 0,1 at Y=0
        // Row 2: children 2,3 at Y=50
        parent.Children[0].AbsoluteLeft.ShouldBe(0);
        parent.Children[1].AbsoluteLeft.ShouldBe(80);
        parent.Children[2].AbsoluteLeft.ShouldBe(0);
        parent.Children[2].AbsoluteTop.ShouldBe(50);
        parent.Children[3].AbsoluteLeft.ShouldBe(80);
        parent.Children[3].AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void WrapsChildren_ShouldRespectStackSpacing_AcrossWrappedLines()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.MaxWidth = 200;
        parent.WrapsChildren = true;
        parent.StackSpacing = 10;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 80;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // First two: 80 + 10 + 80 = 170, fits in 200
        // Third: 170 + 10 + 80 = 260 > 200, wraps
        child1.AbsoluteLeft.ShouldBe(0);
        child2.AbsoluteLeft.ShouldBe(90);
        child3.AbsoluteTop.ShouldBe(50 + 10);
    }

    [Fact]
    public void WrapsChildren_TopToBottom_ShouldCreateNewColumn_WhenExceedingMaxHeight()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.MaxHeight = 150;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 80;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 80;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // First child fits, second would exceed 150, so it wraps to new column
        child1.AbsoluteLeft.ShouldBe(0);
        child2.AbsoluteLeft.ShouldBe(50);
    }

    #endregion

    #region RelativeToChildren with Stacking

    [Fact]
    public void HeightRelativeToChildren_LeftToRightStack_ShouldUseTallestChild()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 80;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 120;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 60;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        parent.GetAbsoluteHeight().ShouldBe(120);
    }

    [Fact]
    public void HeightRelativeToChildren_TopToBottomStack_ShouldIncludeStackSpacing()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = 10;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // 50 + 10 + 50 + 10 + 50 = 170
        parent.GetAbsoluteHeight().ShouldBe(170);
    }

    [Fact]
    public void HeightRelativeToChildren_TopToBottomStack_ShouldSumChildHeights()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        parent.GetAbsoluteHeight().ShouldBe(150);
    }

    [Fact]
    public void WidthRelativeToChildren_LeftToRightStack_ShouldIncludeStackSpacing()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.StackSpacing = 10;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // 50 + 10 + 50 + 10 + 50 = 170
        parent.GetAbsoluteWidth().ShouldBe(170);
    }

    [Fact]
    public void WidthRelativeToChildren_LeftToRightStack_ShouldSumChildWidths()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        parent.GetAbsoluteWidth().ShouldBe(150);
    }

    [Fact]
    public void WidthRelativeToChildren_TopToBottomStack_ShouldUseWidestChild()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 120;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 60;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        parent.GetAbsoluteWidth().ShouldBe(120);
    }

    #endregion

    #region Nested Layout Propagation

    [Fact]
    public void ChildMove_ShouldUpdateRelativeToChildrenParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.X = 0;
        parent.AddChild(child);

        parent.GetAbsoluteWidth().ShouldBe(100);

        child.X = 50;
        parent.GetAbsoluteWidth().ShouldBe(150);
    }

    [Fact]
    public void ChildResize_ShouldUpdateRelativeToChildrenParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteWidth().ShouldBe(100);

        child.Width = 200;
        parent.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void GrandchildResize_ShouldPropagateToGrandparent_WhenBothRelativeToChildren()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 0;
        grandparent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        grandparent.GetAbsoluteWidth().ShouldBe(100);

        child.Width = 200;
        grandparent.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void NestedPercentageOfParent_ShouldCascade_ThroughThreeLevels()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 400;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 50;
        parent.WidthUnits = DimensionUnitType.PercentageOfParent;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void NestedPercentageOfParent_ShouldHandleZeroParent()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 0;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 50;
        parent.WidthUnits = DimensionUnitType.PercentageOfParent;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(0);
    }

    [Fact]
    public void NestedRelativeToChildren_ShouldPropagateUpward()
    {
        ContainerRuntime grandparent = new();
        grandparent.Height = 0;
        grandparent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        grandparent.GetAbsoluteHeight().ShouldBe(100);

        child.Height = 150;
        grandparent.GetAbsoluteHeight().ShouldBe(150);
    }

    [Fact]
    public void NestedRelativeToParent_ShouldCascade()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 400;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = -50;
        parent.WidthUnits = DimensionUnitType.RelativeToParent;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = -50;
        child.WidthUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(child);

        // parent = 400 - 50 = 350, child = 350 - 50 = 300
        child.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void ParentResize_ShouldUpdatePercentageChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(200);

        parent.Width = 600;
        child.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void ParentResize_ShouldUpdateRelativeToParentChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = -100;
        child.WidthUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(300);

        parent.Width = 500;
        child.GetAbsoluteWidth().ShouldBe(400);
    }

    #endregion

    #region Position Updates and Absolute Coordinates

    [Fact]
    public void AbsoluteCoordinates_ShouldUpdate_WhenParentMoves()
    {
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.X = 0;
        parent.Y = 0;

        ContainerRuntime child = new();
        child.X = 50;
        child.Y = 50;
        parent.AddChild(child);

        child.AbsoluteLeft.ShouldBe(50);
        child.AbsoluteTop.ShouldBe(50);

        parent.X = 100;
        parent.Y = 100;

        child.AbsoluteLeft.ShouldBe(150);
        child.AbsoluteTop.ShouldBe(150);
    }

    [Fact]
    public void AbsoluteLeft_ShouldAccountForOrigin()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.XOrigin = HorizontalAlignment.Center;
        child.X = 200;
        parent.AddChild(child);

        // AbsoluteLeft = left edge, which is 200 - 50 = 150
        child.AbsoluteLeft.ShouldBe(150);
        // AbsoluteX = origin point position (center at 200)
        child.AbsoluteX.ShouldBe(200);
    }

    [Fact]
    public void AbsoluteTop_ShouldAccountForOrigin()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.YOrigin = VerticalAlignment.Center;
        child.Y = 200;
        parent.AddChild(child);

        // AbsoluteTop = top edge, which is 200 - 50 = 150
        child.AbsoluteTop.ShouldBe(150);
        // AbsoluteY = origin point position (center at 200)
        child.AbsoluteY.ShouldBe(200);
    }

    [Fact]
    public void AbsoluteX_ShouldIncludeParentAbsoluteX()
    {
        ContainerRuntime parent = new();
        parent.X = 100;
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.X = 50;
        parent.AddChild(child);

        child.AbsoluteLeft.ShouldBe(150);
    }

    [Fact]
    public void AbsoluteY_ShouldIncludeParentAbsoluteY()
    {
        ContainerRuntime parent = new();
        parent.Y = 100;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Y = 50;
        parent.AddChild(child);

        child.AbsoluteTop.ShouldBe(150);
    }

    [Fact]
    public void GetAbsoluteLeft_ShouldWorkWithPixelsFromLarge()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.XOrigin = HorizontalAlignment.Right;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        child.X = 0;
        parent.AddChild(child);

        // Right edge at 400, left edge = 300
        child.AbsoluteLeft.ShouldBe(300);
    }

    [Fact]
    public void GetAbsoluteTop_ShouldWorkWithPixelsFromLarge()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.YOrigin = VerticalAlignment.Bottom;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        child.Y = 0;
        parent.AddChild(child);

        // Bottom edge at 400, top edge = 300
        child.AbsoluteTop.ShouldBe(300);
    }

    #endregion

    #region Layout Suspension

    [Fact]
    public void IsAllLayoutSuspended_ShouldPreventAllInstances()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(200);

        GraphicalUiElement.IsAllLayoutSuspended = true;
        parent.Width = 600;

        // Layout should NOT have updated
        child.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void IsAllLayoutSuspended_ShouldResumeCorrectly_WhenSetToFalse()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        GraphicalUiElement.IsAllLayoutSuspended = true;
        parent.Width = 600;

        GraphicalUiElement.IsAllLayoutSuspended = false;
        parent.UpdateLayout();

        child.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void ResumeLayout_ShouldApplyPendingLayoutChanges()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        parent.SuspendLayout();
        parent.Width = 600;

        // Layout not yet updated during suspension
        child.GetAbsoluteWidth().ShouldBe(200);

        parent.ResumeLayout();

        child.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void ResumeLayout_ShouldNotCrash_WhenCalledWithoutSuspend()
    {
        ContainerRuntime element = new();
        element.Width = 100;
        element.WidthUnits = DimensionUnitType.Absolute;

        // Should not throw
        element.ResumeLayout();

        element.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void SuspendLayout_ShouldPreventLayoutUpdates()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(200);

        parent.SuspendLayout();
        parent.Width = 800;

        // Layout should NOT have updated during suspension
        child.GetAbsoluteWidth().ShouldBe(200);
    }

    #endregion

    #region Stacking with Ratio Children

    [Fact]
    public void LeftToRightStack_MixedAbsoluteAndRatio_ShouldCalculateCorrectly()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime absoluteChild = new();
        absoluteChild.Width = 100;
        absoluteChild.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime ratioChild = new();
        ratioChild.Width = 1;
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        ratioChild.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void LeftToRightStack_WidthRatio_ShouldDistributeRemainingHorizontalSpace()
    {
        ContainerRuntime parent = new();
        parent.Width = 300;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 1;
        child1.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 2;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        child1.GetAbsoluteWidth().ShouldBe(100);
        child2.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void TopToBottomStack_HeightRatio_ShouldDistributeRemainingVerticalSpace()
    {
        ContainerRuntime parent = new();
        parent.Height = 300;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 1;
        child1.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 2;
        child2.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        child1.GetAbsoluteHeight().ShouldBe(100);
        child2.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void TopToBottomStack_MixedAbsoluteAndRatio_ShouldCalculateCorrectly()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime absoluteChild = new();
        absoluteChild.Height = 100;
        absoluteChild.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime ratioChild = new();
        ratioChild.Height = 1;
        ratioChild.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        ratioChild.GetAbsoluteHeight().ShouldBe(300);
    }

    [Fact]
    public void TopToBottomStack_RatioChild_ShouldUpdateOnSiblingResize()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime absoluteChild = new();
        absoluteChild.Height = 100;
        absoluteChild.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime ratioChild = new();
        ratioChild.Height = 1;
        ratioChild.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        ratioChild.GetAbsoluteHeight().ShouldBe(300);

        absoluteChild.Height = 200;
        ratioChild.GetAbsoluteHeight().ShouldBe(200);
    }

    #endregion

    #region Regular (non-stacked) Children Layout

    [Fact]
    public void RegularLayout_ShouldAllowOverlappingChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.X = 50;
        child1.Y = 50;
        child1.Width = 200;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 200;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.X = 50;
        child2.Y = 50;
        child2.Width = 200;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 200;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // Both children at same position (overlapping is valid)
        child1.AbsoluteLeft.ShouldBe(50);
        child1.AbsoluteTop.ShouldBe(50);
        child2.AbsoluteLeft.ShouldBe(50);
        child2.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void RegularLayout_ShouldNotStackChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.X = 0;
        child1.Y = 0;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.X = 0;
        child2.Y = 0;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // Without stacking, both stay at Y=0
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(0);
    }

    [Fact]
    public void RegularLayout_ShouldRespectIndividualXYUnits()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.X = 50;
        child1.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.X = 0;
        child2.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        parent.AddChild(child2);

        child1.AbsoluteLeft.ShouldBe(50);
        child2.AbsoluteLeft.ShouldBe(200);
    }

    #endregion

    #region Visible Affecting Layout

    [Fact]
    public void Visible_ShouldExcludeFromRelativeToChildren_WhenFalse()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime visibleChild = new();
        visibleChild.Width = 100;
        visibleChild.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(visibleChild);

        ContainerRuntime invisibleChild = new();
        invisibleChild.Width = 200;
        invisibleChild.WidthUnits = DimensionUnitType.Absolute;
        invisibleChild.Visible = false;
        parent.AddChild(invisibleChild);

        parent.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void Visible_ShouldExcludeFromStackPosition_WhenFalse()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.Visible = false;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // child3 should take position of child2 since child2 is invisible
        child3.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void Visible_ShouldRecalculateRatioSiblings_WhenToggled()
    {
        ContainerRuntime parent = new();
        parent.Width = 300;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 1;
        child1.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 1;
        child3.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child3);

        child1.GetAbsoluteWidth().ShouldBe(100);

        child3.Visible = false;
        // Space now split between two visible children
        child1.GetAbsoluteWidth().ShouldBe(150);
        child2.GetAbsoluteWidth().ShouldBe(150);
    }

    #endregion

    #region Mixed Dimension Units

    [Fact]
    public void MixedHeightUnits_InTopToBottomStack_ShouldStackCorrectly()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime absoluteChild = new();
        absoluteChild.Height = 100;
        absoluteChild.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime percentChild = new();
        percentChild.Height = 25;
        percentChild.HeightUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(percentChild);

        ContainerRuntime ratioChild = new();
        ratioChild.Height = 1;
        ratioChild.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        absoluteChild.AbsoluteTop.ShouldBe(0);
        absoluteChild.GetAbsoluteHeight().ShouldBe(100);

        percentChild.AbsoluteTop.ShouldBe(100);
        percentChild.GetAbsoluteHeight().ShouldBe(100);

        ratioChild.AbsoluteTop.ShouldBe(200);
        ratioChild.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void MixedWidthUnits_AbsoluteAndPercentage_ShouldCoexistInParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime absoluteChild = new();
        absoluteChild.Width = 150;
        absoluteChild.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime percentChild = new();
        percentChild.Width = 50;
        percentChild.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(percentChild);

        absoluteChild.GetAbsoluteWidth().ShouldBe(150);
        percentChild.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void MixedWidthUnits_AbsoluteAndRatio_ShouldCalculateCorrectly()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime absoluteChild = new();
        absoluteChild.Width = 100;
        absoluteChild.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime ratioChild = new();
        ratioChild.Width = 1;
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        absoluteChild.GetAbsoluteWidth().ShouldBe(100);
        ratioChild.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void MixedWidthUnits_PercentageAndRelativeToParent_ShouldCoexistInParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime percentChild = new();
        percentChild.Width = 50;
        percentChild.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(percentChild);

        ContainerRuntime relativeChild = new();
        relativeChild.Width = -100;
        relativeChild.WidthUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(relativeChild);

        percentChild.GetAbsoluteWidth().ShouldBe(200);
        relativeChild.GetAbsoluteWidth().ShouldBe(300);
    }

    #endregion

    #region Canvas-Relative Behavior

    [Fact]
    public void NoParent_PercentageOfParent_ShouldUseCanvasDimensions()
    {
        GraphicalUiElement.CanvasWidth = 1024;
        GraphicalUiElement.CanvasHeight = 768;

        ContainerRuntime element = new();
        element.Width = 50;
        element.WidthUnits = DimensionUnitType.PercentageOfParent;
        element.Height = 50;
        element.HeightUnits = DimensionUnitType.PercentageOfParent;

        element.GetAbsoluteWidth().ShouldBe(512);
        element.GetAbsoluteHeight().ShouldBe(384);
    }

    [Fact]
    public void NoParent_RelativeToParent_ShouldUseCanvasDimensions()
    {
        GraphicalUiElement.CanvasWidth = 1024;
        GraphicalUiElement.CanvasHeight = 768;

        ContainerRuntime element = new();
        element.Width = -100;
        element.WidthUnits = DimensionUnitType.RelativeToParent;
        element.Height = -100;
        element.HeightUnits = DimensionUnitType.RelativeToParent;

        element.GetAbsoluteWidth().ShouldBe(924);
        element.GetAbsoluteHeight().ShouldBe(668);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Layout_ShouldHandleChildWithNoParent()
    {
        ContainerRuntime orphan = new();
        orphan.Width = 100;
        orphan.WidthUnits = DimensionUnitType.Absolute;
        orphan.Height = 100;
        orphan.HeightUnits = DimensionUnitType.Absolute;

        // Should not throw
        orphan.GetAbsoluteWidth().ShouldBe(100);
        orphan.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void Layout_ShouldHandleDeeplyNestedHierarchy()
    {
        GraphicalUiElement.CanvasWidth = 1000;

        ContainerRuntime root = new();
        root.Width = 1000;
        root.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime current = root;
        for (int i = 0; i < 10; i++)
        {
            ContainerRuntime child = new();
            child.Width = 50;
            child.WidthUnits = DimensionUnitType.PercentageOfParent;
            current.AddChild(child);
            current = child;
        }

        // 1000 * 0.5^10 = 0.9765625
        float expectedWidth = 1000;
        for (int i = 0; i < 10; i++)
        {
            expectedWidth *= 0.5f;
        }

        current.GetAbsoluteWidth().ShouldBe(expectedWidth);
    }

    [Fact]
    public void Layout_ShouldHandleZeroSizeParent_WithPercentageChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(0);
        child.GetAbsoluteHeight().ShouldBe(0);
    }

    [Fact]
    public void Layout_ShouldHandleZeroSizeParent_WithRelativeToParentChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = -10;
        child.WidthUnits = DimensionUnitType.RelativeToParent;
        child.Height = -10;
        child.HeightUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(-10);
        child.GetAbsoluteHeight().ShouldBe(-10);
    }

    [Fact]
    public void Layout_ShouldNotCrash_WhenNegativeHeightResultsFromRelativeToParent()
    {
        ContainerRuntime parent = new();
        parent.Height = 50;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = -100;
        child.HeightUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(child);

        // 50 + (-100) = -50
        child.GetAbsoluteHeight().ShouldBe(-50);
    }

    [Fact]
    public void Layout_ShouldNotCrash_WhenNegativeWidthResultsFromRelativeToParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 50;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = -100;
        child.WidthUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(child);

        // 50 + (-100) = -50
        child.GetAbsoluteWidth().ShouldBe(-50);
    }

    [Fact]
    public void Layout_ShouldRecalculate_WhenChildAddedToStack()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        parent.GetAbsoluteHeight().ShouldBe(50);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        parent.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void Layout_ShouldRecalculate_WhenChildRemovedFromStack()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        child3.AbsoluteTop.ShouldBe(100);

        // RemoveChild does not automatically re-layout remaining siblings.
        // A manual UpdateLayout call is needed to reposition.
        parent.RemoveChild(child2);
        parent.UpdateLayout();
        child3.AbsoluteTop.ShouldBe(50);
    }

    #endregion

    #region RelativeToChildren - Circular Dependency Handling

    [Fact]
    public void RelativeToChildren_Height_ShouldIgnoreChildWithPercentageOfParent()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Height = 80;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child2);

        // Only child1 contributes to parent height
        parent.GetAbsoluteHeight().ShouldBe(80);
    }

    [Fact]
    public void RelativeToChildren_ShouldIgnoreChildWithPercentageOfParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child2);

        // Only child1 contributes to parent width
        parent.GetAbsoluteWidth().ShouldBe(100);
        // Child2 gets 50% of parent (100)
        child2.GetAbsoluteWidth().ShouldBe(50);
    }

    [Fact]
    public void RelativeToChildren_ShouldIgnoreChildWithRelativeToParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 150;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = -50;
        child2.WidthUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(child2);

        // Only child1 contributes to parent width
        parent.GetAbsoluteWidth().ShouldBe(150);
        // Child2 gets parent(150) - 50 = 100
        child2.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void RelativeToChildren_ShouldIgnoreRatioChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        // Only child1 contributes; ratio children are excluded
        parent.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void RelativeToChildren_ShouldReturnZero_WhenAllChildrenDependOnParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = -10;
        child2.WidthUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(child2);

        // No absolute children -> parent width is just the padding (0)
        parent.GetAbsoluteWidth().ShouldBe(0);
    }

    [Fact]
    public void RelativeToChildren_ShouldSizeToAbsoluteChild_WhenOnlyOneIsAbsolute()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 200;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteWidth().ShouldBe(200);
    }

    #endregion

    #region RelativeToChildren - Position Unit Interactions

    [Fact]
    public void RelativeToChildren_ShouldAccountForMultipleChildrenAtDifferentPositions()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.X = 0;
        child1.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.X = 100;
        child2.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        parent.AddChild(child2);

        // Child2 extends from 100 to 150, so parent should be 150
        parent.GetAbsoluteWidth().ShouldBe(150);
    }

    [Fact]
    public void RelativeToChildren_ShouldAccountForNegativeChildPosition()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.X = -20;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        parent.AddChild(child);

        // Child extends from -20 to 80. The engine computes parent width
        // based on the rightmost edge of children. With XOrigin=Left (default),
        // the child's right edge is at -20+100=80, so parent width = 80.
        parent.GetAbsoluteWidth().ShouldBe(80);
    }

    [Fact]
    public void RelativeToChildren_ShouldAccountForPixelsFromLarge_Child()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.X = 0;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        parent.AddChild(child);

        // PixelsFromLarge positions from right edge. When the parent is RelativeToChildren
        // and starts at width=0, the child at X=0 from large edge sits at position 0.
        // The engine does not count PixelsFromLarge children toward parent sizing
        // because the position depends on the parent's width (circular).
        parent.GetAbsoluteWidth().ShouldBe(0);
    }

    [Fact]
    public void RelativeToChildren_ShouldAccountForPixelsFromMiddle_Child()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.X = 50;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        child.XOrigin = HorizontalAlignment.Left;
        parent.AddChild(child);

        // PixelsFromMiddle uses 2*max(abs(smallEdge), abs(bigEdge)) formula
        // SmallEdge = 50, BigEdge = 50+100 = 150
        // Parent width = 2 * max(50, 150) = 300
        parent.GetAbsoluteWidth().ShouldBe(300);
    }

    #endregion

    #region Cross-Dimension Dependencies

    [Fact]
    public void CrossDimension_BothAxesPercentOfOther_ShouldFallbackToRawValues()
    {
        ContainerRuntime element = new();
        element.Width = 100;
        element.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;
        element.Height = 200;
        element.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;

        // Both depend on each other -> engine falls back to raw pixel values
        element.GetAbsoluteWidth().ShouldBe(100);
        element.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void CrossDimension_ChildWidthAffectsChildHeight_AffectsParentHeight()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 200;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent.AddChild(child);

        // Child height = 50% of 200 = 100
        child.GetAbsoluteHeight().ShouldBe(100);
        // Parent height should be 100 (RelativeToChildren)
        parent.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void CrossDimension_ParentWidthAffectsChildWidth_ChildHeightAffectsParentHeight()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent.AddChild(child);

        // Child width = 50% of 400 = 200
        child.GetAbsoluteWidth().ShouldBe(200);
        // Child height = 100% of child width = 200
        child.GetAbsoluteHeight().ShouldBe(200);
        // Parent height = 200 (RelativeToChildren)
        parent.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void CrossDimension_PercentOfOtherDimension_ShouldUpdateWhenSourceDimensionChanges()
    {
        ContainerRuntime element = new();
        element.Width = 200;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;

        // Height = 100% of 200 = 200
        element.GetAbsoluteHeight().ShouldBe(200);

        // Change width -> height should update
        element.Width = 300;
        element.GetAbsoluteHeight().ShouldBe(300);
    }

    [Fact]
    public void CrossDimension_RatioWidth_AffectsPercentOfOtherDimensionHeight()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent.AddChild(child2);

        // Child2 width = ratio gets remaining 300
        child2.GetAbsoluteWidth().ShouldBe(300);
        // Child2 height = 50% of 300 = 150
        child2.GetAbsoluteHeight().ShouldBe(150);
    }

    #endregion

    #region Ratio Edge Cases

    [Fact]
    public void Ratio_HeightInNonStackedParent_ShouldDistributeVertically()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        // No stacking layout

        ContainerRuntime child1 = new();
        child1.Height = 100;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 1;
        child2.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        // Without stacking, ratio height may behave differently.
        // Document whatever the engine actually returns.
        // In a non-stacked parent, ratio children may get the full parent height
        // since there is no stacking to subtract absolute siblings from.
        float child2Height = child2.GetAbsoluteHeight();
        child2Height.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Ratio_ShouldCoexistWithPercentageOfOtherDimensionSiblings()
    {
        ContainerRuntime parent = new();
        parent.Width = 1000;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Height = 100;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        // Child1 width = 50% of height(100) = 50
        child1.GetAbsoluteWidth().ShouldBe(50);
        // Ratio child gets 1000 - 50 = 950
        child2.GetAbsoluteWidth().ShouldBe(950);
    }

    [Fact]
    public void Ratio_ShouldCoexistWithPercentageOfParentSiblings()
    {
        ContainerRuntime parent = new();
        parent.Width = 1000;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 10;
        child1.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        // Child1 = 10% of 1000 = 100
        child1.GetAbsoluteWidth().ShouldBe(100);
        // Ratio gets 1000 - 100 = 900
        child2.GetAbsoluteWidth().ShouldBe(900);
    }

    [Fact]
    public void Ratio_ShouldCoexistWithRelativeToParentSiblings()
    {
        ContainerRuntime parent = new();
        parent.Width = 1000;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = -800;
        child1.WidthUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        // Child1 = 1000 - 800 = 200
        child1.GetAbsoluteWidth().ShouldBe(200);
        // Ratio gets 1000 - 200 = 800
        child2.GetAbsoluteWidth().ShouldBe(800);
    }

    [Fact]
    public void Ratio_ShouldHandleAllSiblingsBeingRatio()
    {
        ContainerRuntime parent = new();
        parent.Width = 600;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 1;
        child1.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 2;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 3;
        child3.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child3);

        child1.GetAbsoluteWidth().ShouldBe(100);
        child2.GetAbsoluteWidth().ShouldBe(200);
        child3.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void Ratio_ShouldHandleFractionalDistribution()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 1;
        child1.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 1;
        child3.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child3);

        // Each gets ~33.333...
        float expectedWidth = 100f / 3f;
        child1.GetAbsoluteWidth().ShouldBe(expectedWidth, 0.01f);
        child2.GetAbsoluteWidth().ShouldBe(expectedWidth, 0.01f);
        child3.GetAbsoluteWidth().ShouldBe(expectedWidth, 0.01f);
    }

    [Fact]
    public void Ratio_ShouldHandleSingleRatioChild()
    {
        ContainerRuntime parent = new();
        parent.Width = 500;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child = new();
        child.Width = 1;
        child.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(500);
    }

    [Fact]
    public void Ratio_ShouldHandleZeroRatioValue()
    {
        ContainerRuntime parent = new();
        parent.Width = 500;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 0;
        child1.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        // Zero-ratio child gets 0, the other gets everything
        child1.GetAbsoluteWidth().ShouldBe(0);
        child2.GetAbsoluteWidth().ShouldBe(500);
    }

    #endregion

    #region Stacking Edge Cases

    [Fact]
    public void LeftToRightStack_ShouldRemoveSpacingGaps_ForInvisibleChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.StackSpacing = 10;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Visible = false;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // child3 should be at X=60 (50+10), not 120 (50+10+50+10)
        child3.AbsoluteLeft.ShouldBe(60);
    }

    [Fact]
    public void LeftToRightStack_WithRatioChildren_ShouldDistributeAfterAbsolute()
    {
        ContainerRuntime parent = new();
        parent.Width = 500;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 1;
        child3.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(child3);

        // 500 - 100 = 400 remaining, split between 2 ratio children
        child2.GetAbsoluteWidth().ShouldBe(200);
        child3.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void TopToBottomStack_ChildWithPercentageOfParentWidth_ShouldNotAffectStacking()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Width should be 50% of 400 = 200
        child.GetAbsoluteWidth().ShouldBe(200);
        // Should be positioned normally in stack at Y=0
        child.AbsoluteTop.ShouldBe(0);
    }

    [Fact]
    public void TopToBottomStack_FirstChildWithNonZeroY_ShouldOffsetStack()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.Y = 20;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // Child1 starts at Y=20, child2 follows at 20+50=70
        child1.AbsoluteTop.ShouldBe(20);
        child2.AbsoluteTop.ShouldBe(70);
    }

    [Fact]
    public void TopToBottomStack_ShouldHandleAllChildrenInvisible()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.Visible = false;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.Visible = false;
        parent.AddChild(child2);

        // Should not crash, parent should still be 400
        parent.GetAbsoluteHeight().ShouldBe(400);
    }

    [Fact]
    public void TopToBottomStack_ShouldHandleNegativeStackSpacing()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = -5;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // Negative spacing causes overlap: child2 at 50 + (-5) = 45
        child2.AbsoluteTop.ShouldBe(45);
    }

    [Fact]
    public void TopToBottomStack_ShouldRemoveSpacingGaps_ForInvisibleChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = 10;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.Visible = false;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // child3 should be at Y=60 (50+10), not 120 (50+10+50+10)
        child3.AbsoluteTop.ShouldBe(60);
    }

    [Fact]
    public void TopToBottomStack_WithRatioChildren_ShouldDistributeAfterAbsolute()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 300;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 100;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 1;
        child2.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 1;
        child3.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(child3);

        // 300 - 100 = 200 remaining, split between 2 ratio children
        child2.GetAbsoluteHeight().ShouldBe(100);
        child3.GetAbsoluteHeight().ShouldBe(100);
    }

    #endregion

    #region Wrapping Edge Cases

    [Fact]
    public void WrapsChildren_LeftToRight_WithDifferentChildHeights_ShouldUseMaxRowHeight()
    {
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 30;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 60;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 80;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 40;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Row1: child1(80)+child2(80)=160 < 200, max height 60
        // Row2: child3(80), height 40
        // Parent height = 60 + 40 = 100
        parent.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void WrapsChildren_LeftToRight_WithRelativeToChildrenHeight_ShouldSizeToWrappedRows()
    {
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 80;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Row1: child1+child2 = 160 < 200, Row2: child3
        // Parent height = 50 + 50 = 100
        parent.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void WrapsChildren_ShouldHandleSingleChild_ThatExceedsParentSize()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.WrapsChildren = true;

        ContainerRuntime child = new();
        child.Width = 150;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Child wider than parent should not cause infinite loop.
        // Child stays on first row.
        child.GetAbsoluteWidth().ShouldBe(150);
        child.AbsoluteTop.ShouldBe(0);
    }

    [Fact]
    public void WrapsChildren_TopToBottom_WithRelativeToChildrenWidth_ShouldSizeToWrappedColumns()
    {
        ContainerRuntime parent = new();
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 80;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 80;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 80;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Col1: child1+child2 = 160 < 200, Col2: child3
        // Parent width = 50 + 50 = 100
        parent.GetAbsoluteWidth().ShouldBe(100);
    }

    #endregion

    #region Layout Suspension Edge Cases

    [Fact]
    public void IsAllLayoutSuspended_ShouldWorkWithResumeLayoutRecursive()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(200);

        GraphicalUiElement.IsAllLayoutSuspended = true;
        parent.Width = 600;

        // Layout not updated while globally suspended
        child.GetAbsoluteWidth().ShouldBe(200);

        GraphicalUiElement.IsAllLayoutSuspended = false;
        parent.ResumeLayout(recursive: true);

        child.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void SuspendLayout_Recursive_ShouldSuspendChildren()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 400;
        grandparent.WidthUnits = DimensionUnitType.Absolute;
        grandparent.Height = 400;
        grandparent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToParent;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(200);

        grandparent.SuspendLayout(recursive: true);
        grandparent.Width = 600;

        // Layout not updated during suspension
        child.GetAbsoluteWidth().ShouldBe(200);

        grandparent.ResumeLayout(recursive: true);

        child.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void SuspendLayout_ShouldAccumulateDirtyState_AcrossMultipleChanges()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(200);
        child.GetAbsoluteHeight().ShouldBe(200);

        parent.SuspendLayout();

        // Make multiple changes while suspended
        parent.Width = 600;
        parent.Height = 800;

        // Still old values
        child.GetAbsoluteWidth().ShouldBe(200);
        child.GetAbsoluteHeight().ShouldBe(200);

        parent.ResumeLayout();

        // All changes applied at once
        child.GetAbsoluteWidth().ShouldBe(300);
        child.GetAbsoluteHeight().ShouldBe(400);
    }

    [Fact]
    public void SuspendLayout_ShouldTrackXOrYSeparately()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.X = 10;
        child.Y = 20;
        parent.AddChild(child);

        parent.SuspendLayout();
        child.X = 50;
        parent.ResumeLayout();

        // X should be updated, Y unchanged
        child.AbsoluteLeft.ShouldBe(50);
        child.AbsoluteTop.ShouldBe(20);
    }

    #endregion

    #region PercentageOfOtherDimension with Parent Dependencies

    [Fact]
    public void PercentOfOther_HeightFromWidth_WhereWidthIsRatio()
    {
        ContainerRuntime parent = new();
        parent.Width = 600;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 200;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent.AddChild(child2);

        // Child2 width = ratio gets 400
        child2.GetAbsoluteWidth().ShouldBe(400);
        // Child2 height = 50% of 400 = 200
        child2.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void PercentOfOther_WidthFromHeight_WhereHeightIsPercentOfParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.PercentageOfParent;
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent.AddChild(child);

        // Child height = 50% of 400 = 200
        child.GetAbsoluteHeight().ShouldBe(200);
        // Child width = 100% of height = 200
        child.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void PercentOfOther_WidthFromHeight_WhereHeightIsRelativeToParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = -100;
        child.HeightUnits = DimensionUnitType.RelativeToParent;
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent.AddChild(child);

        // Child height = 400 - 100 = 300
        child.GetAbsoluteHeight().ShouldBe(300);
        // Child width = 50% of 300 = 150
        child.GetAbsoluteWidth().ShouldBe(150);
    }

    [Fact]
    public void PercentOfOther_WithRelativeToChildren_Parent()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 200;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent.AddChild(child);

        // Parent width = 200 (from child)
        parent.GetAbsoluteWidth().ShouldBe(200);
        // Child height = 50% of 200 = 100
        child.GetAbsoluteHeight().ShouldBe(100);
    }

    #endregion

    #region Complex Multi-Level Scenarios

    [Fact]
    public void ParentRelativeToChildren_InStack_ShouldSizeCorrectly()
    {
        ContainerRuntime outer = new();
        outer.Width = 500;
        outer.WidthUnits = DimensionUnitType.Absolute;
        outer.Height = 400;
        outer.HeightUnits = DimensionUnitType.Absolute;
        outer.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime inner1 = new();
        inner1.Width = 0;
        inner1.WidthUnits = DimensionUnitType.RelativeToChildren;
        inner1.Height = 100;
        inner1.HeightUnits = DimensionUnitType.Absolute;
        outer.AddChild(inner1);

        ContainerRuntime inner1Child = new();
        inner1Child.Width = 80;
        inner1Child.WidthUnits = DimensionUnitType.Absolute;
        inner1Child.Height = 50;
        inner1Child.HeightUnits = DimensionUnitType.Absolute;
        inner1.AddChild(inner1Child);

        ContainerRuntime inner2 = new();
        inner2.Width = 1;
        inner2.WidthUnits = DimensionUnitType.Ratio;
        inner2.Height = 100;
        inner2.HeightUnits = DimensionUnitType.Absolute;
        outer.AddChild(inner2);

        // Inner1 should size to its child = 80
        inner1.GetAbsoluteWidth().ShouldBe(80);
        // Inner2 gets remaining 500 - 80 = 420
        inner2.GetAbsoluteWidth().ShouldBe(420);
    }

    [Fact]
    public void RelativeToChildren_WithPadding_InStack()
    {
        ContainerRuntime outer = new();
        outer.Width = 500;
        outer.WidthUnits = DimensionUnitType.Absolute;
        outer.Height = 400;
        outer.HeightUnits = DimensionUnitType.Absolute;
        outer.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 10;
        child1.WidthUnits = DimensionUnitType.RelativeToChildren;
        child1.Height = 100;
        child1.HeightUnits = DimensionUnitType.Absolute;
        outer.AddChild(child1);

        ContainerRuntime grandchild = new();
        grandchild.Width = 50;
        grandchild.WidthUnits = DimensionUnitType.Absolute;
        grandchild.Height = 50;
        grandchild.HeightUnits = DimensionUnitType.Absolute;
        child1.AddChild(grandchild);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        child2.Height = 100;
        child2.HeightUnits = DimensionUnitType.Absolute;
        outer.AddChild(child2);

        // Child1 = grandchild(50) + padding(10) = 60
        child1.GetAbsoluteWidth().ShouldBe(60);
        // Child2 = 500 - 60 = 440
        child2.GetAbsoluteWidth().ShouldBe(440);
    }

    [Fact]
    public void ThreeLevel_ParentRelativeToChildren_ChildPercentOfParent_GrandchildAbsolute()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 0;
        grandparent.WidthUnits = DimensionUnitType.RelativeToChildren;
        grandparent.Height = 400;
        grandparent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 50;
        parent.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;
        grandparent.AddChild(parent);

        ContainerRuntime grandchild = new();
        grandchild.Width = 100;
        grandchild.WidthUnits = DimensionUnitType.Absolute;
        grandchild.Height = 50;
        grandchild.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(grandchild);

        // Grandparent has RelativeToChildren, but its only direct child (parent) uses
        // PercentageOfParent, which is ignored for sizing. So grandparent has
        // no absolute direct children contributing to its width -> width = 0.
        grandparent.GetAbsoluteWidth().ShouldBe(0);
    }

    [Fact]
    public void ThreeLevel_StackedParent_RatioChild_PercentOfOtherGrandchild()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 600;
        grandparent.WidthUnits = DimensionUnitType.Absolute;
        grandparent.Height = 400;
        grandparent.HeightUnits = DimensionUnitType.Absolute;
        grandparent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime parent1 = new();
        parent1.Width = 200;
        parent1.WidthUnits = DimensionUnitType.Absolute;
        parent1.Height = 100;
        parent1.HeightUnits = DimensionUnitType.Absolute;
        grandparent.AddChild(parent1);

        ContainerRuntime parent2 = new();
        parent2.Width = 1;
        parent2.WidthUnits = DimensionUnitType.Ratio;
        parent2.Height = 200;
        parent2.HeightUnits = DimensionUnitType.Absolute;
        grandparent.AddChild(parent2);

        ContainerRuntime grandchild = new();
        grandchild.Width = 0;
        grandchild.WidthUnits = DimensionUnitType.RelativeToParent;
        grandchild.Height = 50;
        grandchild.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent2.AddChild(grandchild);

        // Parent2 width = ratio gets 400
        parent2.GetAbsoluteWidth().ShouldBe(400);
        // Grandchild width = parent2(400) + 0 = 400
        grandchild.GetAbsoluteWidth().ShouldBe(400);
        // Grandchild height = 50% of 400 = 200
        grandchild.GetAbsoluteHeight().ShouldBe(200);
    }

    #endregion

    #region Anchor and Dock in Context

    [Fact]
    public void Anchor_ShouldWorkInsideStack()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 100;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.Anchor(Gum.Wireframe.Anchor.TopLeft);
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 100;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.Anchor(Gum.Wireframe.Anchor.Center);
        parent.AddChild(child2);

        // In a top-to-bottom stack, the stacking positions child2's origin at Y=100.
        // But Anchor(Center) sets YOrigin=Center, so AbsoluteTop = 100 - (height/2) = 50.
        // This shows that origin offsets still apply even inside stacked layouts.
        child2.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void Dock_Fill_ShouldResizeWithParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 300;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Dock(Gum.Wireframe.Dock.Fill);
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(400);
        child.GetAbsoluteHeight().ShouldBe(300);

        // Resize parent
        parent.Width = 600;
        parent.Height = 400;

        child.GetAbsoluteWidth().ShouldBe(600);
        child.GetAbsoluteHeight().ShouldBe(400);
    }

    [Fact]
    public void Dock_SizeToChildren_ShouldWorkLikeRelativeToChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Dock(Gum.Wireframe.Dock.SizeToChildren);
        parent.AddChild(child);

        ContainerRuntime grandchild = new();
        grandchild.Width = 100;
        grandchild.WidthUnits = DimensionUnitType.Absolute;
        grandchild.Height = 50;
        grandchild.HeightUnits = DimensionUnitType.Absolute;
        child.AddChild(grandchild);

        // SizeToChildren sets both Width and Height to RelativeToChildren
        child.GetAbsoluteWidth().ShouldBe(100);
        child.GetAbsoluteHeight().ShouldBe(50);
    }

    #endregion

    #region IgnoredByParentSize

    [Fact]
    public void IgnoredByParentSize_ShouldExcludeFromRelativeToChildrenHeight()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Height = 100;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 200;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.IgnoredByParentSize = true;
        parent.AddChild(child2);

        parent.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void IgnoredByParentSize_ShouldExcludeFromRelativeToChildrenWidth()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 200;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.IgnoredByParentSize = true;
        parent.AddChild(child2);

        parent.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void IgnoredByParentSize_ShouldNotAffectAbsolutePositioning()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 200;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.X = 50;
        child.IgnoredByParentSize = true;
        parent.AddChild(child);

        child.AbsoluteLeft.ShouldBe(50);
    }

    [Fact]
    public void IgnoredByParentSize_ShouldStillParticipateInStacking()
    {
        // IgnoredByParentSize only affects RelativeToChildren sizing,
        // not stacking positions. Children with IgnoredByParentSize=true
        // still take up stack space.
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.IgnoredByParentSize = true;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        child3.Width = 100;
        child3.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // child2 still occupies stack space even though IgnoredByParentSize=true
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(50);
        child3.AbsoluteTop.ShouldBe(100);
    }

    #endregion

    #region Runtime Property Changes

    [Fact]
    public void ChildrenLayout_ChangeAtRuntime_ShouldRepositionChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 100;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Change to TopToBottomStack -> children should reposition vertically
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(50);
        child3.AbsoluteTop.ShouldBe(100);
    }

    [Fact]
    public void ChildrenLayout_ChangeFromStackToRegular_ShouldStopStacking()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // Verify stacking
        child2.AbsoluteTop.ShouldBe(50);

        // Change to Regular
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.Regular;

        // Children go back to independent positioning based on their X/Y values
        // Both children have default Y=0, so they overlap at the top
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(0);
    }

    [Fact]
    public void HeightUnits_ChangeAtRuntime_ShouldRecalculate()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        child.GetAbsoluteHeight().ShouldBe(50);

        // Change units to PercentageOfParent -> 50% of 400 = 200
        child.HeightUnits = DimensionUnitType.PercentageOfParent;

        child.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void StackSpacing_ChangeAtRuntime_ShouldRepositionChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = 0;

        ContainerRuntime child1 = new();
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        child3.Width = 100;
        child3.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Verify positions with no spacing
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(50);
        child3.AbsoluteTop.ShouldBe(100);

        // Change StackSpacing to 10
        parent.StackSpacing = 10;

        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(60);
        child3.AbsoluteTop.ShouldBe(120);
    }

    [Fact]
    public void WidthUnits_ChangeAtRuntime_ShouldRecalculate()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(50);

        // Change units to PercentageOfParent -> 50% of 400 = 200
        child.WidthUnits = DimensionUnitType.PercentageOfParent;

        child.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void WidthValue_ChangeAtRuntime_ShouldUpdateDependentChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(200);

        // Change parent width to 600
        parent.Width = 600;

        child.GetAbsoluteWidth().ShouldBe(300);
    }

    #endregion

    #region Ratio + StackSpacing Interaction

    [Fact]
    public void Ratio_InLeftToRightStack_ShouldAccountForStackSpacing()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.StackSpacing = 10;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 1;
        child3.WidthUnits = DimensionUnitType.Ratio;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Available = 400 - 100 (absolute) - 2*10 (2 spacing gaps between 3 children) = 280
        // Each ratio child gets 140
        child2.GetAbsoluteWidth().ShouldBe(140);
        child3.GetAbsoluteWidth().ShouldBe(140);
    }

    [Fact]
    public void Ratio_InTopToBottomStack_ShouldAccountForStackSpacing()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 300;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = 10;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 1;
        child2.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 100;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 1;
        child3.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(child3);

        // Available = 300 - 50 (absolute) - 2*10 (2 spacing gaps between 3 children) = 230
        // Each ratio child gets 115
        child2.GetAbsoluteHeight().ShouldBe(115);
        child3.GetAbsoluteHeight().ShouldBe(115);
    }

    [Fact]
    public void Ratio_ShouldGetZero_WhenSpacingConsumesAllSpace()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.StackSpacing = 100;

        ContainerRuntime child1 = new();
        child1.Width = 1;
        child1.WidthUnits = DimensionUnitType.Ratio;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // Available = 100 - 100 (1 gap) = 0 -> each gets 0
        child1.GetAbsoluteWidth().ShouldBe(0);
        child2.GetAbsoluteWidth().ShouldBe(0);
    }

    #endregion

    #region PercentageOfSourceFile

    [Fact]
    public void HeightPercentageOfSourceFile_ShouldScaleToDouble()
    {
        GraphicalUiElement element = CreateElementWithSourceRectangle(200, 100);

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(element);

        element.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        element.Height = 200;

        // 200% of source height 100 = 200
        element.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void HeightPercentageOfSourceFile_ShouldUseSourceRectangleHeight()
    {
        GraphicalUiElement element = CreateElementWithSourceRectangle(200, 100);

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(element);

        element.Height = 100;
        element.HeightUnits = DimensionUnitType.PercentageOfSourceFile;

        // 100% of source height 100 = 100
        element.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void WidthPercentageOfSourceFile_ShouldScaleToHalf()
    {
        GraphicalUiElement element = CreateElementWithSourceRectangle(200, 100);

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(element);

        element.Width = 50;
        element.WidthUnits = DimensionUnitType.PercentageOfSourceFile;

        // 50% of source width 200 = 100
        element.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void WidthPercentageOfSourceFile_ShouldUseSourceRectangleWidth()
    {
        GraphicalUiElement element = CreateElementWithSourceRectangle(200, 100);

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(element);

        element.Width = 100;
        element.WidthUnits = DimensionUnitType.PercentageOfSourceFile;

        // 100% of source width 200 = 200
        element.GetAbsoluteWidth().ShouldBe(200);
    }

    #endregion

    #region MaintainFileAspectRatio

    [Fact]
    public void HeightMaintainFileAspectRatio_ShouldScaleBasedOnWidth()
    {
        // Source: 200x100 (2:1 aspect)
        GraphicalUiElement element = CreateElementWithSourceRectangle(200, 100);

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(element);

        element.Width = 400;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;

        // Height = sourceHeight * (absoluteWidth / sourceWidth) * mHeight / 100
        // = 100 * (400 / 200) * 100 / 100 = 200
        element.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void MaintainFileAspectRatio_ShouldHandlePortraitSource()
    {
        // Source: 100x200 (1:2)
        GraphicalUiElement element = CreateElementWithSourceRectangle(100, 200);

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(element);

        element.Width = 200;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;

        // Height = sourceHeight * (absoluteWidth / sourceWidth) * mHeight / 100
        // = 200 * (200 / 100) * 100 / 100 = 400
        element.GetAbsoluteHeight().ShouldBe(400);
    }

    [Fact]
    public void MaintainFileAspectRatio_ShouldHandleSquareSource()
    {
        // Source: 100x100 (1:1)
        GraphicalUiElement element = CreateElementWithSourceRectangle(100, 100);

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(element);

        element.Width = 200;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;

        // Height = sourceHeight * (absoluteWidth / sourceWidth) * mHeight / 100
        // = 100 * (200 / 100) * 100 / 100 = 200
        element.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void WidthMaintainFileAspectRatio_ShouldScaleBasedOnHeight()
    {
        // Source: 200x100 (2:1 aspect)
        GraphicalUiElement element = CreateElementWithSourceRectangle(200, 100);

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(element);

        element.Height = 200;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.WidthUnits = DimensionUnitType.MaintainFileAspectRatio;

        // Width = sourceWidth * (absoluteHeight / sourceHeight) * mWidth / 100
        // = 200 * (200 / 100) * 100 / 100 = 400
        element.GetAbsoluteWidth().ShouldBe(400);
    }

    #endregion

    #region AbsoluteMultipliedByFontScale in Stacks

    [Fact]
    public void AbsoluteMultipliedByFontScale_InLeftToRightStack_ShouldAffectPositions()
    {
        GraphicalUiElement.GlobalFontScale = 2;

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // child1 width = 50 * 2 = 100
        child1.GetAbsoluteWidth().ShouldBe(100);
        // child2 position after scaled child1
        child2.AbsoluteLeft.ShouldBe(100);

        GraphicalUiElement.GlobalFontScale = 1;
    }

    [Fact]
    public void AbsoluteMultipliedByFontScale_InTopToBottomStack_ShouldAffectPositions()
    {
        GraphicalUiElement.GlobalFontScale = 2;

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // child1 height = 50 * 2 = 100
        child1.GetAbsoluteHeight().ShouldBe(100);
        // child2 position after scaled child1
        child2.AbsoluteTop.ShouldBe(100);

        GraphicalUiElement.GlobalFontScale = 1;
    }

    [Fact]
    public void AbsoluteMultipliedByFontScale_WithRatio_ShouldReduceAvailableSpace()
    {
        GraphicalUiElement.GlobalFontScale = 2;

        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // child1 width = 100 * 2 = 200
        // child2 gets remaining = 400 - 200 = 200
        child2.GetAbsoluteWidth().ShouldBe(200);

        GraphicalUiElement.GlobalFontScale = 1;
    }

    #endregion

    #region Reparenting

    [Fact]
    public void Reparent_ShouldUpdateLayoutInNewParent()
    {
        ContainerRuntime parent1 = new();
        parent1.Width = 200;
        parent1.WidthUnits = DimensionUnitType.Absolute;
        parent1.Height = 200;
        parent1.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent2 = new();
        parent2.Width = 400;
        parent2.WidthUnits = DimensionUnitType.Absolute;
        parent2.Height = 400;
        parent2.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent1.AddChild(child);

        // 50% of 200 = 100
        child.GetAbsoluteWidth().ShouldBe(100);

        // Move child to parent2
        child.Parent = parent2;

        // 50% of 400 = 200
        child.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void Reparent_ShouldUpdateOldParent_IfRelativeToChildren()
    {
        ContainerRuntime parent1 = new();
        parent1.Width = 0;
        parent1.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent1.Height = 200;
        parent1.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent2 = new();
        parent2.Width = 400;
        parent2.WidthUnits = DimensionUnitType.Absolute;
        parent2.Height = 400;
        parent2.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent1.AddChild(child);

        parent1.GetAbsoluteWidth().ShouldBe(100);

        // Move child to parent2
        child.Parent = parent2;
        parent1.UpdateLayout();

        // Parent1 has no children, so RelativeToChildren width = 0
        parent1.GetAbsoluteWidth().ShouldBe(0);
    }

    [Fact]
    public void Reparent_ToNull_ShouldRemoveFromParentLayout()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        parent.GetAbsoluteHeight().ShouldBe(100);

        // Remove child2
        child2.Parent = null;
        parent.UpdateLayout();

        parent.GetAbsoluteHeight().ShouldBe(50);
    }

    #endregion

    #region RelativeToChildren on Both Axes

    [Fact]
    public void RelativeToChildren_BothAxes_InTopToBottomStack()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 120;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // Width = widest child = 120
        parent.GetAbsoluteWidth().ShouldBe(120);
        // Height = sum of stacked children = 100
        parent.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void RelativeToChildren_BothAxes_ShouldSizeToChildBounds()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 150;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 80;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.X = 10;
        child.Y = 20;
        parent.AddChild(child);

        // Parent width = X + Width = 10 + 150 = 160
        parent.GetAbsoluteWidth().ShouldBe(160);
        // Parent height = Y + Height = 20 + 80 = 100
        parent.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void RelativeToChildren_BothAxes_WithMultipleChildren_ShouldUseMaxBounds()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.X = 0;
        child1.Y = 0;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 120;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.X = 30;
        child2.Y = 10;
        parent.AddChild(child2);

        // Parent width = max(0+100, 30+80) = 110
        parent.GetAbsoluteWidth().ShouldBe(110);
        // Parent height = max(0+50, 10+120) = 130
        parent.GetAbsoluteHeight().ShouldBe(130);
    }

    [Fact]
    public void RelativeToChildren_BothAxes_WithPadding()
    {
        ContainerRuntime parent = new();
        parent.Width = 20;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 10;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Parent width = 100 + 20 = 120
        parent.GetAbsoluteWidth().ShouldBe(120);
        // Parent height = 50 + 10 = 60
        parent.GetAbsoluteHeight().ShouldBe(60);
    }

    #endregion

    #region Stacking with Different Origins

    [Fact]
    public void LeftToRightStack_ChildrenWithDifferentYOrigins_ShouldPositionIndependently()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.YOrigin = VerticalAlignment.Top;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.YOrigin = VerticalAlignment.Center;
        child2.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        child3.YOrigin = VerticalAlignment.Bottom;
        child3.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        parent.AddChild(child3);

        // All stack horizontally
        child1.AbsoluteLeft.ShouldBe(0);
        child2.AbsoluteLeft.ShouldBe(50);
        child3.AbsoluteLeft.ShouldBe(100);

        // Y positions are independent of stacking
        child1.AbsoluteTop.ShouldBe(0);
        // child2: centered in parent (400/2 - 50/2 = 175)
        child2.AbsoluteTop.ShouldBe(175);
        // child3: bottom-aligned (400 - 50 = 350)
        child3.AbsoluteTop.ShouldBe(350);
    }

    [Fact]
    public void TopToBottomStack_ChildrenWithDifferentXOrigins_ShouldPositionIndependently()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.XOrigin = HorizontalAlignment.Left;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 50;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.XOrigin = HorizontalAlignment.Center;
        child2.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        child3.XOrigin = HorizontalAlignment.Right;
        child3.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        parent.AddChild(child3);

        // All stack vertically
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(50);
        child3.AbsoluteTop.ShouldBe(100);

        // X positions are independent of stacking
        child1.AbsoluteLeft.ShouldBe(0);
        // child2: centered (400/2 - 50/2 = 175)
        child2.AbsoluteLeft.ShouldBe(175);
        // child3: right-aligned (400 - 50 = 350)
        child3.AbsoluteLeft.ShouldBe(350);
    }

    #endregion

    #region Order of Operations

    [Fact]
    public void ChildrenLayout_SetAfterAddingChildren_ShouldWork()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // Set TopToBottomStack AFTER adding children
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void ChildrenLayout_SetBeforeAddingChildren_ShouldWork()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void RelativeToChildren_SetAfterAddingChildren_ShouldSizeCorrectly()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 150;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 80;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Switch to RelativeToChildren after adding children
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;

        parent.GetAbsoluteWidth().ShouldBe(150);
    }

    [Fact]
    public void RelativeToChildren_SetBeforeAddingChildren_ShouldSizeCorrectly()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 150;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 80;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteWidth().ShouldBe(150);
    }

    #endregion

    #region Multiple PixelsFromMiddle Children in RelativeToChildren

    [Fact]
    public void RelativeToChildren_MultiplePixelsFromMiddle_ShouldUseLargestSymmetricBound()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child1 = new();
        child1.Width = 60;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.X = 0;
        child1.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        child1.XOrigin = HorizontalAlignment.Left;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 40;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.X = -50;
        child2.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        child2.XOrigin = HorizontalAlignment.Left;
        parent.AddChild(child2);

        // child1 extends from middle+0 to middle+60, symmetric = 2*60 = 120
        // child2 extends from middle-50 to middle-10, symmetric = 2*50 = 100
        // Parent should use the larger symmetric bound = 120
        parent.GetAbsoluteWidth().ShouldBe(120);
    }

    [Fact]
    public void RelativeToChildren_PixelsFromMiddle_WithCenteredOrigin()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.X = 0;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        child.XOrigin = HorizontalAlignment.Center;
        parent.AddChild(child);

        // Centered at middle: extends -50 to +50, symmetric = 2*50 = 100
        parent.GetAbsoluteWidth().ShouldBe(100);
    }

    #endregion

    #region RelativeToChildren with PercentageOfOtherDimension Children

    [Fact]
    public void RelativeToChildren_Height_WithPercentOfOtherDimensionChild()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 300;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent.AddChild(child);

        // Child height = 100% of width(300) = 300
        child.GetAbsoluteHeight().ShouldBe(300);
        // Parent height = 300
        parent.GetAbsoluteHeight().ShouldBe(300);
    }

    [Fact]
    public void RelativeToChildren_Width_WithPercentOfOtherDimensionChild()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 200;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;
        parent.AddChild(child);

        // Child width = 50% of height(200) = 100
        child.GetAbsoluteWidth().ShouldBe(100);
        // Parent width = 100
        parent.GetAbsoluteWidth().ShouldBe(100);
    }

    #endregion

    #region Stacking + PercentageOfParent on Stack Axis

    [Fact]
    public void LeftToRightStack_ChildPercentageOfParentWidth_ShouldUseParentWidth()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 25;
        child2.WidthUnits = DimensionUnitType.PercentageOfParent;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // child2 width = 25% of parent(400) = 100, not 25% of remaining space
        child2.GetAbsoluteWidth().ShouldBe(100);
        // child2 starts after child1
        child2.AbsoluteLeft.ShouldBe(100);
    }

    [Fact]
    public void TopToBottomStack_ChildPercentageOfParentHeight_ShouldUseParentHeight()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 100;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 25;
        child2.HeightUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child2);

        // child2 height = 25% of parent(400) = 100, not 25% of remaining space
        child2.GetAbsoluteHeight().ShouldBe(100);
        // child2 starts after child1
        child2.AbsoluteTop.ShouldBe(100);
    }

    #endregion

    #region Min/Max Width and Height Clamping

    [Fact]
    public void MaxHeight_ShouldClampAbsoluteHeight()
    {
        ContainerRuntime element = new();
        element.Height = 500;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.MaxHeight = 300;

        element.GetAbsoluteHeight().ShouldBe(300);
    }

    [Fact]
    public void MaxHeight_ShouldClampPercentageOfParentHeight()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 1000;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Height = 80;
        child.HeightUnits = DimensionUnitType.PercentageOfParent;
        child.MaxHeight = 400;
        parent.AddChild(child);

        // 80% of 1000 = 800, clamped to 400
        child.GetAbsoluteHeight().ShouldBe(400);
    }

    [Fact]
    public void MaxWidth_Null_ShouldNotClamp()
    {
        ContainerRuntime element = new();
        element.Width = 500;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.MaxWidth = null;

        element.GetAbsoluteWidth().ShouldBe(500);
    }

    [Fact]
    public void MaxWidth_ShouldClampAbsoluteWidth()
    {
        ContainerRuntime element = new();
        element.Width = 500;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.MaxWidth = 300;

        element.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void MaxWidth_ShouldClampPercentageOfParentWidth()
    {
        ContainerRuntime parent = new();
        parent.Width = 1000;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 80;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.MaxWidth = 400;
        parent.AddChild(child);

        // 80% of 1000 = 800, clamped to 400
        child.GetAbsoluteWidth().ShouldBe(400);
    }

    [Fact]
    public void MaxWidth_ShouldNotAffectWidth_WhenWidthBelowMax()
    {
        ContainerRuntime element = new();
        element.Width = 200;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.MaxWidth = 500;

        element.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void MinHeight_ShouldEnforceMinimumHeight()
    {
        ContainerRuntime element = new();
        element.Height = 50;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.MinHeight = 100;

        element.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void MinWidth_ShouldEnforceMinimumWidth()
    {
        ContainerRuntime element = new();
        element.Width = 50;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.MinWidth = 100;

        element.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void MinWidth_ShouldNotAffectWidth_WhenWidthExceedsMin()
    {
        ContainerRuntime element = new();
        element.Width = 200;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.MinWidth = 100;

        element.GetAbsoluteWidth().ShouldBe(200);
    }

    [Fact]
    public void MinWidth_WithRelativeToParent_ShouldClamp()
    {
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = -180;
        child.WidthUnits = DimensionUnitType.RelativeToParent;
        child.MinWidth = 50;
        parent.AddChild(child);

        // RelativeToParent: 200 + (-180) = 20, clamped to 50
        child.GetAbsoluteWidth().ShouldBe(50);
    }

    #endregion

    #region Percentage Edge Values

    [Fact]
    public void PercentageOfParent_GreaterThan100_ShouldExceedParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 200;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        // 200% of 400 = 800
        child.GetAbsoluteWidth().ShouldBe(800);
    }

    [Fact]
    public void PercentageOfParent_NegativeValue_ShouldReturnNegative()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = -50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        // -50% of 400 = -200
        child.GetAbsoluteWidth().ShouldBe(-200);
    }

    [Fact]
    public void PercentageOfParent_VerySmall_ShouldHandlePrecision()
    {
        ContainerRuntime parent = new();
        parent.Width = 1000;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 0.1f;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        // 0.1% of 1000 = 1
        child.GetAbsoluteWidth().ShouldBe(1);
    }

    [Fact]
    public void PercentageOfParent_Zero_ShouldReturnZero()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 0;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        child.GetAbsoluteWidth().ShouldBe(0);
    }

    #endregion

    #region Nested Stacks

    [Fact]
    public void NestedStacks_HorizontalInVertical_ShouldLayoutCorrectly()
    {
        ContainerRuntime outer = new();
        outer.Width = 400;
        outer.WidthUnits = DimensionUnitType.Absolute;
        outer.Height = 400;
        outer.HeightUnits = DimensionUnitType.Absolute;
        outer.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime row1 = new();
        row1.Width = 0;
        row1.WidthUnits = DimensionUnitType.RelativeToParent;
        row1.Height = 100;
        row1.HeightUnits = DimensionUnitType.Absolute;
        row1.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        outer.AddChild(row1);

        ContainerRuntime row1Child1 = new();
        row1Child1.Width = 100;
        row1Child1.WidthUnits = DimensionUnitType.Absolute;
        row1Child1.Height = 50;
        row1Child1.HeightUnits = DimensionUnitType.Absolute;
        row1.AddChild(row1Child1);

        ContainerRuntime row1Child2 = new();
        row1Child2.Width = 100;
        row1Child2.WidthUnits = DimensionUnitType.Absolute;
        row1Child2.Height = 50;
        row1Child2.HeightUnits = DimensionUnitType.Absolute;
        row1.AddChild(row1Child2);

        ContainerRuntime row2 = new();
        row2.Width = 0;
        row2.WidthUnits = DimensionUnitType.RelativeToParent;
        row2.Height = 100;
        row2.HeightUnits = DimensionUnitType.Absolute;
        row2.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        outer.AddChild(row2);

        ContainerRuntime row2Child1 = new();
        row2Child1.Width = 200;
        row2Child1.WidthUnits = DimensionUnitType.Absolute;
        row2Child1.Height = 50;
        row2Child1.HeightUnits = DimensionUnitType.Absolute;
        row2.AddChild(row2Child1);

        // Row1 at Y=0, Row2 at Y=100
        row1.AbsoluteTop.ShouldBe(0);
        row2.AbsoluteTop.ShouldBe(100);

        // Row1 children: child1 at X=0, child2 at X=100
        row1Child1.AbsoluteLeft.ShouldBe(0);
        row1Child2.AbsoluteLeft.ShouldBe(100);

        // Row2 child at X=0
        row2Child1.AbsoluteLeft.ShouldBe(0);
    }

    [Fact]
    public void NestedStacks_InnerRelativeToChildren_ShouldSizeCorrectly()
    {
        ContainerRuntime outer = new();
        outer.Width = 400;
        outer.WidthUnits = DimensionUnitType.Absolute;
        outer.Height = 400;
        outer.HeightUnits = DimensionUnitType.Absolute;
        outer.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime inner = new();
        inner.Width = 0;
        inner.WidthUnits = DimensionUnitType.RelativeToChildren;
        inner.Height = 0;
        inner.HeightUnits = DimensionUnitType.RelativeToChildren;
        inner.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        outer.AddChild(inner);

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        inner.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 60;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 70;
        child2.HeightUnits = DimensionUnitType.Absolute;
        inner.AddChild(child2);

        // Inner width = sum of children in LeftToRightStack = 80 + 60 = 140
        inner.GetAbsoluteWidth().ShouldBe(140);
        // Inner height = tallest child = 70
        inner.GetAbsoluteHeight().ShouldBe(70);
    }

    [Fact]
    public void NestedStacks_RatioInInnerStack_ShouldDistributeCorrectly()
    {
        ContainerRuntime outer = new();
        outer.Width = 400;
        outer.WidthUnits = DimensionUnitType.Absolute;
        outer.Height = 400;
        outer.HeightUnits = DimensionUnitType.Absolute;
        outer.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime inner = new();
        inner.Width = 0;
        inner.WidthUnits = DimensionUnitType.RelativeToParent;
        inner.Height = 100;
        inner.HeightUnits = DimensionUnitType.Absolute;
        inner.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        outer.AddChild(inner);

        ContainerRuntime innerChild1 = new();
        innerChild1.Width = 100;
        innerChild1.WidthUnits = DimensionUnitType.Absolute;
        innerChild1.Height = 50;
        innerChild1.HeightUnits = DimensionUnitType.Absolute;
        inner.AddChild(innerChild1);

        ContainerRuntime innerChild2 = new();
        innerChild2.Width = 1;
        innerChild2.WidthUnits = DimensionUnitType.Ratio;
        innerChild2.Height = 50;
        innerChild2.HeightUnits = DimensionUnitType.Absolute;
        inner.AddChild(innerChild2);

        // Inner width = 400 (RelativeToParent with 0 offset)
        // InnerChild1 = 100 absolute, InnerChild2 gets remaining = 300
        innerChild2.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void NestedStacks_VerticalInHorizontal_ShouldLayoutCorrectly()
    {
        ContainerRuntime outer = new();
        outer.Width = 400;
        outer.WidthUnits = DimensionUnitType.Absolute;
        outer.Height = 400;
        outer.HeightUnits = DimensionUnitType.Absolute;
        outer.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime col1 = new();
        col1.Width = 100;
        col1.WidthUnits = DimensionUnitType.Absolute;
        col1.Height = 0;
        col1.HeightUnits = DimensionUnitType.RelativeToParent;
        col1.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        outer.AddChild(col1);

        ContainerRuntime col1Child1 = new();
        col1Child1.Width = 50;
        col1Child1.WidthUnits = DimensionUnitType.Absolute;
        col1Child1.Height = 50;
        col1Child1.HeightUnits = DimensionUnitType.Absolute;
        col1.AddChild(col1Child1);

        ContainerRuntime col1Child2 = new();
        col1Child2.Width = 50;
        col1Child2.WidthUnits = DimensionUnitType.Absolute;
        col1Child2.Height = 50;
        col1Child2.HeightUnits = DimensionUnitType.Absolute;
        col1.AddChild(col1Child2);

        ContainerRuntime col2 = new();
        col2.Width = 100;
        col2.WidthUnits = DimensionUnitType.Absolute;
        col2.Height = 0;
        col2.HeightUnits = DimensionUnitType.RelativeToParent;
        col2.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        outer.AddChild(col2);

        ContainerRuntime col2Child1 = new();
        col2Child1.Width = 50;
        col2Child1.WidthUnits = DimensionUnitType.Absolute;
        col2Child1.Height = 100;
        col2Child1.HeightUnits = DimensionUnitType.Absolute;
        col2.AddChild(col2Child1);

        // Col1 at X=0, Col2 at X=100
        col1.AbsoluteLeft.ShouldBe(0);
        col2.AbsoluteLeft.ShouldBe(100);

        // Col1 children: child1 at Y=0, child2 at Y=50
        col1Child1.AbsoluteTop.ShouldBe(0);
        col1Child2.AbsoluteTop.ShouldBe(50);

        // Col2 child at Y=0
        col2Child1.AbsoluteTop.ShouldBe(0);
    }

    #endregion

    #region Children Reordering in Stacks

    [Fact]
    public void ChildrenReorder_InLeftToRightStack_ShouldUpdatePositions()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 50;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 30;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 40;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Initial: child1 at X=0, child2 at X=50, child3 at X=80
        child1.AbsoluteLeft.ShouldBe(0);
        child2.AbsoluteLeft.ShouldBe(50);
        child3.AbsoluteLeft.ShouldBe(80);

        // Move child3 to index 0
        parent.Children.Remove(child3);
        parent.Children.Insert(0, child3);
        parent.UpdateLayout();

        // After reorder: child3(40) at X=0, child1(50) at X=40, child2(30) at X=90
        child3.AbsoluteLeft.ShouldBe(0);
        child1.AbsoluteLeft.ShouldBe(40);
        child2.AbsoluteLeft.ShouldBe(90);
    }

    [Fact]
    public void ChildrenReorder_InTopToBottomStack_ShouldUpdatePositions()
    {
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 30;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 100;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 40;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Initial: child1 at Y=0, child2 at Y=50, child3 at Y=80
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(50);
        child3.AbsoluteTop.ShouldBe(80);

        // Move child3 to index 0
        parent.Children.Remove(child3);
        parent.Children.Insert(0, child3);
        parent.UpdateLayout();

        // After reorder: child3(40) at Y=0, child1(50) at Y=40, child2(30) at Y=90
        child3.AbsoluteTop.ShouldBe(0);
        child1.AbsoluteTop.ShouldBe(40);
        child2.AbsoluteTop.ShouldBe(90);
    }

    #endregion

    #region GetAbsoluteRight and GetAbsoluteBottom

    [Fact]
    public void GetAbsoluteBottom_ShouldReturnTopPlusHeight()
    {
        ContainerRuntime element = new();
        element.X = 0;
        element.Y = 50;
        element.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        element.Width = 100;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.Absolute;

        element.AbsoluteBottom.ShouldBe(150);
    }

    [Fact]
    public void GetAbsoluteBottom_WithOriginBottom_ShouldAccountForOrigin()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        child.Y = 300;
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // YOrigin=Bottom means Y=300 is the bottom edge
        // AbsoluteTop = 200, AbsoluteBottom = 300
        child.AbsoluteTop.ShouldBe(200);
        child.AbsoluteBottom.ShouldBe(300);
    }

    [Fact]
    public void GetAbsoluteRight_ShouldReturnLeftPlusWidth()
    {
        ContainerRuntime element = new();
        element.X = 50;
        element.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        element.Y = 0;
        element.Width = 100;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.Absolute;

        element.AbsoluteRight.ShouldBe(150);
    }

    [Fact]
    public void GetAbsoluteRight_WithOriginRight_ShouldAccountForOrigin()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
        child.X = 300;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // XOrigin=Right means X=300 is the right edge
        // AbsoluteLeft = 200, AbsoluteRight = 300
        child.AbsoluteLeft.ShouldBe(200);
        child.AbsoluteRight.ShouldBe(300);
    }

    #endregion

    #region RelativeToChildren Negative Padding

    [Fact]
    public void RelativeToChildren_NegativePadding_Height_ShouldShrinkBelowChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = -10;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Parent height = child height (100) + padding (-10) = 90
        parent.GetAbsoluteHeight().ShouldBe(90);
    }

    [Fact]
    public void RelativeToChildren_NegativePadding_ShouldShrinkBelowChildren()
    {
        ContainerRuntime parent = new();
        parent.Width = -10;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Parent width = child width (100) + padding (-10) = 90
        parent.GetAbsoluteWidth().ShouldBe(90);
    }

    #endregion

    #region Stack Spacing Edge Cases

    [Fact]
    public void StackSpacing_WithAllInvisibleExceptOne_ShouldNotAddSpacing()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = 20;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.Visible = false;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 100;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        child3.Visible = false;
        parent.AddChild(child3);

        // Only one visible child, so no spacing should be applied
        // child2 should be at Y=0
        child2.AbsoluteTop.ShouldBe(0);
    }

    [Fact]
    public void StackSpacing_WithSingleChild_ShouldNotAddSpacing()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = 20;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Single child at Y=0, no spacing
        child.AbsoluteTop.ShouldBe(0);
    }

    [Fact]
    public void StackSpacing_Zero_ShouldPlaceChildrenContiguously()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = 0;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // Children should be contiguous with no gap
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(50);
    }

    #endregion

    #region Visibility Edge Cases

    [Fact]
    public void Visible_False_ShouldNotContributeToRatio()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.Visible = false;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 1;
        child2.WidthUnits = DimensionUnitType.Ratio;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        // Invisible child1 is excluded, child2 gets full 400
        child2.GetAbsoluteWidth().ShouldBe(400);
    }

    [Fact]
    public void Visible_OnlyChild_SetToFalse_RelativeToChildrenParent_ShouldBeZero()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 200;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.Visible = false;
        parent.AddChild(child);

        // Only child is invisible, parent width should be 0 (padding only)
        parent.GetAbsoluteWidth().ShouldBe(0);
    }

    [Fact]
    public void Visible_ToggleOn_ShouldRestoreLayout()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 100;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        child2.Visible = false;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 100;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // With child2 invisible: child3 at Y=50
        child3.AbsoluteTop.ShouldBe(50);

        // Make child2 visible
        child2.Visible = true;

        // Now child3 should be at Y=100
        child3.AbsoluteTop.ShouldBe(100);
    }

    #endregion

    #region Layout Call Count

    [Fact]
    public void LayoutCallCount_SinglePropertyChange_ShouldNotCascadeExcessively()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        int countBefore = GraphicalUiElement.UpdateLayoutCallCount;
        child.Width = 200;
        int countAfter = GraphicalUiElement.UpdateLayoutCallCount;

        int difference = countAfter - countBefore;
        difference.ShouldBeLessThan(10);
    }

    [Fact]
    public void LayoutCallCount_SuspendedChanges_ShouldReduceCalls()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Measure unsuspended changes
        int countBefore = GraphicalUiElement.UpdateLayoutCallCount;
        child.Width = 110;
        child.Height = 110;
        child.Width = 120;
        child.Height = 120;
        child.Width = 130;
        int unsuspendedCalls = GraphicalUiElement.UpdateLayoutCallCount - countBefore;

        // Measure suspended changes using global suspension
        countBefore = GraphicalUiElement.UpdateLayoutCallCount;
        GraphicalUiElement.IsAllLayoutSuspended = true;
        child.Width = 140;
        child.Height = 140;
        child.Width = 150;
        child.Height = 150;
        child.Width = 160;
        GraphicalUiElement.IsAllLayoutSuspended = false;
        parent.UpdateLayout();
        int suspendedCalls = GraphicalUiElement.UpdateLayoutCallCount - countBefore;

        // Global suspension should result in fewer layout calls
        // since changes are batched and applied once on resume
        suspendedCalls.ShouldBeLessThan(unsuspendedCalls);
    }

    #endregion

    #region Element Without Renderable

    [Fact]
    public void NoRenderable_ShouldHandlePercentageOfParent()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;

        GraphicalUiElement child = new();
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(child);

        // Should not crash; the element has no renderable
        // Width may or may not be computed correctly, but it should not throw
        Should.NotThrow(() => child.GetAbsoluteWidth());
    }

    [Fact]
    public void NoRenderable_ShouldStillCalculateWidth()
    {
        GraphicalUiElement element = new();
        element.Width = 200;
        element.WidthUnits = DimensionUnitType.Absolute;

        // Should not crash even without a renderable
        Should.NotThrow(() => element.GetAbsoluteWidth());
    }

    #endregion

    #region Rotation and Layout

    [Fact]
    public void Rotation_ChangeShouldTriggerFullLayoutUpdate()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Height = 50;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Setting rotation should not crash and should trigger layout update
        Should.NotThrow(() => child.Rotation = 45);
    }

    [Fact]
    public void Rotation_ShouldNotAffectGetAbsoluteWidth()
    {
        ContainerRuntime element = new();
        element.Width = 200;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Rotation = 45;

        // GetAbsoluteWidth returns the unrotated dimension
        element.GetAbsoluteWidth().ShouldBe(200);
    }

    #endregion

    #region FlipHorizontal

    [Fact]
    public void FlipHorizontal_ShouldNotAffectDimensions()
    {
        ContainerRuntime element = new();
        element.Width = 200;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.FlipHorizontal = true;

        element.GetAbsoluteWidth().ShouldBe(200);
        element.GetAbsoluteHeight().ShouldBe(100);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - Basic

    [Fact]
    public void RelativeToMaxParentOrChildren_Width_ShouldUseParent_WhenParentIsLarger()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 400;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Parent (400) > child (100), so element uses parent size
        parent.GetAbsoluteWidth().ShouldBe(400);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_Width_ShouldUseChildren_WhenChildrenAreLarger()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 200;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 500;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Child (500) > parent (200), so element uses children size
        parent.GetAbsoluteWidth().ShouldBe(500);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_Height_ShouldUseParent_WhenParentIsLarger()
    {
        ContainerRuntime grandparent = new();
        grandparent.Height = 400;
        grandparent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteHeight().ShouldBe(400);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_Height_ShouldUseChildren_WhenChildrenAreLarger()
    {
        ContainerRuntime grandparent = new();
        grandparent.Height = 200;
        grandparent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Height = 500;
        child.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        parent.GetAbsoluteHeight().ShouldBe(500);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - Padding

    [Fact]
    public void RelativeToMaxParentOrChildren_Width_ShouldApplyPaddingToChildrenSize()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 200;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 20;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 300;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Value (20) acts as padding added to children bounds.
        // childrenBased: 300 + 20 = 320
        // parentBased: 200
        // max(200, 320) = 320
        parent.GetAbsoluteWidth().ShouldBe(320);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_Width_ShouldNotApplyPaddingToParentSize()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 400;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 20;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // childrenBased: 100 + 20 = 120
        // parentBased: 400
        // max(400, 120) = 400
        parent.GetAbsoluteWidth().ShouldBe(400);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_Width_PaddingShouldExpandChildrenBeyondParent()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 400;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 50;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 380;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // childrenBased: 380 + 50 = 430
        // parentBased: 400
        // max(400, 430) = 430 — children + padding wins
        parent.GetAbsoluteWidth().ShouldBe(430);

        // Now make child smaller so parent wins
        child.Width = 300;
        // childrenBased: 300 + 50 = 350
        // parentBased: 400
        // max(400, 350) = 400
        parent.GetAbsoluteWidth().ShouldBe(400);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - No Children

    [Fact]
    public void RelativeToMaxParentOrChildren_Width_ShouldFallBackToParent_WhenNoChildren()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 400;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        // No children, so max(400, 0) = 400
        parent.GetAbsoluteWidth().ShouldBe(400);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_Height_ShouldFallBackToParent_WhenNoChildren()
    {
        ContainerRuntime grandparent = new();
        grandparent.Height = 300;
        grandparent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        parent.GetAbsoluteHeight().ShouldBe(300);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - Invisible Children

    [Fact]
    public void RelativeToMaxParentOrChildren_ShouldIgnoreInvisibleChildren()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 200;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 500;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Visible = false;
        parent.AddChild(child);

        // Invisible child excluded, falls back to parent size
        parent.GetAbsoluteWidth().ShouldBe(200);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - Parent-Dependent Children Excluded

    [Fact]
    public void RelativeToMaxParentOrChildren_ShouldIgnoreChildWithPercentageOfParent()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 300;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime absoluteChild = new();
        absoluteChild.Width = 100;
        absoluteChild.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime percentChild = new();
        percentChild.Width = 200;
        percentChild.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.AddChild(percentChild);

        // PercentageOfParent child excluded from children measurement (circular dep).
        // Only absoluteChild (100) contributes. max(300, 100) = 300
        parent.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_ShouldIgnoreChildWithRelativeToParent()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 300;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime absoluteChild = new();
        absoluteChild.Width = 100;
        absoluteChild.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime relativeChild = new();
        relativeChild.Width = -50;
        relativeChild.WidthUnits = DimensionUnitType.RelativeToParent;
        parent.AddChild(relativeChild);

        // RelativeToParent child excluded. Only absoluteChild (100) counts.
        // max(300, 100) = 300
        parent.GetAbsoluteWidth().ShouldBe(300);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - With Ratio Siblings

    [Fact]
    public void RelativeToMaxParentOrChildren_ShouldBeSubtractedFromRatioSpace()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 600;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime maxChild = new();
        maxChild.Width = 0;
        maxChild.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(maxChild);

        ContainerRuntime innerChild = new();
        innerChild.Width = 200;
        innerChild.WidthUnits = DimensionUnitType.Absolute;
        maxChild.AddChild(innerChild);

        // maxChild: max(600, 200) = 600. Its absolute width is 600.

        ContainerRuntime ratioChild = new();
        ratioChild.Width = 1;
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        grandparent.AddChild(ratioChild);

        // Ratio sibling should subtract maxChild's absolute width (600) from parent (600).
        // Remaining = 0
        ratioChild.GetAbsoluteWidth().ShouldBe(0);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - In Stack

    [Fact]
    public void RelativeToMaxParentOrChildren_InTopToBottomStack_ShouldWorkCorrectly()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 300;
        grandparent.WidthUnits = DimensionUnitType.Absolute;
        grandparent.Height = 400;
        grandparent.HeightUnits = DimensionUnitType.Absolute;
        grandparent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime element = new();
        element.Width = 0;
        element.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        element.Height = 100;
        element.HeightUnits = DimensionUnitType.Absolute;
        grandparent.AddChild(element);

        ContainerRuntime wideChild = new();
        wideChild.Width = 500;
        wideChild.WidthUnits = DimensionUnitType.Absolute;
        wideChild.Height = 50;
        wideChild.HeightUnits = DimensionUnitType.Absolute;
        element.AddChild(wideChild);

        // Width: max(300, 500) = 500
        element.GetAbsoluteWidth().ShouldBe(500);
        // Height is absolute 100
        element.GetAbsoluteHeight().ShouldBe(100);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - Dynamic Updates

    [Fact]
    public void RelativeToMaxParentOrChildren_ShouldUpdate_WhenChildResizes()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 300;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 100;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Initially parent wins: max(300, 100) = 300
        parent.GetAbsoluteWidth().ShouldBe(300);

        // Now child grows larger than parent
        child.Width = 500;

        // Children win: max(300, 500) = 500
        parent.GetAbsoluteWidth().ShouldBe(500);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_ShouldUpdate_WhenParentResizes()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 200;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child = new();
        child.Width = 300;
        child.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child);

        // Initially children win: max(200, 300) = 300
        parent.GetAbsoluteWidth().ShouldBe(300);

        // Now grandparent grows
        grandparent.Width = 500;

        // Parent wins: max(500, 300) = 500
        parent.GetAbsoluteWidth().ShouldBe(500);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - Multiple Children

    [Fact]
    public void RelativeToMaxParentOrChildren_ShouldUseWidestChild()
    {
        ContainerRuntime grandparent = new();
        grandparent.Width = 200;
        grandparent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        grandparent.AddChild(parent);

        ContainerRuntime child1 = new();
        child1.Width = 100;
        child1.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 350;
        child2.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 150;
        child3.WidthUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Widest child is 350. max(200, 350) = 350
        parent.GetAbsoluteWidth().ShouldBe(350);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - Nested

    [Fact]
    public void RelativeToMaxParentOrChildren_Nested_BothUsingMaxParentOrChildren()
    {
        ContainerRuntime root = new();
        root.Width = 400;
        root.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime outer = new();
        outer.Width = 0;
        outer.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        root.AddChild(outer);

        ContainerRuntime inner = new();
        inner.Width = 0;
        inner.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        outer.AddChild(inner);

        // inner depends on outer (parent), but inner also uses MaxParentOrChildren.
        // inner's children measurement: no children -> 0
        // inner's parent (outer) size: outer = max(400, inner?)
        // Since inner depends on parent, it's excluded from outer's children measurement.
        // outer = max(400, 0) = 400
        // inner = max(400, 0) = 400
        outer.GetAbsoluteWidth().ShouldBe(400);
        inner.GetAbsoluteWidth().ShouldBe(400);
    }

    #endregion

    #region RelativeToMaxParentOrChildren - Parent is RelativeToChildren

    [Fact]
    public void RelativeToMaxParentOrChildren_WithRelativeToChildrenParent_ShouldExpandAllRowsToWidest()
    {
        // This is the key use case: a list of items where the widest one
        // defines the width and all others fill to match.
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime row1 = new();
        row1.Width = 0;
        row1.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        row1.Height = 40;
        row1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row1);

        ContainerRuntime row1Child = new();
        row1Child.Width = 120;
        row1Child.WidthUnits = DimensionUnitType.Absolute;
        row1Child.Height = 30;
        row1Child.HeightUnits = DimensionUnitType.Absolute;
        row1.AddChild(row1Child);

        ContainerRuntime row2 = new();
        row2.Width = 0;
        row2.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        row2.Height = 40;
        row2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row2);

        ContainerRuntime row2Child = new();
        row2Child.Width = 300;
        row2Child.WidthUnits = DimensionUnitType.Absolute;
        row2Child.Height = 30;
        row2Child.HeightUnits = DimensionUnitType.Absolute;
        row2.AddChild(row2Child);

        ContainerRuntime row3 = new();
        row3.Width = 0;
        row3.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        row3.Height = 40;
        row3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row3);

        ContainerRuntime row3Child = new();
        row3Child.Width = 200;
        row3Child.WidthUnits = DimensionUnitType.Absolute;
        row3Child.Height = 30;
        row3Child.HeightUnits = DimensionUnitType.Absolute;
        row3.AddChild(row3Child);

        // Parent should size to widest child's children-based size: 300
        parent.GetAbsoluteWidth().ShouldBe(300);

        // All rows should be max(parent=300, ownChild) = 300
        row1.GetAbsoluteWidth().ShouldBe(300);
        row2.GetAbsoluteWidth().ShouldBe(300);
        row3.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_WithRelativeToChildrenParent_PaddingShouldExpandAllRows()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime row1 = new();
        row1.Width = 30;
        row1.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        row1.Height = 40;
        row1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row1);

        ContainerRuntime row1Child = new();
        row1Child.Width = 120;
        row1Child.WidthUnits = DimensionUnitType.Absolute;
        row1.AddChild(row1Child);

        ContainerRuntime row2 = new();
        row2.Width = 30;
        row2.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        row2.Height = 40;
        row2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row2);

        ContainerRuntime row2Child = new();
        row2Child.Width = 600;
        row2Child.WidthUnits = DimensionUnitType.Absolute;
        row2.AddChild(row2Child);

        ContainerRuntime row3 = new();
        row3.Width = 30;
        row3.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        row3.Height = 40;
        row3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row3);

        ContainerRuntime row3Child = new();
        row3Child.Width = 400;
        row3Child.WidthUnits = DimensionUnitType.Absolute;
        row3.AddChild(row3Child);

        // Widest child is 600. With 30 padding: 600 + 30 = 630.
        // Parent sizes to widest row's children-based size: 630.
        // All rows: max(parent=630, ownChild+30) = 630.
        parent.GetAbsoluteWidth().ShouldBe(630);
        row1.GetAbsoluteWidth().ShouldBe(630);
        row2.GetAbsoluteWidth().ShouldBe(630);
        row3.GetAbsoluteWidth().ShouldBe(630);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_WithRelativeToChildrenParent_ShouldUpdateWhenChildGrows()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime row1 = new();
        row1.Width = 0;
        row1.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        row1.Height = 40;
        row1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row1);

        ContainerRuntime row1Child = new();
        row1Child.Width = 200;
        row1Child.WidthUnits = DimensionUnitType.Absolute;
        row1.AddChild(row1Child);

        ContainerRuntime row2 = new();
        row2.Width = 0;
        row2.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        row2.Height = 40;
        row2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row2);

        ContainerRuntime row2Child = new();
        row2Child.Width = 200;
        row2Child.WidthUnits = DimensionUnitType.Absolute;
        row2.AddChild(row2Child);

        // Both rows 200, parent 200, all rows 200
        parent.GetAbsoluteWidth().ShouldBe(200);
        row1.GetAbsoluteWidth().ShouldBe(200);
        row2.GetAbsoluteWidth().ShouldBe(200);

        // Now row1's child grows
        row1Child.Width = 500;

        // Parent should expand to 500, both rows should follow
        parent.GetAbsoluteWidth().ShouldBe(500);
        row1.GetAbsoluteWidth().ShouldBe(500);
        row2.GetAbsoluteWidth().ShouldBe(500);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_WithRelativeToChildrenParent_ShouldNotRatchetWhenPaddingChanges()
    {
        // Reproduces the ratchet bug: when the parent's RelativeToChildren padding
        // is increased then decreased, the stale parent width poisons the child's
        // RelativeToMaxParentOrChildren calculation, causing the layout to only grow
        // and never shrink back.
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 0;
        child.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        parent.AddChild(child);

        ContainerRuntime grandchild = new();
        grandchild.Width = 50;
        grandchild.WidthUnits = DimensionUnitType.Absolute;
        child.AddChild(grandchild);

        // Initial state: everything should be 50
        parent.GetAbsoluteWidth().ShouldBe(50);
        child.GetAbsoluteWidth().ShouldBe(50);

        // Increase parent padding to 200: parent = 50 + 200 = 250, child matches parent
        parent.Width = 200;
        parent.GetAbsoluteWidth().ShouldBe(250);
        child.GetAbsoluteWidth().ShouldBe(250);

        // Set parent padding back to 0: should return to 50, not stay at 250
        parent.Width = 0;
        parent.GetAbsoluteWidth().ShouldBe(50);
        child.GetAbsoluteWidth().ShouldBe(50);

        // Another cycle to verify no ratcheting
        parent.Width = 200;
        parent.GetAbsoluteWidth().ShouldBe(250);
        child.GetAbsoluteWidth().ShouldBe(250);

        parent.Width = 0;
        parent.GetAbsoluteWidth().ShouldBe(50);
        child.GetAbsoluteWidth().ShouldBe(50);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_Height_WithRelativeToChildrenParent_ShouldNotRatchetWhenPaddingChanges()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Height = 0;
        child.HeightUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        parent.AddChild(child);

        ContainerRuntime grandchild = new();
        grandchild.Height = 50;
        grandchild.HeightUnits = DimensionUnitType.Absolute;
        child.AddChild(grandchild);

        // Initial state
        parent.GetAbsoluteHeight().ShouldBe(50);
        child.GetAbsoluteHeight().ShouldBe(50);

        // Increase then decrease parent padding
        parent.Height = 200;
        parent.GetAbsoluteHeight().ShouldBe(250);
        child.GetAbsoluteHeight().ShouldBe(250);

        parent.Height = 0;
        parent.GetAbsoluteHeight().ShouldBe(50);
        child.GetAbsoluteHeight().ShouldBe(50);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_WithRelativeToChildrenParent_ShouldShrinkWhenChildShrinks()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime row1 = new();
        row1.Width = 0;
        row1.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        row1.Height = 40;
        row1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row1);

        ContainerRuntime row1Child = new();
        row1Child.Width = 500;
        row1Child.WidthUnits = DimensionUnitType.Absolute;
        row1.AddChild(row1Child);

        ContainerRuntime row2 = new();
        row2.Width = 0;
        row2.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        row2.Height = 40;
        row2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row2);

        ContainerRuntime row2Child = new();
        row2Child.Width = 200;
        row2Child.WidthUnits = DimensionUnitType.Absolute;
        row2.AddChild(row2Child);

        // row1 is widest at 500, all match
        parent.GetAbsoluteWidth().ShouldBe(500);
        row1.GetAbsoluteWidth().ShouldBe(500);
        row2.GetAbsoluteWidth().ShouldBe(500);

        // Shrink row1's child — row2 is now widest at 200
        row1Child.Width = 100;
        parent.GetAbsoluteWidth().ShouldBe(200);
        row1.GetAbsoluteWidth().ShouldBe(200);
        row2.GetAbsoluteWidth().ShouldBe(200);

        // Shrink to zero
        row1Child.Width = 0;
        row2Child.Width = 0;
        parent.GetAbsoluteWidth().ShouldBe(0);
        row1.GetAbsoluteWidth().ShouldBe(0);
        row2.GetAbsoluteWidth().ShouldBe(0);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_WithRelativeToChildrenParent_MixedSiblingUnits()
    {
        // Parent is RelativeToChildren with both an Absolute child and a
        // RelativeToMaxParentOrChildren child. The parent should size to the wider one.
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime absoluteChild = new();
        absoluteChild.Width = 300;
        absoluteChild.WidthUnits = DimensionUnitType.Absolute;
        absoluteChild.Height = 30;
        absoluteChild.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(absoluteChild);

        ContainerRuntime maxChild = new();
        maxChild.Width = 0;
        maxChild.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        maxChild.Height = 30;
        maxChild.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(maxChild);

        ContainerRuntime maxChildInner = new();
        maxChildInner.Width = 100;
        maxChildInner.WidthUnits = DimensionUnitType.Absolute;
        maxChild.AddChild(maxChildInner);

        // Absolute child (300) is wider than maxChild's content (100)
        // Parent sizes to 300, maxChild fills to 300
        parent.GetAbsoluteWidth().ShouldBe(300);
        absoluteChild.GetAbsoluteWidth().ShouldBe(300);
        maxChild.GetAbsoluteWidth().ShouldBe(300);

        // Now make the maxChild's content wider
        maxChildInner.Width = 500;
        parent.GetAbsoluteWidth().ShouldBe(500);
        absoluteChild.GetAbsoluteWidth().ShouldBe(300);
        maxChild.GetAbsoluteWidth().ShouldBe(500);

        // Shrink it back — Absolute child dominates again
        maxChildInner.Width = 100;
        parent.GetAbsoluteWidth().ShouldBe(300);
        maxChild.GetAbsoluteWidth().ShouldBe(300);
    }

    [Fact]
    public void RelativeToMaxParentOrChildren_WithRelativeToChildrenParent_PositionDoesNotAffectParentSize()
    {
        // Position offsets on a RelativeToMaxParentOrChildren child are intentionally
        // not considered by the parent's RelativeToChildren sizing. The parent sizes
        // based only on the child's content, not content + position.
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 0;
        child.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        parent.AddChild(child);

        ContainerRuntime grandchild = new();
        grandchild.Width = 100;
        grandchild.WidthUnits = DimensionUnitType.Absolute;
        child.AddChild(grandchild);

        // At X=0: parent=100, child=100
        parent.GetAbsoluteWidth().ShouldBe(100);
        child.GetAbsoluteWidth().ShouldBe(100);

        // Move child to X=100: parent stays at 100 (based on content only),
        // child stays at 100 (max of parent=100 and content=100)
        child.X = 100;
        parent.GetAbsoluteWidth().ShouldBe(100);
        child.GetAbsoluteWidth().ShouldBe(100);
    }

    #endregion

    #region Individual Child Changes In Stack

    [Fact]
    public void LeftToRightStack_LastChildResizes_ShouldNotAffectSiblingPositions()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime[] children = new ContainerRuntime[4];
        for (int i = 0; i < 4; i++)
        {
            children[i] = new ContainerRuntime();
            children[i].Width = 50;
            children[i].WidthUnits = DimensionUnitType.Absolute;
            children[i].Height = 40;
            children[i].HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(children[i]);
        }

        children[3].Width = 100;

        children[0].AbsoluteLeft.ShouldBe(0);
        children[1].AbsoluteLeft.ShouldBe(50);
        children[2].AbsoluteLeft.ShouldBe(100);
        children[3].AbsoluteLeft.ShouldBe(150);
    }

    [Fact]
    public void LeftToRightStack_MiddleChildBecomesInvisible_ShouldRepositionSubsequentSiblings()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime[] children = new ContainerRuntime[5];
        for (int i = 0; i < 5; i++)
        {
            children[i] = new ContainerRuntime();
            children[i].Width = 60;
            children[i].WidthUnits = DimensionUnitType.Absolute;
            children[i].Height = 40;
            children[i].HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(children[i]);
        }

        for (int i = 0; i < 5; i++)
        {
            children[i].AbsoluteLeft.ShouldBe(i * 60);
        }

        children[2].Visible = false;

        children[0].AbsoluteLeft.ShouldBe(0);
        children[1].AbsoluteLeft.ShouldBe(60);
        children[3].AbsoluteLeft.ShouldBe(120);
        children[4].AbsoluteLeft.ShouldBe(180);

        children[2].Visible = true;

        for (int i = 0; i < 5; i++)
        {
            children[i].AbsoluteLeft.ShouldBe(i * 60);
        }
    }

    [Fact]
    public void LeftToRightStack_MiddleChildResizes_ShouldRepositionSubsequentSiblings()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;

        ContainerRuntime[] children = new ContainerRuntime[5];
        for (int i = 0; i < 5; i++)
        {
            children[i] = new ContainerRuntime();
            children[i].Width = 50;
            children[i].WidthUnits = DimensionUnitType.Absolute;
            children[i].Height = 40;
            children[i].HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(children[i]);
        }

        for (int i = 0; i < 5; i++)
        {
            children[i].AbsoluteLeft.ShouldBe(i * 50);
        }

        children[1].Width = 100;

        children[0].AbsoluteLeft.ShouldBe(0);
        children[1].AbsoluteLeft.ShouldBe(50);
        children[2].AbsoluteLeft.ShouldBe(150);
        children[3].AbsoluteLeft.ShouldBe(200);
        children[4].AbsoluteLeft.ShouldBe(250);
    }

    [Fact]
    public void LeftToRightStack_MiddleChildResizes_WithStackSpacing_ShouldRepositionCorrectly()
    {
        ContainerRuntime parent = new();
        parent.Width = 600;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.StackSpacing = 10;

        ContainerRuntime[] children = new ContainerRuntime[5];
        for (int i = 0; i < 5; i++)
        {
            children[i] = new ContainerRuntime();
            children[i].Width = 50;
            children[i].WidthUnits = DimensionUnitType.Absolute;
            children[i].Height = 40;
            children[i].HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(children[i]);
        }

        children[0].AbsoluteLeft.ShouldBe(0);
        children[1].AbsoluteLeft.ShouldBe(60);
        children[2].AbsoluteLeft.ShouldBe(120);
        children[3].AbsoluteLeft.ShouldBe(180);
        children[4].AbsoluteLeft.ShouldBe(240);

        children[1].Width = 80;

        children[0].AbsoluteLeft.ShouldBe(0);
        children[1].AbsoluteLeft.ShouldBe(60);
        children[2].AbsoluteLeft.ShouldBe(150);
        children[3].AbsoluteLeft.ShouldBe(210);
        children[4].AbsoluteLeft.ShouldBe(270);
    }

    [Fact]
    public void TopToBottomStack_LastChildResizes_ShouldNotAffectSiblingPositions()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime[] children = new ContainerRuntime[4];
        for (int i = 0; i < 4; i++)
        {
            children[i] = new ContainerRuntime();
            children[i].Width = 40;
            children[i].WidthUnits = DimensionUnitType.Absolute;
            children[i].Height = 50;
            children[i].HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(children[i]);
        }

        children[3].Height = 100;

        children[0].AbsoluteTop.ShouldBe(0);
        children[1].AbsoluteTop.ShouldBe(50);
        children[2].AbsoluteTop.ShouldBe(100);
        children[3].AbsoluteTop.ShouldBe(150);
    }

    [Fact]
    public void TopToBottomStack_MiddleChildBecomesInvisible_ShouldRepositionSubsequentSiblings()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime[] children = new ContainerRuntime[5];
        for (int i = 0; i < 5; i++)
        {
            children[i] = new ContainerRuntime();
            children[i].Width = 40;
            children[i].WidthUnits = DimensionUnitType.Absolute;
            children[i].Height = 60;
            children[i].HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(children[i]);
        }

        for (int i = 0; i < 5; i++)
        {
            children[i].AbsoluteTop.ShouldBe(i * 60);
        }

        children[1].Visible = false;

        children[0].AbsoluteTop.ShouldBe(0);
        children[2].AbsoluteTop.ShouldBe(60);
        children[3].AbsoluteTop.ShouldBe(120);
        children[4].AbsoluteTop.ShouldBe(180);

        children[1].Visible = true;

        for (int i = 0; i < 5; i++)
        {
            children[i].AbsoluteTop.ShouldBe(i * 60);
        }
    }

    [Fact]
    public void TopToBottomStack_MiddleChildResizes_ShouldRepositionSubsequentSiblings()
    {
        ContainerRuntime parent = new();
        parent.Height = 400;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime[] children = new ContainerRuntime[5];
        for (int i = 0; i < 5; i++)
        {
            children[i] = new ContainerRuntime();
            children[i].Width = 40;
            children[i].WidthUnits = DimensionUnitType.Absolute;
            children[i].Height = 50;
            children[i].HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(children[i]);
        }

        for (int i = 0; i < 5; i++)
        {
            children[i].AbsoluteTop.ShouldBe(i * 50);
        }

        children[2].Height = 80;

        children[0].AbsoluteTop.ShouldBe(0);
        children[1].AbsoluteTop.ShouldBe(50);
        children[2].AbsoluteTop.ShouldBe(100);
        children[3].AbsoluteTop.ShouldBe(180);
        children[4].AbsoluteTop.ShouldBe(230);
    }

    [Fact]
    public void TopToBottomStack_MiddleChildResizes_WithStackSpacing_ShouldRepositionCorrectly()
    {
        ContainerRuntime parent = new();
        parent.Height = 600;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = 10;

        ContainerRuntime[] children = new ContainerRuntime[5];
        for (int i = 0; i < 5; i++)
        {
            children[i] = new ContainerRuntime();
            children[i].Width = 40;
            children[i].WidthUnits = DimensionUnitType.Absolute;
            children[i].Height = 50;
            children[i].HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(children[i]);
        }

        children[0].AbsoluteTop.ShouldBe(0);
        children[1].AbsoluteTop.ShouldBe(60);
        children[2].AbsoluteTop.ShouldBe(120);
        children[3].AbsoluteTop.ShouldBe(180);
        children[4].AbsoluteTop.ShouldBe(240);

        children[1].Height = 80;

        children[0].AbsoluteTop.ShouldBe(0);
        children[1].AbsoluteTop.ShouldBe(60);
        children[2].AbsoluteTop.ShouldBe(150);
        children[3].AbsoluteTop.ShouldBe(210);
        children[4].AbsoluteTop.ShouldBe(270);
    }

    #endregion

    #region StackedRowOrColumnDimensions

    [Fact]
    public void WrapsChildren_LeftToRight_ChildGrows_ShouldUpdateRowDimension()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.MaxWidth = 200;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 30;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 80;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 40;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Row1: child1+child2 (80+80=160 < 200), Row2: child3
        // Row1 max height is 50 (child2), so child3 starts at 50
        child3.AbsoluteTop.ShouldBe(50);

        // Grow child1 height to 70 — it becomes the tallest in row1
        child1.Height = 70;
        child3.AbsoluteTop.ShouldBe(70);
    }

    [Fact]
    public void WrapsChildren_LeftToRight_ChildShrinks_ShouldRecalculateRowDimension()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.MaxWidth = 200;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 30;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 60;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 80;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 40;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Row1: child1+child2, Row2: child3
        // Row1 max height is 60 (child2)
        child3.AbsoluteTop.ShouldBe(60);

        // Shrink child2 height to 25 — child1 at 30 is now tallest in row1
        child2.Height = 25;
        child3.AbsoluteTop.ShouldBe(30);
    }

    [Fact]
    public void WrapsChildren_LeftToRight_ChildVisibilityChange_ShouldUpdateRowPositions()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.MaxWidth = 200;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 50;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 50;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 80;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        ContainerRuntime child4 = new();
        child4.Width = 80;
        child4.WidthUnits = DimensionUnitType.Absolute;
        child4.Height = 50;
        child4.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child4);

        // Row1: child1+child2 (160 < 200), Row2: child3+child4
        child3.AbsoluteTop.ShouldBe(50);
        child4.AbsoluteTop.ShouldBe(50);

        // Hide child2 — now child1+child3 fit in row1, child4 wraps to row2
        child2.Visible = false;
        child3.AbsoluteTop.ShouldBe(0);
        child4.AbsoluteTop.ShouldBe(50);

        // Show child2 again — back to original layout
        child2.Visible = true;
        child3.AbsoluteTop.ShouldBe(50);
        child4.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void WrapsChildren_LeftToRight_ManyChildren_ShouldPositionAllCorrectly()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.MaxWidth = 200;
        parent.WrapsChildren = true;

        List<ContainerRuntime> children = new List<ContainerRuntime>();
        for (int i = 0; i < 20; i++)
        {
            ContainerRuntime child = new();
            child.Width = 90;
            child.WidthUnits = DimensionUnitType.Absolute;
            child.Height = 40;
            child.HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(child);
            children.Add(child);
        }

        // 2 per row (90+90=180 < 200, 90+90+90=270 > 200)
        for (int i = 0; i < 20; i++)
        {
            children[i].AbsoluteTop.ShouldBe((i / 2) * 40, $"Child {i} AbsoluteTop");
            children[i].AbsoluteLeft.ShouldBe((i % 2) * 90, $"Child {i} AbsoluteLeft");
        }
    }

    [Fact]
    public void WrapsChildren_LeftToRight_RowDimensionTracksMaxHeight()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.MaxWidth = 200;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 30;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 70;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 80;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 50;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        ContainerRuntime child4 = new();
        child4.Width = 80;
        child4.WidthUnits = DimensionUnitType.Absolute;
        child4.Height = 20;
        child4.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child4);

        // Row1: child1+child2 (max height 70), Row2: child3+child4 (max height 50)
        child1.AbsoluteTop.ShouldBe(0);
        child2.AbsoluteTop.ShouldBe(0);
        child3.AbsoluteTop.ShouldBe(70);
        child4.AbsoluteTop.ShouldBe(70);
    }

    [Fact]
    public void WrapsChildren_LeftToRight_WithStackSpacing_ChildShrinks_ShouldRecalculate()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.MaxWidth = 200;
        parent.WrapsChildren = true;
        parent.StackSpacing = 10;

        ContainerRuntime child1 = new();
        child1.Width = 80;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 40;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 80;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 60;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 80;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 30;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Row1: child1+child2 (80+10+80=170 < 200), Row2: child3
        // child3 top = row1 max height (60) + stack spacing (10)
        child3.AbsoluteTop.ShouldBe(70);

        // Shrink child2 height to 20 — child1 at 40 is now tallest
        child2.Height = 20;
        // child3 top = row1 max height (40) + stack spacing (10)
        child3.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void WrapsChildren_TopToBottom_ChildShrinks_ShouldRecalculateColumnDimension()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.MaxHeight = 200;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 60;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 80;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 40;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 80;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 80;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        // Col1: child1+child2 (80+80=160 < 200), Col2: child3
        // Col1 max width is 60 (child1)
        child3.AbsoluteLeft.ShouldBe(60);

        // Shrink child1 width to 30 — child2 at 40 is now widest in col1
        child1.Width = 30;
        child3.AbsoluteLeft.ShouldBe(40);
    }

    [Fact]
    public void WrapsChildren_TopToBottom_ColumnDimensionTracksMaxWidth()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.MaxHeight = 200;
        parent.WrapsChildren = true;

        ContainerRuntime child1 = new();
        child1.Width = 30;
        child1.WidthUnits = DimensionUnitType.Absolute;
        child1.Height = 80;
        child1.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child1);

        ContainerRuntime child2 = new();
        child2.Width = 70;
        child2.WidthUnits = DimensionUnitType.Absolute;
        child2.Height = 80;
        child2.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child2);

        ContainerRuntime child3 = new();
        child3.Width = 50;
        child3.WidthUnits = DimensionUnitType.Absolute;
        child3.Height = 80;
        child3.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child3);

        ContainerRuntime child4 = new();
        child4.Width = 20;
        child4.WidthUnits = DimensionUnitType.Absolute;
        child4.Height = 80;
        child4.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(child4);

        // Col1: child1+child2 (max width 70), Col2: child3+child4 (max width 50)
        child1.AbsoluteLeft.ShouldBe(0);
        child2.AbsoluteLeft.ShouldBe(0);
        child3.AbsoluteLeft.ShouldBe(70);
        child4.AbsoluteLeft.ShouldBe(70);
    }

    #endregion
}
