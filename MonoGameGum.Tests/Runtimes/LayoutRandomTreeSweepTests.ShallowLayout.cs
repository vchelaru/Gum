using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public partial class LayoutRandomTreeSweepTests
{
    /// <summary>
    /// The first element whose shallow layout (<c>UpdateLayout(true, 0)</c>, which showing and moving an element
    /// run) changes the tree, as "path: first difference", or null when none does.
    /// </summary>
    private static string? FindShallowLayoutThatMovesTheTree(Node root)
    {
        string before = Snapshot(root, includeHidden: false);
        List<Node> all = Walk(root).ToList();
        for (int i = 0; i < all.Count; i++)
        {
            all[i].Runtime.UpdateLayout(true, 0);
            string after = Snapshot(root, includeHidden: false);
            if (after != before)
            {
                return $"node {i}: {FirstDifference(before, after).Split('\n')[0]}";
            }
        }
        return null;
    }

    /// <summary>
    /// A shallow layout of any one element of a laid-out tree changes nothing: it places what it touches where a full
    /// layout put it (#6315).
    /// </summary>
    [Theory]
    [InlineData(true)]
    public void Sweep_ShallowLayoutOfAnyElement_ShouldLeaveTheLaidOutTreeAlone(bool wellFormed)
    {
        List<string> failures = new();
        for (int seed = 0; seed < Seeds; seed++)
        {
            Node root = GenerateTree(seed, wellFormed);
            foreach (Node node in Walk(root))
            {
                node.Visible = true;
            }
            BuildSuspended(root);

            string? moved = FindShallowLayoutThatMovesTheTree(root);
            if (moved != null)
            {
                failures.Add($"seed {seed}: {moved}");
            }
        }

        _output.WriteLine($"{failures.Count} of {Seeds}: {string.Join("; ", failures)}");
        failures.Count.ShouldBe(0, string.Join("; ", failures.Take(5)));
    }

    /// <summary>
    /// A shallow layout of any hidden element leaves every visible element where it was, and showing that element
    /// afterwards gives the tree showing it without the shallow layout gives (#6366). Hidden elements' own stored
    /// rectangles are not compared: the shallow layout refreshes them on purpose.
    /// </summary>
    [Fact]
    public void Sweep_ShallowLayoutOfAHiddenElement_ShouldNotMoveTheVisibleTreeOrChangeWhatShowingItGives()
    {
        List<string> failures = new();
        int hiddenTargets = 0;
        for (int seed = 0; seed < Seeds; seed++)
        {
            Node probe = GenerateTree(seed, wellFormed: true);
            int hiddenCount = Walk(probe).Skip(1).Count(n => !n.Visible);
            for (int hiddenNumber = 0; hiddenNumber < hiddenCount; hiddenNumber++)
            {
                hiddenTargets++;

                Node shallow = GenerateTree(seed, wellFormed: true);
                BuildSuspended(shallow);
                Node target = Walk(shallow).Skip(1).Where(n => !n.Visible).ElementAt(hiddenNumber);
                string before = Snapshot(shallow, includeHidden: false);
                target.Runtime.UpdateLayout(true, 0);
                string after = Snapshot(shallow, includeHidden: false);
                if (after != before)
                {
                    failures.Add($"seed {seed} hidden {hiddenNumber}: {FirstDifference(before, after).Split('\n')[0]}");
                    continue;
                }

                target.Visible = true;
                target.Runtime.Visible = true;
                string shownAfterShallow = Snapshot(shallow, includeHidden: false);

                Node plain = GenerateTree(seed, wellFormed: true);
                BuildSuspended(plain);
                Node plainTarget = Walk(plain).Skip(1).Where(n => !n.Visible).ElementAt(hiddenNumber);
                plainTarget.Visible = true;
                plainTarget.Runtime.Visible = true;
                string shownPlain = Snapshot(plain, includeHidden: false);
                if (shownAfterShallow != shownPlain)
                {
                    failures.Add($"seed {seed} hidden {hiddenNumber} shown: {FirstDifference(shownPlain, shownAfterShallow).Split('\n')[0]}");
                }
            }
        }

        _output.WriteLine($"{failures.Count} of {hiddenTargets} hidden targets: {string.Join("; ", failures)}");
        hiddenTargets.ShouldBeGreaterThan(0);
        failures.Count.ShouldBe(0, string.Join("; ", failures.Take(5)));
    }
}
