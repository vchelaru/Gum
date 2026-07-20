using System;

namespace Gum.Managers;

/// <summary>
/// Headless reorder/sort helpers over <see cref="ITreeNodeMutable"/>, relocated from
/// ElementTreeViewManager.cs (part of #3845) once its reparent/reorder logic (#3841) had already
/// converted these off <c>TreeNodeCollection</c>. The algorithm bodies are unchanged from that PR -
/// this is a pure file-location move, not a behavior change. Pinned by
/// <c>TreeNodeMutationExtensionsTests</c> in <c>GumToolUnitTests</c>, which stays there (it builds
/// its test trees from the WinForms-typed <c>GumTreeNode</c>).
/// </summary>
public static class TreeNodeMutationExtensions
{
    /// <summary>
    /// Moves <paramref name="node"/> to <paramref name="desiredIndex"/> among its current parent's
    /// children. No-op if it is already there, or if it currently has no parent.
    /// </summary>
    /// <remarks>
    /// Shared by ElementTreeViewManager's element/instance and behavior-instance reorder paths
    /// (<c>RefreshElementTreeNode</c>/<c>RefreshBehaviorTreeNode</c>), which both reorder a node
    /// within its already-correct parent after any reparenting has already happened.
    /// </remarks>
    public static void MoveToIndex(this ITreeNodeMutable node, int desiredIndex)
    {
        ITreeNodeMutable? parent = node.Parent;
        if (parent == null || parent.IndexOfChild(node) == desiredIndex)
        {
            return;
        }

        parent.RemoveChild(node);
        parent.InsertChild(desiredIndex, node);
    }

    /// <summary>
    /// Sorts a node's direct children alphabetically by name, with folders appearing before files.
    /// </summary>
    /// <param name="parentNode">The node whose children should be sorted.</param>
    /// <param name="recursive">
    /// If true, recursively sorts all child node collections (except within Screen, Component, Standard, or Behavior element nodes).
    /// Default is false.
    /// </param>
    /// <remarks>
    /// The sort order places folders (Components and Screens subfolders) before individual elements,
    /// and within each category, nodes are sorted alphabetically by their Text property.
    /// When recursive is true, the method will not sort children of element nodes (Screen, Component, Standard, Behavior)
    /// as these typically contain instances and states that should maintain their specific order.
    /// </remarks>
    public static void SortByName(this ITreeNodeMutable parentNode, bool recursive = false)
    {
        int lastObjectExclusive = parentNode.ChildCount;
        int whereObjectBelongs;
        for (int i = 0 + 1; i < lastObjectExclusive; i++)
        {
            ITreeNodeMutable first = parentNode.GetChildAt(i);
            ITreeNodeMutable second = parentNode.GetChildAt(i - 1);
            if (FirstComesBeforeSecond(first, second))
            {
                if (i == 1)
                {
                    ITreeNodeMutable movingNode = parentNode.GetChildAt(i);
                    parentNode.RemoveChildAt(i);

                    parentNode.InsertChild(0, movingNode);
                    continue;
                }

                for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                {
                    second = parentNode.GetChildAt(whereObjectBelongs);
                    if (!FirstComesBeforeSecond(parentNode.GetChildAt(i), second))
                    {
                        ITreeNodeMutable movingNode = parentNode.GetChildAt(i);

                        parentNode.RemoveChildAt(i);
                        parentNode.InsertChild(whereObjectBelongs + 1, movingNode);
                        break;
                    }
                    else if (whereObjectBelongs == 0 && FirstComesBeforeSecond(parentNode.GetChildAt(i), parentNode.GetChildAt(0)))
                    {
                        ITreeNodeMutable movingNode = parentNode.GetChildAt(i);
                        parentNode.RemoveChildAt(i);
                        parentNode.InsertChild(0, movingNode);
                        break;
                    }
                }
            }
        }

        if(recursive)
        {
            for (int i = 0; i < parentNode.ChildCount; i++)
            {
                ITreeNodeMutable childNode = parentNode.GetChildAt(i);

                var sortInner = childNode.IsScreenTreeNode() == false &&
                    childNode.IsComponentTreeNode() == false &&
                    childNode.IsStandardElementTreeNode() == false &&
                    childNode.IsBehaviorTreeNode() == false;

                if(sortInner)
                {
                    childNode.SortByName(recursive);
                }
            }
        }
    }

    /// <summary>
    /// Recursively removes children tagged with <typeparamref name="T"/> that fail
    /// <paramref name="shouldKeep"/>. A child not tagged <typeparamref name="T"/> (e.g. a folder
    /// node) is never removed itself, but is recursed into so stale descendants are still pruned.
    /// </summary>
    /// <remarks>
    /// Relocated from ElementTreeViewManager's <c>AddAndRemoveScreensComponentsStandardsAndBehaviors</c>,
    /// where it existed as two near-identical local functions (<c>RemoveScreenRecursively</c>,
    /// <c>RemoveComponentRecursively</c>) differing only by save type - this generic version
    /// replaces both call sites.
    /// </remarks>
    public static void RemoveRecursivelyIfStale<T>(this ITreeNodeMutable containerNode, Func<T, bool> shouldKeep)
        where T : class
    {
        for (int i = containerNode.ChildCount - 1; i > -1; i--)
        {
            ITreeNodeMutable child = containerNode.GetChildAt(i);

            if (child.Tag is T tag)
            {
                if (!shouldKeep(tag))
                {
                    containerNode.RemoveChildAt(i);
                }
            }
            else
            {
                child.RemoveRecursivelyIfStale(shouldKeep);
            }
        }
    }

    private static bool FirstComesBeforeSecond(ITreeNodeMutable first, ITreeNodeMutable second)
    {
        bool isFirstDirectory = first.IsComponentsFolderTreeNode() || first.IsScreensFolderTreeNode();
        bool isSecondDirectory = second.IsComponentsFolderTreeNode() || second.IsScreensFolderTreeNode();

        if (isFirstDirectory && !isSecondDirectory)
        {
            return true;
        }
        else if (!isFirstDirectory && isSecondDirectory)
        {
            return false;
        }
        else
        {
            return first.Text.CompareTo(second.Text) < 0;
        }
    }
}
