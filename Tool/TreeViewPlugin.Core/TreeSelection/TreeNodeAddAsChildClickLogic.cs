namespace Gum.Controls;

/// <summary>
/// Decides whether a click should add the clicked node as a child of whatever is currently
/// selected, instead of running the ordinary selection reaction (#4837 - Ctrl+Shift-click on a
/// top-level component/screen). The decision depends only on its arguments, not on any live
/// control state, so it can be unit-tested without click plumbing.
/// </summary>
public class TreeNodeAddAsChildClickLogic
{
    /// <summary>
    /// True when Ctrl+Shift are both held on a left-click that lands on a node other than the
    /// current selection, and something is already selected to add it under. The caller still
    /// owns deciding whether the clicked node is actually addable (e.g. a top-level element) and
    /// performing the add.
    /// </summary>
    public bool ShouldAddAsChild(
        bool hasClickedNode,
        bool hasExistingSelection,
        bool clickedNodeIsSelectedNode,
        TreePointerButton button,
        TreeModifierKeys effectiveModifiers)
    {
        if (!hasClickedNode || !hasExistingSelection || clickedNodeIsSelectedNode || button != TreePointerButton.Left)
        {
            return false;
        }

        const TreeModifierKeys controlShift = TreeModifierKeys.Control | TreeModifierKeys.Shift;
        return (effectiveModifiers & controlShift) == controlShift;
    }
}
