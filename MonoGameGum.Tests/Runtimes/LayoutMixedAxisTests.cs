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
/// Section 5 of LAYOUT_TEST_PLAN.md: a child depends on its parent along one axis while the parent
/// depends on the child along the other, in a Regular-layout parent. Tests drive layout through
/// public properties only and assert on absolute positions and sizes.
/// </summary>
public class LayoutMixedAxisTests : BaseTestClass
{
    const string WrappingText = "The quick brown fox jumps over the lazy dog again and again";

    class TexturedRenderable : InvisibleRenderable, IAspectRatio
    {
        public float AspectRatio { get; set; } = 1;
    }

    public enum ParentAxes
    {
        AbsoluteWidth_RelativeToChildrenHeight,
        RelativeToChildrenWidth_AbsoluteHeight,
        PercentageOfParentWidth_RelativeToChildrenHeight,
        RelativeToMaxParentOrChildrenWidth_RelativeToChildrenHeight,
        RelativeToChildrenWidth_RelativeToChildrenHeight,
    }

    public enum SelfDependency
    {
        PercentageOfOtherDimension,
        MaintainFileAspectRatio,
        WrappingText,
        RelativeToChildrenHoldingWrappingText,
    }

    public enum Operation
    {
        Build,
        ResizeGrandparentAndBack,
        ChangeDrivingValue,
        SecondUpdateLayout,
    }

    class Tree
    {
        public ContainerRuntime Grandparent = null!;
        public ContainerRuntime Parent = null!;
        public GraphicalUiElement Child = null!;
        public GraphicalUiElement? Inner;

        public List<GraphicalUiElement> All
        {
            get
            {
                List<GraphicalUiElement> all = new() { Grandparent, Parent, Child };
                if (Inner != null)
                {
                    all.Add(Inner);
                }
                return all;
            }
        }
    }

    #region Helpers

    static ContainerRuntime CreateContainer(float width, DimensionUnitType widthUnits, float height, DimensionUnitType heightUnits)
    {
        ContainerRuntime container = new();
        container.Width = width;
        container.WidthUnits = widthUnits;
        container.Height = height;
        container.HeightUnits = heightUnits;
        return container;
    }

    static TextRuntime CreateWrappingText(float width, DimensionUnitType widthUnits)
    {
        TextRuntime text = new();
        text.Text = WrappingText;
        text.Width = width;
        text.WidthUnits = widthUnits;
        text.Height = 0;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        return text;
    }

    /// <summary>
    /// Height of the wrapping text laid out alone at a fixed width, measured from the text itself.
    /// </summary>
    static float MeasureWrappedHeight(float width)
    {
        TextRuntime reference = CreateWrappingText(width, DimensionUnitType.Absolute);
        return reference.AbsoluteHeight;
    }

    static int MeasureWrappedLineCount(float width)
    {
        TextRuntime reference = CreateWrappingText(width, DimensionUnitType.Absolute);
        return reference.WrappedText.Count;
    }

    static float[] Snapshot(Tree tree)
    {
        List<float> values = new();
        foreach (GraphicalUiElement element in tree.All)
        {
            values.Add(element.AbsoluteLeft);
            values.Add(element.AbsoluteTop);
            values.Add(element.AbsoluteWidth);
            values.Add(element.AbsoluteHeight);
        }
        return values.ToArray();
    }

    static Tree BuildTree(ParentAxes parentAxes, float grandparentWidth, float grandparentHeight, float parentFixedSize,
        DimensionUnitType childDrivenUnit, float childDrivenValue, SelfDependency selfDependency,
        float percentageOfOther, float aspectRatio)
    {
        Tree tree = new();
        tree.Grandparent = CreateContainer(grandparentWidth, DimensionUnitType.Absolute, grandparentHeight, DimensionUnitType.Absolute);

        tree.Parent = parentAxes switch
        {
            ParentAxes.AbsoluteWidth_RelativeToChildrenHeight =>
                CreateContainer(parentFixedSize, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren),
            ParentAxes.RelativeToChildrenWidth_AbsoluteHeight =>
                CreateContainer(0, DimensionUnitType.RelativeToChildren, parentFixedSize, DimensionUnitType.Absolute),
            ParentAxes.PercentageOfParentWidth_RelativeToChildrenHeight =>
                CreateContainer(50, DimensionUnitType.PercentageOfParent, 0, DimensionUnitType.RelativeToChildren),
            ParentAxes.RelativeToMaxParentOrChildrenWidth_RelativeToChildrenHeight =>
                CreateContainer(0, DimensionUnitType.RelativeToMaxParentOrChildren, 0, DimensionUnitType.RelativeToChildren),
            // Width value is padding, so the parent has a width even though the child is ignored on that axis.
            ParentAxes.RelativeToChildrenWidth_RelativeToChildrenHeight =>
                CreateContainer(parentFixedSize, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.RelativeToChildren),
            _ => throw new ArgumentOutOfRangeException(nameof(parentAxes)),
        };
        tree.Grandparent.AddChild(tree.Parent);

        bool isWidthDriven = parentAxes != ParentAxes.RelativeToChildrenWidth_AbsoluteHeight;

        GraphicalUiElement child;
        switch (selfDependency)
        {
            case SelfDependency.PercentageOfOtherDimension:
                child = new ContainerRuntime();
                break;
            case SelfDependency.MaintainFileAspectRatio:
                child = new GraphicalUiElement(new TexturedRenderable { AspectRatio = aspectRatio });
                break;
            case SelfDependency.WrappingText:
                child = CreateWrappingText(0, DimensionUnitType.Absolute);
                break;
            case SelfDependency.RelativeToChildrenHoldingWrappingText:
                child = new ContainerRuntime();
                tree.Inner = CreateWrappingText(0, DimensionUnitType.RelativeToParent);
                child.AddChild(tree.Inner);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(selfDependency));
        }

        DimensionUnitType selfUnit = selfDependency switch
        {
            SelfDependency.PercentageOfOtherDimension => DimensionUnitType.PercentageOfOtherDimension,
            SelfDependency.MaintainFileAspectRatio => DimensionUnitType.MaintainFileAspectRatio,
            _ => DimensionUnitType.RelativeToChildren,
        };
        float selfValue = selfDependency switch
        {
            SelfDependency.PercentageOfOtherDimension => percentageOfOther,
            SelfDependency.MaintainFileAspectRatio => 100,
            _ => 0,
        };

        if (isWidthDriven)
        {
            child.Width = childDrivenValue;
            child.WidthUnits = childDrivenUnit;
            child.Height = selfValue;
            child.HeightUnits = selfUnit;
        }
        else
        {
            child.Height = childDrivenValue;
            child.HeightUnits = childDrivenUnit;
            child.Width = selfValue;
            child.WidthUnits = selfUnit;
        }

        tree.Child = child;
        tree.Parent.AddChild(child);
        return tree;
    }

    static void SetDrivenValue(Tree tree, ParentAxes parentAxes, float value)
    {
        if (parentAxes == ParentAxes.RelativeToChildrenWidth_AbsoluteHeight)
        {
            tree.Child.Height = value;
        }
        else
        {
            tree.Child.Width = value;
        }
    }

    #endregion

    #region Named cases

    [Fact]
    public void M2_RelativeToChildrenWidthParent_ShouldMeasurePercentageOfOtherDimensionChild_DrivenByParentHeight()
    {
        ContainerRuntime parent = CreateContainer(0, DimensionUnitType.RelativeToChildren, 300, DimensionUnitType.Absolute);
        ContainerRuntime child = CreateContainer(100, DimensionUnitType.PercentageOfOtherDimension, 50, DimensionUnitType.PercentageOfParent);
        parent.AddChild(child);

        child.AbsoluteHeight.ShouldBe(150);
        child.AbsoluteWidth.ShouldBe(150);
        parent.AbsoluteWidth.ShouldBe(150);
    }

    [Fact]
    public void M3_RelativeToChildrenHeightParent_ShouldMeasureWrappedTextChild_AtParentGivenWidth()
    {
        float childWidth = 150;
        float expectedHeight = MeasureWrappedHeight(childWidth);
        MeasureWrappedLineCount(childWidth).ShouldBeGreaterThan(1);
        ContainerRuntime parent = CreateContainer(300, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren);
        TextRuntime text = CreateWrappingText(50, DimensionUnitType.PercentageOfParent);

        parent.AddChild(text);

        text.AbsoluteWidth.ShouldBe(childWidth);
        text.AbsoluteHeight.ShouldBe(expectedHeight);
        parent.AbsoluteHeight.ShouldBe(expectedHeight);
    }

    [Fact]
    public void M4_RelativeToChildrenHeightParent_ShouldMeasureMaintainFileAspectRatioChild_AtParentGivenWidth()
    {
        ContainerRuntime parent = CreateContainer(400, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren);
        GraphicalUiElement child = new(new TexturedRenderable { AspectRatio = 2 });
        child.Width = 50;
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Height = 100;
        child.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;

        parent.AddChild(child);

        child.AbsoluteWidth.ShouldBe(200);
        child.AbsoluteHeight.ShouldBe(100);
        parent.AbsoluteHeight.ShouldBe(100);
    }

    [Fact]
    public void M5_RelativeToChildrenHeightParent_ShouldMeasureNestedWrappedText_ThroughRelativeToChildrenChild()
    {
        float childWidth = 150;
        float expectedHeight = MeasureWrappedHeight(childWidth);
        MeasureWrappedLineCount(childWidth).ShouldBeGreaterThan(1);
        ContainerRuntime parent = CreateContainer(300, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren);
        ContainerRuntime child = CreateContainer(50, DimensionUnitType.PercentageOfParent, 0, DimensionUnitType.RelativeToChildren);
        TextRuntime text = CreateWrappingText(0, DimensionUnitType.RelativeToParent);
        child.AddChild(text);

        parent.AddChild(child);

        text.AbsoluteWidth.ShouldBe(childWidth);
        text.AbsoluteHeight.ShouldBe(expectedHeight);
        child.AbsoluteHeight.ShouldBe(expectedHeight);
        parent.AbsoluteHeight.ShouldBe(expectedHeight);
    }

    [Theory]
    [InlineData(SelfDependency.PercentageOfOtherDimension)]
    [InlineData(SelfDependency.MaintainFileAspectRatio)]
    [InlineData(SelfDependency.WrappingText)]
    [InlineData(SelfDependency.RelativeToChildrenHoldingWrappingText)]
    public void M9_ChildHeight_ShouldFollowGrandparentWidth_WhenGrandparentNarrowsAndWidens(SelfDependency selfDependency)
    {
        float wideWidth = 400;
        float narrowWidth = 200;
        float percentageOfOther = 50;
        float aspectRatio = 2;
        // Parent is 50% of the grandparent and the child 50% of the parent.
        float wideChildWidth = wideWidth / 4;
        float narrowChildWidth = narrowWidth / 4;
        float wideHeight = selfDependency switch
        {
            SelfDependency.PercentageOfOtherDimension => wideChildWidth * percentageOfOther / 100,
            SelfDependency.MaintainFileAspectRatio => wideChildWidth / aspectRatio,
            _ => MeasureWrappedHeight(wideChildWidth),
        };
        float narrowHeight = selfDependency switch
        {
            SelfDependency.PercentageOfOtherDimension => narrowChildWidth * percentageOfOther / 100,
            SelfDependency.MaintainFileAspectRatio => narrowChildWidth / aspectRatio,
            _ => MeasureWrappedHeight(narrowChildWidth),
        };
        Tree tree = BuildTree(ParentAxes.PercentageOfParentWidth_RelativeToChildrenHeight, wideWidth, 300, 0,
            DimensionUnitType.PercentageOfParent, 50, selfDependency, percentageOfOther, aspectRatio);

        tree.Grandparent.Width = narrowWidth;

        tree.Child.AbsoluteWidth.ShouldBe(narrowChildWidth);
        tree.Child.AbsoluteHeight.ShouldBe(narrowHeight);
        tree.Parent.AbsoluteHeight.ShouldBe(narrowHeight);

        tree.Grandparent.Width = wideWidth;

        tree.Child.AbsoluteWidth.ShouldBe(wideChildWidth);
        tree.Child.AbsoluteHeight.ShouldBe(wideHeight);
        tree.Parent.AbsoluteHeight.ShouldBe(wideHeight);
    }

    [Fact]
    public void M10_RelativeToMaxParentOrChildrenHeightParent_ShouldTakeLargerOfGrandparentAndChild_WithoutRatchet()
    {
        ContainerRuntime grandparent = CreateContainer(400, DimensionUnitType.Absolute, 300, DimensionUnitType.Absolute);
        ContainerRuntime parent = CreateContainer(300, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToMaxParentOrChildren);
        grandparent.AddChild(parent);
        // Child is 150 wide; height is 100% (150) then 400% (600) of that.
        ContainerRuntime child = CreateContainer(50, DimensionUnitType.PercentageOfParent, 100, DimensionUnitType.PercentageOfOtherDimension);
        parent.AddChild(child);

        parent.AbsoluteHeight.ShouldBe(300);

        child.Height = 400;
        parent.AbsoluteHeight.ShouldBe(600);
        grandparent.UpdateLayout();
        parent.AbsoluteHeight.ShouldBe(600);

        child.Height = 100;
        parent.AbsoluteHeight.ShouldBe(300);
        child.AbsoluteHeight.ShouldBe(150);
    }

    [Fact]
    public void M11_RelativeToChildrenParent_ShouldIgnorePercentageChildOnItsAxis_AndMeasureItOnTheOther()
    {
        // Width value is padding: the parent is 200 wide from padding alone.
        ContainerRuntime parent = CreateContainer(200, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.RelativeToChildren);
        ContainerRuntime child = CreateContainer(50, DimensionUnitType.PercentageOfParent, 80, DimensionUnitType.Absolute);
        parent.AddChild(child);

        parent.AbsoluteWidth.ShouldBe(200);
        parent.AbsoluteHeight.ShouldBe(80);
        child.AbsoluteWidth.ShouldBe(100);

        parent.UpdateLayout();

        parent.AbsoluteWidth.ShouldBe(200);
        child.AbsoluteWidth.ShouldBe(100);
    }

    #endregion

    #region Ratio child in a measuring stack or grid

    static float[] SnapshotAll(GraphicalUiElement parent)
    {
        List<float> values = new()
        {
            parent.AbsoluteLeft, parent.AbsoluteTop, parent.AbsoluteWidth, parent.AbsoluteHeight,
        };
        foreach (GraphicalUiElement child in parent.Children)
        {
            values.Add(child.AbsoluteLeft);
            values.Add(child.AbsoluteTop);
            values.Add(child.AbsoluteWidth);
            values.Add(child.AbsoluteHeight);
        }
        return values.ToArray();
    }

    [Fact]
    public void LeftToRightStack_RelativeToChildrenWidth_ShouldIgnoreRatioChild_AndBeStable()
    {
        float padding = 50;
        float siblingWidth = 100;
        ContainerRuntime parent = CreateContainer(padding, DimensionUnitType.RelativeToChildren, 100, DimensionUnitType.Absolute);
        parent.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        parent.AddChild(CreateContainer(siblingWidth, DimensionUnitType.Absolute, 50, DimensionUnitType.Absolute));
        ContainerRuntime ratioChild = CreateContainer(1, DimensionUnitType.Ratio, 50, DimensionUnitType.Absolute);
        parent.AddChild(ratioChild);

        parent.AbsoluteWidth.ShouldBe(siblingWidth + padding);
        ratioChild.AbsoluteWidth.ShouldBe(padding);
        ratioChild.AbsoluteLeft.ShouldBe(siblingWidth);
        float[] before = SnapshotAll(parent);
        parent.UpdateLayout();
        SnapshotAll(parent).ShouldBe(before);
    }

    [Fact]
    public void TopToBottomStack_RelativeToChildrenHeight_ShouldIgnoreRatioChild_AndBeStable()
    {
        float padding = 50;
        float siblingHeight = 100;
        ContainerRuntime parent = CreateContainer(100, DimensionUnitType.Absolute, padding, DimensionUnitType.RelativeToChildren);
        parent.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        parent.AddChild(CreateContainer(50, DimensionUnitType.Absolute, siblingHeight, DimensionUnitType.Absolute));
        ContainerRuntime ratioChild = CreateContainer(50, DimensionUnitType.Absolute, 1, DimensionUnitType.Ratio);
        parent.AddChild(ratioChild);

        parent.AbsoluteHeight.ShouldBe(siblingHeight + padding);
        ratioChild.AbsoluteHeight.ShouldBe(padding);
        ratioChild.AbsoluteTop.ShouldBe(siblingHeight);
        float[] before = SnapshotAll(parent);
        parent.UpdateLayout();
        SnapshotAll(parent).ShouldBe(before);
    }

    [Fact]
    public void AutoGridHorizontal_RelativeToChildrenWidth_ShouldIgnoreRatioChild_AndBeStable()
    {
        float padding = 20;
        float siblingWidth = 100;
        int columns = 2;
        ContainerRuntime grid = CreateContainer(padding, DimensionUnitType.RelativeToChildren, 100, DimensionUnitType.Absolute);
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = columns;
        grid.AutoGridVerticalCells = 1;
        grid.AddChild(CreateContainer(siblingWidth, DimensionUnitType.Absolute, 50, DimensionUnitType.Absolute));
        ContainerRuntime ratioChild = CreateContainer(1, DimensionUnitType.Ratio, 50, DimensionUnitType.Absolute);
        grid.AddChild(ratioChild);

        float expectedGridWidth = siblingWidth * columns + padding;
        grid.AbsoluteWidth.ShouldBe(expectedGridWidth);
        ratioChild.AbsoluteWidth.ShouldBe(expectedGridWidth / columns);
        float[] before = SnapshotAll(grid);
        grid.UpdateLayout();
        SnapshotAll(grid).ShouldBe(before);
    }

    [Fact]
    public void AutoGridVertical_RelativeToChildrenHeight_ShouldIgnoreRatioChild_AndBeStable()
    {
        float padding = 20;
        float siblingHeight = 100;
        int rows = 2;
        ContainerRuntime grid = CreateContainer(100, DimensionUnitType.Absolute, padding, DimensionUnitType.RelativeToChildren);
        grid.ChildrenLayout = ChildrenLayout.AutoGridVertical;
        grid.AutoGridHorizontalCells = 1;
        grid.AutoGridVerticalCells = rows;
        grid.AddChild(CreateContainer(50, DimensionUnitType.Absolute, siblingHeight, DimensionUnitType.Absolute));
        ContainerRuntime ratioChild = CreateContainer(50, DimensionUnitType.Absolute, 1, DimensionUnitType.Ratio);
        grid.AddChild(ratioChild);

        float expectedGridHeight = siblingHeight * rows + padding;
        grid.AbsoluteHeight.ShouldBe(expectedGridHeight);
        ratioChild.AbsoluteHeight.ShouldBe(expectedGridHeight / rows);
        float[] before = SnapshotAll(grid);
        grid.UpdateLayout();
        SnapshotAll(grid).ShouldBe(before);
    }

    #endregion

    #region Matrix

    static readonly DimensionUnitType[] DrivenUnits =
    {
        DimensionUnitType.PercentageOfParent,
        DimensionUnitType.RelativeToParent,
        DimensionUnitType.Ratio,
        DimensionUnitType.RelativeToMaxParentOrChildren,
    };

    public static IEnumerable<object[]> MatrixCases()
    {
        foreach (ParentAxes parentAxes in Enum.GetValues<ParentAxes>())
        {
            foreach (DimensionUnitType drivenUnit in DrivenUnits)
            {
                foreach (SelfDependency selfDependency in Enum.GetValues<SelfDependency>())
                {
                    // Text derives height from width, so it has no mirror on a height-driven child.
                    bool isTextBased = selfDependency == SelfDependency.WrappingText ||
                        selfDependency == SelfDependency.RelativeToChildrenHoldingWrappingText;
                    if (isTextBased && parentAxes == ParentAxes.RelativeToChildrenWidth_AbsoluteHeight)
                    {
                        continue;
                    }

                    foreach (Operation operation in Enum.GetValues<Operation>())
                    {
                        yield return new object[] { parentAxes, drivenUnit, selfDependency, operation };
                    }
                }
            }
        }
    }

    static float InitialDrivenValue(DimensionUnitType unit) => unit switch
    {
        DimensionUnitType.PercentageOfParent => 50,
        DimensionUnitType.RelativeToParent => -40,
        DimensionUnitType.Ratio => 1,
        _ => 0,
    };

    static float ChangedDrivenValue(DimensionUnitType unit) => unit switch
    {
        DimensionUnitType.PercentageOfParent => 25,
        DimensionUnitType.RelativeToParent => -100,
        DimensionUnitType.Ratio => 2,
        _ => 350,
    };

    [Theory]
    [MemberData(nameof(MatrixCases))]
    public void Matrix_ShouldMatchModel(ParentAxes parentAxes, DimensionUnitType drivenUnit, SelfDependency selfDependency, Operation operation)
    {
        float grandparentWidth = 400;
        float grandparentHeight = 300;
        float parentFixedSize = 300;
        float percentageOfOther = 50;
        float aspectRatio = 2;
        float drivenValue = InitialDrivenValue(drivenUnit);
        bool isWidthDriven = parentAxes != ParentAxes.RelativeToChildrenWidth_AbsoluteHeight;

        Tree tree = BuildTree(parentAxes, grandparentWidth, grandparentHeight, parentFixedSize,
            drivenUnit, drivenValue, selfDependency, percentageOfOther, aspectRatio);

        if (operation == Operation.ChangeDrivingValue)
        {
            drivenValue = ChangedDrivenValue(drivenUnit);
            SetDrivenValue(tree, parentAxes, drivenValue);
        }

        // The model: only a RelativeToMaxParentOrChildren child counts toward a parent measured
        // from children on the driven axis, and it contributes its own value as padding.
        float childContribution = drivenUnit == DimensionUnitType.RelativeToMaxParentOrChildren ? drivenValue : 0;
        float parentDriven = parentAxes switch
        {
            ParentAxes.AbsoluteWidth_RelativeToChildrenHeight => parentFixedSize,
            ParentAxes.RelativeToChildrenWidth_AbsoluteHeight => parentFixedSize,
            ParentAxes.PercentageOfParentWidth_RelativeToChildrenHeight => grandparentWidth * 50 / 100,
            ParentAxes.RelativeToMaxParentOrChildrenWidth_RelativeToChildrenHeight => Math.Max(grandparentWidth, childContribution),
            _ => childContribution + parentFixedSize,
        };
        float childDriven = drivenUnit switch
        {
            DimensionUnitType.PercentageOfParent => parentDriven * drivenValue / 100,
            DimensionUnitType.RelativeToParent => parentDriven + drivenValue,
            DimensionUnitType.Ratio => parentDriven,
            _ => Math.Max(parentDriven, drivenValue),
        };
        float childMeasured = selfDependency switch
        {
            SelfDependency.PercentageOfOtherDimension => childDriven * percentageOfOther / 100,
            SelfDependency.MaintainFileAspectRatio => isWidthDriven ? childDriven / aspectRatio : childDriven * aspectRatio,
            _ => MeasureWrappedHeight(childDriven),
        };

        float[] before = Snapshot(tree);
        if (operation == Operation.ResizeGrandparentAndBack)
        {
            tree.Grandparent.Width = grandparentWidth / 2;
            tree.Grandparent.Height = grandparentHeight / 2;
            tree.Grandparent.Width = grandparentWidth;
            tree.Grandparent.Height = grandparentHeight;
            Snapshot(tree).ShouldBe(before);
        }
        else if (operation == Operation.SecondUpdateLayout)
        {
            foreach (GraphicalUiElement element in tree.All)
            {
                element.UpdateLayout();
                Snapshot(tree).ShouldBe(before, $"after {element.GetType().Name}.UpdateLayout()");
            }
        }

        float childWidth = isWidthDriven ? childDriven : childMeasured;
        float childHeight = isWidthDriven ? childMeasured : childDriven;
        tree.Child.AbsoluteLeft.ShouldBe(0);
        tree.Child.AbsoluteTop.ShouldBe(0);
        tree.Child.AbsoluteWidth.ShouldBe(childWidth, tolerance: 0.001f);
        tree.Child.AbsoluteHeight.ShouldBe(childHeight, tolerance: 0.001f);
        if (isWidthDriven)
        {
            tree.Parent.AbsoluteWidth.ShouldBe(parentDriven, tolerance: 0.001f);
            tree.Parent.AbsoluteHeight.ShouldBe(childMeasured, tolerance: 0.001f);
        }
        else
        {
            tree.Parent.AbsoluteWidth.ShouldBe(childMeasured, tolerance: 0.001f);
            tree.Parent.AbsoluteHeight.ShouldBe(parentDriven, tolerance: 0.001f);
        }
        if (tree.Inner != null)
        {
            tree.Inner.AbsoluteWidth.ShouldBe(childDriven, tolerance: 0.001f);
            tree.Inner.AbsoluteHeight.ShouldBe(childMeasured, tolerance: 0.001f);
        }
    }

    #endregion
}
