using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Assigning Width runs a layout unless nothing but the element's own width can change (#8059). The first test
/// pins the skip; the rest pin the cases that must still lay out.
/// </summary>
public class LayoutWidthSetterShortCircuitTests : BaseTestClass
{
    class FakeText : InvisibleRenderable, IText
    {
        public float DescenderHeight { get; set; }
        public float FontScale { get; set; } = 1;
        public float WrappedTextWidth { get; set; }
        public float WrappedTextHeight { get; set; }
        public string? RawText { get; set; } = "text";
        public string? StoredMarkupText => RawText;
        float? IText.Width { get; set; }
        public TextOverflowVerticalMode TextOverflowVerticalMode { get; set; }
        public int PreRenderUpdates;
        public void SetNeedsRefreshToTrue() { }
        public void UpdatePreRenderDimensions() { PreRenderUpdates++; }
    }

    static int LayoutsDuring(System.Action action)
    {
        int before = GraphicalUiElement.UpdateLayoutCallCount;
        action();
        return GraphicalUiElement.UpdateLayoutCallCount - before;
    }

    #region Skipped

    [Fact]
    public void Width_ShouldNotLayOut_WhenAbsoluteLeafInRegularParent()
    {
        ContainerRuntime parent = new();
        ContainerRuntime leaf = new() { Width = 10 };
        parent.AddChild(leaf);

        int sizeChanges = 0;
        leaf.SizeChanged += (_, _) => sizeChanges++;

        int layouts = LayoutsDuring(() => leaf.Width = 25);

        layouts.ShouldBe(0);
        leaf.GetAbsoluteWidth().ShouldBe(25);
        sizeChanges.ShouldBe(1);
    }

    [Fact]
    public void Width_ShouldNotLayOut_WhenAbsoluteLeafHasNoParent()
    {
        ContainerRuntime leaf = new() { Width = 10 };

        LayoutsDuring(() => leaf.Width = 25).ShouldBe(0);
        leaf.GetAbsoluteWidth().ShouldBe(25);
    }

    [Fact]
    public void Width_ShouldClampToMax_WhenSkippingLayout()
    {
        ContainerRuntime leaf = new() { Width = 10, MaxWidth = 30, MinWidth = 5 };

        leaf.Width = 100;
        leaf.GetAbsoluteWidth().ShouldBe(30);
        leaf.Width = 1;
        leaf.GetAbsoluteWidth().ShouldBe(5);
    }

    #endregion

    #region Must still lay out

    [Fact]
    public void Width_ShouldMoveElement_WhenXOriginIsCenter()
    {
        ContainerRuntime parent = new() { Width = 200 };
        ContainerRuntime leaf = new() { Width = 10, X = 100, XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center };
        parent.AddChild(leaf);

        leaf.Width = 50;

        leaf.AbsoluteLeft.ShouldBe(75);
    }

    [Fact]
    public void Width_ShouldMoveElement_WhenParentIsFlippedHorizontally()
    {
        ContainerRuntime parent = new() { Width = 200 };
        parent.FlipHorizontal = true;
        ContainerRuntime leaf = new() { Width = 10, X = 0 };
        parent.AddChild(leaf);
        float leftAt10 = leaf.AbsoluteLeft;

        leaf.Width = 50;

        leaf.AbsoluteLeft.ShouldBe(leftAt10 - 40);
    }

    [Fact]
    public void Width_ShouldMoveSiblings_WhenParentStacks()
    {
        ContainerRuntime parent = new() { ChildrenLayout = ChildrenLayout.LeftToRightStack };
        ContainerRuntime first = new() { Width = 10 };
        ContainerRuntime second = new() { Width = 10 };
        parent.AddChild(first);
        parent.AddChild(second);

        first.Width = 40;

        second.AbsoluteLeft.ShouldBe(40);
    }

    [Fact]
    public void Width_ShouldResizeParent_WhenParentIsRelativeToChildren()
    {
        ContainerRuntime parent = new() { WidthUnits = DimensionUnitType.RelativeToChildren, Width = 0 };
        ContainerRuntime leaf = new() { Width = 10 };
        parent.AddChild(leaf);

        leaf.Width = 60;

        parent.GetAbsoluteWidth().ShouldBe(60);
    }

    [Fact]
    public void Width_ShouldResizeRatioSibling_WhenSiblingUsesRatio()
    {
        ContainerRuntime parent = new() { Width = 100 };
        ContainerRuntime fixedChild = new() { Width = 20 };
        ContainerRuntime ratioChild = new() { Width = 1, WidthUnits = DimensionUnitType.Ratio };
        parent.AddChild(fixedChild);
        parent.AddChild(ratioChild);

        fixedChild.Width = 70;

        ratioChild.GetAbsoluteWidth().ShouldBe(30);
    }

    [Fact]
    public void Width_ShouldResizeChildren_WhenChildrenFollowParentWidth()
    {
        ContainerRuntime parent = new() { Width = 100 };
        ContainerRuntime child = new() { Width = 50, WidthUnits = DimensionUnitType.PercentageOfParent };
        parent.AddChild(child);

        parent.Width = 200;

        child.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void Width_ShouldResizeHeight_WhenHeightIsPercentageOfWidth()
    {
        ContainerRuntime leaf = new() { Width = 10, Height = 100, HeightUnits = DimensionUnitType.PercentageOfOtherDimension };

        leaf.Width = 40;

        leaf.GetAbsoluteHeight().ShouldBe(40);
    }

    [Fact]
    public void Width_ShouldApplyAfterResume_WhenLayoutIsSuspended()
    {
        ContainerRuntime parent = new() { Width = 200 };
        ContainerRuntime leaf = new() { Width = 10 };
        parent.AddChild(leaf);

        int layouts = LayoutsDuring(() =>
        {
            leaf.SuspendLayout();
            leaf.Width = 80;
            leaf.ResumeLayout();
        });

        layouts.ShouldBeGreaterThan(0);
        leaf.GetAbsoluteWidth().ShouldBe(80);
    }

    [Fact]
    public void Width_ShouldApplyAfterShown_WhenElementIsHidden()
    {
        ContainerRuntime parent = new() { Width = 200 };
        ContainerRuntime leaf = new() { Width = 10 };
        parent.AddChild(leaf);
        leaf.Visible = false;

        leaf.Width = 80;
        leaf.Visible = true;

        leaf.GetAbsoluteWidth().ShouldBe(80);
    }

    [Fact]
    public void Width_ShouldLayOut_WhenElementIsRotated()
    {
        ContainerRuntime leaf = new() { Width = 10, Rotation = 90 };

        LayoutsDuring(() => leaf.Width = 25).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Width_ShouldLayOut_WhenContainedObjectIsText()
    {
        FakeText renderable = new();
        GraphicalUiElement text = new(renderable);
        text.Width = 10;

        LayoutsDuring(() => text.Width = 25).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Width_ShouldRaiseSizeChangedOnce_WhenParentStacks()
    {
        ContainerRuntime parent = new() { ChildrenLayout = ChildrenLayout.TopToBottomStack };
        ContainerRuntime leaf = new() { Width = 10 };
        parent.AddChild(leaf);
        int sizeChanges = 0;
        leaf.SizeChanged += (_, _) => sizeChanges++;

        leaf.Width = 25;

        sizeChanges.ShouldBe(1);
    }

    #endregion
}
