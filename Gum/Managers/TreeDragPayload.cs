using System.Collections.Generic;
using System.Linq;

namespace Gum.Managers;

/// <summary>
/// Carries what is being dragged out of the element tree, alongside the drag's data object rather
/// than inside it.
/// </summary>
/// <remarks>
/// <para>
/// Drag payloads cannot hold Gum's own objects. A WPF <c>DataObject</c> serializes anything it is
/// given, Gum's <c>*Save</c> types are not <c>[Serializable]</c>, and the values come back null on
/// the drop side. So the data object carries only <see cref="DataFormat"/> as a marker, and the real
/// payload travels here.
/// </para>
/// <para>
/// A single static slot is enough: <c>DragDrop.DoDragDrop</c> is synchronous, so only one drag can
/// be in flight at a time. Set it immediately before the call and clear it in a <c>finally</c>.
/// </para>
/// <para>
/// This replaces both the previous per-search-result slot and the trick of putting live
/// <c>TreeNode</c> objects on the data object, which forced every drop target to scan formats and
/// pattern-match on a widget type just to find them.
/// </para>
/// </remarks>
internal static class TreeDragPayload
{
    /// <summary>
    /// The marker format put on the drag data object. Presence of this format means "look here".
    /// </summary>
    public const string DataFormat = "GumDraggedTreeNodes";

    /// <summary>
    /// The nodes being dragged, when the drag started from the tree. Null for a drag that has no
    /// nodes behind it, such as one started from the flat search results.
    /// </summary>
    public static IReadOnlyList<GumTreeNode>? Nodes { get; private set; }

    /// <summary>
    /// What the dragged items stand for - an <c>ElementSave</c>, <c>InstanceSave</c>, and so on.
    /// This is all any drop target outside the tree needs.
    /// </summary>
    public static IReadOnlyList<object?>? Tags { get; private set; }

    public static void SetNodes(IReadOnlyList<GumTreeNode> nodes)
    {
        Nodes = nodes;
        Tags = nodes.Select(node => node.Tag).ToList();
    }

    /// <summary>
    /// Sets a payload that has tags but no tree nodes behind it - a search result standing in for a
    /// node that may not be realized in the tree.
    /// </summary>
    public static void SetTags(IReadOnlyList<object?> tags)
    {
        Nodes = null;
        Tags = tags;
    }

    public static void Clear()
    {
        Nodes = null;
        Tags = null;
    }
}
