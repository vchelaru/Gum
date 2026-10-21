using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Showing an element resolves the layout work that was skipped because it, or an ancestor, was hidden, whichever
/// mode the layout was suspended in and whichever element is resumed (#6316).
/// </summary>
public partial class LayoutVisibilityOrderTests
{
    #region Suspension combined with hiding

    [Fact]
    public void OuterSuspendedNonRecursively_StackFilledWhileHiddenThenShown_ShouldLayOutWhenOuterIsResumed()
    {
        // #6316, verbatim.
        ContainerRuntime outer = new();
        SetAbsolute(outer, 100, 100);
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        SetAbsolute(stack, 100, 100);
        outer.AddChild(stack);
        List<RectangleRuntime> rows = new() { CreateRow(), CreateRow(), CreateRow() };

        outer.SuspendLayout();
        outer.Visible = false;
        stack.Visible = false;
        foreach (RectangleRuntime row in rows)
        {
            stack.AddChild(row);
        }
        stack.Visible = true;
        outer.Visible = true;
        outer.ResumeLayout();

        rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20 });
    }

    [Fact]
    public void ShownElementSuspendedItself_ResumedNonRecursively_ShouldLayOutDescendantsFilledWhileHidden()
    {
        ContainerRuntime outer = new();
        SetAbsolute(outer, 100, 100);
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        SetAbsolute(stack, 100, 100);
        outer.AddChild(stack);
        List<RectangleRuntime> rows = new() { CreateRow(), CreateRow(), CreateRow() };

        stack.SuspendLayout();
        outer.Visible = false;
        stack.Visible = false;
        foreach (RectangleRuntime row in rows)
        {
            stack.AddChild(row);
        }
        stack.Visible = true;
        outer.Visible = true;
        stack.ResumeLayout();

        rows.Select(row => row.AbsoluteTop).ShouldBe(new float[] { 0, 10, 20 });
    }

    [Fact]
    public void ChildSuspendedByTheUser_ShouldStayPendingWhenTheShownParentIsResumedNonRecursively()
    {
        // What the user suspended on a child stays theirs to resume, as NonRecursiveResume_ShouldLeaveSeparatelySuspendedChildPending
        // pins without hiding.
        ContainerRuntime outer = new();
        SetAbsolute(outer, 100, 100);
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        SetAbsolute(stack, 100, 100);
        outer.AddChild(stack);
        List<RectangleRuntime> rows = new() { CreateRow(), CreateRow(), CreateRow() };
        foreach (RectangleRuntime row in rows)
        {
            stack.AddChild(row);
        }

        outer.SuspendLayout();
        outer.Visible = false;
        stack.Visible = false;
        stack.SuspendLayout();
        rows[1].Height = 30;
        outer.Visible = true;
        outer.ResumeLayout();
        float whileChildSuspended = rows[2].AbsoluteTop;
        stack.Visible = true;
        stack.ResumeLayout();

        whileChildSuspended.ShouldBe(20, "the stack is still suspended, so it has not laid out the taller row");
        rows[2].AbsoluteTop.ShouldBe(40);
    }

    public static IEnumerable<object[]> Edits()
    {
        yield return new object[] { ContentOp.AddRows };
        yield return new object[] { ContentOp.ResizeRows };
        yield return new object[] { ContentOp.ReparentFirstRowToOuter };
        yield return new object[] { ContentOp.ResizeMiddleContainer };
    }

    /// <summary>Every non-empty set of suspended containers and every non-empty set of hidden ones, suspended
    /// recursively or not, suspended before or after hiding, with the edit made once both have happened. The
    /// containers are shown and resumed in every order, resuming before or after showing.</summary>
    [Theory]
    [MemberData(nameof(Edits))]
    public void SuspendingAndHidingAnyContainers_ShouldLayOutOnceShownAndResumedInAnyOrder(ContentOp edit)
    {
        s_controlSkips = 0;
        List<string> failures = new();
        int cases = 0;
        foreach (Sizing sizing in new[] { Sizing.Fixed, Sizing.ToChildren })
        {
            foreach (StackKind kind in new[] { StackKind.TopToBottom })
            {
                for (int levels = 2; levels <= 3; levels++)
                {
                    int containerCount = levels + 1;
                    for (int hideMask = 1; hideMask < (1 << containerCount); hideMask++)
                    {
                        for (int suspendMask = 1; suspendMask < (1 << containerCount); suspendMask++)
                        {
                            foreach (bool recursive in new[] { false, true })
                            {
                                foreach (bool suspendFirst in new[] { false, true })
                                {
                                    List<Step> steps = new() { new Step(StepKind.Op, Op: ContentOp.AddRows) };
                                    List<Step> hides = Enumerable.Range(0, containerCount)
                                        .Where(i => (hideMask & (1 << i)) != 0)
                                        .Select(i => new Step(StepKind.Hide, i)).ToList();
                                    List<Step> suspends = Enumerable.Range(0, containerCount)
                                        .Where(i => (suspendMask & (1 << i)) != 0)
                                        .Select(i => new Step(StepKind.Suspend, i, Recursive: recursive)).ToList();
                                    steps.AddRange(suspendFirst ? suspends.Concat(hides) : hides.Concat(suspends));
                                    steps.Add(new Step(StepKind.Op, Op: edit));

                                    foreach (RevealOrder reveal in new[] { RevealOrder.OuterFirst, RevealOrder.StackFirst })
                                    {
                                        foreach (bool resumeBeforeReveal in new[] { false, true })
                                        {
                                            foreach (bool resumeOuterFirst in new[] { false, true })
                                            {
                                                cases++;
                                                string? failure = RunAndCompare(levels, kind, sizing, outerStacks: false, steps, reveal,
                                                    resumeBeforeReveal, resumeOuterFirst);
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
                    }
                }
            }
        }

        s_controlSkips.ShouldBeLessThan(cases / 10, "only a few sequences should be left out for differing without any hiding");
        failures.Count.ShouldBe(0, $"{failures.Count} of {cases} differ ({s_controlSkips} left out), first: {string.Join(" || ", failures.Take(3))}");
    }

    private static Step RandomSuspensionStep(Random random, int containerCount, bool includeHiding)
    {
        int pick = random.Next(0, includeHiding ? 9 : 5);
        switch (pick)
        {
            case 0:
                return new Step(StepKind.Suspend, random.Next(0, containerCount), Recursive: random.Next(0, 2) == 0);
            case 1:
                return new Step(StepKind.Resume, random.Next(0, containerCount));
            case 2:
            case 3:
                return new Step(StepKind.Op, Op: (ContentOp)random.Next(0, Enum.GetValues<ContentOp>().Length));
            case 4:
                return new Step(StepKind.Op, Op: ContentOp.AddRows);
            case 5:
            case 6:
                return new Step(StepKind.Hide, random.Next(0, containerCount));
            case 7:
                return new Step(StepKind.Show, random.Next(0, containerCount));
            default:
                return new Step(random.Next(0, 2) == 0 ? StepKind.HideRow : StepKind.ShowRow, random.Next(0, 4));
        }
    }

    [Theory]
    [InlineData(Sizing.Fixed)]
    [InlineData(Sizing.ToChildren)]
    [InlineData(Sizing.Percent)]
    public void RandomSuspendResumeHideShowAndEditSequences_ShouldEndWithTheLayoutOfTheSameEditsOnAVisibleTree(Sizing sizing)
    {
        const bool includeHiding = true;
        s_controlSkips = 0;
        List<string> failures = new();
        const int cases = 2000;
        for (int seed = 0; seed < cases; seed++)
        {
            Random random = new(seed + 90000);
            int levels = random.Next(1, 5);
            StackKind kind = (StackKind)random.Next(0, 4);
            bool outerStacks = random.Next(0, 2) == 0;
            List<Step> steps = new();
            if (random.Next(0, 2) == 0)
            {
                steps.Add(new Step(StepKind.Op, Op: ContentOp.AddRows));
            }
            int stepCount = random.Next(5, 18);
            for (int i = 0; i < stepCount; i++)
            {
                steps.Add(RandomSuspensionStep(random, levels + 1, includeHiding));
            }
            RevealOrder reveal = (RevealOrder)random.Next(0, 4);
            bool resumeBeforeReveal = random.Next(0, 2) == 0;
            bool resumeOuterFirst = random.Next(0, 2) == 0;

            string? failure = RunAndCompare(levels, kind, sizing, outerStacks, steps, reveal, resumeBeforeReveal, resumeOuterFirst);
            if (failure != null)
            {
                failures.Add($"seed {seed}: {failure}");
            }
        }

        s_controlSkips.ShouldBeLessThan(cases / 10, "only a few sequences should be left out for differing without any hiding");
        failures.Count.ShouldBe(0, $"{failures.Count} of {cases} differ ({s_controlSkips} left out), first: {string.Join(" || ", failures.Take(3))}");
    }

    #endregion
}
