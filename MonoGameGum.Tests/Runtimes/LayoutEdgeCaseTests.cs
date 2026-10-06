using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Edge cases from the suspected-defect list in LAYOUT_TEST_PLAN.md. Tests drive layout through
/// public properties only and assert on absolute positions and sizes.
/// </summary>
public class LayoutEdgeCaseTests : BaseTestClass
{
    class TexturedRenderable : InvisibleRenderable, IAspectRatio, ITextureCoordinate
    {
        public float AspectRatio { get; set; } = 1;
        public System.Drawing.Rectangle? SourceRectangle { get; set; }
        bool ITextureCoordinate.Wrap { get; set; }
        public float? TextureWidth { get; set; }
        public float? TextureHeight { get; set; }
    }

    static ContainerRuntime CreateContainer(float width, float height)
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.Width = width;
        container.Height = height;
        return container;
    }

    static void AssertRelayoutChangesNothing(GraphicalUiElement element)
    {
        float left = element.AbsoluteLeft;
        float top = element.AbsoluteTop;
        float width = element.AbsoluteWidth;
        float height = element.AbsoluteHeight;

        element.UpdateLayout();

        element.AbsoluteLeft.ShouldBe(left);
        element.AbsoluteTop.ShouldBe(top);
        element.AbsoluteWidth.ShouldBe(width);
        element.AbsoluteHeight.ShouldBe(height);
    }

    #region AutoGrid

    [Fact]
    public void AutoGrid_FillChildHeight_ShouldMatchRowPitch_WhenRowsSpillPastVerticalCells()
    {
        ContainerRuntime grid = new();
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 2;
        grid.AutoGridVerticalCells = 2;
        grid.WidthUnits = DimensionUnitType.Absolute;
        grid.Width = 200;
        grid.HeightUnits = DimensionUnitType.RelativeToChildren;
        grid.Height = 0;

        ContainerRuntime sizingChild = CreateContainer(100, 100);
        grid.AddChild(sizingChild);
        for (int i = 0; i < 5; i++)
        {
            ContainerRuntime fillChild = new();
            fillChild.Dock(Dock.Fill);
            grid.AddChild(fillChild);
        }

        grid.AbsoluteHeight.ShouldBe(300);
        grid.Children[5].AbsoluteHeight.ShouldBe(100);
    }

    [Fact]
    public void AutoGridHorizontal_FixedSizeParent_ShouldPlaceExtraRowBelowBounds()
    {
        ContainerRuntime grid = CreateContainer(299, 183);
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 1;
        grid.AutoGridVerticalCells = 1;
        grid.AddChild(CreateContainer(50, 50));
        grid.AddChild(CreateContainer(50, 50));

        grid.Children[0].AbsoluteTop.ShouldBe(0);
        grid.Children[1].AbsoluteTop.ShouldBe(183);
    }

    [Fact]
    public void AutoGridVertical_FixedSizeParent_ShouldPlaceExtraColumnRightOfBounds()
    {
        ContainerRuntime grid = CreateContainer(299, 183);
        grid.ChildrenLayout = ChildrenLayout.AutoGridVertical;
        grid.AutoGridHorizontalCells = 1;
        grid.AutoGridVerticalCells = 1;
        grid.AddChild(CreateContainer(50, 50));
        grid.AddChild(CreateContainer(50, 50));

        grid.Children[0].AbsoluteLeft.ShouldBe(0);
        grid.Children[1].AbsoluteLeft.ShouldBe(299);
    }

    [Fact]
    public void AutoGrid_FixedSizeParent_ShouldOverflowBounds_WhenChildrenExceedCells()
    {
        ContainerRuntime grid = CreateContainer(200, 200);
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 2;
        grid.AutoGridVerticalCells = 2;
        for (int i = 0; i < 6; i++)
        {
            ContainerRuntime fillChild = new();
            fillChild.Dock(Dock.Fill);
            grid.AddChild(fillChild);
        }

        // Cells keep the size set by the cell counts; extra rows spill below the grid.
        grid.AbsoluteHeight.ShouldBe(200);
        grid.Children[5].AbsoluteHeight.ShouldBe(100);
        grid.Children[4].AbsoluteTop.ShouldBe(200);
    }

    [Fact]
    public void AutoGrid_RelativeToChildrenHeight_ShouldIgnoreInvisibleChildren()
    {
        ContainerRuntime grid = new();
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 2;
        grid.AutoGridVerticalCells = 1;
        grid.WidthUnits = DimensionUnitType.Absolute;
        grid.Width = 200;
        grid.HeightUnits = DimensionUnitType.RelativeToChildren;
        grid.Height = 0;
        for (int i = 0; i < 3; i++)
        {
            grid.AddChild(CreateContainer(100, 100));
        }

        grid.Children[0].Visible = false;

        grid.AbsoluteHeight.ShouldBe(100);
    }

    [Fact]
    public void AutoGrid_RelativeToChildrenWidth_ShouldUseOneColumn_WhenHorizontalCellsIsZero()
    {
        ContainerRuntime grid = new();
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 0;
        grid.AutoGridVerticalCells = 2;
        grid.WidthUnits = DimensionUnitType.RelativeToChildren;
        grid.Width = 0;
        grid.HeightUnits = DimensionUnitType.Absolute;
        grid.Height = 200;
        grid.AddChild(CreateContainer(100, 100));

        grid.AbsoluteWidth.ShouldBe(100);
    }

    [Fact]
    public void AutoGrid_RatioChild_ShouldFillItsCell()
    {
        ContainerRuntime grid = CreateContainer(400, 400);
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 2;
        grid.AutoGridVerticalCells = 2;
        for (int i = 0; i < 3; i++)
        {
            grid.AddChild(CreateContainer(100, 100));
        }
        ContainerRuntime ratioChild = CreateContainer(1, 100);
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        grid.AddChild(ratioChild);

        ratioChild.AbsoluteWidth.ShouldBe(200);
    }

    [Fact]
    public void AutoGrid_SettingChildX_ShouldKeepCellOffset()
    {
        ContainerRuntime grid = CreateContainer(400, 400);
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 2;
        grid.AutoGridVerticalCells = 2;
        for (int i = 0; i < 4; i++)
        {
            grid.AddChild(CreateContainer(50, 50));
        }

        grid.Children[1].X = 10;

        grid.Children[1].AbsoluteLeft.ShouldBe(210);
    }

    [Fact]
    public void AutoGrid_HidingChildWhileSuspended_ShouldRepositionSiblingsOnResume()
    {
        ContainerRuntime grid = CreateContainer(400, 400);
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 2;
        grid.AutoGridVerticalCells = 2;
        for (int i = 0; i < 4; i++)
        {
            grid.AddChild(CreateContainer(50, 50));
        }

        grid.Children[0].SuspendLayout();
        grid.Children[0].Visible = false;
        grid.Children[0].ResumeLayout();

        grid.Children[1].AbsoluteLeft.ShouldBe(0);
    }

    #endregion

    #region Stacks

    [Fact]
    public void LeftToRightStack_LaterChildWithPercentageX_ShouldStayInStack()
    {
        ContainerRuntime stack = CreateContainer(400, 100);
        stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        stack.AddChild(CreateContainer(50, 50));
        ContainerRuntime second = CreateContainer(50, 50);
        second.XUnits = GeneralUnitType.Percentage;
        second.X = 0;
        stack.AddChild(second);

        second.AbsoluteLeft.ShouldBe(50);
    }

    [Fact(Skip = "Behavior change pending decision: #5766")]
    public void TopToBottomStack_LaterChildWithCenterOrigin_ShouldNotOverlapPreviousSibling()
    {
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.AddChild(CreateContainer(50, 50));
        ContainerRuntime second = CreateContainer(50, 50);
        second.YOrigin = VerticalAlignment.Center;
        stack.AddChild(second);

        second.AbsoluteTop.ShouldBe(50);
    }

    // Intended (#5767): Ratio shares with every sibling on the same axis, whatever the stack direction.
    [Fact]
    public void LeftToRightStack_RatioHeight_ShouldSubtractSiblingsBesideIt()
    {
        ContainerRuntime stack = CreateContainer(300, 100);
        stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        stack.AddChild(CreateContainer(100, 40));
        ContainerRuntime ratioChild = CreateContainer(100, 1);
        ratioChild.HeightUnits = DimensionUnitType.Ratio;
        stack.AddChild(ratioChild);

        ratioChild.AbsoluteHeight.ShouldBe(100 - 40);
    }

    // Intended (#5767): the TopToBottomStack mirror of the case above.
    [Fact]
    public void TopToBottomStack_RatioWidth_ShouldSubtractSiblingsBesideIt()
    {
        ContainerRuntime stack = CreateContainer(100, 300);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.AddChild(CreateContainer(40, 100));
        ContainerRuntime ratioChild = CreateContainer(1, 100);
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        stack.AddChild(ratioChild);

        ratioChild.AbsoluteWidth.ShouldBe(100 - 40);
    }

    [Fact]
    public void UseFixedStackChildrenSize_RelativeToChildrenHeight_ShouldIgnoreInvisibleChildren()
    {
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.UseFixedStackChildrenSize = true;
        stack.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.Height = 0;
        for (int i = 0; i < 3; i++)
        {
            stack.AddChild(CreateContainer(50, 50));
        }

        stack.Children[2].Visible = false;

        stack.AbsoluteHeight.ShouldBe(100);
    }

    [Fact]
    public void UseFixedStackChildrenSize_ShouldUseFirstVisibleChildHeight_WhenFirstChildIsHidden()
    {
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.UseFixedStackChildrenSize = true;
        ContainerRuntime hidden = CreateContainer(50, 10);
        hidden.Visible = false;
        stack.AddChild(hidden);
        stack.AddChild(CreateContainer(50, 50));
        ContainerRuntime last = CreateContainer(50, 50);
        stack.AddChild(last);

        last.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void WrapsChildren_HidingTallestItemInRow_ShouldShrinkRow()
    {
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        stack.WrapsChildren = true;
        stack.AddChild(CreateContainer(40, 20));
        ContainerRuntime tallest = CreateContainer(40, 80);
        stack.AddChild(tallest);
        ContainerRuntime nextRow = CreateContainer(40, 20);
        stack.AddChild(nextRow);
        nextRow.AbsoluteTop.ShouldBe(80);

        tallest.Visible = false;

        nextRow.AbsoluteTop.ShouldBe(0);
    }

    #endregion

    #region Ratio

    [Fact]
    public void Ratio_ShouldNotGoNegative_WhenSiblingsExceedParent()
    {
        ContainerRuntime parent = CreateContainer(100, 100);
        parent.AddChild(CreateContainer(150, 10));
        ContainerRuntime ratioChild = CreateContainer(1, 10);
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        ratioChild.AbsoluteWidth.ShouldBe(0);
    }

    [Fact]
    public void Ratio_UnderRenderablelessParent_ShouldAccountForRelativeToChildrenSibling()
    {
        GraphicalUiElement screen = new GraphicalUiElement(null);
        ContainerRuntime ratioChild = CreateContainer(1, 10);
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        ratioChild.ElementGueContainingThis = screen;
        ContainerRuntime sizedChild = new();
        sizedChild.WidthUnits = DimensionUnitType.RelativeToChildren;
        sizedChild.Width = 0;
        sizedChild.AddChild(CreateContainer(100, 10));
        sizedChild.ElementGueContainingThis = screen;

        screen.UpdateLayout();

        ratioChild.AbsoluteWidth.ShouldBe(700);
    }

    // Children of an element without a renderable (an old-style screen) have no Parent; they are
    // held through ElementGueContainingThis and size against the canvas.
    static ContainerRuntime AddParentlessChild(GraphicalUiElement container, float width, float height)
    {
        ContainerRuntime child = CreateContainer(width, height);
        child.ElementGueContainingThis = container;
        return child;
    }

    [Fact]
    public void Ratio_UnderRenderablelessParent_ShouldSubtractAbsoluteSibling()
    {
        GraphicalUiElement screen = new GraphicalUiElement(null);
        AddParentlessChild(screen, 100, 10);
        ContainerRuntime ratioChild = AddParentlessChild(screen, 1, 10);
        ratioChild.WidthUnits = DimensionUnitType.Ratio;

        screen.UpdateLayout();

        ratioChild.AbsoluteWidth.ShouldBe(GraphicalUiElement.CanvasWidth - 100);
    }

    [Fact]
    public void Ratio_UnderRenderablelessParent_ShouldSplitBetweenRatioSiblings()
    {
        GraphicalUiElement screen = new GraphicalUiElement(null);
        ContainerRuntime first = AddParentlessChild(screen, 1, 10);
        first.WidthUnits = DimensionUnitType.Ratio;
        ContainerRuntime second = AddParentlessChild(screen, 3, 10);
        second.WidthUnits = DimensionUnitType.Ratio;

        screen.UpdateLayout();

        first.AbsoluteWidth.ShouldBe(GraphicalUiElement.CanvasWidth / 4);
        second.AbsoluteWidth.ShouldBe(GraphicalUiElement.CanvasWidth * 3 / 4);
    }

    [Fact]
    public void Ratio_UnderRenderablelessParent_ShouldUpdate_WhenSiblingWidthChanges()
    {
        GraphicalUiElement screen = new GraphicalUiElement(null);
        ContainerRuntime absoluteChild = AddParentlessChild(screen, 100, 10);
        ContainerRuntime ratioChild = AddParentlessChild(screen, 1, 10);
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        screen.UpdateLayout();

        absoluteChild.Width = 300;

        ratioChild.AbsoluteWidth.ShouldBe(GraphicalUiElement.CanvasWidth - 300);
    }

    [Fact]
    public void Ratio_UnderRenderablelessStackingParent_ShouldSubtractStackSpacing()
    {
        GraphicalUiElement container = new GraphicalUiElement(null);
        container.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        container.StackSpacing = 10;
        AddParentlessChild(container, 50, 100);
        ContainerRuntime ratioChild = AddParentlessChild(container, 50, 1);
        ratioChild.HeightUnits = DimensionUnitType.Ratio;

        container.UpdateLayout();

        ratioChild.AbsoluteHeight.ShouldBe(GraphicalUiElement.CanvasHeight - 100 - 10);
    }

    [Fact]
    public void ParentlessChildOfStackingContainer_ShouldRestackSiblings_WhenItsHeightChanges()
    {
        GraphicalUiElement container = new GraphicalUiElement(null);
        container.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        ContainerRuntime first = AddParentlessChild(container, 50, 50);
        ContainerRuntime second = AddParentlessChild(container, 50, 50);
        container.UpdateLayout();
        second.AbsoluteTop.ShouldBe(50);

        first.Height = 80;

        second.AbsoluteTop.ShouldBe(80);
    }

    #endregion

    #region Dimension units

    [Fact]
    public void MaintainFileAspectRatio_ShouldStayFinite_WhenAspectRatioIsZero()
    {
        TexturedRenderable renderable = new() { AspectRatio = 0 };
        GraphicalUiElement element = new(renderable);
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;
        element.Height = 100;

        float.IsFinite(element.AbsoluteHeight).ShouldBeTrue();
    }

    [Fact]
    public void PercentageOfOtherDimensionAndMaintainFileAspectRatio_ShouldBeStable_AcrossLayouts()
    {
        TexturedRenderable renderable = new() { AspectRatio = 2 };
        GraphicalUiElement element = new(renderable);
        element.Width = 100;
        element.Height = 100;
        element.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;

        AssertRelayoutChangesNothing(element);
    }

    [Fact]
    public void RelativeToChildren_MaintainFileAspectRatioChild_ShouldBeStable_AcrossLayouts()
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        TexturedRenderable renderable = new() { AspectRatio = 2 };
        GraphicalUiElement child = new(renderable);
        child.XUnits = GeneralUnitType.Percentage;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Width = 100;
        child.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;
        child.Height = 100;
        parent.AddChild(child);

        child.AbsoluteHeight.ShouldBe(50);
        AssertRelayoutChangesNothing(child);
    }

    [Fact]
    public void ScreenPixel_ShouldDivideByCameraZoom_WhenManagersAreAttached()
    {
        Camera camera = new();
        camera.Zoom = 2;
        Mock<IRenderer> renderer = new();
        renderer.Setup(item => item.Camera).Returns(camera);
        Mock<ISystemManagers> managers = new();
        managers.Setup(item => item.Renderer).Returns(renderer.Object);
        ContainerRuntime element = new();
        element.AttachManagersOnly(managers.Object);

        element.WidthUnits = DimensionUnitType.ScreenPixel;
        element.Width = 100;

        element.AbsoluteWidth.ShouldBe(50);
    }

    [Fact]
    public void ScreenPixel_ShouldUpdateOnNextLayout_WhenZoomChanges()
    {
        Camera camera = new();
        camera.Zoom = 1;
        Mock<IRenderer> renderer = new();
        renderer.Setup(item => item.Camera).Returns(camera);
        Mock<ISystemManagers> managers = new();
        managers.Setup(item => item.Renderer).Returns(renderer.Object);
        ContainerRuntime element = new();
        element.AttachManagersOnly(managers.Object);
        element.WidthUnits = DimensionUnitType.ScreenPixel;
        element.Width = 100;

        // Camera zoom is not a layout property, so the element keeps its size until the next layout.
        camera.Zoom = 2;
        element.AbsoluteWidth.ShouldBe(100);

        element.UpdateLayout();

        element.AbsoluteWidth.ShouldBe(50);
    }

    #endregion

    #region Position units

    // Only code can set X/Y units to PercentageOfFile; saved projects store PositionUnitType, which has no such value.
    [Fact]
    public void PercentageOfFile_ShouldUseTextureSize()
    {
        TexturedRenderable renderable = new() { TextureWidth = 200, TextureHeight = 100 };
        GraphicalUiElement element = new(renderable);
        element.XUnits = GeneralUnitType.PercentageOfFile;
        element.X = 50;
        element.YUnits = GeneralUnitType.PercentageOfFile;
        element.Y = 50;

        element.AbsoluteLeft.ShouldBe(100);
        element.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void PixelsFromMiddleInverted_ShouldContributeSymmetricHeight_ToRelativeToMaxParentOrChildrenParent()
    {
        GraphicalUiElement.CanvasHeight = 0;
        ContainerRuntime parent = new();
        parent.HeightUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        parent.Height = 0;
        ContainerRuntime child = CreateContainer(20, 20);
#pragma warning disable CS0618 // obsolete unit still loads from older projects
        child.YUnits = GeneralUnitType.PixelsFromMiddleInverted;
#pragma warning restore CS0618
        child.Y = 10;
        parent.AddChild(child);

        parent.AbsoluteHeight.ShouldBe(20);
    }

    #endregion

    #region Setters

    [Fact]
    public void SettingX_ShouldMatchFullLayout_WhenParentIsFlipped()
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        parent.FlipHorizontal = true;
        ContainerRuntime child = CreateContainer(50, 50);
        parent.AddChild(child);

        child.X = 10;

        AssertRelayoutChangesNothing(child);
    }

    [Fact]
    public void SettingX_ShouldGrowRelativeToMaxParentOrChildrenParent()
    {
        ContainerRuntime grandparent = CreateContainer(100, 100);
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        parent.Width = 0;
        grandparent.AddChild(parent);
        ContainerRuntime child = CreateContainer(50, 50);
        parent.AddChild(child);

        child.X = 200;

        parent.AbsoluteWidth.ShouldBe(250);
    }

    [Fact]
    public void SettingY_ShouldKeepStackPosition_ForParentlessChildOfStackingContainer()
    {
        ContainerRuntime component = CreateContainer(100, 400);
        component.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        GraphicalUiElement first = new(new InvisibleRenderable(), component);
        first.Height = 50;
        GraphicalUiElement second = new(new InvisibleRenderable(), component);
        second.Height = 50;
        second.Parent = null;
        component.UpdateLayout();
        float stackedTop = second.AbsoluteTop;

        second.Y = 5;

        second.AbsoluteTop.ShouldBe(stackedTop + 5);
    }

    [Fact]
    public void Dock_ShouldRaiseSizeChangedOnce()
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        ContainerRuntime child = CreateContainer(50, 50);
        parent.AddChild(child);
        int sizeChangedCount = 0;
        child.SizeChanged += (_, _) => sizeChangedCount++;

        child.Dock(Dock.Fill);

        sizeChangedCount.ShouldBe(1);
    }

    [Fact]
    public void Dock_FillVertically_ShouldNotChangeTextHorizontalAlignment()
    {
        TextRuntime text = new();
        text.HorizontalAlignment = HorizontalAlignment.Right;

        text.Dock(Dock.FillVertically);

        text.HorizontalAlignment.ShouldBe(HorizontalAlignment.Right);
    }

    [Fact]
    public void PositionChanged_ShouldBeRaised_WhenXIsPlacedWithoutLayout()
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        ContainerRuntime child = CreateContainer(50, 50);
        parent.AddChild(child);
        int positionChangedCount = 0;
        child.PositionChanged += (_, _) => positionChangedCount++;

        child.X = 10;

        positionChangedCount.ShouldBe(1);
    }

    [Fact]
    public void PositionChanged_ShouldBeRaised_WhenYIsPlacedWithoutLayout()
    {
        ContainerRuntime element = CreateContainer(50, 50);
        int positionChangedCount = 0;
        element.PositionChanged += (_, _) => positionChangedCount++;

        element.Y = 10;

        positionChangedCount.ShouldBe(1);
    }

    [Fact]
    public void PositionChanged_HandlerSettingX_ShouldTerminateWithHandlerValue()
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        ContainerRuntime child = CreateContainer(50, 50);
        child.XUnits = GeneralUnitType.PixelsFromMiddle;
        parent.AddChild(child);
        child.PositionChanged += (_, _) => child.X = 30;

        child.X = 10;

        child.X.ShouldBe(30);
        child.AbsoluteLeft.ShouldBe(230);
    }

    #endregion

    #region Hierarchy

    [Fact]
    public void RemoveChild_ShouldRestackRemainingSiblings()
    {
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.AddChild(CreateContainer(50, 50));
        ContainerRuntime middle = CreateContainer(50, 50);
        stack.AddChild(middle);
        ContainerRuntime last = CreateContainer(50, 50);
        stack.AddChild(last);

        stack.RemoveChild(middle);

        last.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void ChildrenClear_ShouldShrinkRelativeToChildrenParent()
    {
        ContainerRuntime parent = new();
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.AddChild(CreateContainer(50, 50));

        parent.Children.Clear();

        parent.AbsoluteHeight.ShouldBe(0);
    }

    [Fact]
    public void RemoveChild_ShouldDeferParentLayoutUntilResume_WhenParentIsSuspended()
    {
        ContainerRuntime stack = CreateStackOfThree(out ContainerRuntime middle, out ContainerRuntime last);
        stack.SuspendLayout();
        int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        stack.RemoveChild(middle);

        last.AbsoluteTop.ShouldBe(100);
        int parentAndLastCalls = GraphicalUiElement.UpdateLayoutCallCount - callsBefore;
        // Only the removed child lays itself out; the suspended parent and its children wait.
        parentAndLastCalls.ShouldBe(1);

        stack.ResumeLayout();

        last.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void RemoveChild_ShouldDeferParentLayout_WhenAllLayoutIsSuspended()
    {
        ContainerRuntime stack = CreateStackOfThree(out ContainerRuntime middle, out ContainerRuntime last);
        GraphicalUiElement.IsAllLayoutSuspended = true;
        int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        stack.RemoveChild(middle);

        GraphicalUiElement.UpdateLayoutCallCount.ShouldBe(callsBefore);
        last.AbsoluteTop.ShouldBe(100);

        GraphicalUiElement.IsAllLayoutSuspended = false;
        stack.UpdateLayout();

        last.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void RemovingManyChildren_WhileParentIsSuspended_ShouldLayOutParentOnceOnResume()
    {
        ContainerRuntime stack = CreateContainer(100, 1000);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        for (int i = 0; i < 10; i++)
        {
            stack.AddChild(CreateContainer(50, 50));
        }
        ContainerRuntime last = stack.Children[9] as ContainerRuntime ?? throw new InvalidOperationException();
        stack.SuspendLayout();

        for (int i = 0; i < 5; i++)
        {
            stack.RemoveChild(stack.Children[0]);
        }
        int callsBeforeResume = GraphicalUiElement.UpdateLayoutCallCount;
        stack.ResumeLayout();
        int resumeCalls = GraphicalUiElement.UpdateLayoutCallCount - callsBeforeResume;

        last.AbsoluteTop.ShouldBe(200);
        int callsBeforeManualLayout = GraphicalUiElement.UpdateLayoutCallCount;
        stack.UpdateLayout();
        int oneParentLayoutCalls = GraphicalUiElement.UpdateLayoutCallCount - callsBeforeManualLayout;
        resumeCalls.ShouldBe(oneParentLayoutCalls);
    }

    [Fact]
    public void ChildrenClear_ShouldLayOutParentOnce()
    {
        ContainerRuntime parent = new();
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        for (int i = 0; i < 10; i++)
        {
            parent.AddChild(CreateContainer(50, 50));
        }
        int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        parent.Children.Clear();
        int clearCalls = GraphicalUiElement.UpdateLayoutCallCount - callsBefore;

        int callsBeforeManualLayout = GraphicalUiElement.UpdateLayoutCallCount;
        parent.UpdateLayout();
        int oneParentLayoutCalls = GraphicalUiElement.UpdateLayoutCallCount - callsBeforeManualLayout;
        // Each removed child lays itself out once, then the parent lays out once.
        clearCalls.ShouldBe(10 + oneParentLayoutCalls);
    }

    [Fact]
    public void ChildrenClear_ShouldKeepCallersSuspension()
    {
        ContainerRuntime parent = new();
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Height = 0;
        parent.AddChild(CreateContainer(50, 50));
        parent.AddChild(CreateContainer(50, 50));
        parent.SuspendLayout();

        parent.Children.Clear();

        parent.IsLayoutSuspended.ShouldBeTrue();
        parent.AbsoluteHeight.ShouldBe(50);
        parent.ResumeLayout();
        parent.AbsoluteHeight.ShouldBe(0);
    }

    [Fact]
    public void RemoveChild_ShouldResizeRatioSiblings_InRegularParent()
    {
        ContainerRuntime parent = CreateContainer(300, 100);
        ContainerRuntime absoluteChild = CreateContainer(100, 10);
        parent.AddChild(absoluteChild);
        ContainerRuntime ratioChild = CreateContainer(1, 10);
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);
        ratioChild.AbsoluteWidth.ShouldBe(200);

        parent.RemoveChild(absoluteChild);

        ratioChild.AbsoluteWidth.ShouldBe(300);
    }

    [Fact]
    public void RemoveChild_ShouldNotLayOutParent_WhenParentLayoutDoesNotDependOnChildren()
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        ContainerRuntime child = CreateContainer(50, 50);
        parent.AddChild(child);
        parent.AddChild(CreateContainer(50, 50));
        int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        parent.RemoveChild(child);

        // Only the removed child lays itself out.
        (GraphicalUiElement.UpdateLayoutCallCount - callsBefore).ShouldBe(1);
    }

    static ContainerRuntime CreateStackOfThree(out ContainerRuntime middle, out ContainerRuntime last)
    {
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.AddChild(CreateContainer(50, 50));
        middle = CreateContainer(50, 50);
        stack.AddChild(middle);
        last = CreateContainer(50, 50);
        stack.AddChild(last);
        return stack;
    }

    #endregion
}
