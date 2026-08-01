using Gum.Managers;

namespace Gum.Controls;

/// <summary>
/// Computes the target node for the Home/End/PageUp/PageDown keys. The walk depends only on the
/// nodes passed in, so it can be unit-tested without key-event plumbing. Callers are responsible
/// for actually applying the selection (single-select vs. range-select) to the returned node.
/// </summary>
public class TreeNodeKeyNavigationLogic
{
    /// <summary>
    /// Finds the target node for the Home key. With <paramref name="shiftDown"/>, the range
    /// should extend to the first root node (if <paramref name="selectedNode"/> is itself a
    /// root) or to the first sibling under its parent; <paramref name="selectRange"/> is set to
    /// true in that case. Otherwise the target is simply the first root node.
    /// </summary>
    public GumTreeNode? GetHomeTarget(
        GumTreeNode selectedNode, GumTreeNodeCollection rootNodes, bool shiftDown, out bool selectRange)
    {
        selectRange = shiftDown;

        if (shiftDown && selectedNode.Parent != null)
        {
            return selectedNode.Parent.FirstNode;
        }

        return rootNodes.Count > 0 ? rootNodes[0] : null;
    }

    /// <summary>
    /// Finds the target node for the End key. With <paramref name="shiftDown"/>, the range
    /// should extend to the last root node (if <paramref name="selectedNode"/> is itself a root)
    /// or to the last sibling under its parent; <paramref name="selectRange"/> is set to true in
    /// that case. Otherwise the target is the last visible node in the tree, walked down from the
    /// first root through expanded last-children (without expanding any branch, in case the tree
    /// is virtual).
    /// </summary>
    public GumTreeNode? GetEndTarget(
        GumTreeNode selectedNode, GumTreeNodeCollection rootNodes, bool shiftDown, out bool selectRange)
    {
        selectRange = shiftDown;

        if (shiftDown)
        {
            if (selectedNode.Parent != null)
            {
                return selectedNode.Parent.LastNode;
            }

            return rootNodes.Count > 0 ? rootNodes[rootNodes.Count - 1] : null;
        }

        if (rootNodes.Count == 0)
        {
            return null;
        }

        // The first root can legitimately be an empty folder, in which case there is no last node
        // to walk down from.
        GumTreeNode? lastNode = rootNodes[0].LastNode;
        while (lastNode is { IsExpanded: true, LastNode: not null })
        {
            lastNode = lastNode.LastNode;
        }

        return lastNode;
    }

    /// <summary>
    /// Walks backward from <paramref name="selectedNode"/> through up to
    /// <paramref name="visibleCount"/> visible nodes (stopping early if the top of the tree is
    /// reached) and returns the resulting node - the target for the PageUp key.
    /// </summary>
    public GumTreeNode GetPageUpTarget(GumTreeNode selectedNode, int visibleCount)
    {
        GumTreeNode current = selectedNode;
        int remaining = visibleCount;
        while (remaining > 0 && current.PrevVisibleNode != null)
        {
            current = current.PrevVisibleNode;
            remaining--;
        }

        return current;
    }

    /// <summary>
    /// Walks forward from <paramref name="selectedNode"/> through up to
    /// <paramref name="visibleCount"/> visible nodes (stopping early if the bottom of the tree is
    /// reached) and returns the resulting node - the target for the PageDown key.
    /// </summary>
    public GumTreeNode GetPageDownTarget(GumTreeNode selectedNode, int visibleCount)
    {
        GumTreeNode current = selectedNode;
        int remaining = visibleCount;
        while (remaining > 0 && current.NextVisibleNode != null)
        {
            current = current.NextVisibleNode;
            remaining--;
        }

        return current;
    }
}
