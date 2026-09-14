namespace Gum.Controls;

/// <summary>
/// How a tree extends its selection across multiple nodes.
/// </summary>
public enum MultiSelectBehavior
{
    /// <summary>
    /// A plain click selects one node; Ctrl+Click adds or removes a node and Shift+Click extends the
    /// range. This is what the element tree uses.
    /// </summary>
    CtrlDown,

    /// <summary>
    /// Every click toggles the clicked node's selected state, with no modifier required.
    /// </summary>
    RegularClick
}
