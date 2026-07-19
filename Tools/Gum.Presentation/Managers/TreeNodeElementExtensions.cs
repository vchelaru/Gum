using Gum.DataTypes;
using Gum.DataTypes.Behaviors;

namespace Gum.Managers;

/// <summary>
/// Headless element-type classification predicates over <see cref="ITreeNode"/>, keyed off
/// <see cref="ITreeNode.Tag"/>. Companion to <see cref="TreeNodeFolderExtensions"/> (which classifies
/// folders/containers). The WinForms <c>TreeNode</c> overloads stay in ElementTreeViewManager.cs for
/// its own WinForms-native call sites until those convert to <see cref="ITreeNode"/>.
/// </summary>
public static class TreeNodeElementExtensions
{
    public static bool IsScreenTreeNode(this ITreeNode treeNode) => treeNode.Tag is ScreenSave;

    public static bool IsComponentTreeNode(this ITreeNode treeNode) => treeNode.Tag is ComponentSave;

    public static bool IsBehaviorTreeNode(this ITreeNode treeNode) => treeNode.Tag is BehaviorSave;

    public static bool IsStandardElementTreeNode(this ITreeNode treeNode) => treeNode.Tag is StandardElementSave;
}
