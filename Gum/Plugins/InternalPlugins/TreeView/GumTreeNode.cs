using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// WinForms <see cref="TreeNode"/> subclass that also implements <see cref="ITreeNodeMutable"/>.
/// Implementing the interface directly on the concrete node - rather than wrapping a plain
/// <see cref="TreeNode"/> per call, the way <see cref="TreeNodeWrapper"/> does for low-frequency,
/// read-only callers - means every node ElementTreeViewManager constructs already satisfies
/// <see cref="ITreeNodeMutable"/> for free: nothing allocates to use it, so it is safe to reach for
/// on ElementTreeViewManager's per-instance construction/refresh hot path.
/// </summary>
/// <remarks>
/// <see cref="System.Windows.Forms.TreeView"/> (and <c>MultiSelectTreeView</c>, which derives from
/// it) does not care what concrete <see cref="TreeNode"/> subtype is added to its <c>Nodes</c>
/// collection, so this type is a drop-in replacement for <c>new TreeNode()</c> wherever
/// ElementTreeViewManager constructs a node - no other WinForms tree-view code needs to change to
/// host it.
/// </remarks>
public class GumTreeNode : TreeNode, ITreeNodeMutable
{
    /// <summary>
    /// Creates an untitled node, mirroring <see cref="TreeNode()"/>.
    /// </summary>
    public GumTreeNode()
    {
    }

    /// <summary>
    /// Creates a node with the given display text, mirroring <see cref="TreeNode(string)"/>.
    /// </summary>
    public GumTreeNode(string text) : base(text)
    {
    }

    object? ITreeNode.Tag => Tag;

    ITreeNode? ITreeNode.Parent => Parent switch
    {
        null => null,
        ITreeNode alreadyImplementsInterface => alreadyImplementsInterface,
        TreeNode plainNode => new TreeNodeWrapper(plainNode)
    };

    /// <inheritdoc/>
    public IEnumerable<ITreeNode> Children =>
        Nodes.Cast<TreeNode>().Select(child => child is ITreeNode alreadyImplementsInterface
            ? alreadyImplementsInterface
            : new TreeNodeWrapper(child));

    FilePath? ITreeNode.GetFullFilePath() => this.GetFullFilePath();

    /// <inheritdoc/>
    public void SetTag(object? tag) => Tag = tag;

    /// <inheritdoc/>
    public ITreeNodeMutable AddChild(string text)
    {
        GumTreeNode child = new GumTreeNode(text);
        Nodes.Add(child);
        return child;
    }

    /// <inheritdoc/>
    public void AddChild(ITreeNodeMutable child) => Nodes.Add(RequireWinFormsNode(child));

    /// <inheritdoc/>
    public void InsertChild(int index, ITreeNodeMutable child) => Nodes.Insert(index, RequireWinFormsNode(child));

    /// <inheritdoc/>
    public void RemoveChild(ITreeNodeMutable child) => Nodes.Remove(RequireWinFormsNode(child));

    /// <inheritdoc/>
    public void RemoveChildAt(int index) => Nodes.RemoveAt(index);

    /// <inheritdoc/>
    public void ClearChildren() => Nodes.Clear();

    private static TreeNode RequireWinFormsNode(ITreeNodeMutable node)
    {
        if (node is TreeNode treeNode)
        {
            return treeNode;
        }

        throw new ArgumentException(
            $"{nameof(node)} must be a WinForms {nameof(TreeNode)} (e.g. {nameof(GumTreeNode)}) to be added under a WinForms tree.",
            nameof(node));
    }
}
