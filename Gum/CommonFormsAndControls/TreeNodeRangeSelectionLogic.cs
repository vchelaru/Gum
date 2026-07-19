using System;
using System.Windows.Forms;

namespace CommonFormsAndControls;

/// <summary>
/// Computes which tree nodes fall in a Shift+Click selection range between an anchor node and a
/// newly-clicked node, walking visible order the same way arrow-key navigation would. Extracted
/// from <see cref="MultiSelectTreeView"/> so the (pure, decision-only) range logic can be
/// unit-tested directly instead of only through mouse-event plumbing.
/// </summary>
public class TreeNodeRangeSelectionLogic
{
    /// <summary>
    /// Invokes <paramref name="selectNode"/> for every node between <paramref name="start"/>
    /// (exclusive) and <paramref name="end"/> (inclusive), walking in visible order. Nodes sharing
    /// a parent are compared directly; otherwise both are walked up to their nearest common
    /// ancestor to determine direction before walking the visible chain between the original nodes.
    /// </summary>
    public void SelectRange(TreeNode start, TreeNode end, Action<TreeNode> selectNode)
    {
        if (start.Parent == end.Parent)
        {
            WalkAndSelect(start, end, selectNode, forward: start.Index < end.Index);
            return;
        }

        TreeNode startAncestor = start;
        TreeNode endAncestor = end;
        int commonDepth = Math.Min(startAncestor.Level, endAncestor.Level);

        while (startAncestor.Level > commonDepth)
        {
            startAncestor = startAncestor.Parent;
        }
        while (endAncestor.Level > commonDepth)
        {
            endAncestor = endAncestor.Parent;
        }

        while (startAncestor.Parent != endAncestor.Parent)
        {
            startAncestor = startAncestor.Parent;
            endAncestor = endAncestor.Parent;
        }

        bool forward = startAncestor.Index == endAncestor.Index
            ? start.Level < end.Level
            : startAncestor.Index < endAncestor.Index;

        WalkAndSelect(start, end, selectNode, forward);
    }

    private static void WalkAndSelect(TreeNode start, TreeNode end, Action<TreeNode> selectNode, bool forward)
    {
        TreeNode? current = start;
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
