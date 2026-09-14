using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Gum.Managers;

/// <summary>
/// The child collection of a <see cref="GumTreeNode"/>, or the root collection of a
/// <see cref="Gum.Controls.GumTreeView"/>. Keeps each node's <see cref="GumTreeNode.Parent"/> and
/// owning collection in step with membership, so a node always knows how to detach itself.
/// </summary>
/// <remarks>
/// This is the piece the WinForms <c>TreeNodeCollection</c> used to provide. It is a real
/// <see cref="ObservableCollection{T}"/> so a WPF <c>TreeView</c> can bind to it directly and pick
/// up structural edits without the tree being rebuilt.
/// </remarks>
public class GumTreeNodeCollection : ObservableCollection<GumTreeNode>
{
    /// <summary>
    /// The node these are the children of, or null when this is a tree's root collection.
    /// </summary>
    internal GumTreeNode? Owner { get; }

    internal GumTreeNodeCollection(GumTreeNode? owner)
    {
        Owner = owner;
    }

    /// <summary>
    /// Creates a root collection - one whose members have no parent node.
    /// </summary>
    public GumTreeNodeCollection() : this(null)
    {
    }

    protected override void InsertItem(int index, GumTreeNode item)
    {
        // Reordering within one collection has to be spelled remove-then-insert, because detaching
        // here would shift the very index the caller computed. Every reorder path already does
        // exactly that, so this only fires on a genuine mistake.
        if (item.OwningCollection == this)
        {
            throw new ArgumentException(
                $"'{item.Text}' is already a child here. Remove it before inserting it at a new index.",
                nameof(item));
        }

        // Reparenting across collections is a single add, matching how the tree-building code
        // moves a node between folders.
        item.DetachFromCurrentCollection();

        base.InsertItem(index, item);

        item.Attach(this);
    }

    protected override void RemoveItem(int index)
    {
        GumTreeNode removed = this[index];

        base.RemoveItem(index);

        removed.Detach();
    }

    protected override void SetItem(int index, GumTreeNode item)
    {
        GumTreeNode replaced = this[index];

        item.DetachFromCurrentCollection();

        base.SetItem(index, item);

        replaced.Detach();
        item.Attach(this);
    }

    protected override void ClearItems()
    {
        GumTreeNode[] cleared = new GumTreeNode[Count];
        CopyTo(cleared, 0);

        base.ClearItems();

        foreach (GumTreeNode node in cleared)
        {
            node.Detach();
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);

        // Whether a node renders an expander depends on whether it has children, and that is not
        // otherwise observable from the node itself.
        Owner?.RaiseChildCountChanged();
    }
}
