using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;

namespace Gum.Layout.Benchmarks;

/// <summary>
/// A built element tree plus the node a benchmark edits.
/// </summary>
/// <param name="Root">The element whose <c>UpdateLayout</c> a full-layout run calls.</param>
/// <param name="Leaf">The element a leaf-edit run changes the width of.</param>
/// <param name="NodeCount">Total elements in the tree, root included.</param>
public sealed record TreeUnderTest(GraphicalUiElement Root, GraphicalUiElement Leaf, int NodeCount);

/// <summary>
/// Builders for the tree shapes the layout benchmarks measure. Each builds the tree with layout
/// globally suspended, then runs one full layout so the first measured run starts from a settled tree.
/// </summary>
public static class LayoutTreeShapes
{
    private const float CanvasWidth = 800;
    private const float CanvasHeight = 600;
    private const int SiblingsPerLevel = 3;

    /// <summary>One Regular container with <paramref name="count"/> absolute children.</summary>
    public static TreeUnderTest FlatRegular(int count)
    {
        return Build(() =>
        {
            ContainerRuntime root = Box(CanvasWidth, CanvasHeight);
            ContainerRuntime leaf = Box(10, 10);
            for (int i = 0; i < count; i++)
            {
                root.AddChild(i == count / 2 ? leaf : Box(10, 10));
            }
            return new TreeUnderTest(root, leaf, count + 1);
        });
    }

    /// <summary>A content-height vertical stack of <paramref name="count"/> absolute children, like a list's inner panel.</summary>
    public static TreeUnderTest VerticalStack(int count)
    {
        return Build(() =>
        {
            ContainerRuntime root = ContentHeightStack(ChildrenLayout.TopToBottomStack, 400);
            ContainerRuntime leaf = Box(100, 20);
            for (int i = 0; i < count; i++)
            {
                root.AddChild(i == count / 2 ? leaf : Box(100, 20));
            }
            return new TreeUnderTest(root, leaf, count + 1);
        });
    }

    /// <summary>A vertical stack of <paramref name="rows"/> content-sized horizontal rows, three absolute cells each.</summary>
    public static TreeUnderTest StackOfContentSizedRows(int rows)
    {
        return Build(() =>
        {
            ContainerRuntime root = ContentHeightStack(ChildrenLayout.TopToBottomStack, 400);
            ContainerRuntime leaf = Box(50, 20);
            int nodes = 1;
            for (int i = 0; i < rows; i++)
            {
                ContainerRuntime row = ContentSized(ChildrenLayout.LeftToRightStack);
                row.AddChild(Box(50, 20));
                row.AddChild(i == rows / 2 ? leaf : Box(50, 20));
                row.AddChild(Box(50, 20));
                root.AddChild(row);
                nodes += 4;
            }
            return new TreeUnderTest(root, leaf, nodes);
        });
    }

    /// <summary>
    /// A chain <paramref name="depth"/> levels deep where every level is a content-sized vertical stack
    /// holding some absolute siblings and the next level.
    /// </summary>
    public static TreeUnderTest DeepContentSized(int depth)
    {
        return Build(() => Chain(depth, _ => ContentSized(ChildrenLayout.TopToBottomStack)));
    }

    /// <summary>A chain <paramref name="depth"/> levels deep where every level is a Regular, percent-of-parent container.</summary>
    public static TreeUnderTest DeepPercentOfParent(int depth)
    {
        return Build(() => Chain(depth, _ => PercentOfParent()));
    }

    /// <summary>
    /// A chain where levels alternate between content-sized stacks (size flows up from children) and
    /// percent-of-parent width containers with content height (width flows down, height flows up).
    /// </summary>
    public static TreeUnderTest ZigZag(int depth)
    {
        return Build(() => Chain(depth, level => level % 2 == 0
            ? ContentSized(ChildrenLayout.TopToBottomStack)
            : PercentWidthContentHeight(ChildrenLayout.TopToBottomStack)));
    }

    /// <summary>A fixed-width horizontal stack of <paramref name="count"/> <c>Ratio</c>-width children.</summary>
    public static TreeUnderTest RatioRow(int count)
    {
        return Build(() =>
        {
            ContainerRuntime root = Box(CanvasWidth, 40);
            root.ChildrenLayout = ChildrenLayout.LeftToRightStack;
            ContainerRuntime leaf = RatioBox();
            for (int i = 0; i < count; i++)
            {
                root.AddChild(i == count / 2 ? leaf : RatioBox());
            }
            return new TreeUnderTest(root, leaf, count + 1);
        });
    }

    private static TreeUnderTest Chain(int depth, Func<int, ContainerRuntime> createLevel)
    {
        ContainerRuntime root = createLevel(0);
        ContainerRuntime current = root;
        int nodes = 1;
        for (int level = 1; level < depth; level++)
        {
            for (int i = 0; i < SiblingsPerLevel; i++)
            {
                current.AddChild(Box(30, 10));
                nodes++;
            }
            ContainerRuntime next = createLevel(level);
            current.AddChild(next);
            current = next;
            nodes++;
        }
        ContainerRuntime leaf = Box(30, 10);
        current.AddChild(leaf);
        return new TreeUnderTest(root, leaf, nodes + 1);
    }

    private static TreeUnderTest Build(Func<TreeUnderTest> create)
    {
        GraphicalUiElement.IsAllLayoutSuspended = true;
        TreeUnderTest tree;
        try
        {
            tree = create();
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
        tree.Root.UpdateLayout();
        return tree;
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

    private static ContainerRuntime RatioBox()
    {
        ContainerRuntime box = Box(1, 20);
        box.WidthUnits = DimensionUnitType.Ratio;
        return box;
    }

    private static ContainerRuntime ContentSized(ChildrenLayout layout)
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.RelativeToChildren;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;
        container.Width = 0;
        container.Height = 0;
        container.ChildrenLayout = layout;
        return container;
    }

    private static ContainerRuntime ContentHeightStack(ChildrenLayout layout, float width)
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.Width = width;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;
        container.Height = 0;
        container.ChildrenLayout = layout;
        return container;
    }

    private static ContainerRuntime PercentOfParent()
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.PercentageOfParent;
        container.HeightUnits = DimensionUnitType.PercentageOfParent;
        container.Width = 100;
        container.Height = 100;
        return container;
    }

    private static ContainerRuntime PercentWidthContentHeight(ChildrenLayout layout)
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.PercentageOfParent;
        container.Width = 100;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;
        container.Height = 0;
        container.ChildrenLayout = layout;
        return container;
    }
}
