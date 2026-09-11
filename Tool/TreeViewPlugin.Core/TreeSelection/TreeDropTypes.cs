using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Managers;

namespace Gum.Controls;

/// <summary>
/// Where a drop lands relative to the row it is over.
/// </summary>
public enum TreeDropKind
{
    None,

    /// <summary>Insert above the target, as a sibling.</summary>
    Before,

    /// <summary>Drop onto the target, appending to its children.</summary>
    Into,

    /// <summary>Drop onto the target as its first child. Used when it is expanded.</summary>
    IntoFirst,

    /// <summary>Insert below the target, as a sibling.</summary>
    After,
}

/// <summary>
/// Asks whether a drop should be allowed, before it happens.
/// </summary>
public sealed class TreeDropValidationEventArgs : EventArgs
{
    /// <summary>Creates the question for a drag of <paramref name="draggedNodes"/> over <paramref name="targetNode"/>.</summary>
    public TreeDropValidationEventArgs(IReadOnlyList<GumTreeNode> draggedNodes, GumTreeNode? targetNode, TreeDropKind kind)
    {
        DraggedNodes = draggedNodes;
        TargetNode = targetNode;
        Kind = kind;
    }

    /// <summary>The nodes being dragged.</summary>
    public IReadOnlyList<GumTreeNode> DraggedNodes { get; }

    /// <summary>The row the drop would land on.</summary>
    public GumTreeNode? TargetNode { get; set; }

    /// <summary>Where on that row it would land.</summary>
    public TreeDropKind Kind { get; set; }

    /// <summary>Set true to permit the drop. Defaults to false.</summary>
    public bool Allow { get; set; }
}

/// <summary>
/// Reports a drop that has been accepted.
/// </summary>
public sealed class TreeDropEventArgs : EventArgs
{
    /// <summary>Creates the report for <paramref name="draggedNodes"/> dropped on <paramref name="targetNode"/>.</summary>
    public TreeDropEventArgs(IReadOnlyList<GumTreeNode> draggedNodes, GumTreeNode? targetNode, TreeDropKind kind)
    {
        DraggedNodes = draggedNodes;
        TargetNode = targetNode;
        Kind = kind;
    }

    /// <summary>The nodes that were dragged.</summary>
    public IReadOnlyList<GumTreeNode> DraggedNodes { get; }

    /// <summary>The row they were dropped on.</summary>
    public GumTreeNode? TargetNode { get; set; }

    /// <summary>Where on that row they landed.</summary>
    public TreeDropKind Kind { get; set; }
}

/// <summary>
/// Decides how a node drag lands on the row under the pointer. Shared by both heads' tree views,
/// which only measure where in the row the pointer is.
/// </summary>
public static class TreeDropLogic
{
    /// <summary>
    /// Fraction of a row's height at its top and bottom that means "insert beside" rather than
    /// "drop onto".
    /// </summary>
    public const double EdgeBandFraction = 0.25;

    /// <summary>
    /// The drop kind for a pointer <paramref name="fractionDownRow"/> of the way down
    /// <paramref name="target"/>'s own row (0 at its top edge, 1 at its bottom edge, children excluded).
    /// </summary>
    public static TreeDropKind GetKind(GumTreeNode target, double fractionDownRow)
    {
        if (fractionDownRow <= EdgeBandFraction)
        {
            return TreeDropKind.Before;
        }

        if (fractionDownRow >= 1 - EdgeBandFraction)
        {
            // Dropping just below an expanded row means "first child", not "next sibling" - the row
            // immediately underneath is its child.
            return target.IsExpanded && target.ChildCount > 0
                ? TreeDropKind.IntoFirst
                : TreeDropKind.After;
        }

        return TreeDropKind.Into;
    }

    /// <summary>
    /// Whether <paramref name="node"/> is one of <paramref name="candidates"/> or inside one of them.
    /// A drop there would move a node into its own subtree.
    /// </summary>
    public static bool IsNodeOrDescendantOfAny(GumTreeNode node, IReadOnlyList<GumTreeNode> candidates)
    {
        for (GumTreeNode? current = node; current != null; current = current.Parent)
        {
            if (candidates.Contains(current))
            {
                return true;
            }
        }

        return false;
    }
}
