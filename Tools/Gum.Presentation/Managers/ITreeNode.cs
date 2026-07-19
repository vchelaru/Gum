using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.Managers;

public interface ITreeNode
{
    object? Tag { get; }
    FilePath GetFullFilePath();
    ITreeNode? Parent { get; }

    /// <summary>
    /// The node's display label. Settable so callers (e.g. a folder rename) can update the label
    /// in place without depending on the concrete, WinForms-coupled tree node implementation.
    /// </summary>
    string Text { get; set; }

    string FullPath { get; }

    /// <summary>
    /// The node's immediate child nodes. Lets callers walk the tree headlessly (e.g. via
    /// <see cref="TreeNodeNavigationExtensions.GetAllChildrenNodesRecursively"/>) instead of
    /// depending on the WinForms <c>TreeNode.Nodes</c> collection.
    /// </summary>
    IEnumerable<ITreeNode> Children { get; }

    void Expand();
}
