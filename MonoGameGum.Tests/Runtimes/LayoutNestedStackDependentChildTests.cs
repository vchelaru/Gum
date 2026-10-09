using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Content-sized stacks nested several levels deep, holding children whose size depends on the stack
/// (percent of parent, RelativeToMaxParentOrChildren), children the stack's size ignores, and
/// descendants of those children. These are the cases where a stacked child is laid out more than once
/// per pass, and where its own descendants must see the final size of the stack around it. All expected
/// numbers are worked out by hand in each test.
/// </summary>
public class LayoutNestedStackDependentChildTests : BaseTestClass
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

    /// <summary>
    /// Wraps <paramref name="inner"/> in <paramref name="levels"/> content-sized TopToBottom stacks, each
    /// with a 7x5 box after it. Returns the outermost stack.
    /// </summary>
    private static ContainerRuntime WrapInStacks(ContainerRuntime inner, int levels, out List<ContainerRuntime> trailingBoxes)
    {
        trailingBoxes = new List<ContainerRuntime>();
        ContainerRuntime current = inner;
        for (int i = 0; i < levels; i++)
        {
            ContainerRuntime outer = ContentSizedStack(ChildrenLayout.TopToBottomStack);
            outer.AddChild(current);
            ContainerRuntime trailing = Box(7, 5);
            outer.AddChild(trailing);
            trailingBoxes.Add(trailing);
            current = outer;
        }
        return current;
    }

    #region Percent-of-parent child with descendants

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void PercentWidthChild_ShouldSizeItsDescendantsFromTheStackWidth_AtAnyNestingDepth(int outerLevels)
    {
        // Innermost stack: A (100x20), C (50% of the stack's width, 30 high) holding G (100% of C, 10 high), B (60x20).
        // C is ignored on the width axis, so the stack is 100 wide (A) and 70 tall (20 + 30 + 20).
        ContainerRuntime stack = ContentSizedStack(ChildrenLayout.TopToBottomStack);
        ContainerRuntime a = Box(100, 20);
        ContainerRuntime c = Box(50, 30);
        c.WidthUnits = DimensionUnitType.PercentageOfParent;
        ContainerRuntime g = Box(100, 10);
        g.WidthUnits = DimensionUnitType.PercentageOfParent;
        c.AddChild(g);
        ContainerRuntime b = Box(60, 20);
        stack.AddChild(a);
        stack.AddChild(c);
        stack.AddChild(b);

        ContainerRuntime outermost = WrapInStacks(stack, outerLevels, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteWidth.ShouldBe(100);
        stack.AbsoluteHeight.ShouldBe(70);
        c.AbsoluteWidth.ShouldBe(50);
        c.AbsoluteTop.ShouldBe(20);
        g.AbsoluteWidth.ShouldBe(50);
        g.AbsoluteLeft.ShouldBe(0);
        g.AbsoluteTop.ShouldBe(20);
        b.AbsoluteTop.ShouldBe(50);
        for (int i = 0; i < trailing.Count; i++)
        {
            // Level i sits on a stack that is 70 + 5 * i tall.
            trailing[i].AbsoluteTop.ShouldBe(70 + 5 * i);
        }
        outermost.AbsoluteHeight.ShouldBe(70 + 5 * outerLevels);

        // Widening A widens C and G with it; narrowing it back restores them.
        a.Width = 200;
        stack.AbsoluteWidth.ShouldBe(200);
        c.AbsoluteWidth.ShouldBe(100);
        g.AbsoluteWidth.ShouldBe(100);

        a.Width = 100;
        stack.AbsoluteWidth.ShouldBe(100);
        c.AbsoluteWidth.ShouldBe(50);
        g.AbsoluteWidth.ShouldBe(50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void PercentHeightChild_ShouldSizeItsDescendantsFromTheStackHeight_AtAnyNestingDepth(int outerLevels)
    {
        // A LeftToRight stack: A (50x40), C (30 wide, 50% of the stack's height) holding G (100% of C), B (20x10).
        // The height is 40 (A) and the width is 50 + 30 + 20 = 100.
        ContainerRuntime stack = ContentSizedStack(ChildrenLayout.LeftToRightStack);
        ContainerRuntime a = Box(50, 40);
        ContainerRuntime c = Box(30, 50);
        c.HeightUnits = DimensionUnitType.PercentageOfParent;
        ContainerRuntime g = Box(10, 100);
        g.HeightUnits = DimensionUnitType.PercentageOfParent;
        c.AddChild(g);
        ContainerRuntime b = Box(20, 10);
        stack.AddChild(a);
        stack.AddChild(c);
        stack.AddChild(b);

        ContainerRuntime outermost = WrapInStacks(stack, outerLevels, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteHeight.ShouldBe(40);
        stack.AbsoluteWidth.ShouldBe(100);
        c.AbsoluteLeft.ShouldBe(50);
        c.AbsoluteHeight.ShouldBe(20);
        g.AbsoluteHeight.ShouldBe(20);
        g.AbsoluteLeft.ShouldBe(50);
        b.AbsoluteLeft.ShouldBe(80);
        for (int i = 0; i < trailing.Count; i++)
        {
            trailing[i].AbsoluteTop.ShouldBe(40 + 5 * i);
        }

        a.Height = 80;
        stack.AbsoluteHeight.ShouldBe(80);
        c.AbsoluteHeight.ShouldBe(40);
        g.AbsoluteHeight.ShouldBe(40);
    }

    #endregion

    #region Ignored child with descendants

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void IgnoredChild_ShouldLayOutItsOwnDescendants_AndTakeRoomInTheStackButNotInItsSize(int outerLevels)
    {
        // Stack: A (60x10), I (80x40, ignored by the stack's size) holding G (50% of I, 8 high), B (30x10).
        // The stack is 60 wide and 10 + 10 = 20 tall, but B sits below I, at 10 + 40 = 50.
        ContainerRuntime stack = ContentSizedStack(ChildrenLayout.TopToBottomStack);
        ContainerRuntime a = Box(60, 10);
        ContainerRuntime ignored = Box(80, 40);
        ignored.IgnoredByParentSize = true;
        ContainerRuntime g = Box(50, 8);
        g.WidthUnits = DimensionUnitType.PercentageOfParent;
        ignored.AddChild(g);
        ContainerRuntime b = Box(30, 10);
        stack.AddChild(a);
        stack.AddChild(ignored);
        stack.AddChild(b);

        ContainerRuntime outermost = WrapInStacks(stack, outerLevels, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteWidth.ShouldBe(60);
        stack.AbsoluteHeight.ShouldBe(20);
        ignored.AbsoluteTop.ShouldBe(10);
        ignored.AbsoluteWidth.ShouldBe(80);
        g.AbsoluteWidth.ShouldBe(40);
        g.AbsoluteTop.ShouldBe(10);
        b.AbsoluteTop.ShouldBe(50);
        for (int i = 0; i < trailing.Count; i++)
        {
            trailing[i].AbsoluteTop.ShouldBe(20 + 5 * i);
        }

        // Growing the ignored child moves B and resizes G, and still leaves the stack's size alone.
        ignored.Width = 120;
        ignored.Height = 70;
        stack.AbsoluteHeight.ShouldBe(20);
        g.AbsoluteWidth.ShouldBe(60);
        b.AbsoluteTop.ShouldBe(80);
    }

    #endregion

    #region RelativeToMaxParentOrChildren child

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void RelativeToMaxParentOrChildrenChild_ShouldFillTheStackWidth_AndSizeItsDescendantsFromIt(int outerLevels)
    {
        // Stack: A (100x20) and M, which is the larger of its parent's width and its own content (a 40 wide
        // leaf), holding G (100% of M). The stack takes M's content size (40) and A (100), so it is 100 wide,
        // M is 100 wide, and so is G.
        ContainerRuntime stack = ContentSizedStack(ChildrenLayout.TopToBottomStack);
        ContainerRuntime a = Box(100, 20);
        ContainerRuntime m = Box(0, 30);
        m.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        ContainerRuntime content = Box(40, 10);
        ContainerRuntime g = Box(100, 10);
        g.WidthUnits = DimensionUnitType.PercentageOfParent;
        m.AddChild(content);
        m.AddChild(g);
        stack.AddChild(a);
        stack.AddChild(m);

        ContainerRuntime outermost = WrapInStacks(stack, outerLevels, out List<ContainerRuntime> trailing);

        outermost.UpdateLayout();
        stack.AbsoluteWidth.ShouldBe(100);
        m.AbsoluteWidth.ShouldBe(100);
        g.AbsoluteWidth.ShouldBe(100);
        m.AbsoluteTop.ShouldBe(20);
        stack.AbsoluteHeight.ShouldBe(50);

        // A narrower A leaves M at its own content width.
        a.Width = 30;
        stack.AbsoluteWidth.ShouldBe(40);
        m.AbsoluteWidth.ShouldBe(40);
        g.AbsoluteWidth.ShouldBe(40);

        a.Width = 100;
        stack.AbsoluteWidth.ShouldBe(100);
        m.AbsoluteWidth.ShouldBe(100);
        g.AbsoluteWidth.ShouldBe(100);
        for (int i = 0; i < trailing.Count; i++)
        {
            trailing[i].AbsoluteTop.ShouldBe(50 + 5 * i);
        }
    }

    #endregion

    #region UseFixedStackChildrenSize

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FixedSizeItems_ShouldPlaceNestedItemContentTheSame_WithAndWithoutTheFixedSizeFastPath(bool useFixedSize)
    {
        // Five items, each a content-sized LeftToRight stack of a 30x12 box, a 20x12 box and a nested
        // content-sized TopToBottom stack of two 10x6 boxes (12 tall). Every item is 12 tall, so the
        // fast path and the full path must agree.
        ContainerRuntime list = ContentSizedStack(ChildrenLayout.TopToBottomStack);
        list.StackSpacing = 3;
        list.UseFixedStackChildrenSize = useFixedSize;
        List<ContainerRuntime> items = new();
        List<ContainerRuntime> innerBoxes = new();
        for (int i = 0; i < 5; i++)
        {
            ContainerRuntime item = ContentSizedStack(ChildrenLayout.LeftToRightStack);
            item.AddChild(Box(30, 12));
            item.AddChild(Box(20, 12));
            ContainerRuntime inner = ContentSizedStack(ChildrenLayout.TopToBottomStack);
            inner.AddChild(Box(10, 6));
            ContainerRuntime second = Box(10, 6);
            inner.AddChild(second);
            innerBoxes.Add(second);
            item.AddChild(inner);
            items.Add(item);
            list.AddChild(item);
        }

        list.UpdateLayout();

        list.AbsoluteHeight.ShouldBe(5 * 12 + 4 * 3);
        list.AbsoluteWidth.ShouldBe(60);
        for (int i = 0; i < 5; i++)
        {
            float expectedTop = i * (12 + 3);
            items[i].AbsoluteTop.ShouldBe(expectedTop);
            items[i].AbsoluteHeight.ShouldBe(12);
            innerBoxes[i].AbsoluteLeft.ShouldBe(50);
            innerBoxes[i].AbsoluteTop.ShouldBe(expectedTop + 6);
        }

        // Growing the first item's content grows every item in the fast path and only that item otherwise;
        // either way the list ends up consistent with its first item.
        ((ContainerRuntime)items[0].Children[0]).Height = 12;
        list.AbsoluteHeight.ShouldBe(5 * 12 + 4 * 3);
    }

    #endregion

    #region Wrapping

    [Fact]
    public void WrappedItems_ShouldPlaceNestedItemContent_AndPushLaterSiblingsOfTheWrappingStack()
    {
        // A wrapping LeftToRight stack 100 wide holds five content-sized TopToBottom items, each a 40x10 and a
        // 30x5 box (40 wide, 15 tall). Two fit per row, so there are three rows and the stack is 45 tall.
        ContainerRuntime outer = ContentSizedStack(ChildrenLayout.TopToBottomStack);
        ContainerRuntime wrapper = ContentSizedStack(ChildrenLayout.LeftToRightStack);
        wrapper.WidthUnits = DimensionUnitType.Absolute;
        wrapper.Width = 100;
        wrapper.WrapsChildren = true;
        List<ContainerRuntime> items = new();
        List<ContainerRuntime> secondBoxes = new();
        for (int i = 0; i < 5; i++)
        {
            ContainerRuntime item = ContentSizedStack(ChildrenLayout.TopToBottomStack);
            item.AddChild(Box(40, 10));
            ContainerRuntime second = Box(30, 5);
            item.AddChild(second);
            items.Add(item);
            secondBoxes.Add(second);
            wrapper.AddChild(item);
        }
        ContainerRuntime after = Box(10, 10);
        outer.AddChild(wrapper);
        outer.AddChild(after);

        outer.UpdateLayout();

        wrapper.AbsoluteHeight.ShouldBe(45);
        after.AbsoluteTop.ShouldBe(45);
        for (int i = 0; i < 5; i++)
        {
            items[i].AbsoluteLeft.ShouldBe((i % 2) * 40);
            items[i].AbsoluteTop.ShouldBe((i / 2) * 15);
            secondBoxes[i].AbsoluteLeft.ShouldBe((i % 2) * 40);
            secondBoxes[i].AbsoluteTop.ShouldBe((i / 2) * 15 + 10);
        }

        // Narrowing to one item per row stacks them all.
        wrapper.Width = 50;
        wrapper.AbsoluteHeight.ShouldBe(75);
        after.AbsoluteTop.ShouldBe(75);
        for (int i = 0; i < 5; i++)
        {
            items[i].AbsoluteLeft.ShouldBe(0);
            items[i].AbsoluteTop.ShouldBe(i * 15);
            secondBoxes[i].AbsoluteTop.ShouldBe(i * 15 + 10);
        }
    }

    #endregion

    #region Text

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Text_ShouldMeasureTheSame_AtAnyNestingDepth(int levels)
    {
        TextRuntime reference = new();
        reference.WidthUnits = DimensionUnitType.RelativeToChildren;
        reference.HeightUnits = DimensionUnitType.RelativeToChildren;
        reference.Width = 0;
        reference.Height = 0;
        reference.Text = "Hello nested layout";
        float expectedTextWidth = reference.AbsoluteWidth;
        float expectedTextHeight = reference.AbsoluteHeight;
        expectedTextWidth.ShouldBeGreaterThan(0);

        // Each level: a 4x6 box, the next level (or the text at the innermost level), then a 9x3 box.
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.Height = 0;
        text.Text = "Hello nested layout";
        GraphicalUiElement current = text;
        List<ContainerRuntime> stacks = new();
        for (int i = 0; i < levels; i++)
        {
            ContainerRuntime stack = ContentSizedStack(ChildrenLayout.TopToBottomStack);
            stack.AddChild(Box(4, 6));
            stack.AddChild(current);
            stack.AddChild(Box(9, 3));
            stacks.Add(stack);
            current = stack;
        }

        current.UpdateLayout();

        text.AbsoluteWidth.ShouldBe(expectedTextWidth);
        text.AbsoluteHeight.ShouldBe(expectedTextHeight);
        text.AbsoluteTop.ShouldBe(6 * levels);
        for (int i = 0; i < levels; i++)
        {
            stacks[i].AbsoluteWidth.ShouldBe(System.Math.Max(9, expectedTextWidth));
            stacks[i].AbsoluteHeight.ShouldBe(expectedTextHeight + (6 + 3) * (i + 1));
        }

        text.Text = "Hi";
        text.AbsoluteWidth.ShouldBeLessThan(expectedTextWidth);
        stacks[levels - 1].AbsoluteHeight.ShouldBe(text.AbsoluteHeight + (6 + 3) * levels);
    }

    #endregion
}
