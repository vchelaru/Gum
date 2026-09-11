using System;
using Gum.Managers;

namespace Gum.Controls;

/// <summary>
/// Computes which tree nodes fall in a Shift+Click selection range between an anchor node and a
/// newly-clicked node, walking visible order the same way arrow-key navigation would. The walk
/// depends only on the nodes passed in, so it can be unit-tested without mouse-event plumbing.
/// </summary>
public class TreeNodeRangeSelectionLogic
{
    /// <summary>
    /// Invokes <paramref name="selectNode"/> for every node between <paramref name="start"/>
    /// (exclusive) and <paramref name="end"/> (inclusive), walking in visible order. Nodes sharing
    /// a parent are compared directly; otherwise both are walked up to their nearest common
    /// ancestor to determine direction before walking the visible chain between the original nodes.
    /// </summary>
    public void SelectRange(GumTreeNode start, GumTreeNode end, Action<GumTreeNode> selectNode)
    {
        if (start.Parent == end.Parent)
        {
            WalkAndSelect(start, end, selectNode, forward: start.Index < end.Index);
            return;
        }

        GumTreeNode startAncestor = start;
        GumTreeNode endAncestor = end;
        int commonDepth = Math.Min(startAncestor.Level, endAncestor.Level);

        // A node deeper than commonDepth (>= 0) always has a parent, so the walks below cannot
        // dereference null.
        while (startAncestor.Level > commonDepth)
        {
            startAncestor = startAncestor.Parent!;
        }
        while (endAncestor.Level > commonDepth)
        {
            endAncestor = endAncestor.Parent!;
        }

        while (startAncestor.Parent != endAncestor.Parent)
        {
            startAncestor = startAncestor.Parent!;
            endAncestor = endAncestor.Parent!;
        }

        bool forward = startAncestor.Index == endAncestor.Index
            ? start.Level < end.Level
            : startAncestor.Index < endAncestor.Index;

        WalkAndSelect(start, end, selectNode, forward);
    }

    private static void WalkAndSelect(
        GumTreeNode start, GumTreeNode end, Action<GumTreeNode> selectNode, bool forward)
    {
        GumTreeNode? current = start;
        while (current != end)
        {
            current = forward ? current.NextVisibleNode : current.PrevVisibleNode;
            if (current == null)
            {
                break;
            }
            selectNode(current);
        }
    }
}
