using Gum.DataTypes;
using Gum.Managers;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// An element that is hidden inside a render target is skipped by layout like it is anywhere else (#7240), and
/// is laid out correctly when it is shown. A hidden render target itself still lays out, because something
/// else may draw it.
/// </summary>
public class HiddenElementLayoutInRenderTargetTests : BaseTestClass
{
    #region Builders

    private sealed class Scene
    {
        public ContainerRuntime Root = null!;
        public ContainerRuntime Shown = null!;
        public ContainerRuntime Hidden = null!;
    }

    private static ContainerRuntime MakeRow()
    {
        ContainerRuntime row = new();
        row.WidthUnits = DimensionUnitType.Absolute;
        row.Width = 48;
        row.HeightUnits = DimensionUnitType.Absolute;
        row.Height = 32;
        return row;
    }

    private static ContainerRuntime MakeStack(int rows)
    {
        ContainerRuntime stack = new();
        stack.WidthUnits = DimensionUnitType.RelativeToParent;
        stack.Width = 0;
        stack.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.Height = 0;
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        for (int i = 0; i < rows; i++)
        {
            stack.Children.Add(MakeRow());
        }
        return stack;
    }

    private static Scene Build(int hiddenRows, bool isRenderTarget, bool hideStack = true)
    {
        Scene scene = new();
        scene.Root = new ContainerRuntime();
        scene.Root.WidthUnits = DimensionUnitType.Absolute;
        scene.Root.Width = 500;
        scene.Root.HeightUnits = DimensionUnitType.Absolute;
        scene.Root.Height = 300;
        scene.Root.IsRenderTarget = isRenderTarget;
        scene.Shown = MakeStack(10);
        scene.Root.Children.Add(scene.Shown);
        scene.Hidden = MakeStack(hiddenRows);
        scene.Hidden.Visible = !hideStack;
        scene.Root.Children.Add(scene.Hidden);
        scene.Root.UpdateLayout();
        return scene;
    }

    private static int Count(Action action)
    {
        int before = GraphicalUiElement.UpdateLayoutCallCount;
        action();
        return GraphicalUiElement.UpdateLayoutCallCount - before;
    }

    private static List<GraphicalUiElement> Flatten(GraphicalUiElement element)
    {
        List<GraphicalUiElement> all = new() { element };
        foreach (GraphicalUiElement child in element.Children.Cast<GraphicalUiElement>())
        {
            all.AddRange(Flatten(child));
        }
        return all;
    }

    private static List<(float, float, float, float)> Rects(GraphicalUiElement root) =>
        Flatten(root).Select(e => (e.AbsoluteLeft, e.AbsoluteTop, e.AbsoluteWidth, e.AbsoluteHeight)).ToList();

    #endregion

    #region Counts

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(40)]
    [InlineData(80)]
    public void RootResize_ShouldLayOutTheSameCount_WithAndWithoutRenderTarget(int hiddenRows)
    {
        Scene plain = Build(hiddenRows, isRenderTarget: false);
        Scene target = Build(hiddenRows, isRenderTarget: true);

        int plainCount = Count(() => plain.Root.Width -= 1);
        int targetCount = Count(() => target.Root.Width -= 1);

        plainCount.ShouldBe(12);
        targetCount.ShouldBe(plainCount);
    }

    [Fact]
    public void RootResize_ShouldNotLayOutHiddenGrandchildren_InRenderTarget()
    {
        Scene scene = Build(10, isRenderTarget: true);
        ContainerRuntime nested = MakeStack(5);
        scene.Hidden.Children[0].AddChild(nested);

        int count = Count(() => scene.Root.Width -= 1);

        count.ShouldBe(12);
    }

    [Fact]
    public void AlphaChange_ShouldNotLayOutAnything_InRenderTarget()
    {
        Scene scene = Build(10, isRenderTarget: true);

        int count = Count(() => scene.Root.Alpha = 100);

        count.ShouldBe(0);
    }

    [Fact]
    public void RootResize_ShouldLayOutTheSameCount_WhenUpdatesAreAppliedWhenInvisible()
    {
        bool original = GraphicalUiElement.AreUpdatesAppliedWhenInvisible;
        GraphicalUiElement.AreUpdatesAppliedWhenInvisible = true;
        try
        {
            Scene plain = Build(10, isRenderTarget: false);
            Scene target = Build(10, isRenderTarget: true);

            int plainCount = Count(() => plain.Root.Width -= 1);
            int targetCount = Count(() => target.Root.Width -= 1);

            targetCount.ShouldBe(plainCount);
        }
        finally
        {
            GraphicalUiElement.AreUpdatesAppliedWhenInvisible = original;
        }
    }

    [Fact]
    public void ClippingRenderTarget_ShouldSkipHiddenDescendants()
    {
        Scene scene = Build(10, isRenderTarget: true);
        scene.Root.ClipsChildren = true;

        int count = Count(() => scene.Root.Width -= 1);

        count.ShouldBe(12);
    }

    #endregion

    #region Showing resolves what was skipped

    [Fact]
    public void ShowingHiddenStack_ShouldResolveRootResize_InRenderTarget()
    {
        Scene scene = Build(10, isRenderTarget: true);

        scene.Root.Width = 300;
        scene.Hidden.Visible = true;

        scene.Hidden.AbsoluteWidth.ShouldBe(300);
        scene.Hidden.AbsoluteHeight.ShouldBe(320);
        scene.Hidden.Children.Last().AbsoluteTop.ShouldBe(scene.Hidden.AbsoluteTop + 9 * 32);
    }

    [Fact]
    public void ShowingHiddenStack_ShouldMatchNeverHiddenLayout_AfterEditsWhileHidden()
    {
        Scene hidden = Build(4, isRenderTarget: true);
        Scene reference = Build(4, isRenderTarget: true, hideStack: false);

        hidden.Root.Width = 320;
        hidden.Hidden.Children.Add(MakeRow());
        hidden.Hidden.Children[0].Height = 50;
        hidden.Hidden.Visible = true;

        reference.Root.Width = 320;
        reference.Hidden.Children.Add(MakeRow());
        reference.Hidden.Children[0].Height = 50;

        Rects(hidden.Root).ShouldBe(Rects(reference.Root));
    }

    [Fact]
    public void ShowingHiddenStack_ShouldStackRowsAddedWhileHidden_InRenderTarget()
    {
        Scene scene = Build(0, isRenderTarget: true);

        scene.Hidden.Children.Add(MakeRow());
        scene.Hidden.Children.Add(MakeRow());
        scene.Hidden.Children.Add(MakeRow());
        scene.Hidden.Visible = true;

        scene.Hidden.Children.Select(c => c.AbsoluteTop - scene.Hidden.AbsoluteTop).ShouldBe(new float[] { 0, 32, 64 });
        scene.Hidden.AbsoluteHeight.ShouldBe(96);
    }

    [Fact]
    public void ShowingHiddenStack_ShouldDropRowsRemovedWhileHidden_InRenderTarget()
    {
        Scene scene = Build(5, isRenderTarget: true);

        scene.Hidden.Children.RemoveAt(0);
        scene.Hidden.Children.RemoveAt(0);
        scene.Hidden.Visible = true;

        scene.Hidden.AbsoluteHeight.ShouldBe(96);
    }

    [Fact]
    public void ShowingHiddenStack_ShouldCountItsHeightForRelativeToChildrenParent_InRenderTarget()
    {
        Scene scene = Build(5, isRenderTarget: true);
        ContainerRuntime wrapper = new();
        wrapper.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        wrapper.WidthUnits = DimensionUnitType.Absolute;
        wrapper.Width = 100;
        wrapper.HeightUnits = DimensionUnitType.RelativeToChildren;
        wrapper.Height = 0;
        scene.Root.Children.Add(wrapper);
        ContainerRuntime inner = MakeStack(3);
        wrapper.Children.Add(inner);
        inner.Visible = false;
        wrapper.AbsoluteHeight.ShouldBe(0);

        inner.Visible = true;

        wrapper.AbsoluteHeight.ShouldBe(96);
    }

    [Fact]
    public void HidingChildOfRelativeToChildrenParent_ShouldShrinkParent_InRenderTarget()
    {
        Scene scene = Build(0, isRenderTarget: true);

        scene.Shown.Visible = false;

        scene.Shown.AbsoluteHeight.ShouldBe(320);
        scene.Hidden.AbsoluteTop.ShouldBe(scene.Root.AbsoluteTop);
    }

    [Fact]
    public void ShowingPercentOfParentChild_ShouldResolveResizeWhileHidden_InRenderTarget()
    {
        Scene scene = Build(0, isRenderTarget: true);
        ContainerRuntime child = new();
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Width = 50;
        child.Visible = false;
        scene.Root.Children.Add(child);

        scene.Root.Width = 200;
        child.Visible = true;

        child.AbsoluteWidth.ShouldBe(100);
    }

    [Fact]
    public void ShowingRatioChild_ShouldShareRemainingWidth_InRenderTarget()
    {
        Scene scene = Build(0, isRenderTarget: true);
        scene.Root.Children.Clear();
        ContainerRuntime row = new();
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.WidthUnits = DimensionUnitType.RelativeToParent;
        row.Width = 0;
        row.Height = 20;
        scene.Root.Children.Add(row);
        ContainerRuntime first = new() { WidthUnits = DimensionUnitType.Ratio, Width = 1 };
        ContainerRuntime second = new() { WidthUnits = DimensionUnitType.Ratio, Width = 1, Visible = false };
        row.Children.Add(first);
        row.Children.Add(second);

        first.AbsoluteWidth.ShouldBe(500);
        second.Visible = true;

        first.AbsoluteWidth.ShouldBe(250);
        second.AbsoluteWidth.ShouldBe(250);
    }

    [Fact]
    public void ShowingChild_ShouldResolve_WhenRenderTargetIsStillHidden()
    {
        Scene scene = Build(10, isRenderTarget: true);
        scene.Root.Visible = false;

        scene.Root.Width = 300;
        scene.Hidden.Visible = true;

        scene.Hidden.AbsoluteWidth.ShouldBe(300);
    }

    [Fact]
    public void HiddenRenderTarget_ShouldStillLayOutShownChildren()
    {
        Scene scene = Build(10, isRenderTarget: true);
        scene.Root.Visible = false;

        scene.Root.Width = 300;

        scene.Shown.AbsoluteWidth.ShouldBe(300);
    }

    [Fact]
    public void ShowingRenderTarget_ShouldNotLeaveHiddenStackStale()
    {
        Scene scene = Build(10, isRenderTarget: true);
        scene.Root.Visible = false;
        scene.Root.Width = 300;
        scene.Root.Visible = true;

        scene.Hidden.Visible = true;

        scene.Hidden.AbsoluteWidth.ShouldBe(300);
    }

    #endregion

    #region Nesting

    [Fact]
    public void HiddenStackInsideNestedRenderTarget_ShouldBeSkipped()
    {
        Scene scene = Build(0, isRenderTarget: true);
        ContainerRuntime inner = new();
        inner.WidthUnits = DimensionUnitType.RelativeToParent;
        inner.Width = 0;
        inner.Height = 100;
        inner.IsRenderTarget = true;
        scene.Root.Children.Add(inner);
        ContainerRuntime hiddenStack = MakeStack(20);
        hiddenStack.Visible = false;
        inner.Children.Add(hiddenStack);
        inner.Children.Add(MakeStack(3));

        int count = Count(() => scene.Root.Width -= 1);

        // The 12 shown, the nested render target and its stack with 3 rows are laid out; the 21 hidden are not
        count.ShouldBe(12 + 1 + 1 + 3);
    }

    [Fact]
    public void HiddenRenderTargetInsideRenderTarget_ShouldStillLayOut()
    {
        Scene scene = Build(0, isRenderTarget: true);
        ContainerRuntime inner = new();
        inner.WidthUnits = DimensionUnitType.PercentageOfParent;
        inner.Width = 50;
        inner.IsRenderTarget = true;
        inner.Visible = false;
        scene.Root.Children.Add(inner);

        scene.Root.Width = 200;

        inner.AbsoluteWidth.ShouldBe(100);
    }

    [Fact]
    public void HiddenChildOfHiddenRenderTargetInsideRenderTarget_ShouldBeSkippedAndResolvedWhenShown()
    {
        Scene scene = Build(0, isRenderTarget: true);
        ContainerRuntime inner = new();
        inner.WidthUnits = DimensionUnitType.PercentageOfParent;
        inner.Width = 50;
        inner.IsRenderTarget = true;
        inner.Visible = false;
        scene.Root.Children.Add(inner);
        ContainerRuntime child = new();
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Width = 50;
        child.Visible = false;
        inner.Children.Add(child);

        scene.Root.Width = 200;
        child.Visible = true;

        child.AbsoluteWidth.ShouldBe(50);
    }

    [Fact]
    public void HiddenRenderTargetAncestor_ShouldNotStopRenderTargetFromLayingOut()
    {
        ContainerRuntime outer = new();
        outer.WidthUnits = DimensionUnitType.Absolute;
        outer.Width = 100;
        ContainerRuntime target = new();
        target.WidthUnits = DimensionUnitType.Absolute;
        target.Width = 100;
        target.IsRenderTarget = true;
        outer.Children.Add(target);
        ContainerRuntime child = new();
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Width = 50;
        target.Children.Add(child);
        outer.Visible = false;

        target.Width = 200;

        child.AbsoluteWidth.ShouldBe(100);
    }

    #endregion

    #region Suspension

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ResumeLayout_ShouldLeaveHiddenStackResolvedWhenShown_InRenderTarget(bool recursive)
    {
        Scene scene = Build(10, isRenderTarget: true);

        scene.Root.SuspendLayout(recursive);
        scene.Root.Width = 300;
        scene.Root.ResumeLayout(recursive);
        scene.Hidden.Visible = true;

        scene.Shown.AbsoluteWidth.ShouldBe(300);
        scene.Hidden.AbsoluteWidth.ShouldBe(300);
        scene.Hidden.AbsoluteHeight.ShouldBe(320);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ResumeLayout_ShouldNotLayOutHiddenStack_InRenderTarget(bool recursive)
    {
        Scene plain = Build(10, isRenderTarget: false);
        Scene target = Build(10, isRenderTarget: true);

        int plainCount = Count(() =>
        {
            plain.Root.SuspendLayout(recursive);
            plain.Root.Width = 300;
            plain.Root.ResumeLayout(recursive);
        });
        int targetCount = Count(() =>
        {
            target.Root.SuspendLayout(recursive);
            target.Root.Width = 300;
            target.Root.ResumeLayout(recursive);
        });

        targetCount.ShouldBe(plainCount);
    }

    [Fact]
    public void GlobalSuspension_ShouldLeaveHiddenStackResolvedWhenShown_InRenderTarget()
    {
        Scene scene = Build(10, isRenderTarget: true);

        GraphicalUiElement.IsAllLayoutSuspended = true;
        scene.Root.Width = 300;
        scene.Hidden.Children.Add(MakeRow());
        GraphicalUiElement.IsAllLayoutSuspended = false;
        scene.Root.UpdateLayout();
        scene.Hidden.Visible = true;

        scene.Hidden.AbsoluteWidth.ShouldBe(300);
        scene.Hidden.AbsoluteHeight.ShouldBe(11 * 32);
    }

    [Fact]
    public void ShowingStackWhileSuspended_ShouldResolveOnResume_InRenderTarget()
    {
        Scene scene = Build(10, isRenderTarget: true);

        scene.Root.Width = 300;
        scene.Hidden.SuspendLayout();
        scene.Hidden.Visible = true;
        scene.Hidden.ResumeLayout();

        scene.Hidden.AbsoluteWidth.ShouldBe(300);
        scene.Hidden.Children.Last().AbsoluteTop.ShouldBe(scene.Hidden.AbsoluteTop + 9 * 32);
    }

    #endregion

    #region Reparenting and toggling the render target

    [Fact]
    public void ReparentingHiddenStackOutOfRenderTarget_ShouldLayOutWhenShown()
    {
        Scene scene = Build(10, isRenderTarget: true);
        ContainerRuntime other = new();
        other.WidthUnits = DimensionUnitType.Absolute;
        other.Width = 200;
        other.HeightUnits = DimensionUnitType.Absolute;
        other.Height = 100;

        scene.Root.Width = 300;
        other.Children.Add(scene.Hidden);
        scene.Hidden.Visible = true;

        scene.Hidden.AbsoluteWidth.ShouldBe(200);
        scene.Hidden.AbsoluteHeight.ShouldBe(320);
    }

    [Fact]
    public void ReparentingHiddenRowIntoShownStack_ShouldStackItWhenShown()
    {
        Scene scene = Build(3, isRenderTarget: true);
        GraphicalUiElement row = scene.Hidden.Children[0];
        row.Visible = false;

        scene.Shown.Children.Add(row);
        row.Visible = true;

        scene.Shown.AbsoluteHeight.ShouldBe(11 * 32);
        row.AbsoluteTop.ShouldBe(scene.Shown.AbsoluteTop + 10 * 32);
    }

    [Fact]
    public void ReparentingShownStackIntoHiddenStack_ShouldResolveWhenAncestorShown()
    {
        Scene scene = Build(2, isRenderTarget: true);
        ContainerRuntime moved = MakeStack(4);
        scene.Root.Children.Add(moved);

        scene.Hidden.Children.Add(moved);
        scene.Root.Width = 300;
        scene.Hidden.Visible = true;

        moved.AbsoluteWidth.ShouldBe(300);
        moved.AbsoluteHeight.ShouldBe(128);
    }

    [Fact]
    public void TurningRenderTargetOff_ShouldSkipHiddenStackAndResolveWhenShown()
    {
        Scene scene = Build(10, isRenderTarget: true);
        scene.Root.IsRenderTarget = false;

        int count = Count(() => scene.Root.Width = 300);
        scene.Hidden.Visible = true;

        count.ShouldBe(12);
        scene.Hidden.AbsoluteWidth.ShouldBe(300);
    }

    [Fact]
    public void TurningRenderTargetOn_ShouldSkipHiddenStackAndResolveWhenShown()
    {
        Scene scene = Build(10, isRenderTarget: false);
        scene.Root.IsRenderTarget = true;

        int count = Count(() => scene.Root.Width = 300);
        scene.Hidden.Visible = true;

        count.ShouldBe(12);
        scene.Hidden.AbsoluteWidth.ShouldBe(300);
    }

    [Fact]
    public void TurningRenderTargetOff_ShouldResolveWorkSkippedWhileItWasOn()
    {
        Scene scene = Build(10, isRenderTarget: true);
        scene.Root.Width = 300;
        scene.Root.IsRenderTarget = false;

        scene.Hidden.Visible = true;

        scene.Hidden.AbsoluteWidth.ShouldBe(300);
    }

    #endregion

    #region Sweep

    [Fact]
    public void RandomEditsAndToggles_ShouldEndWithTheSameLayout_WithAndWithoutRenderTarget()
    {
        int cases = SweepScale.Cases(200);
        for (int seed = 0; seed < cases; seed++)
        {
            Scene plain = Build(6, isRenderTarget: false);
            Scene target = Build(6, isRenderTarget: true);
            Random randomPlain = new(seed);
            Random randomTarget = new(seed);

            int stepCount = 3 + seed % 6;
            for (int step = 0; step < stepCount; step++)
            {
                int op = randomPlain.Next(7);
                randomTarget.Next(7);
                Apply(plain, op, randomPlain);
                Apply(target, op, randomTarget);
            }
            plain.Hidden.Visible = true;
            plain.Shown.Visible = true;
            target.Hidden.Visible = true;
            target.Shown.Visible = true;
            if (GraphicalUiElement.IsAllLayoutSuspended) GraphicalUiElement.IsAllLayoutSuspended = false;

            Rects(target.Root).ShouldBe(Rects(plain.Root), $"seed {seed}");
        }
    }

    private static void Apply(Scene scene, int op, Random random)
    {
        switch (op)
        {
            case 0: scene.Root.Width = 200 + random.Next(300); break;
            case 1: scene.Hidden.Visible = !scene.Hidden.Visible; break;
            case 2: scene.Shown.Visible = !scene.Shown.Visible; break;
            case 3: scene.Hidden.Children.Add(MakeRow()); break;
            case 4:
                if (scene.Hidden.Children.Count > 0) scene.Hidden.Children.RemoveAt(0);
                break;
            case 5:
                scene.Root.SuspendLayout();
                scene.Root.Width = 100 + random.Next(300);
                scene.Root.ResumeLayout();
                break;
            default:
                if (scene.Hidden.Children.Count > 0) scene.Hidden.Children[0].Height = 10 + random.Next(40);
                break;
        }
    }

    #endregion
}
