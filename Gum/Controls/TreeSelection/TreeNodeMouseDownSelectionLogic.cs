using System.Windows.Input;

namespace Gum.Controls;

/// <summary>
/// Decides whether pressing a mouse button over a tree node should react (select it / raise
/// <c>ReactToClickedNode</c>) immediately, or defer to mouse-up. The decision depends only on its
/// arguments, not on any live control state, so it can be unit-tested without mouse-event plumbing.
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
        MouseButton button,
        ModifierKeys effectiveModifiers,
        MultiSelectBehavior multiSelectBehavior,
        bool isSelectingOnPush)
    {
        if (isNodeInMultiSelection && button == MouseButton.Right && effectiveModifiers != ModifierKeys.None)
        {
            return false;
        }

        if (effectiveModifiers == ModifierKeys.None && multiSelectBehavior != MultiSelectBehavior.RegularClick &&
            isNodeInMultiSelection)
        {
            return false;
        }

        return isSelectingOnPush || effectiveModifiers == ModifierKeys.Shift ||
               effectiveModifiers == ModifierKeys.Control || button == MouseButton.Right;
    }
}
