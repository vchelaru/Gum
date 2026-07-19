using Gum.DataTypes;
using Gum.DataTypes.Behaviors;

namespace Gum.Managers;

/// <summary>
/// Headless root-selecting search helpers mirroring the WinForms <c>TreeNode</c>-typed overloads
/// in ElementTreeViewManager.cs that pick one of its four category root fields (Screens,
/// Components, Standard Elements, Behaviors) before searching. Each selects the matching root
/// from <see cref="IElementTreeRoots"/> and delegates to
/// <see cref="TreeNodeSearchExtensions.GetTreeNodeForTag(ITreeNode, object)"/>. The WinForms
/// overloads stay in ElementTreeViewManager.cs for its WinForms-native call sites.
/// </summary>
public static class TreeNodeRootSearchExtensions
{
    /// <summary>
    /// Selects the root matching <paramref name="elementSave"/>'s runtime type (Screen, Component,
    /// or Standard Element) and searches it for the matching node. Returns null for a null
    /// <paramref name="elementSave"/> or an unrecognized element type.
    /// </summary>
    public static ITreeNode? GetTreeNodeFor(this IElementTreeRoots roots, ElementSave? elementSave) =>
        elementSave switch
        {
            null => null,
            ScreenSave screenSave => roots.GetTreeNodeFor(screenSave),
            ComponentSave componentSave => roots.GetTreeNodeFor(componentSave),
            StandardElementSave standardElementSave => roots.GetTreeNodeFor(standardElementSave),
            _ => null
        };

    public static ITreeNode? GetTreeNodeFor(this IElementTreeRoots roots, ScreenSave screenSave) =>
        roots.Screens?.GetTreeNodeForTag(screenSave);

    public static ITreeNode? GetTreeNodeFor(this IElementTreeRoots roots, ComponentSave componentSave) =>
        roots.Components?.GetTreeNodeForTag(componentSave);

    public static ITreeNode? GetTreeNodeFor(this IElementTreeRoots roots, StandardElementSave standardElementSave) =>
        roots.StandardElements?.GetTreeNodeForTag(standardElementSave);

    public static ITreeNode? GetTreeNodeFor(this IElementTreeRoots roots, BehaviorSave behavior) =>
        roots.Behaviors?.GetTreeNodeForTag(behavior);

    /// <summary>
    /// Searches <paramref name="container"/> for the node tagged with <paramref name="tag"/>. When
    /// <paramref name="container"/> is omitted, the root is inferred from <paramref name="tag"/>'s
    /// runtime type (Screen/Component/Standard Element/Behavior); an unrecognized type or a root
    /// that hasn't been created yet returns null.
    /// </summary>
    public static ITreeNode? GetTreeNodeForTag(this IElementTreeRoots roots, object tag, ITreeNode? container = null)
    {
        container ??= tag switch
        {
            ScreenSave => roots.Screens,
            ComponentSave => roots.Components,
            StandardElementSave => roots.StandardElements,
            BehaviorSave => roots.Behaviors,
            _ => null
        };

        return container?.GetTreeNodeForTag(tag);
    }
}
