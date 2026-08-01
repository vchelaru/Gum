using System.Windows.Input;

namespace Gum.Controls;

/// <summary>
/// Which selection strategy a click on a tree node should use. Returned by
/// <see cref="TreeNodeClickDispatchLogic.GetReaction"/> for <see cref="GumTreeView"/> to act on.
/// </summary>
public enum TreeNodeClickReaction
{
    /// <summary>The clicked node was null and the selection may become empty: clear it.</summary>
    DeselectAll,

    /// <summary>
    /// The clicked node was null, but <see cref="GumTreeView.AlwaysHaveOneNodeSelected"/>
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
/// Decides which selection strategy applies to a clicked tree node. The decision depends only on its
/// arguments, not on any live control state, so it can be unit-tested without click plumbing. The
/// caller still owns performing the actual selection mutation.
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
        ModifierKeys effectiveModifiers,
        MultiSelectBehavior multiSelectBehavior)
    {
        if (!hasClickedNode)
        {
            return alwaysHaveOneNodeSelected ? TreeNodeClickReaction.None : TreeNodeClickReaction.DeselectAll;
        }

        if (!hasExistingSelection ||
            effectiveModifiers == ModifierKeys.Control ||
            multiSelectBehavior == MultiSelectBehavior.RegularClick)
        {
            return TreeNodeClickReaction.ToggleSelection;
        }

        if (effectiveModifiers == ModifierKeys.Shift)
        {
            return TreeNodeClickReaction.RangeSelect;
        }

        return TreeNodeClickReaction.SingleSelect;
    }
}
