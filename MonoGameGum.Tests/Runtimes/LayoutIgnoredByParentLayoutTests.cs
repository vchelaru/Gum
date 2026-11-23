using System;
using System.Collections.Generic;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// IgnoredByParentLayout (#8410): the parent's ChildrenLayout skips the child, which is placed as if the parent
/// were Regular, and its siblings stack or fill grid cells as if it were not there. Parent sizing still counts it
/// the way a Regular parent counts a child; IgnoredByParentSize controls that.
/// </summary>
public class LayoutIgnoredByParentLayoutTests : BaseTestClass
{
    static ContainerRuntime Box(float width, float height, float x = 0, float y = 0)
    {
        ContainerRuntime box = new();
        box.WidthUnits = DimensionUnitType.Absolute;
        box.HeightUnits = DimensionUnitType.Absolute;
        box.Width = width;
        box.Height = height;
        box.X = x;
        box.Y = y;
        return box;
    }

    static ContainerRuntime Parent(ChildrenLayout layout, float width, float height)
    {
        ContainerRuntime parent = Box(width, height);
        parent.ChildrenLayout = layout;
        return parent;
    }

    static ContainerRuntime Flagged(float width, float height, float x, float y)
    {
        ContainerRuntime box = Box(width, height, x, y);
        box.IgnoredByParentLayout = true;
        return box;
    }

    // A stack child sized (main, cross) along the stack.
    static ContainerRuntime StackChild(ChildrenLayout stack, float main, float cross) =>
        stack == ChildrenLayout.TopToBottomStack ? Box(cross, main) : Box(main, cross);

    static float MainPosition(ChildrenLayout stack, GraphicalUiElement element) =>
        stack == ChildrenLayout.TopToBottomStack ? element.AbsoluteTop : element.AbsoluteLeft;

    static float CrossPosition(ChildrenLayout stack, GraphicalUiElement element) =>
        stack == ChildrenLayout.TopToBottomStack ? element.AbsoluteLeft : element.AbsoluteTop;

    #region Stacks

    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack, 0, false)]
    [InlineData(ChildrenLayout.TopToBottomStack, 1, false)]
    [InlineData(ChildrenLayout.TopToBottomStack, 3, false)]
    [InlineData(ChildrenLayout.TopToBottomStack, 0, true)]
    [InlineData(ChildrenLayout.TopToBottomStack, 1, true)]
    [InlineData(ChildrenLayout.TopToBottomStack, 3, true)]
    [InlineData(ChildrenLayout.LeftToRightStack, 0, false)]
    [InlineData(ChildrenLayout.LeftToRightStack, 1, false)]
    [InlineData(ChildrenLayout.LeftToRightStack, 3, false)]
    [InlineData(ChildrenLayout.LeftToRightStack, 0, true)]
    [InlineData(ChildrenLayout.LeftToRightStack, 1, true)]
    [InlineData(ChildrenLayout.LeftToRightStack, 3, true)]
    public void Stack_ShouldPlaceSiblingsAsIfFlaggedChildWereAbsent_AtFirstMiddleAndLastIndex(ChildrenLayout stack, int flaggedIndex, bool wraps)
    {
        ContainerRuntime parent = Parent(stack, 200, 200);
        parent.WrapsChildren = wraps;
        parent.StackSpacing = 5;
        ContainerRuntime[] stacked = { StackChild(stack, 20, 10), StackChild(stack, 20, 10), StackChild(stack, 20, 10) };
        ContainerRuntime flagged = Flagged(30, 40, x: 7, y: 9);
        foreach (var child in stacked)
        {
            parent.AddChild(child);
        }
        parent.Children.Insert(flaggedIndex, flagged);
        parent.UpdateLayout();

        MainPosition(stack, stacked[0]).ShouldBe(0);
        MainPosition(stack, stacked[1]).ShouldBe(25);
        MainPosition(stack, stacked[2]).ShouldBe(50);
        CrossPosition(stack, stacked[2]).ShouldBe(0);
        flagged.AbsoluteLeft.ShouldBe(7);
        flagged.AbsoluteTop.ShouldBe(9);
        flagged.AbsoluteWidth.ShouldBe(30);
        flagged.AbsoluteHeight.ShouldBe(40);
    }

    [Fact]
    public void Stack_ShouldPlaceSiblingsAsIfAbsent_WhenSeveralChildrenAreFlagged()
    {
        ContainerRuntime parent = Parent(ChildrenLayout.LeftToRightStack, 200, 200);
        ContainerRuntime first = Flagged(5, 5, x: 100, y: 100);
        ContainerRuntime a = Box(20, 10);
        ContainerRuntime middle = Flagged(5, 5, x: 120, y: 130);
        ContainerRuntime b = Box(20, 10);
        parent.AddChild(first);
        parent.AddChild(a);
        parent.AddChild(middle);
        parent.AddChild(b);

        a.AbsoluteLeft.ShouldBe(0);
        b.AbsoluteLeft.ShouldBe(20);
        first.AbsoluteLeft.ShouldBe(100);
        first.AbsoluteTop.ShouldBe(100);
        middle.AbsoluteLeft.ShouldBe(120);
        middle.AbsoluteTop.ShouldBe(130);
    }

    [Fact]
    public void Stack_ShouldPlaceEveryChildByItsOwnPosition_WhenAllChildrenAreFlagged()
    {
        ContainerRuntime parent = Parent(ChildrenLayout.TopToBottomStack, 200, 200);
        parent.StackSpacing = 5;
        ContainerRuntime a = Flagged(20, 20, x: 1, y: 2);
        ContainerRuntime b = Flagged(20, 20, x: 3, y: 4);
        parent.AddChild(a);
        parent.AddChild(b);

        a.AbsoluteLeft.ShouldBe(1);
        a.AbsoluteTop.ShouldBe(2);
        b.AbsoluteLeft.ShouldBe(3);
        b.AbsoluteTop.ShouldBe(4);
    }

    [Fact]
    public void Stack_ShouldHonorFlaggedChildUnitsAndOrigin_WhenItIsALaterChild()
    {
        // A later stacked child has its main-axis units and origin ignored; a flagged one is not stacked.
        ContainerRuntime parent = Parent(ChildrenLayout.TopToBottomStack, 200, 200);
        parent.AddChild(Box(10, 20));
        ContainerRuntime flagged = Flagged(10, 10, x: 0, y: 0);
        flagged.YUnits = GeneralUnitType.PixelsFromLarge;
        flagged.YOrigin = VerticalAlignment.Bottom;
        flagged.XUnits = GeneralUnitType.PixelsFromMiddle;
        flagged.XOrigin = HorizontalAlignment.Center;
        parent.AddChild(flagged);

        flagged.AbsoluteTop.ShouldBe(190);
        flagged.AbsoluteLeft.ShouldBe(95);
    }

    [Fact]
    public void Stack_ShouldTreatFirstUnflaggedChildAsFirst_WhenAFlaggedChildPrecedesIt()
    {
        // The first stacked child keeps its main-axis units and origin; a flagged child before it does not make it later.
        ContainerRuntime parent = Parent(ChildrenLayout.TopToBottomStack, 200, 200);
        parent.AddChild(Flagged(10, 10, x: 0, y: 0));
        ContainerRuntime first = Box(10, 20);
        first.YUnits = GeneralUnitType.PixelsFromLarge;
        first.YOrigin = VerticalAlignment.Bottom;
        parent.AddChild(first);

        first.AbsoluteTop.ShouldBe(180);
    }

    [Theory]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    public void WrappingStack_ShouldNotGiveFlaggedChildASlotOrLetItSizeALine(ChildrenLayout stack)
    {
        // Main size 50 holds two 20-long children per line. The flagged child would be the second
        // slot of the first line and, at 40 across, set that line's cross size.
        ContainerRuntime parent = stack == ChildrenLayout.LeftToRightStack
            ? Parent(stack, 50, 200)
            : Parent(stack, 200, 50);
        parent.WrapsChildren = true;
        ContainerRuntime a = StackChild(stack, 20, 10);
        ContainerRuntime flagged = Flagged(40, 40, x: 3, y: 60);
        ContainerRuntime b = StackChild(stack, 20, 10);
        ContainerRuntime c = StackChild(stack, 20, 10);
        parent.AddChild(a);
        parent.AddChild(flagged);
        parent.AddChild(b);
        parent.AddChild(c);

        MainPosition(stack, a).ShouldBe(0);
        MainPosition(stack, b).ShouldBe(20);
        CrossPosition(stack, b).ShouldBe(0);
        MainPosition(stack, c).ShouldBe(0);
        CrossPosition(stack, c).ShouldBe(10);
        flagged.AbsoluteLeft.ShouldBe(3);
        flagged.AbsoluteTop.ShouldBe(60);
    }

    [Fact]
    public void Stack_ShouldNotSubtractFlaggedSiblingOrItsSpacing_FromRatioSize()
    {
        ContainerRuntime parent = Parent(ChildrenLayout.TopToBottomStack, 50, 100);
        parent.StackSpacing = 5;
        parent.AddChild(Box(10, 20));
        parent.AddChild(Flagged(10, 30, x: 0, y: 0));
        ContainerRuntime ratio = Box(10, 1);
        ratio.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratio);

        ratio.AbsoluteTop.ShouldBe(25);
        ratio.AbsoluteHeight.ShouldBe(75);
    }

    [Fact]
    public void FixedSizeStack_ShouldMeasureFromFirstUnflaggedChild_WhenFlaggedChildIsFirst()
    {
        ContainerRuntime parent = Parent(ChildrenLayout.TopToBottomStack, 200, 200);
        parent.UseFixedStackChildrenSize = true;
        ContainerRuntime flagged = Flagged(10, 50, x: 0, y: 5);
        ContainerRuntime a = Box(10, 20);
        ContainerRuntime b = Box(10, 20);
        ContainerRuntime c = Box(10, 20);
        parent.AddChild(flagged);
        parent.AddChild(a);
        parent.AddChild(b);
        parent.AddChild(c);
        parent.UpdateLayout();

        a.AbsoluteTop.ShouldBe(0);
        b.AbsoluteTop.ShouldBe(20);
        c.AbsoluteTop.ShouldBe(40);
        flagged.AbsoluteTop.ShouldBe(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void FixedSizeStackSizedToChildren_ShouldCountFlaggedChildExtent(int flaggedIndex)
    {
        // The fixed-size measure reads only the first stacked child; a flagged child still counts as in a Regular parent.
        ContainerRuntime parent = Parent(ChildrenLayout.TopToBottomStack, 100, 0);
        parent.UseFixedStackChildrenSize = true;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.AddChild(Box(10, 20));
        parent.AddChild(Box(10, 20));
        parent.Children.Insert(flaggedIndex, Flagged(10, 200, x: 0, y: 0));
        parent.UpdateLayout();

        parent.AbsoluteHeight.ShouldBe(200);
    }

    [Fact]
    public void FixedSizeStackSizedToChildren_ShouldMeasureFlaggedChildSizedByItsOwnContent()
    {
        // Built while suspended, so the flagged child's size is known only once the stack lays it out.
        ContainerRuntime parent = Parent(ChildrenLayout.TopToBottomStack, 100, 0);
        parent.UseFixedStackChildrenSize = true;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.SuspendLayout(true);
        parent.AddChild(Box(10, 20));
        parent.AddChild(Box(10, 20));
        ContainerRuntime flagged = Flagged(0, 0, x: 0, y: 0);
        flagged.WidthUnits = DimensionUnitType.RelativeToChildren;
        flagged.HeightUnits = DimensionUnitType.RelativeToChildren;
        flagged.AddChild(Box(10, 200));
        parent.AddChild(flagged);
        parent.ResumeLayout(true);

        parent.AbsoluteHeight.ShouldBe(200);
    }

    [Fact]
    public void NestedStack_ShouldStillStackItsOwnChildren_WhenItIsFlaggedInsideAnOuterStack()
    {
        ContainerRuntime outer = Parent(ChildrenLayout.TopToBottomStack, 200, 200);
        ContainerRuntime a = Box(10, 20);
        ContainerRuntime inner = Flagged(0, 0, x: 50, y: 60);
        inner.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        inner.WidthUnits = DimensionUnitType.RelativeToChildren;
        inner.HeightUnits = DimensionUnitType.RelativeToChildren;
        ContainerRuntime innerFirst = Box(10, 10);
        ContainerRuntime innerSecond = Box(10, 10);
        inner.AddChild(innerFirst);
        inner.AddChild(innerSecond);
        ContainerRuntime b = Box(10, 20);
        outer.AddChild(a);
        outer.AddChild(inner);
        outer.AddChild(b);

        b.AbsoluteTop.ShouldBe(20);
        inner.AbsoluteLeft.ShouldBe(50);
        inner.AbsoluteTop.ShouldBe(60);
        inner.AbsoluteWidth.ShouldBe(20);
        inner.AbsoluteHeight.ShouldBe(10);
        innerFirst.AbsoluteLeft.ShouldBe(50);
        innerSecond.AbsoluteLeft.ShouldBe(60);
    }

    [Fact]
    public void RegularParent_ShouldPlaceChildTheSame_WhetherOrNotItIsFlagged()
    {
        ContainerRuntime parent = Box(200, 200);
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        ContainerRuntime child = Box(30, 40, x: 7, y: 9);
        parent.AddChild(child);
        float leftBefore = child.AbsoluteLeft;
        float topBefore = child.AbsoluteTop;
        float parentWidthBefore = parent.AbsoluteWidth;

        child.IgnoredByParentLayout = true;

        child.AbsoluteLeft.ShouldBe(leftBefore);
        child.AbsoluteTop.ShouldBe(topBefore);
        parent.AbsoluteWidth.ShouldBe(parentWidthBefore);
        parentWidthBefore.ShouldBe(37);
    }

    #endregion

    #region Triggers

    [Fact]
    public void Stack_ShouldRestackSiblings_WhenFlagIsToggledAtRuntime()
    {
        ContainerRuntime parent = Parent(ChildrenLayout.TopToBottomStack, 200, 200);
        ContainerRuntime a = Box(10, 20);
        ContainerRuntime toggled = Box(10, 30, x: 0, y: 100);
        ContainerRuntime b = Box(10, 20);
        parent.AddChild(a);
        parent.AddChild(toggled);
        parent.AddChild(b);
        toggled.AbsoluteTop.ShouldBe(120);
        b.AbsoluteTop.ShouldBe(150);

        toggled.IgnoredByParentLayout = true;

        toggled.AbsoluteTop.ShouldBe(100);
        b.AbsoluteTop.ShouldBe(20);

        toggled.IgnoredByParentLayout = false;

        toggled.AbsoluteTop.ShouldBe(120);
        b.AbsoluteTop.ShouldBe(150);
    }

    [Fact]
    public void Stack_ShouldNotMoveSiblings_WhenInvisibleFlaggedChildIsShown()
    {
        ContainerRuntime parent = Parent(ChildrenLayout.TopToBottomStack, 200, 200);
        ContainerRuntime a = Box(10, 20);
        ContainerRuntime flagged = Flagged(10, 30, x: 0, y: 100);
        flagged.Visible = false;
        ContainerRuntime b = Box(10, 20);
        parent.AddChild(a);
        parent.AddChild(flagged);
        parent.AddChild(b);
        b.AbsoluteTop.ShouldBe(20);

        flagged.Visible = true;

        b.AbsoluteTop.ShouldBe(20);
        flagged.AbsoluteTop.ShouldBe(100);
    }

    #endregion

    #region AutoGrid

    [Theory]
    [InlineData(ChildrenLayout.AutoGridHorizontal)]
    [InlineData(ChildrenLayout.AutoGridVertical)]
    public void AutoGrid_ShouldGiveFlaggedChildNoCellAndSizeItFromTheWholeParent(ChildrenLayout grid)
    {
        ContainerRuntime parent = Parent(grid, 100, 100);
        parent.AutoGridHorizontalCells = 2;
        parent.AutoGridVerticalCells = 2;
        ContainerRuntime flagged = Flagged(100, 50, x: 3, y: 4);
        flagged.WidthUnits = DimensionUnitType.PercentageOfParent;
        flagged.HeightUnits = DimensionUnitType.PercentageOfParent;
        ContainerRuntime a = Box(10, 10);
        ContainerRuntime b = Box(10, 10);
        ContainerRuntime c = Box(10, 10);
        parent.AddChild(flagged);
        parent.AddChild(a);
        parent.AddChild(b);
        parent.AddChild(c);

        a.AbsoluteLeft.ShouldBe(0);
        a.AbsoluteTop.ShouldBe(0);
        if (grid == ChildrenLayout.AutoGridHorizontal)
        {
            b.AbsoluteLeft.ShouldBe(50);
            b.AbsoluteTop.ShouldBe(0);
            c.AbsoluteLeft.ShouldBe(0);
            c.AbsoluteTop.ShouldBe(50);
        }
        else
        {
            b.AbsoluteLeft.ShouldBe(0);
            b.AbsoluteTop.ShouldBe(50);
            c.AbsoluteLeft.ShouldBe(50);
            c.AbsoluteTop.ShouldBe(0);
        }
        flagged.AbsoluteLeft.ShouldBe(3);
        flagged.AbsoluteTop.ShouldBe(4);
        flagged.AbsoluteWidth.ShouldBe(100);
        flagged.AbsoluteHeight.ShouldBe(50);
    }

    [Theory]
    [InlineData(ChildrenLayout.AutoGridHorizontal)]
    [InlineData(ChildrenLayout.AutoGridVertical)]
    public void AutoGridSizedToChildren_ShouldNotAddACellForFlaggedChild_AndCountItsExtentLikeARegularParent(ChildrenLayout grid)
    {
        // Two cells fit the two unflagged children; a third counted child would add a row (or column).
        bool horizontal = grid == ChildrenLayout.AutoGridHorizontal;
        ContainerRuntime parent = Parent(grid, 100, 100);
        parent.AutoGridHorizontalCells = horizontal ? 2 : 1;
        parent.AutoGridVerticalCells = horizontal ? 1 : 2;
        if (horizontal)
        {
            parent.HeightUnits = DimensionUnitType.RelativeToChildren;
            parent.Height = 0;
        }
        else
        {
            parent.WidthUnits = DimensionUnitType.RelativeToChildren;
            parent.Width = 0;
        }
        parent.AddChild(Box(10, 10));
        parent.AddChild(Box(10, 10));
        parent.AddChild(horizontal ? Flagged(5, 5, x: 0, y: 40) : Flagged(5, 5, x: 40, y: 0));

        (horizontal ? parent.AbsoluteHeight : parent.AbsoluteWidth).ShouldBe(45);
    }

    #endregion

    #region Parent sizing

    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack, false, false, 120)]
    [InlineData(ChildrenLayout.TopToBottomStack, true, false, 40)]
    [InlineData(ChildrenLayout.TopToBottomStack, false, true, 80)]
    [InlineData(ChildrenLayout.TopToBottomStack, true, true, 40)]
    [InlineData(ChildrenLayout.LeftToRightStack, false, false, 120)]
    [InlineData(ChildrenLayout.LeftToRightStack, true, false, 40)]
    [InlineData(ChildrenLayout.LeftToRightStack, false, true, 80)]
    [InlineData(ChildrenLayout.LeftToRightStack, true, true, 40)]
    public void StackSizedToChildren_ShouldCountChildByIgnoredBySizeAndLayoutFlags(ChildrenLayout stack,
        bool ignoredByParentSize, bool ignoredByParentLayout, float expectedMainSize)
    {
        // Two stacked children take 40. The third is 30 long at a main-axis offset of 50: stacked, it adds
        // 80 after them; placed as in a Regular parent, it reaches 80 from the start.
        bool vertical = stack == ChildrenLayout.TopToBottomStack;
        ContainerRuntime parent = Parent(stack, 200, 200);
        if (vertical)
        {
            parent.HeightUnits = DimensionUnitType.RelativeToChildren;
            parent.Height = 0;
        }
        else
        {
            parent.WidthUnits = DimensionUnitType.RelativeToChildren;
            parent.Width = 0;
        }
        parent.AddChild(StackChild(stack, 20, 10));
        parent.AddChild(StackChild(stack, 20, 10));
        ContainerRuntime third = vertical ? Box(10, 30, x: 0, y: 50) : Box(30, 10, x: 50, y: 0);
        third.IgnoredByParentSize = ignoredByParentSize;
        third.IgnoredByParentLayout = ignoredByParentLayout;
        parent.AddChild(third);

        (vertical ? parent.AbsoluteHeight : parent.AbsoluteWidth).ShouldBe(expectedMainSize);
    }

    #endregion

    #region Variable path

    [Fact]
    public void SetProperty_ShouldSetIgnoredByParentLayout_AndTryGetPropertyShouldReadIt()
    {
        ContainerRuntime element = new();

        element.SetProperty(nameof(GraphicalUiElement.IgnoredByParentLayout), true);

        element.IgnoredByParentLayout.ShouldBeTrue();
        element.TryGetProperty(nameof(GraphicalUiElement.IgnoredByParentLayout), out object? value).ShouldBeTrue();
        value.ShouldBe(true);
    }

    [Fact]
    public void ToGraphicalUiElement_ShouldCoverStackWithOutline_WhileStackedChildrenKeepTheirPlaces()
    {
        // The #8410 use case: a vertical-stack Unit holds an image and a health bar; an outline sized to the
        // whole Unit is added as a child, flagged so the stack skips it.
        GumProjectSave project = new();
        StandardElementSave containerStandard = new() { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });
        project.StandardElements.Add(containerStandard);

        ComponentSave unit = new() { Name = "Unit", BaseType = "Container" };
        StateSave state = new() { Name = "Default", ParentContainer = unit };
        unit.States.Add(state);
        foreach (var name in new[] { "Image", "Health", "Outline" })
        {
            unit.Instances.Add(new InstanceSave { Name = name, BaseType = "Container", ParentContainer = unit });
        }
        void Set(string name, string type, object value) =>
            state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
        Set("ChildrenLayout", "ChildrenLayout", ChildrenLayout.TopToBottomStack);
        Set("StackSpacing", "float", 2f);
        Set("WidthUnits", "DimensionUnitType", DimensionUnitType.RelativeToChildren);
        Set("HeightUnits", "DimensionUnitType", DimensionUnitType.RelativeToChildren);
        Set("Width", "float", 0f);
        Set("Height", "float", 0f);
        Set("Image.Width", "float", 32f);
        Set("Image.Height", "float", 32f);
        Set("Health.Width", "float", 32f);
        Set("Health.Height", "float", 8f);
        Set("Outline.WidthUnits", "DimensionUnitType", DimensionUnitType.RelativeToParent);
        Set("Outline.HeightUnits", "DimensionUnitType", DimensionUnitType.RelativeToParent);
        Set("Outline.Width", "float", 0f);
        Set("Outline.Height", "float", 0f);
        Set("Outline.IgnoredByParentLayout", "bool", true);
        project.Components.Add(unit);
        ObjectFinder.Self.GumProjectSave = project;

        GraphicalUiElement unitRuntime = unit.ToGraphicalUiElement();
        GraphicalUiElement image = unitRuntime.GetGraphicalUiElementByName("Image")!;
        GraphicalUiElement health = unitRuntime.GetGraphicalUiElementByName("Health")!;
        GraphicalUiElement outline = unitRuntime.GetGraphicalUiElementByName("Outline")!;

        image.AbsoluteTop.ShouldBe(0);
        health.AbsoluteTop.ShouldBe(34);
        unitRuntime.AbsoluteWidth.ShouldBe(32);
        unitRuntime.AbsoluteHeight.ShouldBe(42);
        outline.AbsoluteLeft.ShouldBe(0);
        outline.AbsoluteTop.ShouldBe(0);
        outline.AbsoluteWidth.ShouldBe(32);
        outline.AbsoluteHeight.ShouldBe(42);
    }

    #endregion

    #region Sweep

    static readonly ChildrenLayout[] SweepLayouts =
    {
        ChildrenLayout.TopToBottomStack, ChildrenLayout.LeftToRightStack,
        ChildrenLayout.AutoGridHorizontal, ChildrenLayout.AutoGridVertical,
    };

    record SweepCase(ChildrenLayout Layout, bool Wraps, float Spacing, List<(float Width, float Height)> Siblings,
        int FlaggedIndex, float FlaggedWidth, float FlaggedHeight, float FlaggedX, float FlaggedY, bool SizedToChildren);

    static SweepCase MakeSweepCase(Random random, bool sizedToChildren)
    {
        var layout = SweepLayouts[random.Next(SweepLayouts.Length)];
        int count = random.Next(1, 6);
        List<(float, float)> siblings = new();
        for (int i = 0; i < count; i++)
        {
            siblings.Add((random.Next(5, 41), random.Next(5, 41)));
        }
        return new SweepCase(layout, random.Next(2) == 0, new[] { 0f, 3f, 7f }[random.Next(3)], siblings,
            random.Next(0, count + 1), random.Next(5, 120), random.Next(5, 120), random.Next(0, 31), random.Next(0, 31), sizedToChildren);
    }

    static (ContainerRuntime Parent, List<ContainerRuntime> Siblings, ContainerRuntime? Flagged) BuildSweepTree(SweepCase sweepCase, bool includeFlagged)
    {
        ContainerRuntime parent = Parent(sweepCase.Layout, 100, 100);
        parent.WrapsChildren = sweepCase.Wraps;
        parent.StackSpacing = sweepCase.Spacing;
        parent.AutoGridHorizontalCells = 2;
        parent.AutoGridVerticalCells = 2;
        if (sweepCase.SizedToChildren)
        {
            parent.WidthUnits = DimensionUnitType.RelativeToChildren;
            parent.HeightUnits = DimensionUnitType.RelativeToChildren;
            parent.Width = 0;
            parent.Height = 0;
        }
        List<ContainerRuntime> siblings = new();
        ContainerRuntime? flagged = null;
        for (int i = 0; i <= sweepCase.Siblings.Count; i++)
        {
            if (i == sweepCase.FlaggedIndex && includeFlagged)
            {
                flagged = Flagged(sweepCase.FlaggedWidth, sweepCase.FlaggedHeight, sweepCase.FlaggedX, sweepCase.FlaggedY);
                parent.AddChild(flagged);
            }
            if (i < sweepCase.Siblings.Count)
            {
                ContainerRuntime sibling = Box(sweepCase.Siblings[i].Width, sweepCase.Siblings[i].Height);
                siblings.Add(sibling);
                parent.AddChild(sibling);
            }
        }
        return (parent, siblings, flagged);
    }

    [Fact]
    public void Sweep_ShouldPlaceSiblingsAsInTheTreeWithoutTheFlaggedChild_AndSizeTheParentAsARegularParentWould()
    {
        // Each case builds the tree with and without a flagged child. Siblings must not move, the flagged child sits at
        // its own position, and a parent sized to its children grows only to the flagged child's far edges.
        Random random = new Random(8411);
        List<string> failures = new();
        int caseCount = SweepScale.Cases(300);
        for (int iteration = 0; iteration < caseCount; iteration++)
        {
            SweepCase sweepCase = MakeSweepCase(random, sizedToChildren: iteration % 2 == 1);
            var (withParent, withSiblings, flagged) = BuildSweepTree(sweepCase, includeFlagged: true);
            var (withoutParent, withoutSiblings, _) = BuildSweepTree(sweepCase, includeFlagged: false);
            string tag = $"#{iteration} {sweepCase.Layout} wrap={sweepCase.Wraps} spacing={sweepCase.Spacing} n={sweepCase.Siblings.Count} " +
                $"flaggedIndex={sweepCase.FlaggedIndex} flagged=({sweepCase.FlaggedX},{sweepCase.FlaggedY},{sweepCase.FlaggedWidth}x{sweepCase.FlaggedHeight}) sized={sweepCase.SizedToChildren}";

            if (!sweepCase.SizedToChildren)
            {
                for (int i = 0; i < withoutSiblings.Count; i++)
                {
                    GraphicalUiElement expected = withoutSiblings[i];
                    GraphicalUiElement actual = withSiblings[i];
                    if (expected.AbsoluteLeft != actual.AbsoluteLeft || expected.AbsoluteTop != actual.AbsoluteTop ||
                        expected.AbsoluteWidth != actual.AbsoluteWidth || expected.AbsoluteHeight != actual.AbsoluteHeight)
                    {
                        failures.Add($"{tag} sibling {i}: {expected.AbsoluteLeft},{expected.AbsoluteTop} vs {actual.AbsoluteLeft},{actual.AbsoluteTop}");
                    }
                }
                if (flagged!.AbsoluteLeft != sweepCase.FlaggedX || flagged.AbsoluteTop != sweepCase.FlaggedY ||
                    flagged.AbsoluteWidth != sweepCase.FlaggedWidth || flagged.AbsoluteHeight != sweepCase.FlaggedHeight)
                {
                    failures.Add($"{tag} flagged at {flagged.AbsoluteLeft},{flagged.AbsoluteTop} {flagged.AbsoluteWidth}x{flagged.AbsoluteHeight}");
                }
            }
            else
            {
                float expectedWidth = Math.Max(withoutParent.AbsoluteWidth, sweepCase.FlaggedX + sweepCase.FlaggedWidth);
                float expectedHeight = Math.Max(withoutParent.AbsoluteHeight, sweepCase.FlaggedY + sweepCase.FlaggedHeight);
                if (withParent.AbsoluteWidth != expectedWidth || withParent.AbsoluteHeight != expectedHeight)
                {
                    failures.Add($"{tag} parent {withParent.AbsoluteWidth}x{withParent.AbsoluteHeight} expected {expectedWidth}x{expectedHeight}");
                }
            }
        }
        failures.ShouldBeEmpty(string.Join("\n", failures));
    }

    #endregion
}
