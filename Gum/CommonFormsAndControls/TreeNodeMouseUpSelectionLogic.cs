using System.Windows.Forms;

namespace CommonFormsAndControls;

/// <summary>
/// Decides whether releasing a mouse button over a tree node should (re)select it. Extracted from
/// <see cref="MultiSelectTreeView"/> so the decision - independent of any live control state - can
/// be unit-tested directly instead of only through <c>OnMouseUp</c> plumbing.
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
        Keys effectiveModifiers,
        MultiSelectBehavior multiSelectBehavior,
        bool isNodeInMultiSelection,
        bool isSelectingOnPush,
        MouseButtons button)
    {
        return button == MouseButtons.Left &&
               effectiveModifiers == Keys.None &&
               multiSelectBehavior != MultiSelectBehavior.RegularClick &&
               (isNodeInMultiSelection || !isSelectingOnPush);
    }
}
