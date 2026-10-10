using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A content-sized stack lays each child out to measure it and again to place it. If every level did both in
/// full, a chain of k nested stacks would cost 2^k layouts (#5927). Laying a child out again is cut down to
/// placing it when the first layout changed nothing inside it and the child would read the same from its
/// parent. These tests pin that the cost stays linear, and that the shortcut gives the same result as laying
/// every child out every time.
/// </summary>
public class LayoutRepeatedChildVisitTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public LayoutRepeatedChildVisitTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static ContainerRuntime Box(float width, float height)
    {
        ContainerRuntime box = new();
        box.WidthUnits = DimensionUnitType.Absolute;
        box.HeightUnits = DimensionUnitType.Absolute;
        box.Width = width;
        box.Height = height;
        return box;
    }

    private static ContainerRuntime ContentSized()
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.RelativeToChildren;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;
        container.Width = 0;
        container.Height = 0;
        container.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        return container;
    }

    private static ContainerRuntime PercentWidthContentHeight()
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.PercentageOfParent;
        container.Width = 100;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;
        container.Height = 0;
        container.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        return container;
    }

    /// <summary>
    /// A chain of <paramref name="depth"/> levels, each holding three 30x10 boxes and the next level, ending in
    /// one more box. With <paramref name="zigZag"/> every other level is 100% of its parent's width.
    /// Returns the root; <paramref name="nodeCount"/> is every element in the tree.
    /// </summary>
    private static ContainerRuntime BuildChain(int depth, bool zigZag, out int nodeCount)
    {
        ContainerRuntime Level(int level) => zigZag && level % 2 == 1 ? PercentWidthContentHeight() : ContentSized();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        try
        {
            ContainerRuntime root = Level(0);
            ContainerRuntime current = root;
            nodeCount = 1;
            for (int level = 1; level < depth; level++)
            {
                for (int i = 0; i < 3; i++)
                {
                    current.AddChild(Box(30, 10));
                    nodeCount++;
                }
                ContainerRuntime next = Level(level);
                current.AddChild(next);
                current = next;
                nodeCount++;
            }
            current.AddChild(Box(30, 10));
            nodeCount++;
            return root;
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
    }

    /// <summary>
    /// A 400-wide, content-high stack that holds, at every level, a nested stack (<c>Ratio</c> width, content
    /// height) and then a 10x5 box, <paramref name="depth"/> levels deep. With <paramref name="ratioOnHeight"/> the
    /// nested stacks are instead content wide and <c>Ratio</c> high inside a fixed-height, left to right root.
    /// </summary>
    private static ContainerRuntime BuildRatioChain(int depth, bool ratioOnHeight, out int nodeCount)
    {
        ContainerRuntime Level(bool isRoot)
        {
            ContainerRuntime stack = new();
            if (ratioOnHeight)
            {
                stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
                stack.WidthUnits = isRoot ? DimensionUnitType.Absolute : DimensionUnitType.RelativeToChildren;
                stack.Width = isRoot ? 400 : 0;
                stack.HeightUnits = isRoot ? DimensionUnitType.Absolute : DimensionUnitType.Ratio;
                stack.Height = isRoot ? 300 : 1;
            }
            else
            {
                stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
                stack.WidthUnits = isRoot ? DimensionUnitType.Absolute : DimensionUnitType.Ratio;
                stack.Width = isRoot ? 400 : 1;
                stack.HeightUnits = DimensionUnitType.RelativeToChildren;
                stack.Height = 0;
            }
            return stack;
        }

        GraphicalUiElement.IsAllLayoutSuspended = true;
        try
        {
            ContainerRuntime root = Level(isRoot: true);
            ContainerRuntime current = root;
            nodeCount = 1;
            for (int level = 1; level <= depth; level++)
            {
                ContainerRuntime next = Level(isRoot: false);
                current.AddChild(next);
                current.AddChild(Box(10, 5));
                current = next;
                nodeCount += 2;
            }
            return root;
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
    }

    private static int CountLayoutCalls(Action layout)
    {
        int before = GraphicalUiElement.UpdateLayoutCallCount;
        layout();
        return GraphicalUiElement.UpdateLayoutCallCount - before;
    }

    #region Cost

    [Theory]
    [InlineData(8, false)]
    [InlineData(20, false)]
    [InlineData(8, true)]
    [InlineData(20, true)]
    public void NestedContentSizedStacks_ShouldCostAboutOneLayoutPerElement_WhenBuiltAndWhenLaidOutAgain(int depth, bool zigZag)
    {
        ContainerRuntime root = BuildChain(depth, zigZag, out int nodeCount);

        int firstLayout = CountLayoutCalls(() => root.UpdateLayout());
        int secondLayout = CountLayoutCalls(() => root.UpdateLayout());

        _output.WriteLine($"depth {depth} zigZag {zigZag}: {nodeCount} elements, first layout {firstLayout} calls, second layout {secondLayout} calls");

        // Doubling at every level would be millions of calls at depth 20; one visit per element is nodeCount.
        firstLayout.ShouldBeLessThanOrEqualTo(nodeCount * 3);
        secondLayout.ShouldBeLessThanOrEqualTo(nodeCount * 3);
    }

    [Fact]
    public void NestedContentSizedStacks_ShouldLayOutTheSameAsLayingEveryChildOutEveryTime()
    {
        // Depth 12 is small enough to lay out in full (about 20 thousand calls) to compare against.
        foreach (bool zigZag in new[] { false, true })
        {
            ContainerRuntime shortcut = BuildChain(12, zigZag, out _);
            shortcut.UpdateLayout();

            GraphicalUiElement.SkipRepeatedChildLayouts = false;
            ContainerRuntime full;
            try
            {
                full = BuildChain(12, zigZag, out _);
                full.UpdateLayout();
            }
            finally
            {
                GraphicalUiElement.SkipRepeatedChildLayouts = true;
            }

            Snapshot(shortcut).ShouldBe(Snapshot(full));
        }
    }

    [Theory]
    [InlineData(8, false)]
    [InlineData(20, false)]
    [InlineData(8, true)]
    [InlineData(20, true)]
    public void NestedStacksSizedWithRatio_ShouldCostAboutOneLayoutPerElement_WhenBuiltAndWhenLaidOutAgain(int depth, bool ratioOnHeight)
    {
        ContainerRuntime root = BuildRatioChain(depth, ratioOnHeight, out int nodeCount);

        int firstLayout = CountLayoutCalls(() => root.UpdateLayout());
        int secondLayout = CountLayoutCalls(() => root.UpdateLayout());

        _output.WriteLine($"depth {depth} ratioOnHeight {ratioOnHeight}: {nodeCount} elements, first layout {firstLayout} calls, second layout {secondLayout} calls");

        firstLayout.ShouldBeLessThanOrEqualTo(nodeCount * 3);
        secondLayout.ShouldBeLessThanOrEqualTo(nodeCount * 3);
    }

    [Fact]
    public void NestedStacksSizedWithRatio_ShouldLayOutTheSameAsLayingEveryChildOutEveryTime()
    {
        // Depth 10 is small enough to lay out in full (about two thousand calls) to compare against.
        foreach (bool ratioOnHeight in new[] { false, true })
        {
            ContainerRuntime shortcut = BuildRatioChain(10, ratioOnHeight, out _);
            shortcut.UpdateLayout();

            GraphicalUiElement.SkipRepeatedChildLayouts = false;
            ContainerRuntime full;
            try
            {
                full = BuildRatioChain(10, ratioOnHeight, out _);
                full.UpdateLayout();
            }
            finally
            {
                GraphicalUiElement.SkipRepeatedChildLayouts = true;
            }

            Snapshot(shortcut).ShouldBe(Snapshot(full));
        }
    }

    #endregion

    #region Results

    [Fact]
    public void NestedContentSizedStacks_ShouldSizeAndPlaceEveryLevel()
    {
        // Four levels. The first three each hold three 30x10 boxes and the next level; the last holds one box.
        // So the last level is 10 high, and going out each level adds its three boxes, 30: 40, 70, 100. All are
        // 30 wide. Each level sits below the boxes of the one around it, at 30 more than that one.
        ContainerRuntime root = BuildChain(4, zigZag: false, out _);

        root.UpdateLayout();

        root.AbsoluteWidth.ShouldBe(30);
        root.AbsoluteHeight.ShouldBe(100);
        GraphicalUiElement level1 = (GraphicalUiElement)root.Children[3];
        level1.AbsoluteHeight.ShouldBe(70);
        level1.AbsoluteTop.ShouldBe(30);
        GraphicalUiElement level2 = (GraphicalUiElement)level1.Children[3];
        level2.AbsoluteHeight.ShouldBe(40);
        level2.AbsoluteTop.ShouldBe(60);
        GraphicalUiElement level3 = (GraphicalUiElement)level2.Children[3];
        level3.AbsoluteHeight.ShouldBe(10);
        level3.AbsoluteTop.ShouldBe(90);
        ((GraphicalUiElement)level3.Children[0]).AbsoluteTop.ShouldBe(90);
    }

    [Fact]
    public void WideningABoxInAZigZagChain_ShouldWidenThePercentLevelsBelowIt()
    {
        // Four levels where levels 1 and 3 are 100% of their parent's width, which their parent's size
        // ignores. The boxes are 30 wide, so every level is 30 wide. Widening a box of the root to 50
        // makes the root 50 wide and level 1, its 100%, 50 wide. Level 2 is sized by its own boxes, so it
        // stays 30 wide, and level 3 is 100% of that.
        ContainerRuntime root = BuildChain(4, zigZag: true, out _);
        root.UpdateLayout();
        GraphicalUiElement level1 = (GraphicalUiElement)root.Children[3];
        GraphicalUiElement level2 = (GraphicalUiElement)level1.Children[3];
        GraphicalUiElement level3 = (GraphicalUiElement)level2.Children[3];
        level1.AbsoluteWidth.ShouldBe(30);

        ((GraphicalUiElement)root.Children[0]).Width = 50;

        root.AbsoluteWidth.ShouldBe(50);
        level1.AbsoluteWidth.ShouldBe(50);
        level2.AbsoluteWidth.ShouldBe(30);
        level3.AbsoluteWidth.ShouldBe(30);
    }

    [Fact]
    public void ChangeMadeByAnEventHandlerDuringLayout_ShouldBeLaidOutLikeAnyOther()
    {
        // D follows A's width, set from A's SizeChanged while the stack is being laid out. A is 20x30 and D
        // becomes 20 wide and stays 10 high, below A at 30. The stack is 20 wide and 40 high. Laying every child
        // out every time ends in the same place.
        string Run(bool skipRepeatedChildLayouts)
        {
            GraphicalUiElement.SkipRepeatedChildLayouts = skipRepeatedChildLayouts;
            try
            {
                // Built with layout suspended so that A is first sized by the layout below, which raises SizeChanged.
                GraphicalUiElement.IsAllLayoutSuspended = true;
                ContainerRuntime stack = ContentSized();
                ContainerRuntime a = Box(20, 30);
                ContainerRuntime d = Box(5, 10);
                a.SizeChanged += (_, _) => d.Width = a.AbsoluteWidth;
                stack.AddChild(a);
                stack.AddChild(d);
                ContainerRuntime outer = ContentSized();
                outer.AddChild(stack);
                outer.AddChild(Box(7, 5));
                GraphicalUiElement.IsAllLayoutSuspended = false;

                outer.UpdateLayout();

                d.AbsoluteWidth.ShouldBe(20);
                d.AbsoluteTop.ShouldBe(30);
                stack.AbsoluteWidth.ShouldBe(20);
                stack.AbsoluteHeight.ShouldBe(40);
                return Snapshot(outer);
            }
            finally
            {
                GraphicalUiElement.IsAllLayoutSuspended = false;
                GraphicalUiElement.SkipRepeatedChildLayouts = true;
            }
        }

        Run(skipRepeatedChildLayouts: true).ShouldBe(Run(skipRepeatedChildLayouts: false));
    }

    [Fact]
    public void WrappingStackWithAPercentChild_ShouldRaiseTheSameEventsAsLayingEveryChildOutEveryTime()
    {
        // The wrapping stack is laid out twice by the stack around it, and each layout of it moves its percent
        // child through other sizes on the way to the final one. Those sizes and places are reported as they
        // are reached, so skipping a layout that would only have repeated them must not drop or add any.
        List<string> Run(bool skipRepeatedChildLayouts)
        {
            GraphicalUiElement.SkipRepeatedChildLayouts = skipRepeatedChildLayouts;
            try
            {
                List<string> events = new();
                ContainerRuntime wrap = ContentSized();
                wrap.HeightUnits = DimensionUnitType.Absolute;
                wrap.Height = 70;
                wrap.WrapsChildren = true;
                wrap.StackSpacing = 2;
                ContainerRuntime[] children =
                {
                    Box(20, 30),
                    Box(20, 30),
                    Box(50, 30)
                };
                children[2].WidthUnits = DimensionUnitType.PercentageOfParent;
                for (int i = 0; i < children.Length; i++)
                {
                    int index = i;
                    children[i].SizeChanged += (_, _) => events.Add($"size {index}");
                    children[i].PositionChanged += (_, _) => events.Add($"position {index}");
                    wrap.AddChild(children[i]);
                }
                ContainerRuntime outer = ContentSized();
                outer.AddChild(wrap);

                outer.UpdateLayout();
                children[0].Width = 40;
                return events;
            }
            finally
            {
                GraphicalUiElement.SkipRepeatedChildLayouts = true;
            }
        }

        List<string> everyTime = Run(skipRepeatedChildLayouts: false);

        everyTime.ShouldNotBeEmpty();
        Run(skipRepeatedChildLayouts: true).ShouldBe(everyTime);
    }

    [Fact]
    public void ChildLaidOutOnOneAxisFirst_ShouldBeLaidOutInFullTheNextTime()
    {
        // A tree from the random sweep (seed 1228, cut down). It is circular on purpose: RelativeToMaxParentOrChildren
        // sizes that follow each other, so the result depends on how often each element is laid out and in
        // which way, and has no figure to work out by hand. C is positioned from the bottom of a parent sized
        // by its children, so the parent first lays out only its width (X), and later all of it. Laying C out
        // for X only does not leave it settled for a full layout, because the last step of a layout measures
        // only the axis it was asked for.
        string Run(bool skipRepeatedChildLayouts)
        {
            GraphicalUiElement.SkipRepeatedChildLayouts = skipRepeatedChildLayouts;
            try
            {
                GraphicalUiElement.IsAllLayoutSuspended = true;
                ContainerRuntime root = new();
                root.ChildrenLayout = ChildrenLayout.TopToBottomStack;
                root.Width = 300;
                root.Height = 200;

                ContainerRuntime grid = new();
                grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
                grid.AutoGridHorizontalCells = 2;
                grid.AutoGridVerticalCells = 2;
                grid.WidthUnits = DimensionUnitType.RelativeToChildren;
                grid.Width = 6;
                grid.HeightUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
                grid.Height = 4;

                ContainerRuntime ratio = new();
                ratio.ChildrenLayout = ChildrenLayout.AutoGridVertical;
                ratio.AutoGridHorizontalCells = 2;
                ratio.AutoGridVerticalCells = 2;
                ratio.WidthUnits = DimensionUnitType.Ratio;
                ratio.Width = 2;
                ratio.HeightUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
                ratio.Height = 4;

                ContainerRuntime followsHeight = new();
                followsHeight.ChildrenLayout = ChildrenLayout.TopToBottomStack;
                followsHeight.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;
                followsHeight.Width = 50;
                followsHeight.HeightUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
                followsHeight.Height = 4;

                ContainerRuntime c = new();
                c.Width = 40;
                c.HeightUnits = DimensionUnitType.RelativeToChildren;
                c.Height = 0;
                c.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;

                ContainerRuntime wrapper = new();
                wrapper.ChildrenLayout = ChildrenLayout.TopToBottomStack;
                wrapper.WrapsChildren = true;
                wrapper.MaxWidth = 105;
                wrapper.MaxHeight = 126;
                wrapper.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
                wrapper.Width = 0;
                wrapper.HeightUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
                wrapper.Height = 4;

                ContainerRuntime inner = new();
                inner.ChildrenLayout = ChildrenLayout.AutoGridVertical;
                inner.AutoGridVerticalCells = 3;
                inner.AutoGridHorizontalCells = 2;
                inner.WidthUnits = DimensionUnitType.PercentageOfParent;
                inner.Width = 25;
                inner.HeightUnits = DimensionUnitType.RelativeToChildren;
                inner.Height = 0;
                inner.Y = 16;

                wrapper.AddChild(inner);
                c.AddChild(wrapper);
                grid.AddChild(ratio);
                grid.AddChild(followsHeight);
                grid.AddChild(c);
                root.AddChild(grid);
                GraphicalUiElement.IsAllLayoutSuspended = false;

                root.UpdateLayout();
                return Snapshot(root);
            }
            finally
            {
                GraphicalUiElement.IsAllLayoutSuspended = false;
                GraphicalUiElement.SkipRepeatedChildLayouts = true;
            }
        }

        Run(skipRepeatedChildLayouts: true).ShouldBe(Run(skipRepeatedChildLayouts: false));
    }

    #endregion

    private static string Snapshot(GraphicalUiElement root)
    {
        System.Text.StringBuilder builder = new();
        void Walk(GraphicalUiElement element, string path)
        {
            builder.Append(path).Append(' ')
                .Append(element.AbsoluteLeft).Append(',').Append(element.AbsoluteTop).Append(' ')
                .Append(element.AbsoluteWidth).Append('x').Append(element.AbsoluteHeight).AppendLine();
            for (int i = 0; i < element.Children.Count; i++)
            {
                Walk((GraphicalUiElement)element.Children[i], path + "." + i);
            }
        }
        Walk(root, "r");
        return builder.ToString();
    }
}
