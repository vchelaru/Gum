namespace Gum.Managers;

// Issue #4123: FlatSearchListBox (WPF) drags a TreeNode tagged with the search result's backing
// object onto the Wireframe canvas (WinForms). TreeNode.Tag does not survive that WPF -> WinForms
// drag boundary when its type isn't [Serializable] -- Gum's *Save types aren't, and TreeNode's own
// serialization silently drops Tag in that case (confirmed empirically: Tag comes back null under
// every format). DragDrop.DoDragDrop is synchronous -- only one drag can be in flight at a time --
// so a single static slot set immediately before the call and cleared in a finally is sufficient to
// carry the real object across in-process instead.
internal static class SearchResultDragPayload
{
    public static object? Current { get; set; }
}
