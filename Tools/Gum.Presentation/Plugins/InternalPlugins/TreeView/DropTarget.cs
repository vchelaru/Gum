using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.TreeView;

/// <summary>
/// Describes a drop destination resolved by the tree view onto an
/// <see cref="ElementSave"/>'s flat <see cref="ElementSave.Instances"/> list.
/// Replaces the overloaded <c>int index</c> that previously meant different
/// things to different downstream consumers (issue #2869).
/// </summary>
/// <param name="ParentElement">
/// The Element whose <see cref="ElementSave.Instances"/> list receives the
/// new (or moved) instance.
/// </param>
/// <param name="ParentInstance">
/// The instance the dropped object should be visually parented under (its
/// <c>.Parent</c> variable). Null when the drop is directly on the element.
/// </param>
/// <param name="Position">
/// Where in <paramref name="ParentElement"/>'s flat instance list the drop
/// lands. See <see cref="DropPosition"/>.
/// </param>
public sealed record DropTarget(
    ElementSave ParentElement,
    InstanceSave? ParentInstance,
    DropPosition Position);

/// <summary>
/// Position within an <see cref="ElementSave"/>'s flat
/// <see cref="ElementSave.Instances"/> list that a drop should land at.
/// Each variant carries the information its consumer needs — no shared
/// <c>int</c> with conflicting meanings.
/// </summary>
public abstract record DropPosition
{
    private DropPosition() { }

    /// <summary>Append to the end of the parent element's flat instances list.</summary>
    public sealed record Append : DropPosition;

    /// <summary>Insert at the given flat-list index in the parent element's instances list.</summary>
    public sealed record InsertAt(int Index) : DropPosition;

    /// <summary>Insert immediately before the given existing sibling in the flat list.</summary>
    public sealed record BeforeSibling(InstanceSave Sibling) : DropPosition;

    /// <summary>Insert immediately after the given existing sibling in the flat list.</summary>
    public sealed record AfterSibling(InstanceSave Sibling) : DropPosition;
}
