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
        for (int seed = 0; seed < SeedCount; seed++)
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

        _output.WriteLine($"{failures.Count} of {SeedCount}: {string.Join("; ", failures)}");
        failures.Count.ShouldBe(0, string.Join("; ", failures.Take(5)));
    }
}
