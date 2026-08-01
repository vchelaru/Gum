using System.Collections.Generic;

namespace Gum.Managers;

/// <summary>
/// Captures and re-applies which tree nodes are expanded, addressing nodes by a text path
/// ("Screens/MainMenu/Buttons") rather than by object identity so a snapshot survives the tree
/// being rebuilt from scratch.
/// </summary>
/// <remarks>
/// The separator is <c>/</c> and the path is built from node <see cref="ITreeNode.Text"/>, not from
/// the widget's own full-path notion. Both are load-bearing: these strings are persisted in user
/// project settings, so changing either format silently discards every user's saved expansion state.
/// </remarks>
public static class TreeNodeExpansionPaths
{
    private const char Separator = '/';

    /// <summary>
    /// The paths of every expanded node reachable from <paramref name="roots"/>.
    /// </summary>
    /// <remarks>
    /// Only descends into expanded nodes. A collapsed node's children cannot be visible, so their
    /// expansion state is not part of what the user sees and is deliberately not captured.
    /// </remarks>
    public static List<string> Capture(IEnumerable<ITreeNode> roots)
    {
        List<string> expandedPaths = new();

        foreach (ITreeNode root in roots)
        {
            CollectExpandedPaths(root, expandedPaths);
        }

        return expandedPaths;
    }

    /// <summary>
    /// Expands the node at each of <paramref name="paths"/> that still exists. Paths that no longer
    /// resolve are skipped, since a snapshot can outlive the elements it referred to.
    /// </summary>
    public static void Apply(IEnumerable<ITreeNode> roots, IEnumerable<string> paths)
    {
        // Materialized because a path is resolved by walking from the roots, once per path.
        IReadOnlyList<ITreeNode> rootList = roots as IReadOnlyList<ITreeNode> ?? new List<ITreeNode>(roots);

        foreach (string path in paths)
        {
            FindByPath(rootList, path)?.Expand();
        }
    }

    /// <summary>
    /// The node addressed by <paramref name="path"/>, or null if no such node exists.
    /// </summary>
    public static ITreeNode? FindByPath(IReadOnlyList<ITreeNode> roots, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        IEnumerable<ITreeNode> currentLevel = roots;
        ITreeNode? found = null;

        foreach (string part in path.Split(Separator))
        {
            found = null;

            foreach (ITreeNode candidate in currentLevel)
            {
                if (candidate.Text == part)
                {
                    found = candidate;
                    currentLevel = candidate.Children;
                    break;
                }
            }

            if (found == null)
            {
                return null;
            }
        }

        return found;
    }

    /// <summary>
    /// The <c>/</c>-joined text path from the root down to <paramref name="node"/>.
    /// </summary>
    public static string GetPath(ITreeNode node)
    {
        List<string> parts = new();

        for (ITreeNode? current = node; current != null; current = current.Parent)
        {
            parts.Insert(0, current.Text);
        }

        return string.Join(Separator.ToString(), parts);
    }

    private static void CollectExpandedPaths(ITreeNode node, List<string> expandedPaths)
    {
        if (!node.IsExpanded)
        {
            return;
        }

        string path = GetPath(node);
        if (!string.IsNullOrEmpty(path))
        {
            expandedPaths.Add(path);
        }

        foreach (ITreeNode child in node.Children)
        {
            CollectExpandedPaths(child, expandedPaths);
        }
    }
}
