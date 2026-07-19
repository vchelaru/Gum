using System.Collections.Generic;

namespace Gum.Managers;

/// <summary>
/// Headless tree-traversal helpers over <see cref="ITreeNode"/>. Companion to
/// <see cref="TreeNodeFolderExtensions"/>/<see cref="TreeNodeElementExtensions"/> (which classify
/// nodes). The WinForms <c>TreeNode</c>-typed <c>GetAllChildrenNodesRecursively</c> overload stays
/// in ElementTreeViewManager.cs for its own WinForms-native call sites; this mirror works for any
/// <see cref="ITreeNode"/> implementation.
/// </summary>
public static class TreeNodeNavigationExtensions
{
    /// <summary>
    /// Gets all descendant nodes in a flattened, depth-first list. The node itself is not included.
    /// </summary>
    public static List<ITreeNode> GetAllChildrenNodesRecursively(this ITreeNode treeNode)
    {
        List<ITreeNode> toReturn = new List<ITreeNode>();

        void Fill(ITreeNode parent)
        {
            foreach (ITreeNode child in parent.Children)
            {
                toReturn.Add(child);
                Fill(child);
            }
        }

        Fill(treeNode);

        return toReturn;
    }
}
