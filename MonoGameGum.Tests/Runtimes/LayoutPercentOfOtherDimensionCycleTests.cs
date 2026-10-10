using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A parent whose one axis is a percent of its other axis, and whose other axis is sized from its children,
/// holding a child that is a percent of the parent on that first axis and a percent of its own first axis on
/// the second. The child's size is then a multiple of the parent's size, so if the parent counted the child
/// its size would have no finite answer (each layout grew it 4x until it reached infinity). The parent
/// ignores such a child when it measures itself, the same rule as any child sized from its parent. Every
/// other shape keeps counting the child. Each test runs for a cycle through the height (the parent's width is
/// a percent of its height) and through the width (mirrored). All expected numbers are worked out by hand.
/// </summary>
public class LayoutPercentOfOtherDimensionCycleTests : BaseTestClass
{
    private static ContainerRuntime Box(DimensionUnitType widthUnits, float width, DimensionUnitType heightUnits, float height)
    {
        ContainerRuntime box = new();
        box.WidthUnits = widthUnits;
        box.Width = width;
        box.HeightUnits = heightUnits;
        box.Height = height;
        return box;
    }

    /// <summary>Sets the axis the cycle runs through (the height when <paramref name="throughHeight"/>) or the other one.</summary>
    private static void SetCycleAxis(ContainerRuntime box, bool throughHeight, DimensionUnitType units, float value)
    {
        if (throughHeight)
        {
            box.HeightUnits = units;
            box.Height = value;
        }
        else
        {
            box.WidthUnits = units;
            box.Width = value;
        }
    }

    private static void SetFollowedAxis(ContainerRuntime box, bool throughHeight, DimensionUnitType units, float value) =>
        SetCycleAxis(box, !throughHeight, units, value);

    private static float CycleSize(GraphicalUiElement element, bool throughHeight) =>
        throughHeight ? element.AbsoluteHeight : element.AbsoluteWidth;

    private static float FollowedSize(GraphicalUiElement element, bool throughHeight) =>
        throughHeight ? element.AbsoluteWidth : element.AbsoluteHeight;

    /// <summary>
    /// Builds P (Absolute 34 along the followed axis, 51 along the cycle axis) holding X, holding Y, with
    /// layout suspended or live, and lays P out once. X's followed axis and cycle axis units are given; Y is a
    /// percent of X on the followed axis and a percent of its own followed axis on the cycle axis unless
    /// the test changes it.
    /// </summary>
    private static (ContainerRuntime parent, ContainerRuntime x, ContainerRuntime y) Build(
        bool throughHeight, bool buildLive,
        DimensionUnitType xFollowedUnits, float xFollowed, DimensionUnitType xCycleUnits, float xCycle,
        DimensionUnitType yFollowedUnits, float yFollowed, DimensionUnitType yCycleUnits, float yCycle)
    {
        ContainerRuntime parent = Box(DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0);
        SetFollowedAxis(parent, throughHeight, DimensionUnitType.Absolute, 34);
        SetCycleAxis(parent, throughHeight, DimensionUnitType.Absolute, 51);
        ContainerRuntime x = Box(DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0);
        SetFollowedAxis(x, throughHeight, xFollowedUnits, xFollowed);
        SetCycleAxis(x, throughHeight, xCycleUnits, xCycle);
        ContainerRuntime y = Box(DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0);
        SetFollowedAxis(y, throughHeight, yFollowedUnits, yFollowed);
        SetCycleAxis(y, throughHeight, yCycleUnits, yCycle);

        GraphicalUiElement.IsAllLayoutSuspended = !buildLive;
        try
        {
            parent.AddChild(x);
            x.AddChild(y);
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
        parent.UpdateLayout();
        return (parent, x, y);
    }

    private static string Snapshot(params GraphicalUiElement[] elements)
    {
        string text = "";
        foreach (GraphicalUiElement element in elements)
        {
            text += $"{element.AbsoluteLeft},{element.AbsoluteTop} {element.AbsoluteWidth}x{element.AbsoluteHeight};";
        }
        return text;
    }

    #region The cycle

    [Theory]
    [InlineData(true, false, DimensionUnitType.PercentageOfParent, 100)]
    [InlineData(true, true, DimensionUnitType.PercentageOfParent, 100)]
    [InlineData(false, false, DimensionUnitType.PercentageOfParent, 100)]
    [InlineData(false, true, DimensionUnitType.PercentageOfParent, 100)]
    [InlineData(true, false, DimensionUnitType.RelativeToParent, 0)]
    [InlineData(false, false, DimensionUnitType.RelativeToParent, 0)]
    public void PercentOfParentChildWithPercentOfOtherDimension_ShouldNotCountTowardAParentThatFollowsItsOwnContentSize(
        bool throughHeight, bool buildLive, DimensionUnitType yFollowedUnits, float yFollowed)
    {
        // X is 200% of its height wide (cycle axis = height) and its height is the larger of P's 51 and its
        // children. Y is as wide as X (100%, or X's width plus 0) and 200% of its own width tall, so Y is 4x
        // X's height. If X counted Y it would need 4x its own height. X ignores Y, so X is 51 tall and
        // 2 * 51 = 102 wide, and Y is 102 wide and 2 * 102 = 204 tall. (Mirrored for width: P is 51 wide and 34
        // tall, X is 51 wide and 102 tall, and Y is 102 tall and 204 wide.)
        (ContainerRuntime parent, ContainerRuntime x, ContainerRuntime y) = Build(throughHeight, buildLive,
            DimensionUnitType.PercentageOfOtherDimension, 200, DimensionUnitType.RelativeToMaxParentOrChildren, 0,
            yFollowedUnits, yFollowed, DimensionUnitType.PercentageOfOtherDimension, 200);

        float xCycle = 51;
        CycleSize(x, throughHeight).ShouldBe(xCycle);
        FollowedSize(x, throughHeight).ShouldBe(xCycle * 2);
        FollowedSize(y, throughHeight).ShouldBe(xCycle * 2);
        CycleSize(y, throughHeight).ShouldBe(xCycle * 4);
        string settled = Snapshot(parent, x, y);
        parent.UpdateLayout();
        Snapshot(parent, x, y).ShouldBe(settled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PercentOfParentChildWithPercentOfOtherDimension_ShouldStayFiniteAfterManyLayouts(bool throughHeight)
    {
        (ContainerRuntime parent, ContainerRuntime x, ContainerRuntime y) = Build(throughHeight, buildLive: false,
            DimensionUnitType.PercentageOfOtherDimension, 200, DimensionUnitType.RelativeToMaxParentOrChildren, 0,
            DimensionUnitType.PercentageOfParent, 100, DimensionUnitType.PercentageOfOtherDimension, 200);

        for (int i = 0; i < 80; i++)
        {
            parent.UpdateLayout();
        }

        float.IsFinite(x.AbsoluteWidth).ShouldBeTrue();
        float.IsFinite(x.AbsoluteHeight).ShouldBeTrue();
        float.IsFinite(y.AbsoluteWidth).ShouldBeTrue();
        float.IsFinite(y.AbsoluteHeight).ShouldBeTrue();
        CycleSize(x, throughHeight).ShouldBe(51);
    }

    #endregion

    #region Each condition, flipped, keeps the old sizing

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ChildOtherAxisAbsolute_ShouldStillCountTowardTheParent(bool throughHeight)
    {
        // Y is 40 across the followed axis (not sized from X), so Y is 200% of 40 = 80 along the cycle axis.
        // X counts Y: X is max(51 from P, 80) = 80 along the cycle axis and 200% of that, 160, across.
        // (Mirrored for width: X is 80 wide and 160 tall.)
        (ContainerRuntime parent, ContainerRuntime x, ContainerRuntime y) = Build(throughHeight, buildLive: false,
            DimensionUnitType.PercentageOfOtherDimension, 200, DimensionUnitType.RelativeToMaxParentOrChildren, 0,
            DimensionUnitType.Absolute, 40, DimensionUnitType.PercentageOfOtherDimension, 200);

        CycleSize(y, throughHeight).ShouldBe(80);
        CycleSize(x, throughHeight).ShouldBe(80);
        FollowedSize(x, throughHeight).ShouldBe(160);
        parent.UpdateLayout();
        CycleSize(x, throughHeight).ShouldBe(80);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ChildAxisNotPercentOfOtherDimension_ShouldStillCountTowardTheParent(bool throughHeight)
    {
        // Y is 80 along the cycle axis (Absolute) and as wide as X. X counts Y: it is max(51, 80) = 80 along
        // the cycle axis and 160 across, and Y is 160 across.
        (ContainerRuntime parent, ContainerRuntime x, ContainerRuntime y) = Build(throughHeight, buildLive: false,
            DimensionUnitType.PercentageOfOtherDimension, 200, DimensionUnitType.RelativeToMaxParentOrChildren, 0,
            DimensionUnitType.PercentageOfParent, 100, DimensionUnitType.Absolute, 80);

        CycleSize(x, throughHeight).ShouldBe(80);
        FollowedSize(x, throughHeight).ShouldBe(160);
        FollowedSize(y, throughHeight).ShouldBe(160);
        parent.UpdateLayout();
        CycleSize(x, throughHeight).ShouldBe(80);
        FollowedSize(y, throughHeight).ShouldBe(160);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void ParentSizedToChildrenOnTheCycleAxis_ShouldSizeTheChildFromTheParentOnTheFirstLayout(bool throughHeight, bool buildLive)
    {
        // X is sized to its children along the cycle axis (RelativeToChildren, not the max with P) and counts
        // Y's 80, so X is 80 along the cycle axis and 200% of that, 160, across. Y is as wide as X: 160, not
        // a size taken from the canvas before X was measured.
        (ContainerRuntime parent, ContainerRuntime x, ContainerRuntime y) = Build(throughHeight, buildLive,
            DimensionUnitType.PercentageOfOtherDimension, 200, DimensionUnitType.RelativeToChildren, 0,
            DimensionUnitType.PercentageOfParent, 100, DimensionUnitType.Absolute, 80);

        CycleSize(x, throughHeight).ShouldBe(80);
        FollowedSize(x, throughHeight).ShouldBe(160);
        FollowedSize(y, throughHeight).ShouldBe(160);
        string settled = Snapshot(parent, x, y);
        parent.UpdateLayout();
        Snapshot(parent, x, y).ShouldBe(settled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ParentAbsoluteOnTheFollowedAxis_ShouldStillCountTheChild(bool throughHeight)
    {
        // X is Absolute 100 across, so Y is 100% = 100 across and 200% of that = 200 along the cycle axis.
        // X sizes to the larger of P's 51 and Y, 200. Nothing is circular: X's width does not follow its height.
        (ContainerRuntime parent, ContainerRuntime x, ContainerRuntime y) = Build(throughHeight, buildLive: false,
            DimensionUnitType.Absolute, 100, DimensionUnitType.RelativeToMaxParentOrChildren, 0,
            DimensionUnitType.PercentageOfParent, 100, DimensionUnitType.PercentageOfOtherDimension, 200);

        FollowedSize(y, throughHeight).ShouldBe(100);
        CycleSize(y, throughHeight).ShouldBe(200);
        CycleSize(x, throughHeight).ShouldBe(200);
        parent.UpdateLayout();
        CycleSize(x, throughHeight).ShouldBe(200);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ParentSizedToChildrenOnTheFollowedAxis_ShouldStillCountTheChild(bool throughHeight)
    {
        // X is sized to its children on both axes and holds a 50 by 10 box next to Y. X is 50 across (Y is
        // sized from X so it does not count there). Y is 100% = 50 across and 200% of that = 100 along the
        // cycle axis. X's width does not follow its height, so there is no cycle and X counts Y: it is 100
        // along the cycle axis.
        ContainerRuntime parent = Box(DimensionUnitType.Absolute, 34, DimensionUnitType.Absolute, 51);
        ContainerRuntime x = Box(DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.RelativeToChildren, 0);
        ContainerRuntime box = Box(DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0);
        SetFollowedAxis(box, throughHeight, DimensionUnitType.Absolute, 50);
        SetCycleAxis(box, throughHeight, DimensionUnitType.Absolute, 10);
        ContainerRuntime y = Box(DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0);
        SetFollowedAxis(y, throughHeight, DimensionUnitType.PercentageOfParent, 100);
        SetCycleAxis(y, throughHeight, DimensionUnitType.PercentageOfOtherDimension, 200);
        parent.AddChild(x);
        x.AddChild(box);
        x.AddChild(y);
        parent.UpdateLayout();

        FollowedSize(x, throughHeight).ShouldBe(50);
        FollowedSize(y, throughHeight).ShouldBe(50);
        CycleSize(y, throughHeight).ShouldBe(100);
        CycleSize(x, throughHeight).ShouldBe(100);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ParentOtherAxisAbsolute_ShouldKeepItsSizeAndSizeTheChildFromIt(bool throughHeight)
    {
        // X is 51 along the cycle axis (Absolute, so it does not follow its children), 200% = 102 across.
        // Y is 102 across and 204 along the cycle axis, overflowing X. Nothing depends on Y.
        (ContainerRuntime parent, ContainerRuntime x, ContainerRuntime y) = Build(throughHeight, buildLive: false,
            DimensionUnitType.PercentageOfOtherDimension, 200, DimensionUnitType.Absolute, 51,
            DimensionUnitType.PercentageOfParent, 100, DimensionUnitType.PercentageOfOtherDimension, 200);

        CycleSize(x, throughHeight).ShouldBe(51);
        FollowedSize(x, throughHeight).ShouldBe(102);
        FollowedSize(y, throughHeight).ShouldBe(102);
        CycleSize(y, throughHeight).ShouldBe(204);
    }

    #endregion
}
