using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A wrapping stack sized to its children, holding children that the wrap pass does not lay out (a Ratio
/// size on the stacking axis) next to children it does. Those children are placed after the stack wrapped
/// and move the siblings stacked behind them, so the stack must measure again once they are placed: a second
/// <c>UpdateLayout()</c> changes nothing. Each test runs with the stack stacking top to bottom and left to
/// right, and builds the tree with layout suspended and live. All expected numbers are worked out by hand
/// in each test.
/// </summary>
public class LayoutWrapIdempotencyTests : BaseTestClass
{
    private static ContainerRuntime Stack(bool vertical, float mainSize)
    {
        ContainerRuntime stack = new();
        stack.ChildrenLayout = vertical ? ChildrenLayout.TopToBottomStack : ChildrenLayout.LeftToRightStack;
        stack.WrapsChildren = true;
        stack.MaxWidth = vertical ? 61 : 125;
        stack.MaxHeight = vertical ? 125 : 61;
        SetMain(stack, vertical, DimensionUnitType.Absolute, mainSize);
        SetCross(stack, vertical, DimensionUnitType.RelativeToChildren, 0);
        return stack;
    }

    private static ContainerRuntime Child(bool vertical, float crossSize, DimensionUnitType mainUnits, float mainValue)
    {
        ContainerRuntime child = new();
        SetCross(child, vertical, DimensionUnitType.RelativeToChildren, crossSize);
        SetMain(child, vertical, mainUnits, mainValue);
        return child;
    }

    private static void SetMain(ContainerRuntime element, bool vertical, DimensionUnitType units, float value)
    {
        if (vertical)
        {
            element.HeightUnits = units;
            element.Height = value;
        }
        else
        {
            element.WidthUnits = units;
            element.Width = value;
        }
    }

    private static void SetCross(ContainerRuntime element, bool vertical, DimensionUnitType units, float value)
    {
        SetMain(element, !vertical, units, value);
    }

    private static void SetMainPosition(ContainerRuntime element, bool vertical, GeneralUnitType units, float value)
    {
        if (vertical)
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

    private static float CrossSize(GraphicalUiElement element, bool vertical) =>
        vertical ? element.AbsoluteWidth : element.AbsoluteHeight;

    private static float MainSize(GraphicalUiElement element, bool vertical) =>
        vertical ? element.AbsoluteHeight : element.AbsoluteWidth;

    private static float CrossPosition(GraphicalUiElement element, bool vertical) =>
        vertical ? element.AbsoluteLeft : element.AbsoluteTop;

    private static float MainPosition(GraphicalUiElement element, bool vertical) =>
        vertical ? element.AbsoluteTop : element.AbsoluteLeft;

    /// <summary>
    /// Runs <paramref name="create"/>, which builds the stack and adds its children, with layout suspended or
    /// live, then lays the stack out once.
    /// </summary>
    private static ContainerRuntime BuildAndLayOut(bool buildLive, Func<ContainerRuntime> create)
    {
        GraphicalUiElement.IsAllLayoutSuspended = !buildLive;
        ContainerRuntime stack;
        try
        {
            stack = create();
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
        stack.UpdateLayout();
        return stack;
    }

    private static string Snapshot(ContainerRuntime stack)
    {
        StringBuilder builder = new();
        List<GraphicalUiElement> all = new() { stack };
        all.AddRange(stack.Children);
        foreach (GraphicalUiElement element in all)
        {
            builder.Append(element.AbsoluteLeft.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(element.AbsoluteTop.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(element.AbsoluteWidth.ToString(CultureInfo.InvariantCulture)).Append('x')
                .Append(element.AbsoluteHeight.ToString(CultureInfo.InvariantCulture)).AppendLine();
        }
        return builder.ToString();
    }

    #region Ratio child positioned from the middle

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void WrappingStack_RatioChildPositionedFromMiddle_ShouldMeasureAfterThatChildMovesItsSiblings(bool vertical, bool buildLive)
    {
        // The stack's main size is 41. A is a Ratio with nothing left (41 - 9 - 46 < 0), so it is 0 long, and
        // it sits at the middle of the stack, 20.5. B (9 long, nudged 16) stacks after A at 20.5 + 16 = 36.5
        // and ends at 45.5, past 41, so B wraps to the second line. C (46 long) ends past 41 wherever it
        // follows B, so it wraps to a third line. Each line is 6 across: the stack is 18 across.
        ContainerRuntime a = null!;
        ContainerRuntime b = null!;
        ContainerRuntime c = null!;
        ContainerRuntime stack = BuildAndLayOut(buildLive, () =>
        {
            ContainerRuntime created = Stack(vertical, mainSize: 41);
            a = Child(vertical, crossSize: 6, DimensionUnitType.Ratio, 1);
            SetMainPosition(a, vertical, GeneralUnitType.PixelsFromMiddle, 0);
            b = Child(vertical, crossSize: 6, DimensionUnitType.Absolute, 9);
            SetMainPosition(b, vertical, GeneralUnitType.PixelsFromSmall, 16);
            c = Child(vertical, crossSize: 6, DimensionUnitType.Absolute, 46);
            created.AddChild(a);
            created.AddChild(b);
            created.AddChild(c);
            return created;
        });
        string afterFirstLayout = Snapshot(stack);

        CrossSize(stack, vertical).ShouldBe(18);
        MainSize(stack, vertical).ShouldBe(41);
        MainSize(a, vertical).ShouldBe(0);
        MainPosition(a, vertical).ShouldBe(20.5f);
        CrossPosition(b, vertical).ShouldBe(6);
        MainPosition(b, vertical).ShouldBe(16);
        CrossPosition(c, vertical).ShouldBe(12);
        MainPosition(c, vertical).ShouldBe(0);
        stack.UpdateLayout();
        Snapshot(stack).ShouldBe(afterFirstLayout);
    }

    #endregion

    #region Percent and Ratio together on the stacking axis

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void WrappingStack_PercentAndRatioOnTheStackingAxis_ShouldMeasureAfterTheRatioChildIsPlaced(bool vertical, bool buildLive)
    {
        // The stack's main size is 41. B is 100% of it (41 long, 52 across), so nothing is left for A (6
        // across), a Ratio, which is 0 long. A sits at the middle, 20.5. B stacks after A at 20.5 and would
        // end at 61.5, past 41, so B wraps to the second line and sits at its start, 0. The first line is 6
        // across and the second 52, so the stack is 58 across.
        ContainerRuntime a = null!;
        ContainerRuntime b = null!;
        ContainerRuntime stack = BuildAndLayOut(buildLive, () =>
        {
            ContainerRuntime created = Stack(vertical, mainSize: 41);
            a = Child(vertical, crossSize: 6, DimensionUnitType.Ratio, 1);
            SetMainPosition(a, vertical, GeneralUnitType.PixelsFromMiddle, 0);
            b = Child(vertical, crossSize: 52, DimensionUnitType.PercentageOfParent, 100);
            created.AddChild(a);
            created.AddChild(b);
            return created;
        });
        string afterFirstLayout = Snapshot(stack);

        MainSize(a, vertical).ShouldBe(0);
        MainPosition(a, vertical).ShouldBe(20.5f);
        MainSize(b, vertical).ShouldBe(41);
        CrossPosition(b, vertical).ShouldBe(6);
        MainPosition(b, vertical).ShouldBe(0);
        CrossSize(stack, vertical).ShouldBe(58);
        MainSize(stack, vertical).ShouldBe(41);
        stack.UpdateLayout();
        Snapshot(stack).ShouldBe(afterFirstLayout);
    }

    #endregion

    #region Any mix of Absolute, percent and Ratio children

    [Fact]
    public void WrappingStack_AnyMixOfAbsolutePercentAndRatioChildren_ShouldNotChangeOnASecondLayout()
    {
        // Seeded stacks of two to five children with Absolute, PercentageOfParent or Ratio sizes on the
        // stacking axis, positioned from the start, middle or end, in either direction, sized to their
        // children across and either fixed or sized to their children along the stack (with a max).
        DimensionUnitType[] mainUnits =
        {
            DimensionUnitType.Absolute, DimensionUnitType.PercentageOfParent, DimensionUnitType.Ratio, DimensionUnitType.Ratio
        };
        GeneralUnitType[] positionUnits =
        {
            GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromMiddle, GeneralUnitType.PixelsFromLarge
        };
        int[] percents = { 25, 50, 100 };
        List<string> failures = new();

        for (int seed = 0; seed < 400; seed++)
        {
            uint state = (uint)seed * 2654435761u + 12345u;
            int Next(int minInclusive, int maxExclusive)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return minInclusive + (int)(state % (uint)(maxExclusive - minInclusive));
            }
            Next(0, 2);
            Next(0, 2);

            bool vertical = Next(0, 2) == 0;
            float mainSize = Next(0, 2) == 0 ? 41 : 60;
            bool sizedAlongStack = Next(0, 2) == 0;
            int childCount = Next(2, 6);

            ContainerRuntime stack = BuildAndLayOut(buildLive: false, () =>
            {
                ContainerRuntime created = Stack(vertical, mainSize);
                if (sizedAlongStack)
                {
                    SetMain(created, vertical, DimensionUnitType.RelativeToChildren, 0);
                    created.MaxWidth = vertical ? 61 : mainSize;
                    created.MaxHeight = vertical ? mainSize : 61;
                }
                for (int i = 0; i < childCount; i++)
                {
                    DimensionUnitType units = mainUnits[Next(0, mainUnits.Length)];
                    float value = units switch
                    {
                        DimensionUnitType.Absolute => Next(3, 50),
                        DimensionUnitType.PercentageOfParent => percents[Next(0, percents.Length)],
                        _ => Next(1, 3)
                    };
                    ContainerRuntime child = Child(vertical, crossSize: Next(3, 30), units, value);
                    SetMainPosition(child, vertical, positionUnits[Next(0, positionUnits.Length)], Next(-10, 20));
                    created.AddChild(child);
                }
                return created;
            });
            string afterFirstLayout = Snapshot(stack);
            stack.UpdateLayout();

            if (Snapshot(stack) != afterFirstLayout)
            {
                failures.Add($"seed {seed}");
            }
        }

        failures.ShouldBeEmpty(string.Join(", ", failures.GetRange(0, System.Math.Min(10, failures.Count))));
    }

    #endregion
}
