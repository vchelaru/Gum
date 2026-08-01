using System.Windows.Input;

namespace Gum.Controls;

/// <summary>
/// Decides whether releasing a mouse button over a tree node should (re)select it. The decision
/// depends only on its arguments, not on any live control state, so it can be unit-tested without
/// mouse-event plumbing.
/// </summary>
public class TreeNodeMouseUpSelectionLogic
{
    /// <summary>
    /// Only the left button selects - a mouse "back"/"forward" (or middle/right) release over a
    /// node must not be treated as a click on that node. With a modifier key held, or with
    /// <see cref="MultiSelectBehavior.RegularClick"/>, mouse-down already handled selection, so
    /// mouse-up must not re-select. Otherwise it selects when either the node is already part of a
    /// multi-selection (a potential drag was deferred to mouse-up) or selection normally happens on
    /// click rather than push (<paramref name="isSelectingOnPush"/> is false).
    /// </summary>
    public bool ShouldSelect(
        ModifierKeys effectiveModifiers,
        MultiSelectBehavior multiSelectBehavior,
        bool isNodeInMultiSelection,
        bool isSelectingOnPush,
        MouseButton button)
    {
        return button == MouseButton.Left &&
               effectiveModifiers == ModifierKeys.None &&
               multiSelectBehavior != MultiSelectBehavior.RegularClick &&
               (isNodeInMultiSelection || !isSelectingOnPush);
    }
}
