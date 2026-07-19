using Gum.DataTypes;

namespace Gum.Managers;

/// <summary>
/// Headless recursive node-search helpers over <see cref="ITreeNode"/>, mirroring the WinForms
/// <c>TreeNode</c>-typed search methods in ElementTreeViewManager.cs. Each walks a container's
/// descendants depth-first (the container itself is never matched) using only
/// <see cref="ITreeNode.Children"/> and <see cref="ITreeNode.Tag"/>, so they work for any
/// <see cref="ITreeNode"/> implementation. The WinForms overloads stay in ElementTreeViewManager.cs
/// for its WinForms-native call sites; the root-selecting element/behavior/directory lookups there
/// still depend on ETVM's WinForms root fields (and, for the directory lookups, project state) and
/// are not mirrored here.
/// </summary>
public static class TreeNodeSearchExtensions
{
    /// <summary>
    /// Searches the container's descendants depth-first for the node whose
    /// <see cref="ITreeNode.Tag"/> is the same reference as <paramref name="tag"/>.
    /// </summary>
    public static ITreeNode? GetTreeNodeForTag(this ITreeNode container, object tag)
    {
        foreach (ITreeNode node in container.Children)
        {
            if (node.Tag == tag)
            {
                return node;
            }

            ITreeNode? childNode = node.GetTreeNodeForTag(tag);
            if (childNode != null)
            {
                return childNode;
            }
        }

        return null;
    }

    /// <summary>
    /// Searches the container's descendants depth-first for the node representing
    /// <paramref name="instanceSave"/>, matched by reference via its <see cref="ITreeNode.Tag"/>.
    /// </summary>
    public static ITreeNode? GetTreeNodeFor(this ITreeNode container, InstanceSave instanceSave) =>
        container.GetTreeNodeForTag(instanceSave);

    /// <summary>
    /// Searches the container's descendants depth-first for the instance node whose
    /// <see cref="InstanceSave.Name"/> equals <paramref name="name"/>.
    /// </summary>
    public static ITreeNode? GetInstanceTreeNodeByName(this ITreeNode container, string name)
    {
        foreach (ITreeNode node in container.Children)
        {
            if (node.Tag is InstanceSave instanceSave && instanceSave.Name == name)
            {
                return node;
            }

            ITreeNode? childNode = node.GetInstanceTreeNodeByName(name);
            if (childNode != null)
            {
                return childNode;
            }
        }

        return null;
    }
}
