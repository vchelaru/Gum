namespace Gum.Managers;

/// <summary>
/// Headless folder-classification predicates, written against
/// <see cref="ITreeNode.Parent"/>/<see cref="ITreeNode.Tag"/>/<see cref="ITreeNode.Text"/> so they
/// work for any <see cref="ITreeNode"/> implementation, not just the tool's concrete node type.
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

    /// <summary>
    /// Determines whether the tree node is part of the Screens folder structure (either the root
    /// Screens folder, a subfolder, or a Screen element within the hierarchy). Unlike
    /// <see cref="IsScreensFolderTreeNode"/>, this returns true for Screen elements themselves, not
    /// just folders.
    /// </summary>
    public static bool IsPartOfScreensFolderStructure(this ITreeNode? treeNode) =>
        treeNode != null &&
        (treeNode.IsTopScreenContainerTreeNode() || treeNode.Parent.IsPartOfScreensFolderStructure());

    /// <summary>
    /// Determines whether the tree node is part of the Components folder structure (either the root
    /// Components folder, a subfolder, or a Component element within the hierarchy). Unlike
    /// <see cref="IsComponentsFolderTreeNode"/>, this returns true for Component elements themselves,
    /// not just folders.
    /// </summary>
    public static bool IsPartOfComponentsFolderStructure(this ITreeNode? treeNode) =>
        treeNode != null &&
        (treeNode.IsTopComponentContainerTreeNode() || treeNode.Parent.IsPartOfComponentsFolderStructure());

    public static bool IsTopStandardElementTreeNode(this ITreeNode treeNode) =>
        treeNode.Parent == null && treeNode.Text == "Standard";

    public static bool IsTopBehaviorTreeNode(this ITreeNode treeNode) =>
        treeNode.Parent == null && treeNode.Text == "Behaviors";

    /// <summary>
    /// Determines whether the tree node is one of the top-level element container folders
    /// (Screens, Components, Standard, or Behaviors) — i.e. has no tag and no parent.
    /// </summary>
    public static bool IsTopElementContainerTreeNode(this ITreeNode treeNode) =>
        treeNode.Tag == null && treeNode.Parent == null;

    /// <summary>
    /// Determines whether the tree node is part of the Standard elements folder structure (the root
    /// Standard folder or a Standard element within it). Mirrors <see cref="IsPartOfScreensFolderStructure"/>.
    /// </summary>
    public static bool IsPartOfStandardElementsFolderStructure(this ITreeNode? treeNode) =>
        treeNode != null &&
        (treeNode.IsTopStandardElementTreeNode() || treeNode.Parent.IsPartOfStandardElementsFolderStructure());
}
