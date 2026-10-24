using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Appending children one at a time to a laid-out stack (#6702): the result must equal building the same tree
/// suspended and laying it out once, and an append must not lay out every existing sibling again.
/// </summary>
public class StackAppendLayoutTests : BaseTestClass
{
    const int RowCount = 100;

    [Fact]
    public void Append_ShouldLayOutLinearly_WhenStackIsFixedSize()
    {
        ContainerRuntime stack = CreateStack(fixedSize: true, wraps: false);

        int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;
        for (int i = 0; i < RowCount; i++)
        {
            stack.AddChild(CreateRow(RowKind.Absolute, i));
        }
        int calls = GraphicalUiElement.UpdateLayoutCallCount - callsBefore;

        // Quadratic before #6702: about RowCount^2 / 2 layouts (every sibling once per append).
        calls.ShouldBeLessThan(RowCount * 20);
    }

    [Fact]
    public void Append_ShouldLayOutLinearly_WhenStackSizesFromChildren()
    {
        ContainerRuntime stack = CreateStack(fixedSize: false, wraps: false);

        int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;
        for (int i = 0; i < RowCount; i++)
        {
            stack.AddChild(CreateRow(RowKind.RelativeToParentWidth, i));
        }
        int calls = GraphicalUiElement.UpdateLayoutCallCount - callsBefore;

        calls.ShouldBeLessThan(RowCount * 20);
    }

    [Fact]
    public void Append_ShouldLayOutLinearly_WhenStackIsInsideContentSizedParent()
    {
        ContainerRuntime outer = new();
        outer.WidthUnits = DimensionUnitType.RelativeToChildren;
        outer.HeightUnits = DimensionUnitType.RelativeToChildren;
        outer.Width = 0;
        outer.Height = 0;
        ContainerRuntime stack = CreateStack(fixedSize: false, wraps: false);
        outer.AddChild(stack);

        int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;
        for (int i = 0; i < RowCount; i++)
        {
            stack.AddChild(CreateRow(RowKind.RelativeToParentWidth, i));
        }
        int calls = GraphicalUiElement.UpdateLayoutCallCount - callsBefore;

        calls.ShouldBeLessThan(RowCount * 20);
    }

    [Fact]
    public void ChildrenAdd_ShouldLayOutLinearly()
    {
        ContainerRuntime stack = CreateStack(fixedSize: false, wraps: false);

        int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;
        for (int i = 0; i < RowCount; i++)
        {
            stack.Children.Add(CreateRow(RowKind.Absolute, i));
        }
        int calls = GraphicalUiElement.UpdateLayoutCallCount - callsBefore;

        calls.ShouldBeLessThan(RowCount * 20);
    }

    [Fact]
    public void ChildrenInsertAtEnd_ShouldLayOutLinearly()
    {
        ContainerRuntime stack = CreateStack(fixedSize: false, wraps: false);

        int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;
        for (int i = 0; i < RowCount; i++)
        {
            stack.Children.Insert(stack.Children.Count, CreateRow(RowKind.Absolute, i));
        }
        int calls = GraphicalUiElement.UpdateLayoutCallCount - callsBefore;

        calls.ShouldBeLessThan(RowCount * 20);
    }

    [Fact]
    public void Clone_ShouldNotKeepTheAppendInProgress_WhenCloningDuringTheAppendLayout()
    {
        ContainerRuntime stack = CreateStack(fixedSize: true, wraps: false);
        stack.AddChild(CreateRow(RowKind.Absolute, 0));
        GraphicalUiElement? clone = null;
        ContainerRuntime appended = CreateRow(RowKind.Absolute, 1);
        appended.PositionChanged += (_, _) => clone ??= stack.Clone();

        stack.AddChild(appended);

        clone.ShouldNotBeNull();
        typeof(GraphicalUiElement)
            .GetField("_appendedChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(clone).ShouldBeNull();
    }

    public static IEnumerable<object[]> Scenarios()
    {
        foreach (bool fixedSize in new[] { true, false })
        {
            foreach (bool wraps in new[] { true, false })
            {
                foreach (RowKind kind in Enum.GetValues<RowKind>())
                {
                    foreach (ChildrenLayout layout in new[] { ChildrenLayout.TopToBottomStack, ChildrenLayout.LeftToRightStack })
                    {
                        yield return new object[] { fixedSize, wraps, kind, layout };
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Append_ShouldMatchSuspendedBuild(bool fixedSize, bool wraps, RowKind kind, ChildrenLayout layout)
    {
        const int count = 12;

        ContainerRuntime appended = CreateStack(fixedSize, wraps, layout);
        for (int i = 0; i < count; i++)
        {
            appended.AddChild(CreateRow(kind, i));
        }

        ContainerRuntime suspended = CreateStack(fixedSize, wraps, layout);
        suspended.SuspendLayout(true);
        for (int i = 0; i < count; i++)
        {
            suspended.AddChild(CreateRow(kind, i));
        }
        suspended.ResumeLayout(true);

        AssertSameLayout(appended, suspended);
    }

    public enum RowKind
    {
        Absolute,
        RelativeToParentWidth,
        PercentageOfParent,
        Ratio,
        Mixed,
        NestedContentSized,
        Centered,
    }

    static ContainerRuntime CreateStack(bool fixedSize, bool wraps, ChildrenLayout layout = ChildrenLayout.TopToBottomStack)
    {
        ContainerRuntime stack = new();
        stack.ChildrenLayout = layout;
        stack.WrapsChildren = wraps;
        stack.StackSpacing = 3;
        stack.Width = 200;
        stack.Height = 300;
        stack.WidthUnits = DimensionUnitType.Absolute;
        stack.HeightUnits = DimensionUnitType.Absolute;
        if (!fixedSize)
        {
            // Sized from the children along the stacking axis, fixed across it.
            if (layout == ChildrenLayout.TopToBottomStack)
            {
                stack.Height = 4;
                stack.HeightUnits = DimensionUnitType.RelativeToChildren;
            }
            else
            {
                stack.Width = 4;
                stack.WidthUnits = DimensionUnitType.RelativeToChildren;
            }
        }
        return stack;
    }

    static ContainerRuntime CreateRow(RowKind kind, int index)
    {
        ContainerRuntime row = new();
        row.Width = 30 + index % 4 * 7;
        row.Height = 20 + index % 3 * 5;
        row.WidthUnits = DimensionUnitType.Absolute;
        row.HeightUnits = DimensionUnitType.Absolute;

        var effectiveKind = kind == RowKind.Mixed ? (RowKind)(index % 7) : kind;
        if (effectiveKind == RowKind.Mixed)
        {
            effectiveKind = RowKind.Absolute;
        }

        switch (effectiveKind)
        {
            case RowKind.RelativeToParentWidth:
                row.Width = -10;
                row.WidthUnits = DimensionUnitType.RelativeToParent;
                break;
            case RowKind.PercentageOfParent:
                row.Width = 20 + index % 3 * 10;
                row.WidthUnits = DimensionUnitType.PercentageOfParent;
                row.Height = 5 + index % 2 * 5;
                row.HeightUnits = DimensionUnitType.PercentageOfParent;
                break;
            case RowKind.Ratio:
                row.Height = 1 + index % 2;
                row.HeightUnits = DimensionUnitType.Ratio;
                row.Width = 1 + index % 3;
                row.WidthUnits = DimensionUnitType.Ratio;
                break;
            case RowKind.NestedContentSized:
                row.WidthUnits = DimensionUnitType.RelativeToChildren;
                row.HeightUnits = DimensionUnitType.RelativeToChildren;
                row.Width = 4;
                row.Height = 2;
                row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
                for (int i = 0; i < 1 + index % 3; i++)
                {
                    ContainerRuntime inner = new();
                    inner.Width = 10 + i * 3;
                    inner.Height = 8 + index % 2 * 4;
                    inner.WidthUnits = DimensionUnitType.Absolute;
                    inner.HeightUnits = DimensionUnitType.Absolute;
                    row.AddChild(inner);
                }
                break;
            case RowKind.Centered:
                row.XUnits = GeneralUnitType.PixelsFromMiddle;
                row.XOrigin = HorizontalAlignment.Center;
                row.YUnits = GeneralUnitType.PixelsFromMiddle;
                row.YOrigin = VerticalAlignment.Center;
                break;
        }
        return row;
    }

    static void AssertSameLayout(GraphicalUiElement actual, GraphicalUiElement expected, string path = "root")
    {
        actual.AbsoluteLeft.ShouldBe(expected.AbsoluteLeft, path);
        actual.AbsoluteTop.ShouldBe(expected.AbsoluteTop, path);
        actual.AbsoluteWidth.ShouldBe(expected.AbsoluteWidth, path);
        actual.AbsoluteHeight.ShouldBe(expected.AbsoluteHeight, path);
        actual.Children.Count.ShouldBe(expected.Children.Count, path);
        for (int i = 0; i < actual.Children.Count; i++)
        {
            AssertSameLayout(actual.Children[i], expected.Children[i], $"{path}/{i}");
        }
    }
}
