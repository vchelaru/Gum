using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A wrapping stack inside content-sized stacks, holding a child that is sized from the stack (percent of
/// parent) or from the room left in it (Ratio). A content-sized stack lays each of its children out more than
/// once, so the wrapping stack is laid out repeatedly, and every one of those layouts has to end with the
/// children in their final place. A repeat that re-runs only the wrapping step (placing the children line by
/// line) and not the pass after it leaves the children that are not part of the wrapping step (percent and
/// Ratio sizes) at the sizes the wrapping step gave them. All expected numbers are worked out by hand in each
/// test.
/// </summary>
public class LayoutWrappingStackNestedTests : BaseTestClass
{
    private static ContainerRuntime Box(float width, float height)
    {
        ContainerRuntime box = new();
        box.WidthUnits = DimensionUnitType.Absolute;
        box.HeightUnits = DimensionUnitType.Absolute;
        box.Width = width;
        box.Height = height;
        return box;
    }

    private static ContainerRuntime ContentSizedStack(ChildrenLayout layout)
    {
        ContainerRuntime stack = new();
        stack.WidthUnits = DimensionUnitType.RelativeToChildren;
        stack.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.Width = 0;
        stack.Height = 0;
        stack.ChildrenLayout = layout;
        return stack;
    }

    /// <summary>A TopToBottom wrapping stack that is 70 tall and as wide as its children.</summary>
    private static ContainerRuntime WrappingColumnStack(float spacing)
    {
        ContainerRuntime stack = ContentSizedStack(ChildrenLayout.TopToBottomStack);
        stack.HeightUnits = DimensionUnitType.Absolute;
        stack.Height = 70;
        stack.WrapsChildren = true;
        stack.StackSpacing = spacing;
        return stack;
    }

    /// <summary>A LeftToRight wrapping stack that is 70 wide and as tall as its children.</summary>
    private static ContainerRuntime WrappingRowStack(float spacing)
    {
        ContainerRuntime stack = ContentSizedStack(ChildrenLayout.LeftToRightStack);
        stack.WidthUnits = DimensionUnitType.Absolute;
        stack.Width = 70;
        stack.WrapsChildren = true;
        stack.StackSpacing = spacing;
        return stack;
    }

    private static ContainerRuntime PercentWidth(float percent, float height)
    {
        ContainerRuntime box = Box(percent, height);
        box.WidthUnits = DimensionUnitType.PercentageOfParent;
        return box;
    }

    private static ContainerRuntime PercentHeight(float width, float percent)
    {
        ContainerRuntime box = Box(width, percent);
        box.HeightUnits = DimensionUnitType.PercentageOfParent;
        return box;
    }

    /// <summary>
    /// Wraps <paramref name="inner"/> in <paramref name="levels"/> content-sized stacks of the given
    /// direction, each with a 7x5 box after it. Returns the outermost stack.
    /// </summary>
    private static ContainerRuntime WrapInStacks(ContainerRuntime inner, int levels, ChildrenLayout direction,
        out List<ContainerRuntime> trailingBoxes)
    {
        trailingBoxes = new List<ContainerRuntime>();
        ContainerRuntime current = inner;
        for (int i = 0; i < levels; i++)
        {
            ContainerRuntime outer = ContentSizedStack(direction);
            outer.AddChild(current);
            ContainerRuntime trailing = Box(7, 5);
            outer.AddChild(trailing);
            trailingBoxes.Add(trailing);
            current = outer;
        }
        return current;
    }

    private static string Snapshot(GraphicalUiElement root)
    {
        StringBuilder builder = new();
        void Walk(GraphicalUiElement element, string path)
        {
            builder.Append(path).Append(' ')
                .Append(element.AbsoluteLeft.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(element.AbsoluteTop.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(element.AbsoluteWidth.ToString(CultureInfo.InvariantCulture)).Append('x')
                .Append(element.AbsoluteHeight.ToString(CultureInfo.InvariantCulture)).AppendLine();
            for (int i = 0; i < element.Children.Count; i++)
            {
                Walk((GraphicalUiElement)element.Children[i], path + "." + i);
            }
        }
        Walk(root, "r");
        return builder.ToString();
    }

    #region Percent child

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(5, 2)]
    [InlineData(2, 4)]
    public void PercentWidthChild_InWrappingColumn_ShouldTakeHalfOfTheWidthOfTheOtherChildren(float spacing, int outerLevels)
    {
        // A (20x30), C (50% of the stack's width, 30 high). C is ignored by the stack's width, so the stack is
        // 20 wide and 70 tall, C is 10 wide, and both fit in one column: C sits at 30 + spacing, ending
        // at most at 30 + 5 + 30 = 65, inside the 70.
        ContainerRuntime stack = WrappingColumnStack(spacing);
        ContainerRuntime a = Box(20, 30);
        ContainerRuntime c = PercentWidth(50, 30);
        stack.AddChild(a);
        stack.AddChild(c);

        ContainerRuntime outermost = WrapInStacks(stack, outerLevels, ChildrenLayout.TopToBottomStack, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteWidth.ShouldBe(20);
        stack.AbsoluteHeight.ShouldBe(70);
        a.AbsoluteLeft.ShouldBe(0);
        a.AbsoluteTop.ShouldBe(0);
        c.AbsoluteWidth.ShouldBe(10);
        c.AbsoluteHeight.ShouldBe(30);
        c.AbsoluteLeft.ShouldBe(0);
        c.AbsoluteTop.ShouldBe(30 + spacing);
        for (int i = 0; i < trailing.Count; i++)
        {
            trailing[i].AbsoluteTop.ShouldBe(70 + 5 * i);
        }
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(5, 3)]
    public void PercentHeightChild_InWrappingRow_ShouldTakeHalfOfTheHeightOfTheOtherChildren(float spacing, int outerLevels)
    {
        // A (30x20), C (30 wide, 50% of the stack's height). C is ignored by the stack's height, so the stack
        // is 70 wide and 20 tall, C is 10 high, and both fit in one row: C sits at x = 30 + spacing.
        ContainerRuntime stack = WrappingRowStack(spacing);
        ContainerRuntime a = Box(30, 20);
        ContainerRuntime c = PercentHeight(30, 50);
        stack.AddChild(a);
        stack.AddChild(c);

        ContainerRuntime outermost = WrapInStacks(stack, outerLevels, ChildrenLayout.TopToBottomStack, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteWidth.ShouldBe(70);
        stack.AbsoluteHeight.ShouldBe(20);
        a.AbsoluteLeft.ShouldBe(0);
        c.AbsoluteHeight.ShouldBe(10);
        c.AbsoluteWidth.ShouldBe(30);
        c.AbsoluteLeft.ShouldBe(30 + spacing);
        c.AbsoluteTop.ShouldBe(0);
        for (int i = 0; i < trailing.Count; i++)
        {
            trailing[i].AbsoluteTop.ShouldBe(20 + 5 * i);
        }
    }

    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    public void PercentWidthChild_ShouldNotDependOnWhichWayTheStacksAroundTheWrappingStackRun(ChildrenLayout outerDirection)
    {
        // The wrapping column from the first test inside two stacks that run left to right or top to bottom.
        // The stack is 20x70 whichever way the stacks around it run, and C is 10 wide.
        ContainerRuntime stack = WrappingColumnStack(2);
        ContainerRuntime a = Box(20, 30);
        ContainerRuntime c = PercentWidth(50, 30);
        stack.AddChild(a);
        stack.AddChild(c);

        ContainerRuntime outermost = WrapInStacks(stack, 2, outerDirection, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteWidth.ShouldBe(20);
        stack.AbsoluteHeight.ShouldBe(70);
        c.AbsoluteWidth.ShouldBe(10);
        c.AbsoluteTop.ShouldBe(32);
    }

    #endregion

    #region Children that wrap

    [Fact]
    public void PercentWidthChild_ThatWrapsToANewColumn_ShouldTakeHalfOfTheWidthOfTheFirstColumn()
    {
        // Spacing 2, 70 tall. A (20x30) at 0, B (20x30) at 32 (ends at 62). C would start at 64 and end at 94,
        // past 70, so it wraps to a second column at x = 20 + 2 = 22, y = 0. C is ignored by the stack's
        // width, so the stack stays 20 wide and C is 50% of that, 10.
        ContainerRuntime stack = WrappingColumnStack(2);
        ContainerRuntime a = Box(20, 30);
        ContainerRuntime b = Box(20, 30);
        ContainerRuntime c = PercentWidth(50, 30);
        stack.AddChild(a);
        stack.AddChild(b);
        stack.AddChild(c);

        ContainerRuntime outermost = WrapInStacks(stack, 2, ChildrenLayout.TopToBottomStack, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteWidth.ShouldBe(20);
        stack.AbsoluteHeight.ShouldBe(70);
        a.AbsoluteTop.ShouldBe(0);
        b.AbsoluteTop.ShouldBe(32);
        b.AbsoluteLeft.ShouldBe(0);
        c.AbsoluteLeft.ShouldBe(22);
        c.AbsoluteTop.ShouldBe(0);
        c.AbsoluteWidth.ShouldBe(10);
    }

    [Fact]
    public void RatioHeightChild_NextToAPercentWidthChild_ShouldTakeTheRoomLeftInTheColumn()
    {
        // Spacing 2. A (20x30), R (20 wide, Ratio 1 high), C (50% of the width, 30 high). The column is 70 tall:
        // 70 - 30 - 30 - 2 * 2 (two gaps) = 6 is left for R. R sits at 32 and C at 32 + 6 + 2 = 40, and
        // C is 10 wide.
        ContainerRuntime stack = WrappingColumnStack(2);
        ContainerRuntime a = Box(20, 30);
        ContainerRuntime r = Box(20, 1);
        r.HeightUnits = DimensionUnitType.Ratio;
        ContainerRuntime c = PercentWidth(50, 30);
        stack.AddChild(a);
        stack.AddChild(r);
        stack.AddChild(c);

        ContainerRuntime outermost = WrapInStacks(stack, 2, ChildrenLayout.TopToBottomStack, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteWidth.ShouldBe(20);
        r.AbsoluteTop.ShouldBe(32);
        r.AbsoluteHeight.ShouldBe(6);
        c.AbsoluteTop.ShouldBe(40);
        c.AbsoluteWidth.ShouldBe(10);
    }

    #endregion

    #region Hidden and ignored children

    [Fact]
    public void PercentWidthChild_WithAHiddenSibling_ShouldStackAsIfTheSiblingWerentThere()
    {
        // A (20x30), H (60x30, hidden), C (50% of the width, 30 high). H takes no room and adds no width, so
        // the stack is 20 wide, C sits at 32 and is 10 wide.
        ContainerRuntime stack = WrappingColumnStack(2);
        ContainerRuntime a = Box(20, 30);
        ContainerRuntime hidden = Box(60, 30);
        hidden.Visible = false;
        ContainerRuntime c = PercentWidth(50, 30);
        stack.AddChild(a);
        stack.AddChild(hidden);
        stack.AddChild(c);

        ContainerRuntime outermost = WrapInStacks(stack, 2, ChildrenLayout.TopToBottomStack, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteWidth.ShouldBe(20);
        c.AbsoluteTop.ShouldBe(32);
        c.AbsoluteWidth.ShouldBe(10);
    }

    [Fact]
    public void PercentWidthChild_WithAChildIgnoredByTheStacksSize_ShouldStillStackAfterIt()
    {
        // A (20x30), I (50x10, ignored by the stack's size), C (50% of the width, 20 high). I adds no width, so
        // the stack is 20 wide, but it still takes room: I sits at 32 and C at 32 + 10 + 2 = 44, ending at
        // 64, inside the 70. C is 10 wide.
        ContainerRuntime stack = WrappingColumnStack(2);
        ContainerRuntime a = Box(20, 30);
        ContainerRuntime ignored = Box(50, 10);
        ignored.IgnoredByParentSize = true;
        ContainerRuntime c = PercentWidth(50, 20);
        stack.AddChild(a);
        stack.AddChild(ignored);
        stack.AddChild(c);

        ContainerRuntime outermost = WrapInStacks(stack, 2, ChildrenLayout.TopToBottomStack, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteWidth.ShouldBe(20);
        ignored.AbsoluteTop.ShouldBe(32);
        ignored.AbsoluteWidth.ShouldBe(50);
        c.AbsoluteLeft.ShouldBe(0);
        c.AbsoluteTop.ShouldBe(44);
        c.AbsoluteWidth.ShouldBe(10);
    }

    #endregion

    #region Edits

    private sealed class Parameters
    {
        public float AWidth = 20;
        public float Spacing = 2;
        public bool AVisible = true;
        public int ExtraBoxes;
    }

    private static ContainerRuntime Build(Parameters parameters, out ContainerRuntime a, out ContainerRuntime stack)
    {
        stack = WrappingColumnStack(parameters.Spacing);
        a = Box(parameters.AWidth, 30);
        a.Visible = parameters.AVisible;
        stack.AddChild(a);
        stack.AddChild(PercentWidth(50, 30));
        for (int i = 0; i < parameters.ExtraBoxes; i++)
        {
            stack.AddChild(Box(40, 30));
        }
        return WrapInStacks(stack, 2, ChildrenLayout.TopToBottomStack, out List<ContainerRuntime> _);
    }

    [Fact]
    public void EditingAWrappingStackInsideStacks_ShouldMatchABuildOfTheEditedTree()
    {
        Parameters parameters = new();
        ContainerRuntime live = Build(parameters, out ContainerRuntime a, out ContainerRuntime stack);
        live.UpdateLayout();

        // A wider A widens C with it: 40 wide, C is 20.
        parameters.AWidth = 40;
        a.Width = 40;
        ContainerRuntime fresh = Build(parameters, out _, out _);
        fresh.UpdateLayout();
        Snapshot(live).ShouldBe(Snapshot(fresh));
        stack.AbsoluteWidth.ShouldBe(40);
        ((GraphicalUiElement)stack.Children[1]).AbsoluteWidth.ShouldBe(20);

        // More spacing moves C down: 30 + 6 = 36, ending at 66, still in the column.
        parameters.Spacing = 6;
        stack.StackSpacing = 6;
        fresh = Build(parameters, out _, out _);
        fresh.UpdateLayout();
        Snapshot(live).ShouldBe(Snapshot(fresh));
        ((GraphicalUiElement)stack.Children[1]).AbsoluteTop.ShouldBe(36);

        // A box (40x30) added at the end starts at 36 + 30 + 6 = 72, past 70, so it wraps to a second column at
        // x = 40 + 6 = 46, y = 0. The stack is now 40 + 6 + 40 = 86 wide, and C is 43.
        parameters.ExtraBoxes = 1;
        ContainerRuntime added = Box(40, 30);
        stack.AddChild(added);
        fresh = Build(parameters, out _, out _);
        fresh.UpdateLayout();
        Snapshot(live).ShouldBe(Snapshot(fresh));
        added.AbsoluteLeft.ShouldBe(46);
        added.AbsoluteTop.ShouldBe(0);
        stack.AbsoluteWidth.ShouldBe(86);
        ((GraphicalUiElement)stack.Children[1]).AbsoluteWidth.ShouldBe(43);

        // Hiding A leaves C and the box: the stack is 40 wide (the box), C is 50% of that, 20.
        parameters.AVisible = false;
        a.Visible = false;
        fresh = Build(parameters, out _, out _);
        fresh.UpdateLayout();
        Snapshot(live).ShouldBe(Snapshot(fresh));
        stack.AbsoluteWidth.ShouldBe(40);
        ((GraphicalUiElement)stack.Children[1]).AbsoluteWidth.ShouldBe(20);

        // Removing the box leaves C alone: with nothing else to size the stack it is 0 wide, and so is C.
        parameters.ExtraBoxes = 0;
        stack.RemoveChild(added);
        fresh = Build(parameters, out _, out _);
        fresh.UpdateLayout();
        Snapshot(live).ShouldBe(Snapshot(fresh));
        stack.AbsoluteWidth.ShouldBe(0);
        ((GraphicalUiElement)stack.Children[1]).AbsoluteWidth.ShouldBe(0);
    }

    [Fact]
    public void LayingOutAgain_ShouldChangeNothing()
    {
        ContainerRuntime stack = WrappingColumnStack(2);
        stack.AddChild(Box(20, 30));
        stack.AddChild(Box(20, 30));
        stack.AddChild(PercentWidth(50, 30));
        ContainerRuntime outermost = WrapInStacks(stack, 3, ChildrenLayout.LeftToRightStack, out List<ContainerRuntime> _);

        outermost.UpdateLayout();
        string first = Snapshot(outermost);
        outermost.UpdateLayout();
        Snapshot(outermost).ShouldBe(first);
        stack.UpdateLayout();
        Snapshot(outermost).ShouldBe(first);
    }

    #endregion
}
