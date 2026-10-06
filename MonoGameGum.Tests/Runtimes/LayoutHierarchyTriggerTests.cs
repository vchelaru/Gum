using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Hierarchy, dependency resolution, triggers and suspension leaves from LAYOUT_TEST_PLAN.md
/// (sections 1.4-1.7 and 6-8). Tests drive layout through public properties, SetProperty and
/// ApplyState only, and assert on absolute positions and sizes or event counts.
/// </summary>
public class LayoutHierarchyTriggerTests : BaseTestClass
{
    class AspectRatioRenderable : InvisibleRenderable, IAspectRatio
    {
        public float AspectRatio { get; set; } = 1;
    }

    record struct Bounds(float Left, float Top, float Width, float Height);

    static Bounds GetBounds(GraphicalUiElement element) =>
        new(element.AbsoluteLeft, element.AbsoluteTop, element.AbsoluteWidth, element.AbsoluteHeight);

    static ContainerRuntime CreateContainer(float width, float height)
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.Width = width;
        container.Height = height;
        return container;
    }

    static ContainerRuntime CreateSizeToChildren()
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.RelativeToChildren;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;
        container.Width = 0;
        container.Height = 0;
        return container;
    }

    #region Min/Max (1.4)

    [Fact]
    public void MinWidth_ShouldWin_WhenGreaterThanMaxWidth()
    {
        ContainerRuntime element = CreateContainer(100, 100);

        element.MaxWidth = 50;
        element.MinWidth = 80;

        // Min wins over Max, the same rule CSS uses.
        element.AbsoluteWidth.ShouldBe(80);
    }

    [Fact]
    public void MaxWidth_ShouldClampRelativeToChildrenWidth()
    {
        ContainerRuntime parent = CreateSizeToChildren();
        parent.AddChild(CreateContainer(200, 200));

        parent.MaxWidth = 100;

        parent.AbsoluteWidth.ShouldBe(100);
    }

    [Fact]
    public void MinHeight_ShouldClampRelativeToChildrenHeight()
    {
        ContainerRuntime parent = CreateSizeToChildren();
        parent.AddChild(CreateContainer(20, 20));

        parent.MinHeight = 50;

        parent.AbsoluteHeight.ShouldBe(50);
    }

    [Fact]
    public void MaxWidth_ShouldClampRatioWidth()
    {
        ContainerRuntime parent = CreateContainer(400, 100);
        ContainerRuntime first = CreateContainer(1, 100);
        first.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(first);
        ContainerRuntime second = CreateContainer(1, 100);
        second.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(second);

        first.MaxWidth = 100;

        first.AbsoluteWidth.ShouldBe(100);
    }

    [Fact]
    public void MaxHeight_ShouldClampMaintainFileAspectRatioHeight()
    {
        AspectRatioRenderable renderable = new() { AspectRatio = 2 };
        GraphicalUiElement element = new(renderable);
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;
        element.Height = 100;

        element.MaxHeight = 30;

        element.AbsoluteHeight.ShouldBe(30);
    }

    [Fact]
    public void MaxWidth_ShouldClampRelativeToMaxParentOrChildrenWidth()
    {
        ContainerRuntime parent = CreateContainer(300, 300);
        ContainerRuntime child = CreateContainer(0, 50);
        child.WidthUnits = DimensionUnitType.RelativeToMaxParentOrChildren;
        parent.AddChild(child);

        child.MaxWidth = 100;

        child.AbsoluteWidth.ShouldBe(100);
    }

    [Fact]
    public void RelativeToChildrenParent_ShouldUseClampedChildSize()
    {
        ContainerRuntime parent = CreateSizeToChildren();
        ContainerRuntime child = CreateContainer(200, 200);
        parent.AddChild(child);

        child.MaxWidth = 100;
        child.MinHeight = 250;

        parent.AbsoluteWidth.ShouldBe(100);
        parent.AbsoluteHeight.ShouldBe(250);
    }

    #endregion

    #region Flags (1.5)

    [Fact]
    public void RotatedParent_ShouldRotateChildOffset()
    {
        ContainerRuntime parent = CreateContainer(100, 100);
        parent.X = 100;
        parent.Y = 100;
        ContainerRuntime child = CreateContainer(20, 20);
        child.X = 10;
        parent.AddChild(child);

        parent.Rotation = 90;

        // Positive rotation is counterclockwise, so the parent's right axis points up the screen.
        child.AbsoluteLeft.ShouldBe(100, tolerance: 0.001f);
        child.AbsoluteTop.ShouldBe(90, tolerance: 0.001f);
    }

    [Fact]
    public void NearQuarterRotation_ShouldPlaceChildLikeExactQuarterRotation()
    {
        ContainerRuntime exactParent = CreateContainer(100, 100);
        exactParent.Rotation = 90;
        ContainerRuntime exactChild = CreateContainer(20, 20);
        exactChild.X = 10;
        exactChild.Y = 30;
        exactParent.AddChild(exactChild);

        ContainerRuntime nearParent = CreateContainer(100, 100);
        nearParent.Rotation = 90.05f;
        ContainerRuntime nearChild = CreateContainer(20, 20);
        nearChild.X = 10;
        nearChild.Y = 30;
        nearParent.AddChild(nearChild);

        nearChild.AbsoluteLeft.ShouldBe(exactChild.AbsoluteLeft);
        nearChild.AbsoluteTop.ShouldBe(exactChild.AbsoluteTop);
    }

    [Fact]
    public void AbsoluteRight_ShouldIgnoreRotation()
    {
        ContainerRuntime element = CreateContainer(100, 20);

        element.Rotation = 90;

        // AbsoluteRight/Bottom are Left + Width and Top + Height in unrotated space, not the
        // rotated bounding box.
        element.AbsoluteRight.ShouldBe(element.AbsoluteLeft + 100);
        element.AbsoluteBottom.ShouldBe(element.AbsoluteTop + 20);
    }

    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall, HorizontalAlignment.Left, 10f, 140f)]
    [InlineData(GeneralUnitType.PixelsFromLarge, HorizontalAlignment.Right, -10f, 10f)]
    [InlineData(GeneralUnitType.PixelsFromMiddle, HorizontalAlignment.Center, 20f, 55f)]
    [InlineData(GeneralUnitType.Percentage, HorizontalAlignment.Left, 10f, 130f)]
    public void FlippedParent_ShouldMirrorChildXUnitsAndOrigin(GeneralUnitType xUnits, HorizontalAlignment xOrigin, float x, float expectedLeft)
    {
        ContainerRuntime parent = CreateContainer(200, 100);
        ContainerRuntime child = CreateContainer(50, 50);
        child.XUnits = xUnits;
        child.XOrigin = xOrigin;
        child.X = x;
        parent.AddChild(child);

        parent.FlipHorizontal = true;

        child.AbsoluteLeft.ShouldBe(expectedLeft);
    }

    static ContainerRuntime CreateFlippedStack(ChildrenLayout layout, GeneralUnitType xUnits, HorizontalAlignment xOrigin, float x)
    {
        ContainerRuntime stack = CreateContainer(200, 300);
        stack.ChildrenLayout = layout;
        for (int i = 0; i < 3; i++)
        {
            ContainerRuntime child = CreateContainer(50, 20);
            child.XUnits = xUnits;
            child.XOrigin = xOrigin;
            child.X = x;
            stack.AddChild(child);
        }
        stack.FlipHorizontal = true;
        return stack;
    }

    [Fact]
    public void FlippedLeftToRightStack_ShouldStackFromRightEdge()
    {
        ContainerRuntime stack = CreateFlippedStack(ChildrenLayout.LeftToRightStack, GeneralUnitType.PixelsFromSmall, HorizontalAlignment.Left, 0);

        stack.Children[0].AbsoluteLeft.ShouldBe(150);
        stack.Children[1].AbsoluteLeft.ShouldBe(100);
        stack.Children[2].AbsoluteLeft.ShouldBe(50);
    }

    [Fact]
    public void FlippedLeftToRightStack_LaterChildWithRightOrigin_ShouldNotOverlapPreviousSibling()
    {
        ContainerRuntime stack = CreateFlippedStack(ChildrenLayout.LeftToRightStack, GeneralUnitType.PixelsFromSmall, HorizontalAlignment.Right, 0);

        stack.Children[1].AbsoluteRight.ShouldBe(stack.Children[0].AbsoluteLeft);
        stack.Children[2].AbsoluteRight.ShouldBe(stack.Children[1].AbsoluteLeft);
    }

    public static TheoryData<string> FlippedLeftToRightStackCases => new()
    {
        "plain", "spacing", "x offset", "large units", "middle units", "percentage units", "wraps", "first hidden", "sized to children"
    };

    [Fact]
    public void FlippedWrappingTopToBottomStack_ShouldMirrorUnflippedColumns()
    {
        ContainerRuntime unflipped = CreateWrappingTopToBottomStack();
        ContainerRuntime flipped = CreateWrappingTopToBottomStack();

        flipped.FlipHorizontal = true;

        float parentWidth = unflipped.AbsoluteWidth;
        for (int i = 0; i < unflipped.Children.Count; i++)
        {
            GraphicalUiElement expected = unflipped.Children[i];
            GraphicalUiElement actual = flipped.Children[i];
            actual.AbsoluteLeft.ShouldBe(parentWidth - (expected.AbsoluteLeft + expected.AbsoluteWidth), $"child {i}");
            actual.AbsoluteTop.ShouldBe(expected.AbsoluteTop, $"child {i}");
        }
    }

    static ContainerRuntime CreateWrappingTopToBottomStack()
    {
        ContainerRuntime stack = CreateContainer(300, 250);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.WrapsChildren = true;
        stack.StackSpacing = 5;
        for (int i = 0; i < 5; i++)
        {
            stack.AddChild(CreateContainer(40 + i * 10, 100));
        }
        return stack;
    }

    // A flipped LeftToRightStack is the unflipped stack reflected across the parent's width:
    // each child keeps its Y and its X becomes parentWidth - (unflippedX + width).
    [Theory]
    [MemberData(nameof(FlippedLeftToRightStackCases))]
    public void FlippedLeftToRightStack_ShouldMirrorUnflippedStack(string scenario)
    {
        ContainerRuntime unflipped = CreateLeftToRightStackScenario(scenario);
        ContainerRuntime flipped = CreateLeftToRightStackScenario(scenario);

        flipped.FlipHorizontal = true;

        flipped.AbsoluteWidth.ShouldBe(unflipped.AbsoluteWidth);
        flipped.AbsoluteHeight.ShouldBe(unflipped.AbsoluteHeight);
        float parentWidth = unflipped.AbsoluteWidth;
        for (int i = 0; i < unflipped.Children.Count; i++)
        {
            GraphicalUiElement expected = unflipped.Children[i];
            GraphicalUiElement actual = flipped.Children[i];
            // Hidden children don't lay out, so they keep their old position.
            if (!expected.Visible)
            {
                continue;
            }
            actual.AbsoluteLeft.ShouldBe(parentWidth - (expected.AbsoluteLeft + expected.AbsoluteWidth), $"child {i}");
            actual.AbsoluteTop.ShouldBe(expected.AbsoluteTop, $"child {i}");
        }
    }

    static ContainerRuntime CreateLeftToRightStackScenario(string scenario)
    {
        ContainerRuntime stack = CreateContainer(200, 300);
        stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        int childCount = scenario == "wraps" ? 5 : 3;
        for (int i = 0; i < childCount; i++)
        {
            ContainerRuntime child = CreateContainer(scenario == "wraps" ? 60 : 50, 20 + i * 5);
            stack.AddChild(child);
        }
        switch (scenario)
        {
            case "spacing":
                stack.StackSpacing = 10;
                break;
            case "x offset":
                foreach (GraphicalUiElement child in stack.Children)
                {
                    child.X = 5;
                }
                break;
            case "large units":
                foreach (GraphicalUiElement child in stack.Children)
                {
                    child.XUnits = GeneralUnitType.PixelsFromLarge;
                    child.XOrigin = HorizontalAlignment.Right;
                }
                break;
            case "middle units":
                foreach (GraphicalUiElement child in stack.Children)
                {
                    child.XUnits = GeneralUnitType.PixelsFromMiddle;
                    child.XOrigin = HorizontalAlignment.Center;
                }
                break;
            case "percentage units":
                foreach (GraphicalUiElement child in stack.Children)
                {
                    child.XUnits = GeneralUnitType.Percentage;
                    child.X = 10;
                }
                break;
            case "wraps":
                stack.WrapsChildren = true;
                stack.StackSpacing = 5;
                break;
            case "first hidden":
                stack.Children[0].Visible = false;
                break;
            case "sized to children":
                stack.WidthUnits = DimensionUnitType.RelativeToChildren;
                stack.Width = 0;
                stack.StackSpacing = 10;
                break;
        }
        return stack;
    }

    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall, 140f)]
    [InlineData(GeneralUnitType.PixelsFromLarge, -60f)]
    [InlineData(GeneralUnitType.PixelsFromMiddle, 40f)]
    public void FlippedTopToBottomStack_ShouldMirrorXAndKeepStacking(GeneralUnitType xUnits, float expectedLeft)
    {
        ContainerRuntime stack = CreateFlippedStack(ChildrenLayout.TopToBottomStack, xUnits, HorizontalAlignment.Left, 10);

        for (int i = 0; i < 3; i++)
        {
            stack.Children[i].AbsoluteLeft.ShouldBe(expectedLeft);
            stack.Children[i].AbsoluteTop.ShouldBe(i * 20);
        }
    }

    [Fact]
    public void FlippedAutoGrid_ShouldMirrorCells()
    {
        ContainerRuntime grid = CreateContainer(400, 400);
        grid.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        grid.AutoGridHorizontalCells = 2;
        grid.AutoGridVerticalCells = 2;
        for (int i = 0; i < 4; i++)
        {
            grid.AddChild(CreateContainer(50, 50));
        }

        grid.FlipHorizontal = true;

        grid.Children[0].AbsoluteLeft.ShouldBe(350);
        grid.Children[1].AbsoluteLeft.ShouldBe(150);
        grid.Children[2].AbsoluteLeft.ShouldBe(350);
        grid.Children[3].AbsoluteLeft.ShouldBe(150);
        grid.Children[2].AbsoluteTop.ShouldBe(200);
    }

    #endregion

    #region Anchor and Dock (1.6)

    [Theory]
    [InlineData(Anchor.CenterHorizontally, Anchor.Top)]
    [InlineData(Anchor.CenterVertically, Anchor.Left)]
    public void GetAnchor_ShouldReadBackSingleAxisAnchorAsFullAnchor_OnDefaultElement(Anchor anchor, Anchor expected)
    {
        GraphicalUiElement element = new(new InvisibleRenderable());

        element.Anchor(anchor);

        // A single-axis anchor leaves the other axis at its default (Top/Left, 0), which is
        // indistinguishable from the matching full anchor.
        element.GetAnchor().ShouldBe(expected);
    }

    [Theory]
    [InlineData(Dock.FillHorizontally, Dock.Top)]
    [InlineData(Dock.FillVertically, Dock.Left)]
    public void GetDock_ShouldReadBackSingleAxisFillAsEdgeDock_OnDefaultElement(Dock dock, Dock expected)
    {
        GraphicalUiElement element = new(new InvisibleRenderable());

        element.Dock(dock);

        // Same ambiguity as the single-axis anchors: the untouched axis sits at the edge.
        element.GetDock().ShouldBe(expected);
    }

    #endregion

    #region Globals (1.7)

    [Fact]
    public void CanvasWidth_ShouldNotRelayout_UntilUpdateLayout()
    {
        ContainerRuntime element = new();
        element.WidthUnits = DimensionUnitType.PercentageOfParent;
        element.Width = 50;
        float widthBefore = element.AbsoluteWidth;

        GraphicalUiElement.CanvasWidth = 1000;

        // The canvas size is a plain static with no trigger; callers relayout after changing it.
        element.AbsoluteWidth.ShouldBe(widthBefore);
        element.UpdateLayout();
        element.AbsoluteWidth.ShouldBe(500);
    }

    [Fact]
    public void AreUpdatesAppliedWhenInvisible_ShouldLayOutInvisibleChild()
    {
        bool oldValue = GraphicalUiElement.AreUpdatesAppliedWhenInvisible;
        try
        {
            GraphicalUiElement.AreUpdatesAppliedWhenInvisible = true;
            ContainerRuntime parent = CreateContainer(200, 100);
            ContainerRuntime child = CreateContainer(50, 50);
            child.WidthUnits = DimensionUnitType.PercentageOfParent;
            child.Width = 50;
            parent.AddChild(child);
            child.Visible = false;

            parent.Width = 300;

            child.AbsoluteWidth.ShouldBe(150);
        }
        finally
        {
            GraphicalUiElement.AreUpdatesAppliedWhenInvisible = oldValue;
        }
    }

    #endregion

    #region Hierarchy (6)

    [Fact]
    public void ChildrenMove_ShouldRestackChildren()
    {
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        ContainerRuntime first = CreateContainer(50, 10);
        stack.AddChild(first);
        ContainerRuntime second = CreateContainer(50, 20);
        stack.AddChild(second);

        stack.Children.Move(1, 0);

        second.AbsoluteTop.ShouldBe(0);
        first.AbsoluteTop.ShouldBe(20);
    }

    [Fact]
    public void AddingChild_WhileParentSuspended_ShouldMatchUnsuspended_OnResume()
    {
        ContainerRuntime unsuspendedParent = CreateSizeToChildren();
        ContainerRuntime unsuspendedChild = CreateContainer(60, 40);
        unsuspendedChild.X = 10;
        unsuspendedParent.AddChild(unsuspendedChild);

        ContainerRuntime parent = CreateSizeToChildren();
        ContainerRuntime child = CreateContainer(60, 40);
        child.X = 10;
        parent.SuspendLayout(recursive: true);
        parent.AddChild(child);
        parent.ResumeLayout(recursive: true);

        GetBounds(parent).ShouldBe(GetBounds(unsuspendedParent));
        GetBounds(child).ShouldBe(GetBounds(unsuspendedChild));
    }

    [Fact]
    public void SetProperty_Parent_ShouldReparentByInstanceName()
    {
        ContainerRuntime root = CreateContainer(400, 400);
        ContainerRuntime panel = CreateContainer(100, 100);
        panel.Name = "Panel";
        panel.X = 50;
        panel.ElementGueContainingThis = root;
        root.AddChild(panel);
        ContainerRuntime child = CreateContainer(20, 20);
        child.Name = "Child";
        child.X = 5;
        child.ElementGueContainingThis = root;
        root.AddChild(child);

        child.SetProperty("Parent", "Panel");

        child.AbsoluteLeft.ShouldBe(55);
    }

    [Fact]
    public void ApplyState_ParentVariables_ShouldAddChildrenInInstanceOrder()
    {
        ComponentSave component = new() { Name = "Component" };
        component.Instances.Add(new InstanceSave { Name = "Stack" });
        component.Instances.Add(new InstanceSave { Name = "A" });
        component.Instances.Add(new InstanceSave { Name = "B" });

        ContainerRuntime root = CreateContainer(400, 400);
        root.ElementSave = component;
        ContainerRuntime stack = CreateContainer(100, 400);
        stack.Name = "Stack";
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.ElementGueContainingThis = root;
        root.AddChild(stack);
        ContainerRuntime a = CreateContainer(50, 10);
        a.Name = "A";
        a.ElementGueContainingThis = root;
        root.AddChild(a);
        ContainerRuntime b = CreateContainer(50, 20);
        b.Name = "B";
        b.ElementGueContainingThis = root;
        root.AddChild(b);

        StateSave state = new() { Name = "Default", ParentContainer = component };
        // Listed in reverse instance order; ApplyState reorders Parent variables by instance index.
        state.Variables.Add(new VariableSave { Name = "B.Parent", Value = "Stack", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "A.Parent", Value = "Stack", SetsValue = true });

        root.ApplyState(state);

        a.AbsoluteTop.ShouldBe(0);
        b.AbsoluteTop.ShouldBe(10);
    }

    [Fact]
    public void PropertyOrder_ShouldNotChangeResult()
    {
        ContainerRuntime parentA = CreateContainer(300, 200);
        ContainerRuntime childA = new();
        childA.WidthUnits = DimensionUnitType.PercentageOfParent;
        childA.Width = 50;
        childA.HeightUnits = DimensionUnitType.RelativeToParent;
        childA.Height = -20;
        childA.XUnits = GeneralUnitType.PixelsFromMiddle;
        childA.XOrigin = HorizontalAlignment.Center;
        childA.X = 10;
        childA.YUnits = GeneralUnitType.PixelsFromLarge;
        childA.YOrigin = VerticalAlignment.Bottom;
        childA.Y = -5;
        parentA.AddChild(childA);

        ContainerRuntime parentB = new();
        ContainerRuntime childB = new();
        parentB.AddChild(childB);
        childB.Y = -5;
        childB.X = 10;
        childB.YOrigin = VerticalAlignment.Bottom;
        childB.XOrigin = HorizontalAlignment.Center;
        childB.YUnits = GeneralUnitType.PixelsFromLarge;
        childB.XUnits = GeneralUnitType.PixelsFromMiddle;
        childB.Height = -20;
        childB.Width = 50;
        childB.HeightUnits = DimensionUnitType.RelativeToParent;
        childB.WidthUnits = DimensionUnitType.PercentageOfParent;
        parentB.Height = 200;
        parentB.Width = 300;
        parentB.HeightUnits = DimensionUnitType.Absolute;
        parentB.WidthUnits = DimensionUnitType.Absolute;

        GetBounds(childB).ShouldBe(GetBounds(childA));
    }

    [Fact]
    public void Clone_ShouldLayOutLikeSource_OnceParented()
    {
        ContainerRuntime sourceParent = CreateContainer(300, 200);
        ContainerRuntime source = new();
        source.WidthUnits = DimensionUnitType.PercentageOfParent;
        source.Width = 50;
        source.HeightUnits = DimensionUnitType.RelativeToParent;
        source.Height = -20;
        source.XUnits = GeneralUnitType.PixelsFromLarge;
        source.XOrigin = HorizontalAlignment.Right;
        source.X = -10;
        sourceParent.AddChild(source);

        ContainerRuntime cloneParent = CreateContainer(300, 200);
        GraphicalUiElement clone = source.Clone();
        cloneParent.AddChild(clone);

        GetBounds(clone).ShouldBe(GetBounds(source));
    }

    #endregion

    #region Dependency resolution (7)

    [Fact]
    public void RelativeToChildren_ShouldIgnoreChildWithPercentageX()
    {
        ContainerRuntime parent = CreateSizeToChildren();
        ContainerRuntime child = CreateContainer(50, 30);
        child.XUnits = GeneralUnitType.Percentage;
        child.X = 50;
        parent.AddChild(child);

        // A Percentage X depends on the parent's width, so the child is left out of the
        // parent's width like a PercentageOfParent-width child. Its height still counts.
        parent.AbsoluteWidth.ShouldBe(0);
        parent.AbsoluteHeight.ShouldBe(30);
    }

    [Fact]
    public void BothAxesMaintainFileAspectRatio_ShouldFallBackToRawValues()
    {
        AspectRatioRenderable renderable = new() { AspectRatio = 2 };
        GraphicalUiElement element = new(renderable);
        element.Width = 100;
        element.Height = 200;
        element.WidthUnits = DimensionUnitType.MaintainFileAspectRatio;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;

        element.AbsoluteWidth.ShouldBe(100);
        element.AbsoluteHeight.ShouldBe(200);
    }

    [Fact]
    public void Ratio_NegativeWidth_ShouldBeTreatedAsZero()
    {
        ContainerRuntime parent = CreateContainer(300, 100);
        ContainerRuntime negative = CreateContainer(-1, 100);
        negative.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(negative);
        ContainerRuntime positive = CreateContainer(2, 100);
        positive.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(positive);

        negative.AbsoluteWidth.ShouldBe(0);
        positive.AbsoluteWidth.ShouldBe(300);
    }

    [Fact]
    public void Ratio_NegativeHeight_ShouldBeTreatedAsZero()
    {
        ContainerRuntime parent = CreateContainer(100, 300);
        ContainerRuntime negative = CreateContainer(100, -1);
        negative.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(negative);
        ContainerRuntime positive = CreateContainer(100, 2);
        positive.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(positive);

        negative.AbsoluteHeight.ShouldBe(0);
        positive.AbsoluteHeight.ShouldBe(300);
    }

    [Fact]
    public void Ratio_InRelativeToChildrenParent_ShouldGetNoSpace()
    {
        ContainerRuntime parent = CreateSizeToChildren();
        parent.AddChild(CreateContainer(100, 50));
        ContainerRuntime ratioChild = CreateContainer(1, 50);
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        parent.AbsoluteWidth.ShouldBe(100);
        ratioChild.AbsoluteWidth.ShouldBe(0);
    }

    [Fact]
    public void Ratio_ShouldSubtractIgnoredByParentSizeSibling()
    {
        ContainerRuntime parent = CreateContainer(300, 100);
        ContainerRuntime ignored = CreateContainer(100, 50);
        ignored.IgnoredByParentSize = true;
        parent.AddChild(ignored);
        ContainerRuntime ratioChild = CreateContainer(1, 50);
        ratioChild.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratioChild);

        ratioChild.AbsoluteWidth.ShouldBe(200);
    }

    #endregion

    #region Triggers (8.1)

    // root (RelativeToChildren) > target (texture-sized, 100x100 at 20,20) > leaf (50% of target)
    static (ContainerRuntime root, ContainerRuntime target, ContainerRuntime leaf) CreateTriggerTree()
    {
        ContainerRuntime root = CreateSizeToChildren();
        ContainerRuntime target = new();
        target.TextureAddress = TextureAddress.Custom;
        target.TextureWidth = 100;
        target.TextureHeight = 100;
        target.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        target.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        target.Width = 100;
        target.Height = 100;
        target.X = 20;
        target.Y = 20;
        root.AddChild(target);
        ContainerRuntime leaf = new();
        leaf.WidthUnits = DimensionUnitType.PercentageOfParent;
        leaf.HeightUnits = DimensionUnitType.PercentageOfParent;
        leaf.Width = 50;
        leaf.Height = 50;
        leaf.X = 10;
        leaf.Y = 5;
        target.AddChild(leaf);
        return (root, target, leaf);
    }

    static Bounds[] Snapshot((ContainerRuntime root, ContainerRuntime target, ContainerRuntime leaf) tree) =>
        new[] { GetBounds(tree.root), GetBounds(tree.target), GetBounds(tree.leaf) };

    static void SetDirectly(GraphicalUiElement element, string propertyName, object value)
    {
        switch (propertyName)
        {
            case nameof(GraphicalUiElement.MinWidth): element.MinWidth = (float)value; break;
            case nameof(GraphicalUiElement.MaxWidth): element.MaxWidth = (float)value; break;
            case nameof(GraphicalUiElement.MinHeight): element.MinHeight = (float)value; break;
            case nameof(GraphicalUiElement.MaxHeight): element.MaxHeight = (float)value; break;
            case nameof(GraphicalUiElement.IgnoredByParentSize): element.IgnoredByParentSize = (bool)value; break;
            case nameof(GraphicalUiElement.Rotation): element.Rotation = (float)value; break;
            case nameof(GraphicalUiElement.FlipHorizontal): element.FlipHorizontal = (bool)value; break;
            case nameof(GraphicalUiElement.TextureWidth): element.TextureWidth = (int)value; break;
            case nameof(GraphicalUiElement.TextureHeight): element.TextureHeight = (int)value; break;
            case nameof(GraphicalUiElement.TextureAddress): element.TextureAddress = (TextureAddress)value; break;
            default: throw new System.ArgumentException(propertyName);
        }
    }

    [Theory]
    [InlineData(nameof(GraphicalUiElement.MinWidth), 300f)]
    [InlineData(nameof(GraphicalUiElement.MaxWidth), 40f)]
    [InlineData(nameof(GraphicalUiElement.MinHeight), 300f)]
    [InlineData(nameof(GraphicalUiElement.MaxHeight), 40f)]
    [InlineData(nameof(GraphicalUiElement.IgnoredByParentSize), true)]
    [InlineData(nameof(GraphicalUiElement.Rotation), 90f)]
    [InlineData(nameof(GraphicalUiElement.FlipHorizontal), true)]
    [InlineData(nameof(GraphicalUiElement.TextureWidth), 200)]
    [InlineData(nameof(GraphicalUiElement.TextureHeight), 200)]
    [InlineData(nameof(GraphicalUiElement.TextureAddress), TextureAddress.EntireTexture)]
    public void SettingLayoutProperty_ShouldMatchFullLayout(string propertyName, object value)
    {
        (ContainerRuntime root, ContainerRuntime target, ContainerRuntime leaf) tree = CreateTriggerTree();
        Bounds[] before = Snapshot(tree);

        SetDirectly(tree.target, propertyName, value);
        Bounds[] afterSetter = Snapshot(tree);
        tree.root.UpdateLayout();

        afterSetter.ShouldNotBe(before);
        Snapshot(tree).ShouldBe(afterSetter);
    }

    [Theory]
    [InlineData(nameof(GraphicalUiElement.MinWidth), 300f)]
    [InlineData(nameof(GraphicalUiElement.MaxWidth), 40f)]
    [InlineData(nameof(GraphicalUiElement.MinHeight), 300f)]
    [InlineData(nameof(GraphicalUiElement.MaxHeight), 40f)]
    [InlineData(nameof(GraphicalUiElement.IgnoredByParentSize), true)]
    [InlineData(nameof(GraphicalUiElement.Rotation), 90f)]
    [InlineData(nameof(GraphicalUiElement.FlipHorizontal), true)]
    [InlineData(nameof(GraphicalUiElement.TextureWidth), 200)]
    [InlineData(nameof(GraphicalUiElement.TextureHeight), 200)]
    [InlineData(nameof(GraphicalUiElement.TextureAddress), TextureAddress.EntireTexture)]
    public void SetProperty_ShouldMatchDirectSetter(string propertyName, object value)
    {
        (ContainerRuntime root, ContainerRuntime target, ContainerRuntime leaf) direct = CreateTriggerTree();
        SetDirectly(direct.target, propertyName, value);

        (ContainerRuntime root, ContainerRuntime target, ContainerRuntime leaf) throughString = CreateTriggerTree();
        throughString.target.SetProperty(propertyName, value);

        Snapshot(throughString).ShouldBe(Snapshot(direct));
    }

    [Theory]
    [InlineData("Width Units", "WidthUnits", DimensionUnitType.PercentageOfParent)]
    [InlineData("Height Units", "HeightUnits", DimensionUnitType.PercentageOfParent)]
    [InlineData("X Units", "XUnits", GeneralUnitType.PixelsFromMiddle)]
    [InlineData("Y Units", "YUnits", GeneralUnitType.PixelsFromLarge)]
    [InlineData("X Origin", "XOrigin", HorizontalAlignment.Right)]
    [InlineData("Y Origin", "YOrigin", VerticalAlignment.Bottom)]
    [InlineData("Texture Width", "TextureWidth", 200)]
    [InlineData("Texture Height", "TextureHeight", 200)]
    [InlineData("Texture Address", "TextureAddress", TextureAddress.EntireTexture)]
    public void SetProperty_NameWithSpaces_ShouldMatchNameWithoutSpaces(string spacedName, string name, object value)
    {
        (ContainerRuntime root, ContainerRuntime target, ContainerRuntime leaf) unspaced = CreateTriggerTree();
        unspaced.target.SetProperty(name, value);

        (ContainerRuntime root, ContainerRuntime target, ContainerRuntime leaf) spaced = CreateTriggerTree();
        spaced.target.SetProperty(spacedName, value);

        Snapshot(spaced).ShouldBe(Snapshot(unspaced));
    }

    [Fact]
    public void SettingX_ShouldMatchFullLayout_WhenParentIsRotated()
    {
        ContainerRuntime parent = CreateContainer(100, 100);
        parent.Rotation = 90;
        ContainerRuntime child = CreateContainer(20, 20);
        parent.AddChild(child);

        child.X = 10;
        Bounds afterSetter = GetBounds(child);
        parent.UpdateLayout();

        GetBounds(child).ShouldBe(afterSetter);
    }

    static StateSave CreateLayoutState(float width, float x)
    {
        StateSave state = new() { Name = "State" };
        state.Variables.Add(new VariableSave { Name = "WidthUnits", Value = DimensionUnitType.PercentageOfParent, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Width", Value = width, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "XUnits", Value = GeneralUnitType.PixelsFromMiddle, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "XOrigin", Value = HorizontalAlignment.Center, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "X", Value = x, SetsValue = true });
        return state;
    }

    static ContainerRuntime CreateDirectlySetChild(ContainerRuntime parent, float width, float x)
    {
        ContainerRuntime child = CreateContainer(10, 10);
        parent.AddChild(child);
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Width = width;
        child.XUnits = GeneralUnitType.PixelsFromMiddle;
        child.XOrigin = HorizontalAlignment.Center;
        child.X = x;
        return child;
    }

    [Fact]
    public void ApplyState_ShouldMatchSettingValuesDirectly()
    {
        ContainerRuntime directChild = CreateDirectlySetChild(CreateContainer(400, 100), width: 50, x: 20);

        ContainerRuntime parent = CreateContainer(400, 100);
        ContainerRuntime stateChild = CreateContainer(10, 10);
        parent.AddChild(stateChild);
        stateChild.ApplyState(CreateLayoutState(width: 50, x: 20));

        GetBounds(stateChild).ShouldBe(GetBounds(directChild));
    }

    [Fact]
    public void InterpolateBetween_ShouldMatchSettingInterpolatedValuesDirectly()
    {
        ContainerRuntime directChild = CreateDirectlySetChild(CreateContainer(400, 100), width: 50, x: 20);

        ContainerRuntime parent = CreateContainer(400, 100);
        ContainerRuntime stateChild = CreateContainer(10, 10);
        parent.AddChild(stateChild);
        stateChild.InterpolateBetween(CreateLayoutState(width: 25, x: 0), CreateLayoutState(width: 75, x: 40), 0.5f);

        GetBounds(stateChild).ShouldBe(GetBounds(directChild));
    }

    [Fact]
    public void InterpolateBetween_ShouldNotLayOut_WhileSuspended()
    {
        ContainerRuntime parent = CreateContainer(400, 100);
        ContainerRuntime child = CreateContainer(10, 10);
        parent.AddChild(child);
        Bounds before = GetBounds(child);

        child.SuspendLayout();
        child.InterpolateBetween(CreateLayoutState(width: 25, x: 0), CreateLayoutState(width: 75, x: 40), 0.5f);
        Bounds whileSuspended = GetBounds(child);
        child.ResumeLayout();

        whileSuspended.ShouldBe(before);
        GetBounds(child).ShouldBe(GetBounds(CreateDirectlySetChild(CreateContainer(400, 100), width: 50, x: 20)));
    }

    #endregion

    #region Propagation (8.2)

    [Fact]
    public void HideThenShow_WhileSuspended_ShouldRestoreLayout()
    {
        ContainerRuntime parent = CreateSizeToChildren();
        ContainerRuntime first = CreateContainer(100, 40);
        parent.AddChild(first);
        ContainerRuntime second = CreateContainer(50, 80);
        second.X = 30;
        parent.AddChild(second);
        Bounds parentBefore = GetBounds(parent);
        Bounds secondBefore = GetBounds(second);

        parent.SuspendLayout(recursive: true);
        second.Visible = false;
        second.Visible = true;
        parent.ResumeLayout(recursive: true);

        GetBounds(parent).ShouldBe(parentBefore);
        GetBounds(second).ShouldBe(secondBefore);
    }

    [Fact]
    public void SizeChangedAndPositionChanged_ShouldNotBeRaised_WhenNothingChanges()
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        ContainerRuntime child = CreateContainer(50, 50);
        child.XUnits = GeneralUnitType.PixelsFromMiddle;
        parent.AddChild(child);
        int sizeChangedCount = 0;
        int positionChangedCount = 0;
        child.SizeChanged += (_, _) => sizeChangedCount++;
        child.PositionChanged += (_, _) => positionChangedCount++;

        parent.UpdateLayout();
        child.X = 10;
        child.Width = 60;

        sizeChangedCount.ShouldBe(1);
        positionChangedCount.ShouldBe(1);
    }

    [Fact]
    public void SizeChanged_HandlerSettingWidth_ShouldTerminateWithHandlerValue()
    {
        ContainerRuntime parent = CreateSizeToChildren();
        ContainerRuntime child = CreateContainer(50, 50);
        parent.AddChild(child);
        child.SizeChanged += (_, _) => child.Width = 30;

        child.Width = 100;

        child.AbsoluteWidth.ShouldBe(30);
        parent.AbsoluteWidth.ShouldBe(30);
    }

    #endregion

    #region Dirty state and suspension (8.3)

    [Fact]
    public void NonRecursiveResume_ShouldLeaveSeparatelySuspendedChildPending()
    {
        ContainerRuntime parent = CreateContainer(200, 100);
        ContainerRuntime child = CreateContainer(50, 50);
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Width = 50;
        parent.AddChild(child);

        parent.SuspendLayout(recursive: true);
        parent.Width = 300;
        child.Height = 70;
        parent.ResumeLayout();
        float widthWhileChildSuspended = child.AbsoluteWidth;
        child.ResumeLayout();

        widthWhileChildSuspended.ShouldBe(100);
        child.AbsoluteWidth.ShouldBe(150);
        child.AbsoluteHeight.ShouldBe(70);
    }

    [Fact]
    public void InvisibleParent_DirtiedThenShown_ShouldApplyPendingLayoutToChild()
    {
        ContainerRuntime parent = CreateContainer(200, 100);
        ContainerRuntime child = CreateContainer(50, 50);
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Width = 50;
        parent.AddChild(child);
        parent.Visible = false;

        parent.Width = 300;
        parent.Visible = true;

        child.AbsoluteWidth.ShouldBe(150);
    }

    [Fact]
    public void InvisibleChild_DirtiedInsideInvisibleParent_ShouldApplyPendingLayout_WhenBothShown()
    {
        ContainerRuntime parent = CreateSizeToChildren();
        ContainerRuntime child = CreateContainer(50, 50);
        parent.AddChild(child);
        parent.Visible = false;
        child.Visible = false;

        child.Height = 80;
        parent.Visible = true;
        child.Visible = true;

        child.AbsoluteHeight.ShouldBe(80);
        parent.AbsoluteHeight.ShouldBe(80);
    }

    [Fact]
    public void InvisibleChild_InsideRenderTarget_ShouldStillLayOut()
    {
        ContainerRuntime renderTarget = CreateContainer(200, 100);
        renderTarget.IsRenderTarget = true;
        ContainerRuntime child = CreateContainer(50, 50);
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Width = 50;
        renderTarget.AddChild(child);
        child.Visible = false;

        renderTarget.Width = 300;

        child.AbsoluteWidth.ShouldBe(150);
    }

    [Fact]
    public void ClearDirtyLayoutState_ShouldDropPendingLayout()
    {
        ContainerRuntime element = CreateContainer(100, 100);

        element.SuspendLayout();
        element.Width = 200;
        element.ClearDirtyLayoutState();
        element.ResumeLayout();

        element.AbsoluteWidth.ShouldBe(100);
    }

    [Fact]
    public void DirtyXThenDirtyY_ShouldUpdateBothAxes_OnResume()
    {
        ContainerRuntime element = CreateContainer(100, 100);

        element.SuspendLayout();
        element.Width = 200;
        element.Height = 150;
        element.ResumeLayout();

        element.AbsoluteWidth.ShouldBe(200);
        element.AbsoluteHeight.ShouldBe(150);
    }

    #endregion
}
