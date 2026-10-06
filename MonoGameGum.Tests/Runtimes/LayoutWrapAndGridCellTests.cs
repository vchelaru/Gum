using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Sections 2.4 (wrapping stacks) and 3.1 (AutoGrid cells) of LAYOUT_TEST_PLAN.md. Tests drive
/// layout through public properties only and assert on absolute positions and sizes.
/// </summary>
public class LayoutWrapAndGridCellTests : BaseTestClass
{
    static ContainerRuntime CreateContainer(float width, float height)
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.Width = width;
        container.Height = height;
        return container;
    }

    /// <summary>
    /// A wrapping stack with an Absolute main axis and the given cross-axis units. Children are
    /// sized (main, cross) along the stack.
    /// </summary>
    static ContainerRuntime CreateWrappingStack(ChildrenLayout stack, float mainSize, DimensionUnitType crossUnits, float crossSize)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime parent = new();
        parent.ChildrenLayout = stack;
        parent.WrapsChildren = true;
        parent.WidthUnits = stacksVertically ? crossUnits : DimensionUnitType.Absolute;
        parent.HeightUnits = stacksVertically ? DimensionUnitType.Absolute : crossUnits;
        parent.Width = stacksVertically ? crossSize : mainSize;
        parent.Height = stacksVertically ? mainSize : crossSize;
        return parent;
    }

    static ContainerRuntime CreateStackChild(ChildrenLayout stack, float main, float cross) =>
        stack == ChildrenLayout.TopToBottomStack ? CreateContainer(cross, main) : CreateContainer(main, cross);

    static float MainPosition(ChildrenLayout stack, GraphicalUiElement element) =>
        stack == ChildrenLayout.TopToBottomStack ? element.AbsoluteTop : element.AbsoluteLeft;

    static float CrossPosition(ChildrenLayout stack, GraphicalUiElement element) =>
        stack == ChildrenLayout.TopToBottomStack ? element.AbsoluteLeft : element.AbsoluteTop;

    static float CrossSize(ChildrenLayout stack, GraphicalUiElement element) =>
        stack == ChildrenLayout.TopToBottomStack ? element.AbsoluteWidth : element.AbsoluteHeight;

    static void SetMainSize(ChildrenLayout stack, GraphicalUiElement element, float value)
    {
        if (stack == ChildrenLayout.TopToBottomStack)
        {
            element.Height = value;
        }
        else
        {
            element.Width = value;
        }
    }

    static void AssertPlacement(ChildrenLayout stack, GraphicalUiElement parent, float[] expectedMain, float[] expectedCross, string context)
    {
        for (int i = 0; i < expectedMain.Length; i++)
        {
            MainPosition(stack, parent.Children[i]).ShouldBe(expectedMain[i], $"{context}: child {i} main");
            CrossPosition(stack, parent.Children[i]).ShouldBe(expectedCross[i], $"{context}: child {i} cross");
        }
    }

    #region Stacks (2.1-2.3)

    static ContainerRuntime CreateStack(ChildrenLayout stack, float width, DimensionUnitType widthUnits, float height, DimensionUnitType heightUnits)
    {
        ContainerRuntime parent = new();
        parent.ChildrenLayout = stack;
        parent.WidthUnits = widthUnits;
        parent.HeightUnits = heightUnits;
        parent.Width = width;
        parent.Height = height;
        return parent;
    }

    static void SetMainPosition(ChildrenLayout stack, GraphicalUiElement element, GeneralUnitType units, float value)
    {
        if (stack == ChildrenLayout.TopToBottomStack)
        {
            element.YUnits = units;
            element.Y = value;
        }
        else
        {
            element.XUnits = units;
            element.X = value;
        }
    }

    // A later child measures its main-axis position from the previous sibling's far edge whatever its
    // units; the value still adds space (Children Layout docs, "Stacking and Units").
    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack, GeneralUnitType.PixelsFromMiddle, 10f, 60f)]
    [InlineData(ChildrenLayout.TopToBottomStack, GeneralUnitType.PixelsFromLarge, 10f, 60f)]
    [InlineData(ChildrenLayout.TopToBottomStack, GeneralUnitType.Percentage, 0f, 50f)]
    [InlineData(ChildrenLayout.LeftToRightStack, GeneralUnitType.PixelsFromMiddle, 10f, 60f)]
    [InlineData(ChildrenLayout.LeftToRightStack, GeneralUnitType.PixelsFromLarge, 10f, 60f)]
    [InlineData(ChildrenLayout.LeftToRightStack, GeneralUnitType.Percentage, 0f, 50f)]
    public void Stack_LaterChildMainAxisUnits_ShouldMeasureFromPreviousSibling(ChildrenLayout stack, GeneralUnitType units,
        float value, float expectedMainPosition)
    {
        ContainerRuntime parent = CreateStack(stack, 400, DimensionUnitType.Absolute, 400, DimensionUnitType.Absolute);
        parent.AddChild(CreateContainer(50, 50));
        ContainerRuntime later = CreateContainer(50, 50);
        SetMainPosition(stack, later, units, value);
        parent.AddChild(later);
        ContainerRuntime last = CreateContainer(50, 50);
        parent.AddChild(last);

        MainPosition(stack, later).ShouldBe(expectedMainPosition);
        MainPosition(stack, last).ShouldBe(expectedMainPosition + 50);
    }

    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    public void Stack_CrossAxisPercentageSizeAndPosition_ShouldUseParentCrossSize(ChildrenLayout stack)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime parent = CreateStack(stack, 400, DimensionUnitType.Absolute, 400, DimensionUnitType.Absolute);
        parent.AddChild(CreateContainer(50, 50));
        ContainerRuntime child = CreateContainer(50, 50);
        if (stacksVertically)
        {
            child.WidthUnits = DimensionUnitType.PercentageOfParent;
            child.Width = 50;
            child.XUnits = GeneralUnitType.Percentage;
            child.X = 25;
        }
        else
        {
            child.HeightUnits = DimensionUnitType.PercentageOfParent;
            child.Height = 50;
            child.YUnits = GeneralUnitType.Percentage;
            child.Y = 25;
        }
        parent.AddChild(child);

        MainPosition(stack, child).ShouldBe(50);
        CrossPosition(stack, child).ShouldBe(100);
        CrossSize(stack, child).ShouldBe(200);
    }

    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    public void Stack_FirstChildHiddenThenShown_ShouldRestack(ChildrenLayout stack)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime parent = stacksVertically
            ? CreateStack(stack, 100, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren)
            : CreateStack(stack, 0, DimensionUnitType.RelativeToChildren, 100, DimensionUnitType.Absolute);
        ContainerRuntime first = CreateContainer(50, 50);
        first.Visible = false;
        parent.AddChild(first);
        parent.AddChild(CreateContainer(50, 50));
        parent.AddChild(CreateContainer(50, 50));
        MainPosition(stack, parent.Children[1]).ShouldBe(0);
        MainPosition(stack, parent.Children[2]).ShouldBe(50);
        (stacksVertically ? parent.AbsoluteHeight : parent.AbsoluteWidth).ShouldBe(100);

        first.Visible = true;

        MainPosition(stack, first).ShouldBe(0);
        MainPosition(stack, parent.Children[1]).ShouldBe(50);
        MainPosition(stack, parent.Children[2]).ShouldBe(100);
        (stacksVertically ? parent.AbsoluteHeight : parent.AbsoluteWidth).ShouldBe(150);
    }

    // Without WrapsChildren the child that crosses the max stays in this line, so the stack grows
    // to the max to hold as much of it as it can. Children keep stacking past it.
    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack, DimensionUnitType.RelativeToChildren, 120, 120)]
    [InlineData(ChildrenLayout.LeftToRightStack, DimensionUnitType.RelativeToChildren, 120, 120)]
    [InlineData(ChildrenLayout.TopToBottomStack, DimensionUnitType.RelativeToChildren, 150, 150)]
    [InlineData(ChildrenLayout.TopToBottomStack, DimensionUnitType.RelativeToMaxParentOrChildren, 120, 120)]
    [InlineData(ChildrenLayout.LeftToRightStack, DimensionUnitType.RelativeToMaxParentOrChildren, 120, 120)]
    public void Stack_SizedToChildrenMainAxisWithMax_ShouldClampToMax_AndNotWrap(ChildrenLayout stack, DimensionUnitType mainUnits, float max, float expectedSize)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime holder = CreateContainer(50, 50);
        ContainerRuntime parent = stacksVertically
            ? CreateStack(stack, 100, DimensionUnitType.Absolute, 0, mainUnits)
            : CreateStack(stack, 0, mainUnits, 100, DimensionUnitType.Absolute);
        if (stacksVertically)
        {
            parent.MaxHeight = max;
        }
        else
        {
            parent.MaxWidth = max;
        }
        holder.AddChild(parent);
        for (int i = 0; i < 3; i++)
        {
            parent.AddChild(CreateContainer(50, 50));
        }

        (stacksVertically ? parent.AbsoluteHeight : parent.AbsoluteWidth).ShouldBe(expectedSize);
        MainPosition(stack, parent.Children[2]).ShouldBe(100);
        CrossPosition(stack, parent.Children[2]).ShouldBe(0);
    }

    // With WrapsChildren the child that crosses the max moves to the next line, so the size stops
    // at the last child that fits.
    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    public void WrappingStack_RelativeToChildrenMainAxisWithMax_ShouldStopAtLastChildThatFits(ChildrenLayout stack)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime parent = stacksVertically
            ? CreateStack(stack, 100, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren)
            : CreateStack(stack, 0, DimensionUnitType.RelativeToChildren, 100, DimensionUnitType.Absolute);
        parent.WrapsChildren = true;
        if (stacksVertically)
        {
            parent.MaxHeight = 120;
        }
        else
        {
            parent.MaxWidth = 120;
        }
        for (int i = 0; i < 3; i++)
        {
            parent.AddChild(CreateContainer(50, 50));
        }

        (stacksVertically ? parent.AbsoluteHeight : parent.AbsoluteWidth).ShouldBe(100);
        MainPosition(stack, parent.Children[2]).ShouldBe(0);
        CrossPosition(stack, parent.Children[2]).ShouldBe(50);
    }

    // A RelativeToMaxParentOrChildren child counts toward its parent by its children-based size,
    // which must be clamped by the child's own max the same way the child's size is.
    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    public void RelativeToChildrenParent_ContainingMaxParentOrChildrenStackWithMax_ShouldMeasureClampedStack(ChildrenLayout stack)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime outer = CreateStack(ChildrenLayout.Regular, 0, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.RelativeToChildren);
        ContainerRuntime inner = stacksVertically
            ? CreateStack(stack, 100, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToMaxParentOrChildren)
            : CreateStack(stack, 0, DimensionUnitType.RelativeToMaxParentOrChildren, 100, DimensionUnitType.Absolute);
        if (stacksVertically)
        {
            inner.MaxHeight = 120;
        }
        else
        {
            inner.MaxWidth = 120;
        }
        outer.AddChild(inner);
        for (int i = 0; i < 3; i++)
        {
            inner.AddChild(CreateContainer(50, 50));
        }

        (stacksVertically ? inner.AbsoluteHeight : inner.AbsoluteWidth).ShouldBe(120);
        (stacksVertically ? outer.AbsoluteHeight : outer.AbsoluteWidth).ShouldBe(120);
    }

    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    public void Stack_ChildClampedByItsOwnMinMax_ShouldStackAndMeasureByClampedSize(ChildrenLayout stack)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime parent = stacksVertically
            ? CreateStack(stack, 100, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren)
            : CreateStack(stack, 0, DimensionUnitType.RelativeToChildren, 100, DimensionUnitType.Absolute);
        ContainerRuntime clampedDown = CreateStackChild(stack, main: 100, cross: 20);
        ContainerRuntime clampedUp = CreateStackChild(stack, main: 10, cross: 20);
        if (stacksVertically)
        {
            clampedDown.MaxHeight = 60;
            clampedUp.MinHeight = 30;
        }
        else
        {
            clampedDown.MaxWidth = 60;
            clampedUp.MinWidth = 30;
        }
        parent.AddChild(clampedDown);
        parent.AddChild(clampedUp);
        ContainerRuntime last = CreateStackChild(stack, main: 10, cross: 20);
        parent.AddChild(last);

        MainPosition(stack, clampedUp).ShouldBe(60);
        MainPosition(stack, last).ShouldBe(90);
        (stacksVertically ? parent.AbsoluteHeight : parent.AbsoluteWidth).ShouldBe(100);
    }

    // The fixed-size fast path is TopToBottomStack only; a LeftToRightStack lays out from each child's own width.
    [Fact]
    public void UseFixedStackChildrenSize_LeftToRightStack_ShouldMatchNonFixedLayout()
    {
        float[] widths = { 30, 50, 20 };
        float[] expectedLefts = { 0, 35, 90 };
        float expectedWidth = 110;

        foreach (bool useFixedSize in new[] { true, false })
        {
            ContainerRuntime parent = CreateStack(ChildrenLayout.LeftToRightStack, 0, DimensionUnitType.RelativeToChildren, 100, DimensionUnitType.Absolute);
            parent.UseFixedStackChildrenSize = useFixedSize;
            parent.StackSpacing = 5;
            foreach (float width in widths)
            {
                parent.AddChild(CreateContainer(width, 20));
            }

            for (int i = 0; i < widths.Length; i++)
            {
                parent.Children[i].AbsoluteLeft.ShouldBe(expectedLefts[i], $"fixed {useFixedSize}, child {i}");
            }
            parent.AbsoluteWidth.ShouldBe(expectedWidth, $"fixed {useFixedSize}");
        }
    }

    [Fact]
    public void UseFixedStackChildrenSize_FirstChildResized_ShouldMoveLaterChildren_AndResizeParent()
    {
        ContainerRuntime parent = CreateStack(ChildrenLayout.TopToBottomStack, 100, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren);
        parent.UseFixedStackChildrenSize = true;
        for (int i = 0; i < 3; i++)
        {
            parent.AddChild(CreateContainer(50, 50));
        }
        parent.Children[2].AbsoluteTop.ShouldBe(100);

        parent.Children[0].Height = 30;

        parent.Children[1].AbsoluteTop.ShouldBe(30);
        parent.Children[2].AbsoluteTop.ShouldBe(60);
        parent.AbsoluteHeight.ShouldBe(90);
    }

    #endregion

    #region Wrapping stacks (2.4)

    [Theory]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    public void WrapsChildren_ParentResized_ShouldRewrapAndUnwrap(ChildrenLayout stack)
    {
        float lineCross = 20;
        ContainerRuntime parent = CreateWrappingStack(stack, mainSize: 100, DimensionUnitType.RelativeToChildren, crossSize: 0);
        for (int i = 0; i < 4; i++)
        {
            parent.AddChild(CreateStackChild(stack, main: 40, cross: lineCross));
        }
        AssertPlacement(stack, parent, new float[] { 0, 40, 0, 40 }, new float[] { 0, 0, 20, 20 }, "two per line");
        CrossSize(stack, parent).ShouldBe(2 * lineCross);

        SetMainSize(stack, parent, 50);

        AssertPlacement(stack, parent, new float[] { 0, 0, 0, 0 }, new float[] { 0, 20, 40, 60 }, "narrowed");
        CrossSize(stack, parent).ShouldBe(4 * lineCross);

        SetMainSize(stack, parent, 200);

        AssertPlacement(stack, parent, new float[] { 0, 40, 80, 120 }, new float[] { 0, 0, 0, 0 }, "widened");
        CrossSize(stack, parent).ShouldBe(lineCross);
    }

    // The wrapping parent's main axis follows its own parent, so resizing the grandparent re-wraps.
    [Theory]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    public void WrapsChildren_ParentPercentageOfGrandparent_ShouldRewrap_WhenGrandparentResizes(ChildrenLayout stack)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime grandparent = CreateContainer(200, 200);
        ContainerRuntime parent = CreateWrappingStack(stack, mainSize: 50, DimensionUnitType.Absolute, crossSize: 200);
        if (stacksVertically)
        {
            parent.HeightUnits = DimensionUnitType.PercentageOfParent;
        }
        else
        {
            parent.WidthUnits = DimensionUnitType.PercentageOfParent;
        }
        grandparent.AddChild(parent);
        for (int i = 0; i < 3; i++)
        {
            parent.AddChild(CreateStackChild(stack, main: 40, cross: 20));
        }
        AssertPlacement(stack, parent, new float[] { 0, 40, 0 }, new float[] { 0, 0, 20 }, "parent 100");

        SetMainSize(stack, grandparent, 400);

        AssertPlacement(stack, parent, new float[] { 0, 40, 80 }, new float[] { 0, 0, 0 }, "parent 200");

        SetMainSize(stack, grandparent, 100);

        AssertPlacement(stack, parent, new float[] { 0, 0, 0 }, new float[] { 0, 20, 40 }, "parent 50");
    }

    [Theory]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    public void WrapsChildren_RemovingChildren_ShouldNotLeaveStaleLineSizes(ChildrenLayout stack)
    {
        ContainerRuntime parent = CreateWrappingStack(stack, mainSize: 100, DimensionUnitType.RelativeToChildren, crossSize: 0);
        parent.AddChild(CreateStackChild(stack, main: 60, cross: 20));
        ContainerRuntime tallLine = CreateStackChild(stack, main: 60, cross: 80);
        parent.AddChild(tallLine);
        ContainerRuntime thirdLine = CreateStackChild(stack, main: 60, cross: 30);
        parent.AddChild(thirdLine);
        CrossPosition(stack, thirdLine).ShouldBe(100);
        CrossSize(stack, parent).ShouldBe(130);

        parent.Children.Remove(tallLine);

        CrossPosition(stack, thirdLine).ShouldBe(20);
        CrossSize(stack, parent).ShouldBe(50);

        // A new, shorter child takes the removed child's line; the line is sized from it alone.
        ContainerRuntime newSecondLine = CreateStackChild(stack, main: 60, cross: 10);
        parent.AddChild(newSecondLine);

        CrossPosition(stack, thirdLine).ShouldBe(20);
        CrossPosition(stack, newSecondLine).ShouldBe(50);
        CrossSize(stack, parent).ShouldBe(60);

        parent.Children.Clear();
        ContainerRuntime onlyChild = CreateStackChild(stack, main: 60, cross: 5);
        parent.AddChild(onlyChild);

        CrossPosition(stack, onlyChild).ShouldBe(0);
        CrossSize(stack, parent).ShouldBe(5);
    }

    [Theory]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    public void WrapsChildren_HidingEveryChildOfALine_ShouldCloseTheLine(ChildrenLayout stack)
    {
        ContainerRuntime parent = CreateWrappingStack(stack, mainSize: 100, DimensionUnitType.RelativeToChildren, crossSize: 0);
        parent.StackSpacing = 5;
        parent.AddChild(CreateStackChild(stack, main: 60, cross: 20));
        ContainerRuntime hiddenLine = CreateStackChild(stack, main: 100, cross: 80);
        parent.AddChild(hiddenLine);
        ContainerRuntime lastLine = CreateStackChild(stack, main: 60, cross: 30);
        parent.AddChild(lastLine);
        CrossPosition(stack, lastLine).ShouldBe(20 + 5 + 80 + 5);

        hiddenLine.Visible = false;

        CrossPosition(stack, lastLine).ShouldBe(20 + 5);
        CrossSize(stack, parent).ShouldBe(20 + 5 + 30);

        hiddenLine.Visible = true;

        CrossPosition(stack, lastLine).ShouldBe(20 + 5 + 80 + 5);
        CrossSize(stack, parent).ShouldBe(20 + 5 + 80 + 5 + 30);
    }

    // A child's cross-axis offset is part of its line's size, so it pushes the next line out.
    [Theory]
    [InlineData(ChildrenLayout.LeftToRightStack, 10f)]
    [InlineData(ChildrenLayout.TopToBottomStack, 10f)]
    [InlineData(ChildrenLayout.LeftToRightStack, -5f)]
    [InlineData(ChildrenLayout.TopToBottomStack, -5f)]
    public void WrapsChildren_ChildWithCrossAxisOffset_ShouldGrowItsLine(ChildrenLayout stack, float offset)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        float childCross = 20;
        ContainerRuntime parent = CreateWrappingStack(stack, mainSize: 100, DimensionUnitType.RelativeToChildren, crossSize: 0);
        ContainerRuntime first = CreateStackChild(stack, main: 60, cross: childCross);
        parent.AddChild(first);
        ContainerRuntime offsetChild = CreateStackChild(stack, main: 60, cross: childCross);
        if (stacksVertically)
        {
            offsetChild.X = offset;
        }
        else
        {
            offsetChild.Y = offset;
        }
        parent.AddChild(offsetChild);
        ContainerRuntime third = CreateStackChild(stack, main: 60, cross: childCross);
        parent.AddChild(third);

        CrossPosition(stack, offsetChild).ShouldBe(childCross + offset);
        CrossPosition(stack, third).ShouldBe(childCross + offset + childCross);
        CrossSize(stack, parent).ShouldBe(childCross + offset + childCross + childCross);
    }

    // Intended (documented on DimensionUnitType.Ratio): Ratio subtracts every visible sibling on its
    // axis, whatever the layout, so a wrapping stack does not give it the rest of its own line.
    [Theory]
    [InlineData(ChildrenLayout.LeftToRightStack, 30f, 70f)]
    [InlineData(ChildrenLayout.TopToBottomStack, 30f, 70f)]
    [InlineData(ChildrenLayout.LeftToRightStack, 120f, 0f)]
    [InlineData(ChildrenLayout.TopToBottomStack, 120f, 0f)]
    public void WrapsChildren_RatioChild_ShouldGetParentMinusEverySibling(ChildrenLayout stack, float siblingMain, float expectedRatioMain)
    {
        bool stacksVertically = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime parent = CreateWrappingStack(stack, mainSize: 100, DimensionUnitType.Absolute, crossSize: 200);
        parent.AddChild(CreateStackChild(stack, main: siblingMain, cross: 20));
        ContainerRuntime ratioChild = CreateStackChild(stack, main: 1, cross: 20);
        if (stacksVertically)
        {
            ratioChild.HeightUnits = DimensionUnitType.Ratio;
        }
        else
        {
            ratioChild.WidthUnits = DimensionUnitType.Ratio;
        }
        parent.AddChild(ratioChild);

        (stacksVertically ? ratioChild.AbsoluteHeight : ratioChild.AbsoluteWidth).ShouldBe(expectedRatioMain);
    }

    [Theory]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    public void WrapsChildren_ChildrenLayoutSwitchedAwayAndBack_ShouldWrapAgain(ChildrenLayout stack)
    {
        ContainerRuntime parent = CreateWrappingStack(stack, mainSize: 100, DimensionUnitType.Absolute, crossSize: 200);
        for (int i = 0; i < 3; i++)
        {
            parent.AddChild(CreateStackChild(stack, main: 40, cross: 20));
        }
        float[] wrappedMain = { 0, 40, 0 };
        float[] wrappedCross = { 0, 0, 20 };
        AssertPlacement(stack, parent, wrappedMain, wrappedCross, "wrapped");

        parent.ChildrenLayout = ChildrenLayout.Regular;

        AssertPlacement(stack, parent, new float[] { 0, 0, 0 }, new float[] { 0, 0, 0 }, "regular");

        parent.ChildrenLayout = stack;

        AssertPlacement(stack, parent, wrappedMain, wrappedCross, "wrapped again");
    }

    #endregion

    #region AutoGrid cells (3.1)

    [Theory]
    [InlineData(ChildrenLayout.AutoGridHorizontal, 3, 2, 300f, 200f, 6)]
    [InlineData(ChildrenLayout.AutoGridVertical, 3, 2, 300f, 200f, 6)]
    [InlineData(ChildrenLayout.AutoGridHorizontal, 1, 4, 100f, 400f, 4)]
    [InlineData(ChildrenLayout.AutoGridVertical, 1, 4, 100f, 400f, 4)]
    [InlineData(ChildrenLayout.AutoGridHorizontal, 4, 1, 400f, 100f, 4)]
    [InlineData(ChildrenLayout.AutoGridVertical, 4, 1, 400f, 100f, 4)]
    [InlineData(ChildrenLayout.AutoGridHorizontal, 3, 3, 100f, 100f, 9)]
    [InlineData(ChildrenLayout.AutoGridVertical, 3, 3, 100f, 100f, 9)]
    public void AutoGrid_NonSquareOrFractionalCells_ShouldPlaceAndSizeFillChildren(ChildrenLayout grid, int columns, int rows,
        float gridWidth, float gridHeight, int childCount)
    {
        float cellWidth = gridWidth / columns;
        float cellHeight = gridHeight / rows;
        ContainerRuntime parent = CreateContainer(gridWidth, gridHeight);
        parent.ChildrenLayout = grid;
        parent.AutoGridHorizontalCells = columns;
        parent.AutoGridVerticalCells = rows;
        for (int i = 0; i < childCount; i++)
        {
            ContainerRuntime child = new();
            child.Dock(Dock.Fill);
            parent.AddChild(child);
        }

        for (int i = 0; i < childCount; i++)
        {
            int column = grid == ChildrenLayout.AutoGridHorizontal ? i % columns : i / rows;
            int row = grid == ChildrenLayout.AutoGridHorizontal ? i / columns : i % rows;
            GraphicalUiElement child = parent.Children[i];
            child.AbsoluteLeft.ShouldBe(cellWidth * column, tolerance: 0.001f, $"child {i} left");
            child.AbsoluteTop.ShouldBe(cellHeight * row, tolerance: 0.001f, $"child {i} top");
            child.AbsoluteWidth.ShouldBe(cellWidth, tolerance: 0.001f, $"child {i} width");
            child.AbsoluteHeight.ShouldBe(cellHeight, tolerance: 0.001f, $"child {i} height");
        }
    }

    // Each child positions itself in its cell as if the cell were its parent (Children Layout docs).
    // The cell under test is the bottom-right one of a 2x2 grid, so it starts at (100, 100).
    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall, 5f, HorizontalAlignment.Left, 105f)]
    [InlineData(GeneralUnitType.PixelsFromMiddle, 0f, HorizontalAlignment.Center, 140f)]
    [InlineData(GeneralUnitType.PixelsFromMiddle, 5f, HorizontalAlignment.Left, 155f)]
    [InlineData(GeneralUnitType.PixelsFromLarge, 0f, HorizontalAlignment.Right, 180f)]
    [InlineData(GeneralUnitType.PixelsFromLarge, -5f, HorizontalAlignment.Center, 185f)]
    [InlineData(GeneralUnitType.Percentage, 50f, HorizontalAlignment.Left, 150f)]
    [InlineData(GeneralUnitType.Percentage, 100f, HorizontalAlignment.Right, 180f)]
    public void AutoGrid_ChildPositionUnitsAndOrigin_ShouldBeRelativeToItsCell(GeneralUnitType units, float value,
        HorizontalAlignment origin, float expectedSmallEdge)
    {
        VerticalAlignment verticalOrigin = origin switch
        {
            HorizontalAlignment.Left => VerticalAlignment.Top,
            HorizontalAlignment.Center => VerticalAlignment.Center,
            _ => VerticalAlignment.Bottom,
        };

        foreach (ChildrenLayout layout in new[] { ChildrenLayout.AutoGridHorizontal, ChildrenLayout.AutoGridVertical })
        {
            ContainerRuntime grid = CreateContainer(200, 200);
            grid.ChildrenLayout = layout;
            grid.AutoGridHorizontalCells = 2;
            grid.AutoGridVerticalCells = 2;
            for (int i = 0; i < 3; i++)
            {
                grid.AddChild(CreateContainer(20, 20));
            }
            ContainerRuntime child = CreateContainer(20, 20);
            child.XUnits = units;
            child.X = value;
            child.XOrigin = origin;
            child.YUnits = units;
            child.Y = value;
            child.YOrigin = verticalOrigin;
            grid.AddChild(child);

            child.AbsoluteLeft.ShouldBe(expectedSmallEdge, $"{layout} left");
            child.AbsoluteTop.ShouldBe(expectedSmallEdge, $"{layout} top");
        }
    }

    // A child is not clamped to its cell: an Absolute size larger than the cell overflows it, and
    // the next child still takes its own cell.
    [Theory]
    [InlineData(ChildrenLayout.AutoGridHorizontal)]
    [InlineData(ChildrenLayout.AutoGridVertical)]
    public void AutoGrid_AbsoluteChildLargerThanCell_ShouldOverflowCell_AndNotMoveSiblings(ChildrenLayout layout)
    {
        ContainerRuntime grid = CreateContainer(200, 200);
        grid.ChildrenLayout = layout;
        grid.AutoGridHorizontalCells = 2;
        grid.AutoGridVerticalCells = 2;
        ContainerRuntime large = CreateContainer(150, 170);
        grid.AddChild(large);
        ContainerRuntime rightAligned = CreateContainer(150, 170);
        rightAligned.XUnits = GeneralUnitType.PixelsFromLarge;
        rightAligned.XOrigin = HorizontalAlignment.Right;
        rightAligned.YUnits = GeneralUnitType.PixelsFromLarge;
        rightAligned.YOrigin = VerticalAlignment.Bottom;
        grid.AddChild(rightAligned);
        ContainerRuntime third = CreateContainer(20, 20);
        grid.AddChild(third);
        bool isHorizontal = layout == ChildrenLayout.AutoGridHorizontal;
        float secondCellLeft = isHorizontal ? 100 : 0;
        float secondCellTop = isHorizontal ? 0 : 100;

        large.AbsoluteLeft.ShouldBe(0);
        large.AbsoluteTop.ShouldBe(0);
        large.AbsoluteWidth.ShouldBe(150);
        large.AbsoluteHeight.ShouldBe(170);
        rightAligned.AbsoluteLeft.ShouldBe(secondCellLeft + 100 - 150);
        rightAligned.AbsoluteTop.ShouldBe(secondCellTop + 100 - 170);
        third.AbsoluteLeft.ShouldBe(isHorizontal ? 0 : 100);
        third.AbsoluteTop.ShouldBe(isHorizontal ? 100 : 0);
        grid.AbsoluteWidth.ShouldBe(200);
        grid.AbsoluteHeight.ShouldBe(200);
    }

    [Fact]
    public void AutoGrid_RelativeToParentChild_ShouldSizeFromItsCell()
    {
        ContainerRuntime grid = CreateContainer(300, 200);
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 3;
        grid.AutoGridVerticalCells = 2;
        grid.AddChild(CreateContainer(10, 10));
        ContainerRuntime child = new();
        child.WidthUnits = DimensionUnitType.RelativeToParent;
        child.Width = -10;
        child.HeightUnits = DimensionUnitType.RelativeToParent;
        child.Height = -20;
        grid.AddChild(child);

        child.AbsoluteLeft.ShouldBe(100);
        child.AbsoluteWidth.ShouldBe(90);
        child.AbsoluteHeight.ShouldBe(80);
    }

    [Theory]
    [InlineData(ChildrenLayout.AutoGridHorizontal)]
    [InlineData(ChildrenLayout.AutoGridVertical)]
    public void AutoGrid_ChildInsertedRemovedOrMoved_ShouldReassignCells(ChildrenLayout layout)
    {
        ContainerRuntime grid = CreateContainer(200, 200);
        grid.ChildrenLayout = layout;
        grid.AutoGridHorizontalCells = 2;
        grid.AutoGridVerticalCells = 2;
        ContainerRuntime a = CreateContainer(10, 10);
        ContainerRuntime b = CreateContainer(10, 10);
        ContainerRuntime c = CreateContainer(10, 10);
        grid.AddChild(a);
        grid.AddChild(b);
        grid.AddChild(c);
        bool isHorizontal = layout == ChildrenLayout.AutoGridHorizontal;
        (float Left, float Top)[] cells = isHorizontal
            ? new[] { (0f, 0f), (100f, 0f), (0f, 100f) }
            : new[] { (0f, 0f), (0f, 100f), (100f, 0f) };

        ContainerRuntime inserted = CreateContainer(10, 10);
        grid.Children.Insert(0, inserted);

        (inserted.AbsoluteLeft, inserted.AbsoluteTop).ShouldBe(cells[0], "inserted");
        (a.AbsoluteLeft, a.AbsoluteTop).ShouldBe(cells[1], "a after insert");
        (b.AbsoluteLeft, b.AbsoluteTop).ShouldBe(cells[2], "b after insert");

        grid.Children.Remove(inserted);

        (a.AbsoluteLeft, a.AbsoluteTop).ShouldBe(cells[0], "a after remove");
        (c.AbsoluteLeft, c.AbsoluteTop).ShouldBe(cells[2], "c after remove");

        grid.Children.Move(2, 0);

        (c.AbsoluteLeft, c.AbsoluteTop).ShouldBe(cells[0], "c after move");
        (a.AbsoluteLeft, a.AbsoluteTop).ShouldBe(cells[1], "a after move");
        (b.AbsoluteLeft, b.AbsoluteTop).ShouldBe(cells[2], "b after move");
    }

    // An IgnoredByParentSize child still takes its cell, but does not size the grid's cells.
    [Fact]
    public void AutoGrid_IgnoredByParentSizeChild_ShouldTakeACell_ButNotSizeTheGrid()
    {
        ContainerRuntime grid = new();
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 2;
        grid.AutoGridVerticalCells = 1;
        grid.WidthUnits = DimensionUnitType.Absolute;
        grid.Width = 200;
        grid.HeightUnits = DimensionUnitType.RelativeToChildren;
        grid.Height = 0;
        ContainerRuntime ignored = CreateContainer(50, 300);
        ignored.IgnoredByParentSize = true;
        grid.AddChild(ignored);
        ContainerRuntime sized = CreateContainer(50, 40);
        grid.AddChild(sized);

        grid.AbsoluteHeight.ShouldBe(40);
        sized.AbsoluteLeft.ShouldBe(100);
    }

    #endregion
}
