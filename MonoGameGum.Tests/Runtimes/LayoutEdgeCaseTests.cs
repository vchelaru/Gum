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

    [Fact]
    public void TopToBottomStack_FirstChildWithCenterOrigin_ShouldKeepOrigin()
    {
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        ContainerRuntime first = CreateContainer(50, 50);
        first.YUnits = GeneralUnitType.PixelsFromMiddle;
        first.YOrigin = VerticalAlignment.Center;
        stack.AddChild(first);

        first.AbsoluteTop.ShouldBe(200 - 25);
    }

    [Fact]
    public void LeftToRightStack_LaterChildWithRightOrigin_ShouldNotOverlapPreviousSibling()
    {
        ContainerRuntime stack = CreateContainer(400, 100);
        stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        stack.AddChild(CreateContainer(50, 50));
        ContainerRuntime second = CreateContainer(50, 50);
        second.XOrigin = HorizontalAlignment.Right;
        second.X = 10;
        stack.AddChild(second);

        second.AbsoluteLeft.ShouldBe(50 + 10);
    }

    [Theory]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    [InlineData(VerticalAlignment.TextBaseline)]
    public void TopToBottomStack_LaterChildWithNonTopOrigin_ShouldNotOverlapPreviousSibling(VerticalAlignment yOrigin)
    {
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.AddChild(CreateContainer(50, 50));
        ContainerRuntime second = CreateContainer(50, 50);
        second.YOrigin = yOrigin;
        stack.AddChild(second);

        second.AbsoluteTop.ShouldBe(50);
    }

    // The migration path for layouts that relied on the old origin overlap (#5766).
    [Fact]
    public void TopToBottomStack_LaterChildWithNegativeY_ShouldOverlapPreviousSibling()
    {
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.AddChild(CreateContainer(50, 50));
        ContainerRuntime second = CreateContainer(50, 50);
        second.Y = -25;
        stack.AddChild(second);

        second.AbsoluteTop.ShouldBe(25);
    }

    [Fact]
    public void TopToBottomStack_LaterChildWithCenterOrigin_ShouldKeepCrossAxisOrigin()
    {
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.AddChild(CreateContainer(50, 50));
        ContainerRuntime second = CreateContainer(50, 50);
        second.XUnits = GeneralUnitType.PixelsFromMiddle;
        second.XOrigin = HorizontalAlignment.Center;
        second.YOrigin = VerticalAlignment.Center;
        stack.AddChild(second);

        second.AbsoluteLeft.ShouldBe(50 - 25);
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

    static ContainerRuntime CreateWrappingStack(ChildrenLayout stack, DimensionUnitType crossUnits, float mainSize,
        float crossSize, float spacing, float[] childMainSizes, float childCrossSize, bool useFixedSize)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime parent = new();
        parent.ChildrenLayout = stack;
        parent.WrapsChildren = true;
        parent.UseFixedStackChildrenSize = useFixedSize;
        parent.StackSpacing = spacing;
        parent.WidthUnits = stacksVertically ? crossUnits : DimensionUnitType.Absolute;
        parent.HeightUnits = stacksVertically ? DimensionUnitType.Absolute : crossUnits;
        parent.Width = stacksVertically ? crossSize : mainSize;
        parent.Height = stacksVertically ? mainSize : crossSize;
        foreach (float childMainSize in childMainSizes)
        {
            parent.AddChild(stacksVertically
                ? CreateContainer(childCrossSize, childMainSize)
                : CreateContainer(childMainSize, childCrossSize));
        }
        return parent;
    }

    // The fixed-size fast path does not apply to a wrapping stack; it lays out like a non-fixed one.
    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack, DimensionUnitType.Absolute)]
    [InlineData(ChildrenLayout.TopToBottomStack, DimensionUnitType.RelativeToChildren)]
    [InlineData(ChildrenLayout.LeftToRightStack, DimensionUnitType.Absolute)]
    [InlineData(ChildrenLayout.LeftToRightStack, DimensionUnitType.RelativeToChildren)]
    public void UseFixedStackChildrenSize_WithWrapsChildren_ShouldMatchNonFixedLayout(ChildrenLayout stack, DimensionUnitType crossUnits)
    {
        float mainSize = 100;
        float crossSize = crossUnits == DimensionUnitType.Absolute ? 200 : 0;
        float spacing = 5;
        float childCrossSize = 40;
        float[] childMainSizes = { 40, 30, 40, 20, 40 };
        float[] expectedMainPositions = { 0, 45, 0, 45, 0 };
        int[] expectedLines = { 0, 0, 1, 1, 2 };
        float expectedParentCross = crossUnits == DimensionUnitType.Absolute ? 200 : 3 * childCrossSize + 2 * spacing;
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;

        ContainerRuntime fixedStack = CreateWrappingStack(stack, crossUnits, mainSize, crossSize, spacing, childMainSizes, childCrossSize, useFixedSize: true);
        ContainerRuntime nonFixedStack = CreateWrappingStack(stack, crossUnits, mainSize, crossSize, spacing, childMainSizes, childCrossSize, useFixedSize: false);

        for (int i = 0; i < childMainSizes.Length; i++)
        {
            GraphicalUiElement child = fixedStack.Children[i];
            GraphicalUiElement reference = nonFixedStack.Children[i];
            float expectedCross = expectedLines[i] * (childCrossSize + spacing);
            (stacksVertically ? child.AbsoluteTop : child.AbsoluteLeft).ShouldBe(expectedMainPositions[i], $"child {i} main");
            (stacksVertically ? child.AbsoluteLeft : child.AbsoluteTop).ShouldBe(expectedCross, $"child {i} cross");
            child.AbsoluteLeft.ShouldBe(reference.AbsoluteLeft, $"child {i} left vs non-fixed");
            child.AbsoluteTop.ShouldBe(reference.AbsoluteTop, $"child {i} top vs non-fixed");
        }
        (stacksVertically ? fixedStack.AbsoluteWidth : fixedStack.AbsoluteHeight).ShouldBe(expectedParentCross);
        (stacksVertically ? nonFixedStack.AbsoluteWidth : nonFixedStack.AbsoluteHeight).ShouldBe(expectedParentCross);
    }

    static ContainerRuntime CreateFixedSizeStack(bool useFixedSize, int offsetChildIndex, float offset)
    {
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.UseFixedStackChildrenSize = useFixedSize;
        stack.StackSpacing = 5;
        stack.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.Height = 0;
        for (int i = 0; i < 3; i++)
        {
            ContainerRuntime child = CreateContainer(50, 50);
            if (i == offsetChildIndex)
            {
                child.Y = offset;
            }
            stack.AddChild(child);
        }
        return stack;
    }

    [Theory]
    [InlineData(-20f)]
    [InlineData(15f)]
    public void UseFixedStackChildrenSize_FirstChildYOffset_ShouldShiftLaterChildren_LikeNonFixedStack(float offset)
    {
        float[] expectedTops = { offset, offset + 55, offset + 110 };
        // The parent measures to the last child's bottom; a part above its top edge is not counted.
        float expectedHeight = offset + 160;

        ContainerRuntime fixedStack = CreateFixedSizeStack(useFixedSize: true, offsetChildIndex: 0, offset);
        ContainerRuntime nonFixedStack = CreateFixedSizeStack(useFixedSize: false, offsetChildIndex: 0, offset);

        for (int i = 0; i < 3; i++)
        {
            fixedStack.Children[i].AbsoluteTop.ShouldBe(expectedTops[i], $"fixed child {i}");
            nonFixedStack.Children[i].AbsoluteTop.ShouldBe(expectedTops[i], $"non-fixed child {i}");
        }
        fixedStack.AbsoluteHeight.ShouldBe(expectedHeight);
        nonFixedStack.AbsoluteHeight.ShouldBe(expectedHeight);
    }

    // Intended: the fast path places each later child by index, so a later child's own Y offset
    // moves only that child, and the parent's height still reaches the last child's bottom.
    [Fact]
    public void UseFixedStackChildrenSize_LaterChildYOffset_ShouldMoveOnlyThatChild()
    {
        float offset = -20;

        ContainerRuntime stack = CreateFixedSizeStack(useFixedSize: true, offsetChildIndex: 1, offset);

        stack.Children[1].AbsoluteTop.ShouldBe(55 + offset);
        stack.Children[2].AbsoluteTop.ShouldBe(110);
        stack.AbsoluteHeight.ShouldBe(160);
    }

    public enum NegativeOffsetCase
    {
        FirstChildMainAxis,
        LaterChildMainAxis,
        CrossAxis,
    }

    // A negative PixelsFromSmall offset moves the child; the part outside the parent's leading edge
    // does not count toward a RelativeToChildren size (Width Units docs, "Ignored Width Values" 3).
    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack, NegativeOffsetCase.FirstChildMainAxis)]
    [InlineData(ChildrenLayout.TopToBottomStack, NegativeOffsetCase.LaterChildMainAxis)]
    [InlineData(ChildrenLayout.TopToBottomStack, NegativeOffsetCase.CrossAxis)]
    [InlineData(ChildrenLayout.LeftToRightStack, NegativeOffsetCase.FirstChildMainAxis)]
    [InlineData(ChildrenLayout.LeftToRightStack, NegativeOffsetCase.LaterChildMainAxis)]
    [InlineData(ChildrenLayout.LeftToRightStack, NegativeOffsetCase.CrossAxis)]
    public void Stack_ChildWithNegativePixelsFromSmallOffset_ShouldMoveChild_AndClipItFromRelativeToChildrenSize(ChildrenLayout stack, NegativeOffsetCase offsetCase)
    {
        float childSize = 50;
        float offset = -20;
        float wideCrossSize = 80;
        int offsetChildIndex = offsetCase == NegativeOffsetCase.LaterChildMainAxis ? 1 : 0;
        float[] expectedMain = offsetCase switch
        {
            NegativeOffsetCase.FirstChildMainAxis => new float[] { -20, 30, 80 },
            NegativeOffsetCase.LaterChildMainAxis => new float[] { 0, 30, 80 },
            _ => new float[] { 0, 50, 100 },
        };
        float[] expectedCross = offsetCase == NegativeOffsetCase.CrossAxis
            ? new float[] { -20, 0, 0 }
            : new float[] { 0, 0, 0 };
        float expectedParentMain = offsetCase == NegativeOffsetCase.CrossAxis ? 150 : 130;
        // The cross-axis case widens the offset child so its visible part (80 - 20) is the largest.
        float expectedParentCross = offsetCase == NegativeOffsetCase.CrossAxis ? wideCrossSize + offset : childSize;
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;

        ContainerRuntime parent = new();
        parent.ChildrenLayout = stack;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        for (int i = 0; i < 3; i++)
        {
            float crossSize = offsetCase == NegativeOffsetCase.CrossAxis && i == 0 ? wideCrossSize : childSize;
            ContainerRuntime child = stacksVertically ? CreateContainer(crossSize, childSize) : CreateContainer(childSize, crossSize);
            if (i == offsetChildIndex)
            {
                bool offsetsMainAxis = offsetCase != NegativeOffsetCase.CrossAxis;
                if (offsetsMainAxis == stacksVertically)
                {
                    child.Y = offset;
                }
                else
                {
                    child.X = offset;
                }
            }
            parent.AddChild(child);
        }

        for (int i = 0; i < 3; i++)
        {
            GraphicalUiElement child = parent.Children[i];
            (stacksVertically ? child.AbsoluteTop : child.AbsoluteLeft).ShouldBe(expectedMain[i], $"child {i} main");
            (stacksVertically ? child.AbsoluteLeft : child.AbsoluteTop).ShouldBe(expectedCross[i], $"child {i} cross");
        }
        (stacksVertically ? parent.AbsoluteHeight : parent.AbsoluteWidth).ShouldBe(expectedParentMain);
        (stacksVertically ? parent.AbsoluteWidth : parent.AbsoluteHeight).ShouldBe(expectedParentCross);
        AssertRelayoutChangesNothing(parent);
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

    [Fact]
    public void ScreenPixel_ShouldStayUnscaled_UntilManagersAreAttachedAndLayoutRuns()
    {
        Camera camera = new();
        camera.Zoom = 2;
        Mock<IRenderer> renderer = new();
        renderer.Setup(item => item.Camera).Returns(camera);
        Mock<ISystemManagers> managers = new();
        managers.Setup(item => item.Renderer).Returns(renderer.Object);
        ContainerRuntime element = new();
        element.WidthUnits = DimensionUnitType.ScreenPixel;
        element.Width = 100;
        element.AbsoluteWidth.ShouldBe(100);

        element.AttachManagersOnly(managers.Object);
        element.UpdateLayout();

        element.AbsoluteWidth.ShouldBe(50);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(-1f, -100f)]
    public void AbsoluteMultipliedByFontScale_ShouldMultiplyByUnusualScales(float scale, float expectedWidth)
    {
        GraphicalUiElement.GlobalFontScale = scale;
        ContainerRuntime element = new();
        element.WidthUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;
        element.Width = 100;

        element.AbsoluteWidth.ShouldBe(expectedWidth);
    }

    // GlobalFontScale is static and is not a layout trigger: an element keeps its size until its next layout.
    [Fact]
    public void AbsoluteMultipliedByFontScale_ShouldKeepSize_UntilNextLayoutAfterGlobalScaleChanges()
    {
        GraphicalUiElement.GlobalFontScale = 1;
        ContainerRuntime element = new();
        element.WidthUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;
        element.Width = 100;

        GraphicalUiElement.GlobalFontScale = 2;
        element.AbsoluteWidth.ShouldBe(100);

        element.UpdateLayout();

        element.AbsoluteWidth.ShouldBe(200);
    }

    // DimensionsBased derives the source rectangle from the size, so the texture size is used and
    // the renderable's own rectangle is ignored.
    [Fact]
    public void PercentageOfSourceFile_ShouldUseTextureSize_WhenTextureAddressIsDimensionsBased()
    {
        TexturedRenderable renderable = new()
        {
            TextureWidth = 200,
            TextureHeight = 100,
            SourceRectangle = new System.Drawing.Rectangle(0, 0, 50, 20),
        };
        GraphicalUiElement element = new(renderable);
        element.TextureAddress = TextureAddress.DimensionsBased;
        element.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        element.Width = 50;
        element.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        element.Height = 50;

        element.AbsoluteWidth.ShouldBe(100);
        element.AbsoluteHeight.ShouldBe(50);
    }

    // With a custom source rectangle, MaintainFileAspectRatio scales the rectangle's height by how far the
    // width was scaled from the rectangle's width, instead of using the whole texture's aspect ratio.
    [Fact]
    public void MaintainFileAspectRatio_ShouldScaleSourceRectangle_WhenRectangleIsSet()
    {
        TexturedRenderable renderable = new() { AspectRatio = 1 };
        GraphicalUiElement element = new(renderable);
        element.TextureAddress = TextureAddress.Custom;
        element.TextureLeft = 0;
        element.TextureTop = 0;
        element.TextureWidth = 50;
        element.TextureHeight = 25;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;
        element.Height = 100;

        element.AbsoluteHeight.ShouldBe(50);
    }

    // A container's renderable has no IAspectRatio at all, so the axis falls back to the 64 pixel default.
    [Fact]
    public void MaintainFileAspectRatio_ShouldFallBackToDefaultSize_WhenRenderableHasNoAspectRatio()
    {
        ContainerRuntime element = new();
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;
        element.Height = 100;

        element.AbsoluteHeight.ShouldBe(64);
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

    // Outside a Text parent, the baseline is the parent's bottom edge.
    [Fact]
    public void PixelsFromBaseline_ShouldMeasureFromParentBottom_WhenParentIsNotText()
    {
        ContainerRuntime parent = CreateContainer(100, 80);
        ContainerRuntime child = CreateContainer(10, 10);
        child.YUnits = GeneralUnitType.PixelsFromBaseline;
        child.Y = -10;
        parent.AddChild(child);

        child.AbsoluteTop.ShouldBe(70);
    }

    [Fact]
    public void TextBaselineOrigin_ShouldPlaceBottomEdgeAtY_WhenElementIsNotText()
    {
        ContainerRuntime parent = CreateContainer(100, 80);
        ContainerRuntime child = CreateContainer(10, 30);
        child.YOrigin = VerticalAlignment.TextBaseline;
        child.Y = 50;
        parent.AddChild(child);

        child.AbsoluteTop.ShouldBe(20);
    }

    // The obsolete PixelsFromMiddleInverted measures upward from the parent's middle.
    [Fact]
    public void PixelsFromMiddleInverted_ShouldPositionUpwardFromParentMiddle()
    {
        ContainerRuntime parent = CreateContainer(100, 100);
        ContainerRuntime child = CreateContainer(10, 10);
#pragma warning disable CS0618 // PixelsFromMiddleInverted is obsolete but still loads from older projects
        child.YUnits = GeneralUnitType.PixelsFromMiddleInverted;
#pragma warning restore CS0618
        child.Y = 10;
        parent.AddChild(child);

        child.AbsoluteTop.ShouldBe(40);
    }

    // Every position unit against every origin: the unit picks the reference point in the parent,
    // the origin picks which point of the child sits on it. Parent 200x200, child 20x20.
    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall, 10f, HorizontalAlignment.Left, 10f)]
    [InlineData(GeneralUnitType.PixelsFromSmall, 10f, HorizontalAlignment.Center, 0f)]
    [InlineData(GeneralUnitType.PixelsFromSmall, 10f, HorizontalAlignment.Right, -10f)]
    [InlineData(GeneralUnitType.PixelsFromMiddle, 10f, HorizontalAlignment.Left, 110f)]
    [InlineData(GeneralUnitType.PixelsFromMiddle, 10f, HorizontalAlignment.Center, 100f)]
    [InlineData(GeneralUnitType.PixelsFromMiddle, 10f, HorizontalAlignment.Right, 90f)]
    [InlineData(GeneralUnitType.PixelsFromLarge, 10f, HorizontalAlignment.Left, 210f)]
    [InlineData(GeneralUnitType.PixelsFromLarge, 10f, HorizontalAlignment.Center, 200f)]
    [InlineData(GeneralUnitType.PixelsFromLarge, 10f, HorizontalAlignment.Right, 190f)]
    [InlineData(GeneralUnitType.Percentage, 25f, HorizontalAlignment.Left, 50f)]
    [InlineData(GeneralUnitType.Percentage, 25f, HorizontalAlignment.Center, 40f)]
    [InlineData(GeneralUnitType.Percentage, 25f, HorizontalAlignment.Right, 30f)]
    public void PositionUnitAndOrigin_ShouldCombine_OnBothAxes(GeneralUnitType units, float value,
        HorizontalAlignment xOrigin, float expectedSmallEdge)
    {
        VerticalAlignment yOrigin = xOrigin switch
        {
            HorizontalAlignment.Left => VerticalAlignment.Top,
            HorizontalAlignment.Center => VerticalAlignment.Center,
            _ => VerticalAlignment.Bottom,
        };
        ContainerRuntime parent = CreateContainer(200, 200);
        ContainerRuntime child = CreateContainer(20, 20);
        child.XUnits = units;
        child.X = value;
        child.XOrigin = xOrigin;
        child.YUnits = units;
        child.Y = value;
        child.YOrigin = yOrigin;
        parent.AddChild(child);

        child.AbsoluteLeft.ShouldBe(expectedSmallEdge, "left");
        child.AbsoluteTop.ShouldBe(expectedSmallEdge, "top");
    }

    #endregion

    #region Setters

    // The element's ClipsChildren lives on the renderable, which keeps it across layouts.
    [Fact]
    public void ClipsChildren_ShouldReachRenderableAndSurviveLayout()
    {
        InvisibleRenderable renderable = new();
        GraphicalUiElement element = new(renderable);

        element.ClipsChildren = true;
        renderable.ClipsChildren.ShouldBeTrue();

        element.UpdateLayout();

        renderable.ClipsChildren.ShouldBeTrue();
        element.ClipsChildren.ShouldBeTrue();
    }

    [Theory]
    [InlineData("ClipsChildren")]
    [InlineData("Clips Children")]
    public void ClipsChildren_ShouldBeSettable_ThroughSetProperty(string propertyName)
    {
        InvisibleRenderable renderable = new();
        GraphicalUiElement element = new(renderable);

        element.SetProperty(propertyName, true);

        renderable.ClipsChildren.ShouldBeTrue();
    }

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
