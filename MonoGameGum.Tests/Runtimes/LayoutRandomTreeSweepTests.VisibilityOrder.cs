using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public partial class LayoutRandomTreeSweepTests
{
    /// <summary>
    /// Whether laying out any one element of the tree to depth 0 leaves the tree as it was. A wrapping stack with a
    /// Ratio child does not (#6315), which has nothing to do with visibility, so the sweep below leaves such trees out.
    /// </summary>
    private static bool ShallowLayoutsLeaveTreeAlone(Node root)
    {
        string before = Snapshot(root, includeHidden: false);
        foreach (Node node in Walk(root))
        {
            node.Runtime.UpdateLayout(true, 0);
            if (Snapshot(root, includeHidden: false) != before)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Random well-formed trees with random parts hidden, edited (resized, respaced, children added and removed)
    /// while hidden and then shown again in a random order, either all at the end or a few at a time between the
    /// edits. Whatever the order, the shown tree must match a fresh build of the same tree (#6307).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Sweep_HidingEditingAndShowingInAnyOrder_ShouldMatchAFreshBuildOfTheSameTree(bool showBetweenEdits)
    {
        Edit[] edits = { Edit.Resize, Edit.Spacing, Edit.AddChild, Edit.RemoveChild, Edit.Resize, Edit.AddChild };
        List<string> failures = new();
        int skipped = 0;
        for (int seed = 0; seed < SeedCount; seed++)
        {
            Node root = GenerateTree(seed, wellFormed: true);
            foreach (Node node in Walk(root))
            {
                node.Visible = true;
            }
            BuildSuspended(root);

            Rng rng = new Rng(seed + 700000);
            List<Node> hidden = new();
            foreach (Node node in Walk(root).Skip(1))
            {
                if (rng.Chance(35))
                {
                    node.Runtime.Visible = false;
                    hidden.Add(node);
                }
            }

            foreach (Edit edit in edits)
            {
                ApplyEdit(root, rng, edit);
                if (showBetweenEdits && hidden.Count > 0)
                {
                    Node shown = hidden[rng.Int(0, hidden.Count)];
                    shown.Runtime.Visible = true;
                    hidden.Remove(shown);
                }
            }

            while (hidden.Count > 0)
            {
                Node shown = hidden[rng.Int(0, hidden.Count)];
                shown.Runtime.Visible = true;
                hidden.Remove(shown);
            }

            string incremental = Snapshot(root, includeHidden: false);
            Node fresh = CloneSpec(root);
            BuildSuspended(fresh);
            string expected = Snapshot(fresh, includeHidden: false);
            if (incremental != expected && !ShallowLayoutsLeaveTreeAlone(fresh))
            {
                skipped++;
                continue;
            }
            if (incremental != expected)
            {
                failures.Add($"seed {seed}: {FirstDifference(expected, incremental).Split('\n')[0]}");
            }
        }

        _output.WriteLine($"{failures.Count} of {SeedCount - skipped} ({skipped} left out for #6315): {string.Join("; ", failures)}");
        skipped.ShouldBeLessThan(SeedCount / 20, "only a few trees should be left out");
        failures.Count.ShouldBe(0, string.Join("; ", failures.Take(5)));
    }

    /// <summary>
    /// The sweep above with <c>SuspendLayout</c> (this element only, or its whole subtree) on a random part of the
    /// tree before anything is hidden, and every suspended element resumed in a random order, before or after the
    /// hidden parts are shown. Showing resolves what hiding skipped whichever suspend mode and resume order the
    /// caller used (#6316). Trees where suspending and editing already differ without any hiding are left out.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Sweep_SuspendingHidingEditingShowingAndResuming_ShouldMatchAFreshBuildOfTheSameTree(bool recursive, bool resumeBeforeShowing)
    {
        Edit[] edits = { Edit.Resize, Edit.Spacing, Edit.AddChild, Edit.RemoveChild, Edit.Resize, Edit.AddChild, Edit.AddChild, Edit.Resize, Edit.Spacing, Edit.AddChild, Edit.Resize, Edit.RemoveChild };
        List<string> failures = new();
        int skipped = 0;
        for (int seed = 0; seed < SeedCount * 2; seed++)
        {
            string? RunOnce(bool hide)
            {
                Node root = GenerateTree(seed, wellFormed: true);
                foreach (Node node in Walk(root))
                {
                    node.Visible = true;
                }
                BuildSuspended(root);

                Rng rng = new Rng(seed + 800000);
                List<Node> suspended = new();
                foreach (Node node in Walk(root))
                {
                    if (rng.Chance(50))
                    {
                        node.Runtime.SuspendLayout(recursive);
                        suspended.Add(node);
                    }
                }
                List<Node> hidden = new();
                foreach (Node node in Walk(root).Skip(1))
                {
                    // Hiding is likelier for a node that sits in a suspended or hidden one, where skipped work builds up.
                    Node? parent = Walk(root).FirstOrDefault(n => n.Children.Contains(node));
                    bool inside = parent != null && (suspended.Contains(parent) || hidden.Contains(parent));
                    if (rng.Chance(inside ? 70 : 30) && hide)
                    {
                        node.Runtime.Visible = false;
                        hidden.Add(node);
                    }
                }

                foreach (Edit edit in edits)
                {
                    ApplyEdit(root, rng, edit);
                }

                void ShowAll()
                {
                    while (hidden.Count > 0)
                    {
                        Node shown = hidden[rng.Int(0, hidden.Count)];
                        shown.Runtime.Visible = true;
                        hidden.Remove(shown);
                    }
                }
                void ResumeAll()
                {
                    while (suspended.Count > 0)
                    {
                        Node resumed = suspended[rng.Int(0, suspended.Count)];
                        resumed.Runtime.ResumeLayout(recursive);
                        suspended.Remove(resumed);
                    }
                }
                if (resumeBeforeShowing)
                {
                    ResumeAll();
                    ShowAll();
                }
                else
                {
                    ShowAll();
                    ResumeAll();
                }
                return Snapshot(root, includeHidden: false);
            }

            Node specAfter = GenerateTree(seed, wellFormed: true);
            string incremental = RunOnce(hide: true)!;
            string control = RunOnce(hide: false)!;
            // The tree the edits end with, built fresh. Edits are the same in both runs, so the control's tree is the
            // one to rebuild.
            Node finalTree = GenerateTree(seed, wellFormed: true);
            foreach (Node node in Walk(finalTree))
            {
                node.Visible = true;
            }
            BuildSuspended(finalTree);
            // Draw what the runs drew before their edits, so the edits are the same.
            Rng replay = new Rng(seed + 800000);
            foreach (Node node in Walk(finalTree))
            {
                replay.Chance(50);
            }
            foreach (Node node in Walk(finalTree).Skip(1))
            {
                replay.Chance(50);
            }
            foreach (Edit edit in edits)
            {
                ApplyEdit(finalTree, replay, edit);
            }
            Node fresh = CloneSpec(finalTree);
            BuildSuspended(fresh);
            string expected = Snapshot(fresh, includeHidden: false);

            if (incremental == expected)
            {
                continue;
            }
            if (control != expected || !ShallowLayoutsLeaveTreeAlone(fresh))
            {
                skipped++;
                continue;
            }
            failures.Add($"seed {seed}: {FirstDifference(expected, incremental).Split('\n')[0]}");
        }

        _output.WriteLine($"{failures.Count} of {SeedCount * 2 - skipped} ({skipped} left out): {string.Join("; ", failures)}");
        skipped.ShouldBeLessThan(SeedCount * 2 / 4, "only some trees should be left out");
        failures.Count.ShouldBe(0, string.Join("; ", failures.Take(5)));
    }
}
