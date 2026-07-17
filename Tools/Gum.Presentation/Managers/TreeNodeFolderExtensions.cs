namespace Gum.Managers;

/// <summary>
/// Headless folder-classification predicates over <see cref="ITreeNode"/>, relocated from
/// ElementTreeViewManager's WinForms-coupled <c>TreeNode</c> extension methods (ADR-0005 Phase 3)
/// so <c>DeleteLogic</c> does not need a WinForms reference. Implemented directly against
/// <see cref="ITreeNode.Parent"/>/<see cref="ITreeNode.Tag"/>/<see cref="ITreeNode.Text"/> rather
/// than unwrapping to the WinForms <c>TreeNode</c> the old overloads relied on
/// (<c>TreeNodeWrapper.Parent</c> already wraps <c>Node.Parent</c> recursively, so the results are
/// identical for the tool's live <c>TreeNodeWrapper</c> nodes, and this now also works for any
/// other <see cref="ITreeNode"/> implementation instead of always returning false).
/// The <c>TreeNode</c>-typed overloads stay in ElementTreeViewManager.cs for WinForms call sites.
/// </summary>
public static class TreeNodeFolderExtensions
{
    public static bool IsTopScreenContainerTreeNode(this ITreeNode treeNode) =>
        treeNode.Parent == null && treeNode.Text == "Screens";

    public static bool IsScreensFolderTreeNode(this ITreeNode? treeNode) =>
        treeNode != null &&
        treeNode.Tag == null &&
        treeNode.Parent != null &&
        (treeNode.Parent.IsScreensFolderTreeNode() || treeNode.Parent.IsTopScreenContainerTreeNode());

    public static bool IsTopComponentContainerTreeNode(this ITreeNode treeNode) =>
        treeNode.Parent == null && treeNode.Text == "Components";

    public static bool IsComponentsFolderTreeNode(this ITreeNode? treeNode) =>
        treeNode != null &&
        treeNode.Tag == null &&
        treeNode.Parent != null &&
        (treeNode.Parent.IsComponentsFolderTreeNode() || treeNode.Parent.IsTopComponentContainerTreeNode());
}
