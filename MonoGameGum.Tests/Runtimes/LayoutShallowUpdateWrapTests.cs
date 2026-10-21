using Gum.DataTypes;
using Gum.Converters;
using RenderingLibrary.Graphics;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A shallow layout (<c>UpdateLayout(true, 0)</c>, which showing a hidden element and moving it run) of a wrapping
/// stack must leave every child where a full layout put it (#6315). The wrap pass places a child whose position
/// or size comes from its siblings only as far as the lines known by then allow, and a full layout finishes it in
/// the pass after, so a shallow layout has to finish it too.
/// </summary>
public class LayoutShallowUpdateWrapTests : BaseTestClass
{
    private static ContainerRuntime Stack(bool vertical, float width, float height)
    {
        ContainerRuntime stack = new();
        stack.ChildrenLayout = vertical ? ChildrenLayout.TopToBottomStack : ChildrenLayout.LeftToRightStack;
        stack.WrapsChildren = true;
        stack.WidthUnits = DimensionUnitType.Absolute;
        stack.Width = width;
        stack.HeightUnits = DimensionUnitType.Absolute;
        stack.Height = height;
        stack.StackSpacing = 5;
        return stack;
    }

    private static ContainerRuntime Fixed(float width, float height)
    {
        ContainerRuntime child = new();
        child.WidthUnits = DimensionUnitType.Absolute;
        child.Width = width;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.Height = height;
        return child;
    }

    private static List<GraphicalUiElement> Everything(GraphicalUiElement root)
    {
        List<GraphicalUiElement> all = new() { root };
        for (int i = 0; i < all.Count; i++)
        {
            foreach (GraphicalUiElement child in all[i].Children!)
            {
                all.Add(child);
            }
        }
        return all;
    }

    private static string Snapshot(GraphicalUiElement root)
    {
        StringBuilder builder = new();
        foreach (GraphicalUiElement element in Everything(root))
        {
            builder.Append(element.AbsoluteLeft).Append(',').Append(element.AbsoluteTop).Append(' ')
                .Append(element.AbsoluteWidth).Append('x').Append(element.AbsoluteHeight).Append(" | ");
        }
        return builder.ToString();
    }

    /// <summary>
    /// Lays the tree out in full, then shallow-lays-out each element in turn (on the element itself and so its
    /// parent's climb too) and checks that nothing moved.
    /// </summary>
    private static void ShallowLayoutOfEveryElementShouldChangeNothing(GraphicalUiElement root)
    {
        root.UpdateLayout();
        string full = Snapshot(root);

        List<GraphicalUiElement> all = Everything(root);
        for (int i = 0; i < all.Count; i++)
        {
            all[i].UpdateLayout(true, 0);
            Snapshot(root).ShouldBe(full, $"after UpdateLayout(true, 0) on element {i}");
        }
    }

    [Fact]
    public void ShallowUpdateLayout_OnWrappingStackWithRatioChild_ShouldNotMoveRatioChild()
    {
        ContainerRuntime stack = Stack(vertical: true, width: 37, height: 29);
        stack.MaxWidth = 42;
        stack.MaxHeight = 84;
        stack.AddChild(Fixed(39, 53));

        ContainerRuntime ratio = new();
        ratio.WidthUnits = DimensionUnitType.Absolute;
        ratio.Width = 29;
        ratio.HeightUnits = DimensionUnitType.Ratio;
        ratio.Height = 1;
        ratio.XOrigin = HorizontalAlignment.Right;
        ratio.X = 17;
        stack.AddChild(ratio);

        ratio.AbsoluteLeft.ShouldBe(32);

        stack.UpdateLayout(true, 0);

        ratio.AbsoluteLeft.ShouldBe(32);
    }

    [Fact]
    public void ShallowUpdateLayout_OnWrappingRowWithRatioWidthChild_ShouldNotMoveRatioChild()
    {
        ContainerRuntime stack = Stack(vertical: false, width: 29, height: 37);
        stack.MaxWidth = 84;
        stack.MaxHeight = 42;
        stack.AddChild(Fixed(53, 39));

        ContainerRuntime ratio = new();
        ratio.WidthUnits = DimensionUnitType.Ratio;
        ratio.Width = 1;
        ratio.HeightUnits = DimensionUnitType.Absolute;
        ratio.Height = 29;
        ratio.YOrigin = VerticalAlignment.Bottom;
        ratio.Y = 17;
        stack.AddChild(ratio);

        ShallowLayoutOfEveryElementShouldChangeNothing(stack);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShallowUpdateLayout_WithRatioChildNotLast_ShouldNotMoveAnyChild(bool vertical)
    {
        ContainerRuntime stack = Stack(vertical, width: 40, height: 40);
        stack.AddChild(Fixed(30, 30));

        ContainerRuntime ratio = new();
        ratio.WidthUnits = vertical ? DimensionUnitType.Absolute : DimensionUnitType.Ratio;
        ratio.HeightUnits = vertical ? DimensionUnitType.Ratio : DimensionUnitType.Absolute;
        ratio.Width = vertical ? 20 : 1;
        ratio.Height = vertical ? 1 : 20;
        ratio.XOrigin = HorizontalAlignment.Right;
        ratio.YOrigin = VerticalAlignment.Bottom;
        ratio.X = 10;
        ratio.Y = 10;
        stack.AddChild(ratio);

        stack.AddChild(Fixed(25, 25));
        stack.AddChild(Fixed(25, 25));

        ShallowLayoutOfEveryElementShouldChangeNothing(stack);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShallowUpdateLayout_WithSeveralWrappedLines_ShouldNotMoveAnyChild(bool vertical)
    {
        ContainerRuntime stack = Stack(vertical, width: 70, height: 70);
        for (int i = 0; i < 3; i++)
        {
            stack.AddChild(Fixed(30, 30));

            ContainerRuntime ratio = new();
            ratio.WidthUnits = vertical ? DimensionUnitType.Absolute : DimensionUnitType.Ratio;
            ratio.HeightUnits = vertical ? DimensionUnitType.Ratio : DimensionUnitType.Absolute;
            ratio.Width = vertical ? 12 + i : 1;
            ratio.Height = vertical ? 1 : 12 + i;
            ratio.XOrigin = HorizontalAlignment.Center;
            ratio.YOrigin = VerticalAlignment.Center;
            ratio.XUnits = GeneralUnitType.PixelsFromMiddle;
            ratio.YUnits = GeneralUnitType.PixelsFromMiddle;
            stack.AddChild(ratio);
        }

        ShallowLayoutOfEveryElementShouldChangeNothing(stack);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShallowUpdateLayout_OnWrappingStackSizedToItsChildren_ShouldNotMoveAnyChild(bool vertical)
    {
        ContainerRuntime stack = Stack(vertical, width: 0, height: 0);
        if (vertical)
        {
            stack.WidthUnits = DimensionUnitType.RelativeToChildren;
            stack.MaxHeight = 70;
            stack.HeightUnits = DimensionUnitType.Absolute;
            stack.Height = 70;
        }
        else
        {
            stack.HeightUnits = DimensionUnitType.RelativeToChildren;
            stack.MaxWidth = 70;
            stack.WidthUnits = DimensionUnitType.Absolute;
            stack.Width = 70;
        }
        stack.AddChild(Fixed(30, 30));

        ContainerRuntime ratio = new();
        ratio.WidthUnits = vertical ? DimensionUnitType.Absolute : DimensionUnitType.Ratio;
        ratio.HeightUnits = vertical ? DimensionUnitType.Ratio : DimensionUnitType.Absolute;
        ratio.Width = vertical ? 20 : 1;
        ratio.Height = vertical ? 1 : 20;
        stack.AddChild(ratio);
        stack.AddChild(Fixed(30, 30));

        ShallowLayoutOfEveryElementShouldChangeNothing(stack);
    }

    [Theory]
    [InlineData(GeneralUnitType.PixelsFromLarge)]
    [InlineData(GeneralUnitType.PixelsFromMiddle)]
    [InlineData(GeneralUnitType.Percentage)]
    public void ShallowUpdateLayout_WithChildPositionedFromItsWrappedLine_ShouldNotMoveAnyChild(GeneralUnitType units)
    {
        ContainerRuntime stack = Stack(vertical: true, width: 80, height: 60);
        stack.AddChild(Fixed(30, 40));

        ContainerRuntime ratio = new();
        ratio.WidthUnits = DimensionUnitType.Absolute;
        ratio.Width = 20;
        ratio.HeightUnits = DimensionUnitType.Ratio;
        ratio.Height = 1;
        ratio.XUnits = units;
        ratio.X = units == GeneralUnitType.Percentage ? 50 : 4;
        ratio.XOrigin = HorizontalAlignment.Right;
        stack.AddChild(ratio);
        stack.AddChild(Fixed(15, 30));

        ShallowLayoutOfEveryElementShouldChangeNothing(stack);
    }

    [Fact]
    public void ShallowUpdateLayout_WithChildHeightAPercentOfParent_ShouldNotMoveAnyChild()
    {
        ContainerRuntime stack = Stack(vertical: true, width: 80, height: 60);
        stack.AddChild(Fixed(30, 40));

        ContainerRuntime percent = new();
        percent.WidthUnits = DimensionUnitType.Absolute;
        percent.Width = 20;
        percent.HeightUnits = DimensionUnitType.PercentageOfParent;
        percent.Height = 50;
        percent.XOrigin = HorizontalAlignment.Right;
        percent.X = 4;
        stack.AddChild(percent);
        stack.AddChild(Fixed(15, 30));

        ShallowLayoutOfEveryElementShouldChangeNothing(stack);
    }

    [Fact]
    public void ShallowUpdateLayout_WithWrappingStackNestedInWrappingStack_ShouldNotMoveAnyChild()
    {
        ContainerRuntime outer = Stack(vertical: true, width: 90, height: 70);
        outer.AddChild(Fixed(40, 40));

        ContainerRuntime inner = Stack(vertical: false, width: 50, height: 40);
        inner.HeightUnits = DimensionUnitType.Ratio;
        inner.Height = 1;
        inner.AddChild(Fixed(30, 15));

        ContainerRuntime ratio = new();
        ratio.WidthUnits = DimensionUnitType.Ratio;
        ratio.Width = 1;
        ratio.HeightUnits = DimensionUnitType.Absolute;
        ratio.Height = 12;
        ratio.YOrigin = VerticalAlignment.Bottom;
        ratio.Y = 3;
        inner.AddChild(ratio);
        inner.AddChild(Fixed(30, 15));
        outer.AddChild(inner);
        outer.AddChild(Fixed(40, 20));

        ShallowLayoutOfEveryElementShouldChangeNothing(outer);
    }

    [Fact]
    public void ShallowUpdateLayout_OnNonWrappingStackWithRatioChild_ShouldNotMoveAnyChild()
    {
        ContainerRuntime stack = Stack(vertical: true, width: 37, height: 120);
        stack.WrapsChildren = false;
        stack.AddChild(Fixed(39, 53));

        ContainerRuntime ratio = new();
        ratio.WidthUnits = DimensionUnitType.Absolute;
        ratio.Width = 29;
        ratio.HeightUnits = DimensionUnitType.Ratio;
        ratio.Height = 1;
        ratio.XOrigin = HorizontalAlignment.Right;
        ratio.X = 17;
        stack.AddChild(ratio);

        ShallowLayoutOfEveryElementShouldChangeNothing(stack);
    }

    [Fact]
    public void ShallowUpdateLayout_OnWrappingStackOfFixedChildren_ShouldLayOutEachChildOnce()
    {
        ContainerRuntime stack = Stack(vertical: true, width: 37, height: 29);
        stack.MaxWidth = 42;
        stack.MaxHeight = 84;
        stack.AddChild(Fixed(39, 53));
        stack.AddChild(Fixed(20, 20));
        stack.AddChild(Fixed(20, 20));
        stack.UpdateLayout();

        int before = GraphicalUiElement.UpdateLayoutCallCount;
        stack.UpdateLayout(true, 0);
        int calls = GraphicalUiElement.UpdateLayoutCallCount - before;

        // The stack, and each of its children once.
        calls.ShouldBe(4);
    }

    [Fact]
    public void ShallowUpdateLayout_OnWrappingStackWithRatioChild_ShouldLayOutOnlyTheRatioChildAgainAndNotItsDescendants()
    {
        ContainerRuntime stack = Stack(vertical: true, width: 37, height: 29);
        stack.MaxWidth = 42;
        stack.MaxHeight = 84;
        stack.AddChild(Fixed(39, 53));

        ContainerRuntime ratio = new();
        ratio.WidthUnits = DimensionUnitType.Absolute;
        ratio.Width = 29;
        ratio.HeightUnits = DimensionUnitType.Ratio;
        ratio.Height = 1;
        stack.AddChild(ratio);
        ratio.AddChild(Fixed(5, 5));
        stack.UpdateLayout();

        int before = GraphicalUiElement.UpdateLayoutCallCount;
        stack.UpdateLayout(true, 0);
        int calls = GraphicalUiElement.UpdateLayoutCallCount - before;

        // The stack, the fixed child, and the Ratio child twice (its x in the wrap pass, then in full). Not the grandchild.
        calls.ShouldBe(4);
    }
}
