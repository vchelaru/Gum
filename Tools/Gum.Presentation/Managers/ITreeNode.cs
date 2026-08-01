using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.Managers;

public interface ITreeNode
{
    object? Tag { get; }
    /// <summary>
    /// The file or folder this node stands for, or null when the project has no location on disk
    /// yet so no path can be derived.
    /// </summary>
    FilePath? GetFullFilePath();
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

    /// <summary>
    /// Whether this node's children are currently shown. Read-only because the underlying widgets
    /// expose expansion as a pair of commands rather than a settable flag; use
    /// <see cref="Expand"/>/<see cref="Collapse"/> to change it.
    /// </summary>
    bool IsExpanded { get; }

    void Expand();

    void Collapse();
}
