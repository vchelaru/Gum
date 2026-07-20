using System.Windows.Forms;

namespace CommonFormsAndControls;

/// <summary>
/// Which selection strategy a click on a tree node should use. Returned by
/// <see cref="TreeNodeClickDispatchLogic.GetReaction"/> for <see cref="MultiSelectTreeView"/>'s
/// caller to act on.
/// </summary>
public enum TreeNodeClickReaction
{
    /// <summary>The clicked node was null and the selection may become empty: clear it.</summary>
    DeselectAll,

    /// <summary>
    /// The clicked node was null, but <see cref="MultiSelectTreeView.AlwaysHaveOneNodeSelected"/>
    /// forbids an empty selection: do nothing.
    /// </summary>
    None,

    /// <summary>Toggle the clicked node's selected state, leaving other selected nodes alone.</summary>
    ToggleSelection,

    /// <summary>Select every node between the previously-selected node and the clicked node.</summary>
    RangeSelect,

    /// <summary>Clear the selection and select only the clicked node.</summary>
    SingleSelect,
}

/// <summary>
/// Decides which selection strategy applies to a clicked tree node. Extracted from
/// <see cref="MultiSelectTreeView.ReactToClickedNode"/> so the dispatch decision - independent of
/// any live control state - can be unit-tested directly instead of only through click plumbing.
/// The caller still owns performing the actual selection mutation.
/// </summary>
public class TreeNodeClickDispatchLogic
{
    /// <summary>
    /// A null <paramref name="hasClickedNode"/> deselects everything, unless
    /// <paramref name="alwaysHaveOneNodeSelected"/> forbids it. Otherwise: Ctrl+Click (or no prior
    /// selection, or <see cref="MultiSelectBehavior.RegularClick"/>) toggles the clicked node;
    /// Shift+Click selects the range from the current selection; a plain click selects only the
    /// clicked node.
    /// </summary>
    public TreeNodeClickReaction GetReaction(
        bool hasClickedNode,
        bool hasExistingSelection,
        bool alwaysHaveOneNodeSelected,
        Keys effectiveModifiers,
        MultiSelectBehavior multiSelectBehavior)
    {
        if (!hasClickedNode)
        {
            return alwaysHaveOneNodeSelected ? TreeNodeClickReaction.None : TreeNodeClickReaction.DeselectAll;
        }

        if (!hasExistingSelection ||
            effectiveModifiers == Keys.Control ||
            multiSelectBehavior == MultiSelectBehavior.RegularClick)
        {
            return TreeNodeClickReaction.ToggleSelection;
        }

        if (effectiveModifiers == Keys.Shift)
        {
            return TreeNodeClickReaction.RangeSelect;
        }

        return TreeNodeClickReaction.SingleSelect;
    }
}
