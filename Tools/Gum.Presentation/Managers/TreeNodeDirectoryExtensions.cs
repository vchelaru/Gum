using System;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Headless mirror of ElementTreeViewManager's directory-path lookup: resolving an absolute
/// on-disk directory to the tree node representing it. Unlike the tag-based searches in
/// <see cref="TreeNodeRootSearchExtensions"/>, the root is picked by matching a relative path
/// prefix ("screens/", "components/", "standards/", "behaviors/") rather than by tag runtime
/// type, and the remaining path is walked by <see cref="ITreeNode.Text"/> segment rather than by
/// <see cref="ITreeNode.Tag"/>. The WinForms <c>TreeNode</c> overloads stay in
/// ElementTreeViewManager.cs for its own WinForms-native call sites.
/// </summary>
public static class TreeNodeDirectoryExtensions
{
    /// <summary>
    /// Resolves <paramref name="absoluteDirectory"/> (made relative to
    /// <paramref name="projectDirectory"/>) to its tree node, selecting the root from
    /// <paramref name="roots"/> based on which top-level category the path falls under. Returns
    /// null if the path isn't under any of the four category roots, a category root hasn't been
    /// created yet, or no node matches the remaining path.
    /// </summary>
    public static ITreeNode? GetTreeNodeFor(this IElementTreeRoots roots, string absoluteDirectory, string projectDirectory)
    {
        string relative = FileManager.MakeRelative(absoluteDirectory, projectDirectory);

        relative = FileManager.Standardize(relative);
        // in the tool we use forward slashes:
        relative = relative.Replace("\\", "/");

        if (relative.StartsWith("screens/"))
        {
            return roots.Screens?.GetTreeNodeFor(relative.Substring("screens/".Length));
        }
        else if (relative.StartsWith("components/"))
        {
            return roots.Components?.GetTreeNodeFor(relative.Substring("components/".Length));
        }
        else if (relative.StartsWith("standards/"))
        {
            return roots.StandardElements?.GetTreeNodeFor(relative.Substring("standards/".Length));
        }
        else if (relative.StartsWith("behaviors/"))
        {
            return roots.Behaviors?.GetTreeNodeFor(relative.Substring("behaviors/".Length));
        }

        return null;
    }

    /// <summary>
    /// Walks <paramref name="container"/>'s descendants one <paramref name="relativeDirectory"/>
    /// segment at a time, matching each segment against a child's <see cref="ITreeNode.Text"/>
    /// (case-insensitive). Returns <paramref name="container"/> itself for an empty path, or null
    /// if any segment has no matching child.
    /// </summary>
    public static ITreeNode? GetTreeNodeFor(this ITreeNode container, string relativeDirectory)
    {
        if (string.IsNullOrEmpty(relativeDirectory))
        {
            return container;
        }

        int indexOfSlash = relativeDirectory.IndexOf('/');
        string whatToLookFor = relativeDirectory;
        string sub = "";

        if (indexOfSlash != -1)
        {
            whatToLookFor = relativeDirectory.Substring(0, indexOfSlash);
            sub = relativeDirectory.Substring(indexOfSlash + 1);
        }

        foreach (ITreeNode node in container.Children)
        {
            if (node.Text.Equals(whatToLookFor, StringComparison.OrdinalIgnoreCase))
            {
                return node.GetTreeNodeFor(sub);
            }
        }

        return null;
    }
}
