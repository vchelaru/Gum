using System.Windows.Forms;

namespace CommonFormsAndControls;

/// <summary>
/// Decides whether pressing a mouse button over a tree node should react (select it / raise
/// <c>ReactToClickedNode</c>) immediately, or defer to mouse-up. Extracted from
/// <see cref="MultiSelectTreeView"/> so the decision - independent of any live control state -
/// can be unit-tested directly instead of only through <c>OnMouseDown</c> plumbing.
/// </summary>
public class TreeNodeMouseDownSelectionLogic
{
    /// <summary>
    /// A right-click with a modifier held on a node that's already part of a multi-selection opens
    /// a context menu without changing selection. Otherwise, with no modifier held and a multi-select
    /// behavior other than <see cref="MultiSelectBehavior.RegularClick"/>, pressing on an
    /// already-multi-selected node is a potential drag - the actual (re)selection is deferred to
    /// mouse-up. Otherwise this reacts immediately when selection normally happens on push
    /// (<paramref name="isSelectingOnPush"/>), when Shift or Control is held (both extend/toggle
    /// selection on press), or on a right-click (to select before showing the context menu).
    /// </summary>
    public bool ShouldReactToClick(
        bool isNodeInMultiSelection,
        MouseButtons button,
        Keys effectiveModifiers,
        MultiSelectBehavior multiSelectBehavior,
        bool isSelectingOnPush)
    {
        if (isNodeInMultiSelection && button == MouseButtons.Right && effectiveModifiers != Keys.None)
        {
            return false;
        }

        if (effectiveModifiers == Keys.None && multiSelectBehavior != MultiSelectBehavior.RegularClick &&
            isNodeInMultiSelection)
        {
            return false;
        }

        return isSelectingOnPush || effectiveModifiers == Keys.Shift || effectiveModifiers == Keys.Control ||
               button == MouseButtons.Right;
    }
}
