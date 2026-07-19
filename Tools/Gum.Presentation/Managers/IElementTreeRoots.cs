namespace Gum.Managers;

/// <summary>
/// Exposes the four top-level category root nodes of the element tree (Screens, Components,
/// Standard Elements, Behaviors) headlessly, so root-selecting searches
/// (<see cref="TreeNodeRootSearchExtensions"/>) can run without the WinForms-native
/// <c>TreeNode</c> fields ElementTreeViewManager stores them in.
/// </summary>
public interface IElementTreeRoots
{
    ITreeNode? Screens { get; }
    ITreeNode? Components { get; }
    ITreeNode? StandardElements { get; }
    ITreeNode? Behaviors { get; }
}
