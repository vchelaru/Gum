using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public partial class LayoutRandomTreeSweepTests
{
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
            if (incremental != expected)
            {
                failures.Add($"seed {seed}: {FirstDifference(expected, incremental).Split('\n')[0]}");
            }
        }

        _output.WriteLine($"{failures.Count} of {SeedCount}: {string.Join("; ", failures)}");
        failures.Count.ShouldBe(0, string.Join("; ", failures.Take(5)));
    }
}
