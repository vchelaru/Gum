using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// The final layout must not depend on the order Visible was flipped, children were added or the tree was
/// edited while parts of it were hidden (#6307). Each test builds the same tree twice: once never hidden
/// (the reference) and once with hide, edit and show steps interleaved in some order. Both must end with the
/// same rectangle on every element.
/// </summary>
public class LayoutVisibilityOrderTests : BaseTestClass
{
    #region Scenario

    public enum StackKind
    {
        TopToBottom,
        LeftToRight,
        TopToBottomWrapping,
        AutoGridHorizontal
    }

    public enum Sizing
    {
        /// <summary>Every container is a fixed 100x100 (the outer one in the issue).</summary>
        Fixed,
        /// <summary>Every container is sized from its children.</summary>
        ToChildren,
        /// <summary>The outer container is fixed at 200x200 and the others are 100% of their parent.</summary>
        Percent
    }

    public enum ContentOp
    {
        AddRows,
        RemoveFirstRow,
        ResizeRows,
        SetSpacing,
        RowsHalfParentWidth,
        GrowOuter,
        SwitchStackToLeftToRight,
        AddTextRow,
        LengthenText,
        AddRatioRow,
        ChangeStackWidthUnitsToRelativeToParent
    }

    public enum StepKind
    {
        Hide,
        Show,
        Op,
        HideRow,
        ShowRow
    }

    public record Step(StepKind Kind, int Target = 0, ContentOp Op = ContentOp.AddRows)
    {
        public override string ToString() => Kind switch
        {
            StepKind.Hide => $"hide[{Target}]",
            StepKind.Show => $"show[{Target}]",
            StepKind.HideRow => $"hideRow[{Target}]",
            StepKind.ShowRow => $"showRow[{Target}]",
            _ => Op.ToString()
        };
    }

    public enum RevealOrder
    {
        OuterFirst,
        StackFirst,
        HideOrder,
        ReverseHideOrder
    }

    public enum RowPhase
    {
        /// <summary>The rows exist before anything is hidden.</summary>
        BeforeHiding,
        /// <summary>The rows are added once everything that will be hidden is.</summary>
        AfterHiding,
        /// <summary>The rows are added after the first hide only.</summary>
        AfterFirstHide
    }

    private sealed class Scenario
    {
        /// <summary>The containers from the outermost to the stack that holds the rows.</summary>
        public List<ContainerRuntime> Containers = new();
        public List<GraphicalUiElement> Rows = new();
        public List<GraphicalUiElement> Siblings = new();
        public ContainerRuntime Outer => Containers[0];
        public ContainerRuntime Stack => Containers[^1];
        public StackKind Kind;
        public Sizing Sizing;
        public List<int> HiddenContainers = new();
        public List<int> HiddenRows = new();
    }

    private static void SetAbsolute(GraphicalUiElement element, float width, float height)
    {
        element.WidthUnits = DimensionUnitType.Absolute;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Width = width;
        element.Height = height;
    }

    private static RectangleRuntime CreateRow(float width = 100, float height = 10)
    {
        RectangleRuntime row = new();
        SetAbsolute(row, width, height);
        return row;
    }

    /// <param name="levels">How many containers hold the stack, so the outer container is the parent at 1,
    /// the grandparent at 2 and so on.</param>
    /// <param name="outerStacks">Whether the outer container stacks, with a sibling above and one below the
    /// first container in it, so hiding a container also moves its siblings.</param>
    private static Scenario Build(int levels, StackKind kind, Sizing sizing, bool outerStacks)
    {
        Scenario scenario = new() { Kind = kind, Sizing = sizing };
        for (int i = 0; i <= levels; i++)
        {
            ContainerRuntime container = new();
            switch (sizing)
            {
                case Sizing.Fixed:
                    SetAbsolute(container, 100, 100);
                    break;
                case Sizing.ToChildren:
                    container.WidthUnits = DimensionUnitType.RelativeToChildren;
                    container.HeightUnits = DimensionUnitType.RelativeToChildren;
                    container.Width = 0;
                    container.Height = 0;
                    break;
                case Sizing.Percent:
                    if (i == 0)
                    {
                        SetAbsolute(container, 200, 200);
                    }
                    else
                    {
                        container.WidthUnits = DimensionUnitType.PercentageOfParent;
                        container.HeightUnits = DimensionUnitType.PercentageOfParent;
                        container.Width = 100;
                        container.Height = 100;
                    }
                    break;
            }
            scenario.Containers.Add(container);
        }

        ContainerRuntime stack = scenario.Stack;
        switch (kind)
        {
            case StackKind.TopToBottom:
                stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
                break;
            case StackKind.LeftToRight:
                stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
                break;
            case StackKind.TopToBottomWrapping:
                stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
                stack.WrapsChildren = true;
                if (sizing == Sizing.ToChildren)
                {
                    stack.MaxHeight = 25;
                }
                else
                {
                    stack.HeightUnits = DimensionUnitType.Absolute;
                    stack.Height = 25;
                }
                break;
            case StackKind.AutoGridHorizontal:
                stack.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
                stack.AutoGridHorizontalCells = 2;
                stack.AutoGridVerticalCells = 3;
                break;
        }

        if (outerStacks)
        {
            scenario.Outer.ChildrenLayout = ChildrenLayout.TopToBottomStack;
            RectangleRuntime above = CreateRow(50, 15);
            RectangleRuntime below = CreateRow(50, 15);
            scenario.Outer.AddChild(above);
            scenario.Siblings.Add(above);
            scenario.Outer.AddChild(scenario.Containers[1]);
            scenario.Outer.AddChild(below);
            scenario.Siblings.Add(below);
        }
        else
        {
            scenario.Outer.AddChild(scenario.Containers[1]);
        }
        for (int i = 1; i < levels; i++)
        {
            scenario.Containers[i].AddChild(scenario.Containers[i + 1]);
        }
        return scenario;
    }

    private static void Apply(Scenario s, ContentOp op)
    {
        switch (op)
        {
            case ContentOp.AddRows:
                for (int i = 0; i < 3; i++)
                {
                    RectangleRuntime row = CreateRow();
                    s.Stack.AddChild(row);
                    s.Rows.Add(row);
                }
                break;
            case ContentOp.RemoveFirstRow:
                if (s.Rows.Count > 0)
                {
                    GraphicalUiElement row = s.Rows[0];
                    s.Stack.RemoveChild(row);
                    s.Rows.RemoveAt(0);
                    // Keep the hidden-row bookkeeping pointing at the same rows.
                    s.HiddenRows = s.HiddenRows.Where(index => index != 0).Select(index => index - 1).ToList();
                }
                break;
            case ContentOp.ResizeRows:
                foreach (GraphicalUiElement row in s.Rows)
                {
                    row.Height = 25;
                }
                break;
            case ContentOp.SetSpacing:
                s.Stack.StackSpacing = 4;
                break;
            case ContentOp.RowsHalfParentWidth:
                // A percent of a parent that is sized from its children is circular, so it has no defined layout.
                if (s.Sizing == Sizing.ToChildren)
                {
                    break;
                }
                foreach (GraphicalUiElement row in s.Rows)
                {
                    row.WidthUnits = DimensionUnitType.PercentageOfParent;
                    row.Width = 50;
                }
                break;
            case ContentOp.GrowOuter:
                if (s.Sizing != Sizing.ToChildren)
                {
                    s.Outer.Width = s.Outer.Width + 60;
                    s.Outer.Height = s.Outer.Height + 60;
                }
                break;
            case ContentOp.SwitchStackToLeftToRight:
                s.Stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
                break;
            case ContentOp.AddTextRow:
            {
                TextRuntime text = new();
                text.Text = "Hello";
                s.Stack.AddChild(text);
                s.Rows.Add(text);
                break;
            }
            case ContentOp.LengthenText:
                foreach (TextRuntime text in s.Rows.OfType<TextRuntime>())
                {
                    text.Text = "A much longer line of text";
                }
                break;
            case ContentOp.AddRatioRow:
            {
                if (s.Sizing == Sizing.ToChildren)
                {
                    break;
                }
                RectangleRuntime ratio = CreateRow();
                ratio.HeightUnits = DimensionUnitType.Ratio;
                ratio.Height = 1;
                s.Stack.AddChild(ratio);
                s.Rows.Add(ratio);
                break;
            }
            case ContentOp.ChangeStackWidthUnitsToRelativeToParent:
                if (s.Sizing == Sizing.ToChildren)
                {
                    break;
                }
                s.Stack.WidthUnits = DimensionUnitType.RelativeToParent;
                s.Stack.Width = -10;
                break;
        }
    }

    private static void Apply(Scenario s, Step step)
    {
        switch (step.Kind)
        {
            case StepKind.Hide:
                s.Containers[step.Target].Visible = false;
                s.HiddenContainers.Add(step.Target);
                break;
            case StepKind.Show:
                s.Containers[step.Target].Visible = true;
                s.HiddenContainers.Remove(step.Target);
                break;
            case StepKind.HideRow:
                if (step.Target < s.Rows.Count)
                {
                    s.Rows[step.Target].Visible = false;
                    s.HiddenRows.Add(step.Target);
                }
                break;
            case StepKind.ShowRow:
                if (step.Target < s.Rows.Count)
                {
                    s.Rows[step.Target].Visible = true;
                    s.HiddenRows.Remove(step.Target);
                }
                break;
            case StepKind.Op:
                Apply(s, step.Op);
                break;
        }
    }

    private static void Reveal(Scenario s, RevealOrder order, IReadOnlyList<int> hideOrder)
    {
        List<int> containers = s.HiddenContainers.Distinct().ToList();
        switch (order)
        {
            case RevealOrder.OuterFirst:
                containers.Sort();
                break;
            case RevealOrder.StackFirst:
                containers.Sort();
                containers.Reverse();
                break;
            case RevealOrder.HideOrder:
                containers = hideOrder.Where(containers.Contains).Distinct().ToList();
                break;
            case RevealOrder.ReverseHideOrder:
                containers = hideOrder.Where(containers.Contains).Distinct().Reverse().ToList();
                break;
        }
        // Rows are shown last in the top-down orders and first in the bottom-up ones.
        bool rowsFirst = order == RevealOrder.StackFirst || order == RevealOrder.ReverseHideOrder;
        if (rowsFirst)
        {
            foreach (int row in s.HiddenRows.ToList())
            {
                Apply(s, new Step(StepKind.ShowRow, row));
            }
        }
        foreach (int container in containers)
        {
            Apply(s, new Step(StepKind.Show, container));
        }
        if (!rowsFirst)
        {
            foreach (int row in s.HiddenRows.ToList())
            {
                Apply(s, new Step(StepKind.ShowRow, row));
            }
        }
    }

    private static string Rectangles(Scenario s)
    {
        StringBuilder builder = new();
        void Append(string name, GraphicalUiElement element)
        {
            builder.Append(name).Append(' ')
                .Append(element.AbsoluteLeft).Append(',').Append(element.AbsoluteTop).Append(' ')
                .Append(element.AbsoluteWidth).Append('x').Append(element.AbsoluteHeight)
                .Append(element.AbsoluteVisible ? "" : " hidden").AppendLine();
        }
        for (int i = 0; i < s.Containers.Count; i++)
        {
            Append($"container{i}", s.Containers[i]);
        }
        for (int i = 0; i < s.Siblings.Count; i++)
        {
            Append($"sibling{i}", s.Siblings[i]);
        }
        for (int i = 0; i < s.Rows.Count; i++)
        {
            Append($"row{i}", s.Rows[i]);
        }
        return builder.ToString();
    }

    /// <summary>Runs the steps (and then reveals everything) and returns the first way the result differs from
    /// the never-hidden reference, or null when it matches.</summary>
    private static string? RunAndCompare(int levels, StackKind kind, Sizing sizing, bool outerStacks,
        IReadOnlyList<Step> steps, RevealOrder revealOrder)
    {
        Scenario reference = Build(levels, kind, sizing, outerStacks);
        foreach (Step step in steps.Where(s => s.Kind == StepKind.Op))
        {
            Apply(reference, step);
        }
        string expected = Rectangles(reference);

        Scenario actual = Build(levels, kind, sizing, outerStacks);
        List<int> hideOrder = new();
        foreach (Step step in steps)
        {
            if (step.Kind == StepKind.Hide)
            {
                hideOrder.Add(step.Target);
            }
            Apply(actual, step);
        }
        Reveal(actual, revealOrder, hideOrder);
        string result = Rectangles(actual);

        if (result == expected)
        {
            return null;
        }
        string[] expectedLines = expected.Split('\n');
        string[] resultLines = result.Split('\n');
        List<string> differences = new();
        for (int i = 0; i < Math.Max(expectedLines.Length, resultLines.Length); i++)
        {
            string e = i < expectedLines.Length ? expectedLines[i].TrimEnd() : "<missing>";
            string r = i < resultLines.Length ? resultLines[i].TrimEnd() : "<missing>";
            if (e != r)
            {
                differences.Add($"expected '{e}' but was '{r}'");
            }
        }
        return $"levels={levels} {kind} {sizing} outerStacks={outerStacks} reveal={revealOrder} steps=[{string.Join(", ", steps)}]: " +
            string.Join("; ", differences.Take(3));
    }

    #endregion

    #region The issue

    [Fact]
    public void RowsAddedToHiddenStackUnderHiddenParent_ShouldStackOnceBothAreShown()
    {
        // #6307, verbatim.
        ContainerRuntime outer = new();
        outer.Width = 100;
        outer.Height = 100;

        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.Width = 100;
        stack.Height = 100;
        outer.AddChild(stack);

        outer.Visible = false;
        stack.Visible = false;

        List<RectangleRuntime> rows = new();
        for (int i = 0; i < 3; i++)
        {
            RectangleRuntime row = new();
            row.Width = 100;
            row.Height = 10;
            stack.AddChild(row);
            rows.Add(row);
        }

        stack.Visible = true;
        outer.Visible = true;

        rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20 });
    }

    [Fact]
    public void RowsAddedToHiddenStackUnderHiddenParent_ShouldStackOnceBothAreShown_InTheOtherShowOrder()
    {
        ContainerRuntime outer = new();
        outer.Width = 100;
        outer.Height = 100;
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.Width = 100;
        stack.Height = 100;
        outer.AddChild(stack);
        outer.Visible = false;
        stack.Visible = false;
        List<RectangleRuntime> rows = new();
        for (int i = 0; i < 3; i++)
        {
            RectangleRuntime row = new();
            row.Width = 100;
            row.Height = 10;
            stack.AddChild(row);
            rows.Add(row);
        }

        outer.Visible = true;
        stack.Visible = true;

        rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20 });
    }

    [Fact]
    public void HiddenStackUnderVisibleParent_RowsAddedWhileHidden_ShouldStackOnceShown()
    {
        // The issue says this order already works.
        ContainerRuntime outer = new();
        outer.Width = 100;
        outer.Height = 100;
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.Width = 100;
        stack.Height = 100;
        outer.AddChild(stack);
        stack.Visible = false;
        List<RectangleRuntime> rows = new();
        for (int i = 0; i < 3; i++)
        {
            RectangleRuntime row = new();
            row.Width = 100;
            row.Height = 10;
            stack.AddChild(row);
            rows.Add(row);
        }

        stack.Visible = true;

        rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20 });
    }

    [Fact]
    public void ShowingTheStackBeforeAddingRows_ShouldStackAsTheyAreAdded()
    {
        ContainerRuntime outer = new();
        outer.Width = 100;
        outer.Height = 100;
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.Width = 100;
        stack.Height = 100;
        outer.AddChild(stack);
        outer.Visible = false;
        stack.Visible = false;
        stack.Visible = true;
        outer.Visible = true;
        List<RectangleRuntime> rows = new();
        for (int i = 0; i < 3; i++)
        {
            RectangleRuntime row = new();
            row.Width = 100;
            row.Height = 10;
            stack.AddChild(row);
            rows.Add(row);
        }

        rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20 });
    }

    #endregion

    #region Exhaustive hide, add and reveal orders

    /// <summary>Every non-empty set of hidden containers, with the rows added at every phase and the containers
    /// revealed in every order, for 1 to 3 containers above the stack with or without a stacking outer container.</summary>
    [Theory]
    [InlineData(StackKind.TopToBottom, Sizing.Fixed)]
    [InlineData(StackKind.TopToBottom, Sizing.ToChildren)]
    [InlineData(StackKind.TopToBottom, Sizing.Percent)]
    [InlineData(StackKind.LeftToRight, Sizing.Fixed)]
    [InlineData(StackKind.LeftToRight, Sizing.ToChildren)]
    [InlineData(StackKind.TopToBottomWrapping, Sizing.Fixed)]
    [InlineData(StackKind.TopToBottomWrapping, Sizing.ToChildren)]
    [InlineData(StackKind.AutoGridHorizontal, Sizing.Fixed)]
    [InlineData(StackKind.AutoGridHorizontal, Sizing.ToChildren)]
    public void AddingRows_WhileAnySetOfContainersIsHidden_ShouldGiveTheSameLayoutWhateverTheOrder(StackKind kind, Sizing sizing)
    {
        List<string> failures = new();
        int cases = 0;
        foreach (bool outerStacks in new[] { false, true })
        {
            for (int levels = 1; levels <= 3; levels++)
            {
                // Containers[0] is the outer one; the last is the stack, which can be hidden too.
                int containerCount = levels + 1;
                for (int mask = 1; mask < (1 << containerCount); mask++)
                {
                    List<int> hidden = Enumerable.Range(0, containerCount).Where(i => (mask & (1 << i)) != 0).ToList();
                    foreach (List<int> hideOrder in Permutations(hidden))
                    {
                        foreach (RowPhase phase in Enum.GetValues<RowPhase>())
                        {
                            List<Step> steps = new();
                            if (phase == RowPhase.BeforeHiding)
                            {
                                steps.Add(new Step(StepKind.Op, Op: ContentOp.AddRows));
                            }
                            for (int i = 0; i < hideOrder.Count; i++)
                            {
                                steps.Add(new Step(StepKind.Hide, hideOrder[i]));
                                if (phase == RowPhase.AfterFirstHide && i == 0)
                                {
                                    steps.Add(new Step(StepKind.Op, Op: ContentOp.AddRows));
                                }
                            }
                            if (phase == RowPhase.AfterHiding)
                            {
                                steps.Add(new Step(StepKind.Op, Op: ContentOp.AddRows));
                            }

                            foreach (RevealOrder reveal in Enum.GetValues<RevealOrder>())
                            {
                                cases++;
                                string? failure = RunAndCompare(levels, kind, sizing, outerStacks, steps, reveal);
                                if (failure != null)
                                {
                                    failures.Add(failure);
                                }
                            }
                        }
                    }
                }
            }
        }

        failures.Count.ShouldBe(0, $"{failures.Count} of {cases} orders differ, first: {string.Join(" || ", failures.Take(4))}");
    }

    private static IEnumerable<List<int>> Permutations(List<int> items)
    {
        if (items.Count <= 1)
        {
            yield return new List<int>(items);
            yield break;
        }
        for (int i = 0; i < items.Count; i++)
        {
            List<int> rest = new(items);
            rest.RemoveAt(i);
            foreach (List<int> permutation in Permutations(rest))
            {
                permutation.Insert(0, items[i]);
                yield return permutation;
            }
        }
    }

    #endregion

    #region One edit made while the stack and its ancestors are hidden

    /// <summary>Hides the stack and the given number of ancestors, makes one edit, and shows them again in
    /// every order. The rows exist (and are laid out) before anything is hidden.</summary>
    [Theory]
    [InlineData(ContentOp.AddRows)]
    [InlineData(ContentOp.RemoveFirstRow)]
    [InlineData(ContentOp.ResizeRows)]
    [InlineData(ContentOp.SetSpacing)]
    [InlineData(ContentOp.RowsHalfParentWidth)]
    [InlineData(ContentOp.GrowOuter)]
    [InlineData(ContentOp.SwitchStackToLeftToRight)]
    [InlineData(ContentOp.AddTextRow)]
    [InlineData(ContentOp.LengthenText)]
    [InlineData(ContentOp.AddRatioRow)]
    [InlineData(ContentOp.ChangeStackWidthUnitsToRelativeToParent)]
    public void EditMadeWhileTheStackAndItsAncestorsAreHidden_ShouldGiveTheLayoutOfAnEditOnAVisibleTree(ContentOp edit)
    {
        List<string> failures = new();
        int cases = 0;
        foreach (StackKind kind in new[] { StackKind.TopToBottom, StackKind.LeftToRight, StackKind.TopToBottomWrapping, StackKind.AutoGridHorizontal })
        {
            foreach (Sizing sizing in Enum.GetValues<Sizing>())
            {
                foreach (bool outerStacks in new[] { false, true })
                {
                    for (int levels = 1; levels <= 3; levels++)
                    {
                        int containerCount = levels + 1;
                        for (int mask = 1; mask < (1 << containerCount); mask++)
                        {
                            List<int> hidden = Enumerable.Range(0, containerCount).Where(i => (mask & (1 << i)) != 0).ToList();
                            foreach (RevealOrder reveal in Enum.GetValues<RevealOrder>())
                            {
                                List<Step> steps = new()
                                {
                                    new Step(StepKind.Op, Op: ContentOp.AddRows),
                                    new Step(StepKind.Op, Op: ContentOp.AddTextRow)
                                };
                                steps.AddRange(hidden.Select(i => new Step(StepKind.Hide, i)));
                                steps.Add(new Step(StepKind.Op, Op: edit));
                                cases++;
                                string? failure = RunAndCompare(levels, kind, sizing, outerStacks, steps, reveal);
                                if (failure != null)
                                {
                                    failures.Add(failure);
                                }
                            }
                        }
                    }
                }
            }
        }

        failures.Count.ShouldBe(0, $"{failures.Count} of {cases} differ, first: {string.Join(" || ", failures.Take(4))}");
    }

    #endregion

    #region Random interleavings

    private static Step RandomStep(Random random, int containerCount)
    {
        switch (random.Next(0, 7))
        {
            case 0:
            case 1:
                return new Step(StepKind.Hide, random.Next(0, containerCount));
            case 2:
            case 3:
                return new Step(StepKind.Show, random.Next(0, containerCount));
            case 4:
                return new Step(StepKind.HideRow, random.Next(0, 4));
            case 5:
                return new Step(StepKind.ShowRow, random.Next(0, 4));
            default:
                return new Step(StepKind.Op, Op: (ContentOp)random.Next(0, Enum.GetValues<ContentOp>().Length));
        }
    }

    [Theory]
    [InlineData(Sizing.Fixed)]
    [InlineData(Sizing.ToChildren)]
    [InlineData(Sizing.Percent)]
    public void RandomHideShowAndEditSequences_ShouldEndWithTheLayoutOfTheSameEditsOnAVisibleTree(Sizing sizing)
    {
        List<string> failures = new();
        const int cases = 1500;
        for (int seed = 0; seed < cases; seed++)
        {
            Random random = new(seed);
            int levels = random.Next(1, 5);
            StackKind kind = (StackKind)random.Next(0, 4);
            bool outerStacks = random.Next(0, 2) == 0;
            List<Step> steps = new();
            // Start from some rows half the time so edits have something to act on.
            if (random.Next(0, 2) == 0)
            {
                steps.Add(new Step(StepKind.Op, Op: ContentOp.AddRows));
            }
            int stepCount = random.Next(4, 16);
            for (int i = 0; i < stepCount; i++)
            {
                steps.Add(RandomStep(random, levels + 1));
            }
            RevealOrder reveal = (RevealOrder)random.Next(0, 4);

            string? failure = RunAndCompare(levels, kind, sizing, outerStacks, steps, reveal);
            if (failure != null)
            {
                failures.Add($"seed {seed}: {failure}");
            }
        }

        failures.Count.ShouldBe(0, $"{failures.Count} of {cases} differ, first: {string.Join(" || ", failures.Take(3))}");
    }

    #endregion

    #region Visible and AbsoluteVisible

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void HidingAnAncestor_ShouldLeaveVisibleTrueAndMakeAbsoluteVisibleFalse_ForEveryDescendant(int levels)
    {
        Scenario scenario = Build(levels, StackKind.TopToBottom, Sizing.Fixed, outerStacks: false);
        Apply(scenario, ContentOp.AddRows);

        scenario.Outer.Visible = false;

        foreach (GraphicalUiElement descendant in scenario.Containers.Skip(1).Concat(scenario.Rows))
        {
            descendant.Visible.ShouldBeTrue();
            descendant.AbsoluteVisible.ShouldBeFalse();
        }
        scenario.Outer.Visible.ShouldBeFalse();
        scenario.Outer.AbsoluteVisible.ShouldBeFalse();
    }

    [Fact]
    public void ShowingTheOuterContainerWhileAMiddleContainerIsHidden_ShouldKeepEverythingBelowItHidden()
    {
        Scenario scenario = Build(3, StackKind.TopToBottom, Sizing.Fixed, outerStacks: false);
        Apply(scenario, ContentOp.AddRows);
        scenario.Outer.Visible = false;
        scenario.Containers[2].Visible = false;

        scenario.Outer.Visible = true;

        scenario.Outer.AbsoluteVisible.ShouldBeTrue();
        scenario.Containers[1].AbsoluteVisible.ShouldBeTrue();
        scenario.Containers[2].AbsoluteVisible.ShouldBeFalse();
        scenario.Stack.AbsoluteVisible.ShouldBeFalse();
        scenario.Rows.ShouldAllBe(row => !row.AbsoluteVisible);
    }

    [Fact]
    public void ElementWithNoParent_ShouldHaveAbsoluteVisibleEqualToVisible()
    {
        ContainerRuntime container = new();

        container.AbsoluteVisible.ShouldBeTrue();
        container.Visible = false;
        container.AbsoluteVisible.ShouldBeFalse();
    }

    [Fact]
    public void VisibleChanged_ShouldRaiseOncePerChange_AndNotWhenSetToTheSameValue()
    {
        Scenario scenario = Build(2, StackKind.TopToBottom, Sizing.Fixed, outerStacks: false);
        Apply(scenario, ContentOp.AddRows);
        int raised = 0;
        scenario.Stack.VisibleChanged += (_, _) => raised++;

        scenario.Outer.Visible = false;
        scenario.Stack.Visible = false;
        scenario.Stack.Visible = false;
        scenario.Stack.Visible = true;
        scenario.Outer.Visible = true;

        raised.ShouldBe(2);
    }

    #endregion

    #region Single toggles

    [Theory]
    [InlineData(StackKind.TopToBottom, 0)]
    [InlineData(StackKind.TopToBottom, 1)]
    [InlineData(StackKind.TopToBottom, 2)]
    [InlineData(StackKind.LeftToRight, 0)]
    [InlineData(StackKind.LeftToRight, 1)]
    [InlineData(StackKind.LeftToRight, 2)]
    [InlineData(StackKind.TopToBottomWrapping, 1)]
    [InlineData(StackKind.AutoGridHorizontal, 0)]
    [InlineData(StackKind.AutoGridHorizontal, 1)]
    [InlineData(StackKind.AutoGridHorizontal, 2)]
    public void ReShowingARow_ShouldPutEverySiblingBackWhereItWas(StackKind kind, int rowIndex)
    {
        Scenario scenario = Build(1, kind, Sizing.Fixed, outerStacks: false);
        Apply(scenario, ContentOp.AddRows);
        string before = Rectangles(scenario);

        scenario.Rows[rowIndex].Visible = false;
        string whileHidden = Rectangles(scenario);
        scenario.Rows[rowIndex].Visible = true;

        whileHidden.ShouldNotBe(before, "hiding a row moves the rows after it, or hides it");
        Rectangles(scenario).ShouldBe(before);
    }

    [Theory]
    [InlineData(StackKind.TopToBottom)]
    [InlineData(StackKind.LeftToRight)]
    [InlineData(StackKind.TopToBottomWrapping)]
    [InlineData(StackKind.AutoGridHorizontal)]
    public void TogglingARowRepeatedly_ShouldAlwaysEndWhereASingleToggleDoes(StackKind kind)
    {
        Scenario scenario = Build(1, kind, Sizing.ToChildren, outerStacks: true);
        Apply(scenario, ContentOp.AddRows);
        string before = Rectangles(scenario);

        for (int i = 0; i < 6; i++)
        {
            scenario.Rows[1].Visible = false;
            scenario.Rows[1].Visible = true;
        }

        Rectangles(scenario).ShouldBe(before);
    }

    [Theory]
    [InlineData(StackKind.TopToBottom)]
    [InlineData(StackKind.LeftToRight)]
    [InlineData(StackKind.TopToBottomWrapping)]
    [InlineData(StackKind.AutoGridHorizontal)]
    public void HidingEveryRowAndShowingThemInReverse_ShouldRestoreTheLayout(StackKind kind)
    {
        Scenario scenario = Build(1, kind, Sizing.ToChildren, outerStacks: false);
        Apply(scenario, ContentOp.AddRows);
        string before = Rectangles(scenario);

        foreach (GraphicalUiElement row in scenario.Rows)
        {
            row.Visible = false;
        }
        for (int i = scenario.Rows.Count - 1; i >= 0; i--)
        {
            scenario.Rows[i].Visible = true;
        }

        Rectangles(scenario).ShouldBe(before);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RowHiddenBeforeOrAfterBeingAdded_ShouldGiveTheSameLayout(bool hideBeforeAdding)
    {
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        SetAbsolute(stack, 100, 100);
        List<RectangleRuntime> rows = new() { CreateRow(), CreateRow(), CreateRow() };
        if (hideBeforeAdding)
        {
            rows[1].Visible = false;
        }
        foreach (RectangleRuntime row in rows)
        {
            stack.AddChild(row);
        }
        if (!hideBeforeAdding)
        {
            rows[1].Visible = false;
        }

        rows[0].AbsoluteTop.ShouldBe(0);
        rows[2].AbsoluteTop.ShouldBe(10);

        rows[1].Visible = true;

        rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20 });
    }

    [Fact]
    public void StackInsideAStack_HiddenWithItsParentAndFilledWhileHidden_ShouldLayOutOnceBothAreShown()
    {
        ContainerRuntime outer = new();
        outer.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        SetAbsolute(outer, 100, 200);
        RectangleRuntime header = CreateRow(100, 20);
        ContainerRuntime inner = new();
        inner.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        inner.WidthUnits = DimensionUnitType.Absolute;
        inner.Width = 100;
        inner.HeightUnits = DimensionUnitType.RelativeToChildren;
        inner.Height = 0;
        outer.AddChild(header);
        outer.AddChild(inner);
        RectangleRuntime footer = CreateRow(100, 20);
        outer.AddChild(footer);
        outer.Visible = false;
        inner.Visible = false;

        List<RectangleRuntime> rows = new();
        for (int i = 0; i < 3; i++)
        {
            RectangleRuntime row = CreateRow();
            inner.AddChild(row);
            rows.Add(row);
        }
        inner.Visible = true;
        outer.Visible = true;

        rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 20, 30, 40 });
        footer.AbsoluteTop.ShouldBe(50);
    }

    #endregion

    #region Work skipped while hidden is done on show

    [Fact]
    public void RowsAddedToAHiddenStack_ShouldNotCostALayoutWhileTheStackAndItsParentAreHidden()
    {
        // Skipping layout for hidden elements is the documented cost saving; recording what was skipped must not
        // turn into doing it.
        Scenario scenario = Build(1, StackKind.TopToBottom, Sizing.Fixed, outerStacks: false);
        scenario.Outer.Visible = false;
        scenario.Stack.Visible = false;
        List<RectangleRuntime> rows = new() { CreateRow(), CreateRow(), CreateRow() };
        int layoutsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        foreach (RectangleRuntime row in rows)
        {
            scenario.Stack.AddChild(row);
        }

        (GraphicalUiElement.UpdateLayoutCallCount - layoutsBefore).ShouldBe(0);
    }

    [Fact]
    public void RowsAddedToAHiddenStack_ShouldBeLaidOutOnceWhenShown_NotOncePerRow()
    {
        Scenario scenario = Build(1, StackKind.TopToBottom, Sizing.Fixed, outerStacks: false);
        scenario.Outer.Visible = false;
        scenario.Stack.Visible = false;
        for (int i = 0; i < 50; i++)
        {
            Apply(scenario, ContentOp.AddRows);
        }
        int layoutsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        scenario.Stack.Visible = true;
        scenario.Outer.Visible = true;

        // A layout per element is 1 outer + 1 stack + 150 rows; a layout per row per pending request would be far more.
        (GraphicalUiElement.UpdateLayoutCallCount - layoutsBefore).ShouldBeLessThan(400);
        scenario.Rows[149].AbsoluteTop.ShouldBe(1490);
    }

    [Fact]
    public void ChildResizedWhileHidden_ShouldResizeTheParentSizedToItsChildrenOnShow()
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        RectangleRuntime child = CreateRow(50, 50);
        parent.AddChild(child);
        parent.Visible = false;
        child.Visible = false;

        child.Height = 80;
        child.Width = 70;
        parent.Visible = true;
        child.Visible = true;

        parent.AbsoluteWidth.ShouldBe(70);
        parent.AbsoluteHeight.ShouldBe(80);
    }

    [Fact]
    public void ParentResizedWhileHidden_ShouldResizeItsPercentRatioAndRelativeChildrenOnShow()
    {
        ContainerRuntime grandparent = new();
        SetAbsolute(grandparent, 200, 100);
        ContainerRuntime parent = new();
        parent.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        parent.WidthUnits = DimensionUnitType.PercentageOfParent;
        parent.Width = 100;
        parent.HeightUnits = DimensionUnitType.PercentageOfParent;
        parent.Height = 100;
        grandparent.AddChild(parent);
        RectangleRuntime percent = CreateRow(0, 10);
        percent.WidthUnits = DimensionUnitType.PercentageOfParent;
        percent.Width = 25;
        RectangleRuntime ratio = CreateRow(0, 10);
        ratio.WidthUnits = DimensionUnitType.Ratio;
        ratio.Width = 1;
        parent.AddChild(percent);
        parent.AddChild(ratio);
        grandparent.Visible = false;
        parent.Visible = false;

        grandparent.Width = 400;
        parent.Visible = true;
        grandparent.Visible = true;

        parent.AbsoluteWidth.ShouldBe(400);
        percent.AbsoluteWidth.ShouldBe(100);
        ratio.AbsoluteWidth.ShouldBe(300);
        ratio.AbsoluteLeft.ShouldBe(100);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TextChangedWhileHidden_ShouldReflowTheStackOnShow(bool hideParentToo)
    {
        ContainerRuntime outer = new();
        SetAbsolute(outer, 200, 200);
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        SetAbsolute(stack, 200, 200);
        outer.AddChild(stack);
        TextRuntime first = new();
        first.Text = "short";
        TextRuntime second = new();
        second.Text = "second";
        stack.AddChild(first);
        stack.AddChild(second);
        float secondTopBefore = second.AbsoluteTop;
        if (hideParentToo)
        {
            outer.Visible = false;
        }
        stack.Visible = false;

        first.Text = "first line" + System.Environment.NewLine + "and another line";
        stack.Visible = true;
        outer.Visible = true;

        second.AbsoluteTop.ShouldBeGreaterThan(secondTopBefore);
        second.AbsoluteTop.ShouldBe(first.AbsoluteTop + first.AbsoluteHeight);
    }

    [Fact]
    public void WithUpdatesAppliedWhenInvisible_HiddenStackFilledUnderHiddenParent_ShouldBeLaidOutAsItIsFilled()
    {
        bool original = GraphicalUiElement.AreUpdatesAppliedWhenInvisible;
        GraphicalUiElement.AreUpdatesAppliedWhenInvisible = true;
        try
        {
            Scenario scenario = Build(1, StackKind.TopToBottom, Sizing.Fixed, outerStacks: false);
            scenario.Outer.Visible = false;
            scenario.Stack.Visible = false;

            Apply(scenario, ContentOp.AddRows);

            scenario.Rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20 });
        }
        finally
        {
            GraphicalUiElement.AreUpdatesAppliedWhenInvisible = original;
        }
    }

    [Fact]
    public void HiddenStackInsideAHiddenRenderTarget_FilledWhileHidden_ShouldBeLaidOutAsItIsFilled()
    {
        // Render targets are laid out while hidden because something else may draw them.
        Scenario scenario = Build(1, StackKind.TopToBottom, Sizing.Fixed, outerStacks: false);
        scenario.Outer.IsRenderTarget = true;
        scenario.Outer.Visible = false;
        scenario.Stack.Visible = false;

        Apply(scenario, ContentOp.AddRows);
        scenario.Stack.Visible = true;
        scenario.Outer.Visible = true;

        scenario.Rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20 });
    }

    [Fact]
    public void ChildAddedToAHiddenElement_ShouldMoveTheSiblingsOfItsParentWhenShown()
    {
        // The row is hidden, which leaves the parent narrower; a child added while it is hidden leaves a pending
        // layout on it. Showing it widens the parent, and the parent's own siblings in the stack have to move.
        ContainerRuntime root = new();
        root.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        SetAbsolute(root, 300, 200);
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 6;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.Height = 7;
        ContainerRuntime hidden = new();
        hidden.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        hidden.WidthUnits = DimensionUnitType.Absolute;
        hidden.Width = 26;
        hidden.HeightUnits = DimensionUnitType.RelativeToChildren;
        hidden.Height = 0;
        RectangleRuntime sibling = CreateRow(30, 21);
        root.AddChild(parent);
        parent.AddChild(hidden);
        root.AddChild(sibling);
        float startWidth = parent.AbsoluteWidth;
        hidden.Visible = false;

        ContainerRuntime added = new();
        SetAbsolute(added, 5, 16);
        hidden.AddChild(added);
        hidden.Visible = true;

        startWidth.ShouldBe(32);
        parent.AbsoluteWidth.ShouldBe(32);
        sibling.AbsoluteLeft.ShouldBe(32);
    }

    [Fact]
    public void StackShownWhileItsParentIsStillHidden_ShouldStillLayItsRowsOutWhenTheParentIsShown()
    {
        // Showing the stack runs a shallow layout of it. That layout must not discard the deeper one that was
        // skipped while it was hidden (#6307).
        Scenario scenario = Build(3, StackKind.TopToBottom, Sizing.ToChildren, outerStacks: false);
        Apply(scenario, ContentOp.AddRows);
        scenario.Containers[1].Visible = false;
        scenario.Stack.Visible = false;
        Apply(scenario, ContentOp.AddRows);

        scenario.Stack.Visible = true;
        scenario.Containers[1].Visible = true;

        scenario.Rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20, 30, 40, 50 });
        scenario.Outer.AbsoluteHeight.ShouldBe(60);
    }

    #endregion

    #region Suspension

    public enum SuspendMode
    {
        Element,
        Recursive,
        Global
    }

    // SuspendLayout() on the outer container only, then showing everything and calling a non-recursive ResumeLayout()
    // is left out: that resume does not visit the hidden descendants that were given work, so it needs a decision on
    // what a non-recursive resume covers (#6316).
    [Theory]
    [InlineData(SuspendMode.Element, true)]
    [InlineData(SuspendMode.Recursive, true)]
    [InlineData(SuspendMode.Recursive, false)]
    [InlineData(SuspendMode.Global, true)]
    [InlineData(SuspendMode.Global, false)]
    public void HidingAndFillingWhileSuspended_ShouldGiveTheLayoutOfAVisibleTree(SuspendMode mode, bool resumeBeforeShowing)
    {
        List<string> failures = new();
        foreach (StackKind kind in Enum.GetValues<StackKind>())
        {
            foreach (Sizing sizing in Enum.GetValues<Sizing>())
            {
                Scenario reference = Build(2, kind, sizing, outerStacks: false);
                Apply(reference, ContentOp.AddRows);
                string expected = Rectangles(reference);

                Scenario scenario = Build(2, kind, sizing, outerStacks: false);
                void Resume()
                {
                    switch (mode)
                    {
                        case SuspendMode.Element:
                            scenario.Outer.ResumeLayout();
                            break;
                        case SuspendMode.Recursive:
                            scenario.Outer.ResumeLayout(recursive: true);
                            break;
                        case SuspendMode.Global:
                            GraphicalUiElement.IsAllLayoutSuspended = false;
                            scenario.Outer.UpdateLayout();
                            break;
                    }
                }

                try
                {
                    switch (mode)
                    {
                        case SuspendMode.Element:
                            scenario.Outer.SuspendLayout();
                            break;
                        case SuspendMode.Recursive:
                            scenario.Outer.SuspendLayout(recursive: true);
                            break;
                        case SuspendMode.Global:
                            GraphicalUiElement.IsAllLayoutSuspended = true;
                            break;
                    }
                    scenario.Outer.Visible = false;
                    scenario.Stack.Visible = false;
                    Apply(scenario, ContentOp.AddRows);
                    if (resumeBeforeShowing)
                    {
                        Resume();
                    }
                    scenario.Stack.Visible = true;
                    scenario.Outer.Visible = true;
                    if (!resumeBeforeShowing)
                    {
                        Resume();
                    }
                }
                finally
                {
                    GraphicalUiElement.IsAllLayoutSuspended = false;
                }

                if (Rectangles(scenario) != expected)
                {
                    failures.Add($"{kind} {sizing}");
                }
            }
        }

        failures.Count.ShouldBe(0, $"{mode} resumeBeforeShowing={resumeBeforeShowing}: {string.Join(", ", failures)}");
    }

    #endregion

    #region Screens (elements with no renderable)

    [Fact]
    public void StackUnderAHiddenScreenChild_FilledWhileHidden_ShouldLayOutOnShow()
    {
        GraphicalUiElement screen = new();
        ContainerRuntime container = new();
        SetAbsolute(container, 100, 100);
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        SetAbsolute(stack, 100, 100);
        container.AddChild(stack);
        container.ElementGueContainingThis = screen;
        container.Visible = false;
        stack.Visible = false;

        List<RectangleRuntime> rows = new();
        for (int i = 0; i < 3; i++)
        {
            RectangleRuntime row = CreateRow();
            stack.AddChild(row);
            rows.Add(row);
        }
        stack.Visible = true;
        container.Visible = true;

        rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20 });
    }

    #endregion
}
