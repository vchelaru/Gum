using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Gum.Collections;

/// <summary>
/// A wrapper collection that presents an ObservableCollection&lt;IRenderableIpso&gt; as ObservableCollection&lt;GraphicalUiElement&gt;.
/// Maintains bidirectional synchronization between the inner collection and this wrapper.
/// </summary>
public class GraphicalUiElementCollection : ObservableCollectionNoReset<GraphicalUiElement>
{
    private static readonly GraphicalUiElementCollection _empty = new GraphicalUiElementCollection();

    /// <summary>
    /// Gets a read-only empty collection that can be safely returned when no children exist.
    /// </summary>
    public static GraphicalUiElementCollection Empty => _empty;

    private readonly ObservableCollection<IRenderableIpso> _innerCollection = default!;
    // Non-null when the inner collection supports silent mutation, letting outer-driven changes
    // sync into it without allocating event args the wrapper's own handler would ignore anyway.
    private readonly ObservableCollectionNoReset<IRenderableIpso>? _innerNoReset;
    private bool _isUpdatingFromInner = false;
    private bool _isUpdatingFromOuter = false;

    /// <summary>
    /// Gets whether this collection is read-only.
    /// </summary>
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// Creates a wrapper around an existing IRenderableIpso collection.
    /// </summary>
    public GraphicalUiElementCollection(ObservableCollection<IRenderableIpso> innerCollection)
    {
        _innerCollection = innerCollection ?? throw new ArgumentNullException(nameof(innerCollection));
        _innerNoReset = innerCollection as ObservableCollectionNoReset<IRenderableIpso>;
        IsReadOnly = false;

        // Subscribe to inner collection changes
        _innerCollection.CollectionChanged += InnerCollection_CollectionChanged;

        // Initialize with existing items
        foreach (var item in _innerCollection.OfType<GraphicalUiElement>())
        {
            base.Items.Add(item);
        }
    }

    /// <summary>
    /// Private constructor for creating the empty read-only singleton.
    /// </summary>
    private GraphicalUiElementCollection()
    {
        _innerCollection = default!; // No backing collection for empty instance
        IsReadOnly = true;
    }

    /// <summary>
    /// Handles changes from the inner IRenderableIpso collection and syncs them to this collection.
    /// </summary>
    private void InnerCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Prevent circular updates
        if (_isUpdatingFromOuter)
            return;

        _isUpdatingFromInner = true;
        try
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        // e.NewStartingIndex is a RAW inner-collection index, which may also hold
                        // non-GraphicalUiElement items this wrapper doesn't mirror (see
                        // ToInnerIndex) - translate to the logical position among mirrored items
                        // rather than assuming they line up.
                        int rawIndex = e.NewStartingIndex;
                        foreach (var item in e.NewItems)
                        {
                            if (item is GraphicalUiElement gue)
                            {
                                base.InsertItem(LogicalIndexOf(rawIndex), gue);
                            }
                            rawIndex++;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        // Key removal off the actual removed object rather than e.OldStartingIndex
                        // (a raw index) - it may not equal this wrapper's logical index once a
                        // non-GraphicalUiElement item is interleaved (see ToInnerIndex), and the
                        // item is already gone from _innerCollection by the time this fires, so a
                        // raw-index lookup there isn't available anyway.
                        foreach (var item in e.OldItems)
                        {
                            if (item is GraphicalUiElement gue)
                            {
                                int logicalIndex = base.Items.IndexOf(gue);
                                if (logicalIndex >= 0)
                                {
                                    base.RemoveAt(logicalIndex);
                                }
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    {
                        // A replacement that is NOT a GraphicalUiElement still has to unmirror
                        // whatever it replaced - otherwise this wrapper keeps holding an item the
                        // inner collection no longer contains, and every later index lookup
                        // against it returns -1 (issue #4585).
                        int oldCount = e.OldItems?.Count ?? 0;
                        int newCount = e.NewItems?.Count ?? 0;
                        for (int i = 0; i < Math.Max(oldCount, newCount); i++)
                        {
                            int replacedIndex = i < oldCount && e.OldItems![i] is GraphicalUiElement replaced
                                ? base.Items.IndexOf(replaced)
                                : -1;

                            if (i < newCount && e.NewItems![i] is GraphicalUiElement replacement)
                            {
                                if (replacedIndex >= 0)
                                {
                                    base.SetItem(replacedIndex, replacement);
                                }
                                else
                                {
                                    // Replacing a raw item with a mirrored one is a logical insert,
                                    // positioned the same way the Add branch positions one.
                                    base.InsertItem(LogicalIndexOf(e.NewStartingIndex + i), replacement);
                                }
                            }
                            else if (replacedIndex >= 0)
                            {
                                base.RemoveAt(replacedIndex);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    // Only a moved GraphicalUiElement changes this wrapper's order - moving a raw
                    // item leaves the mirrored sequence intact. e.NewStartingIndex is a RAW index,
                    // so translate it rather than assuming it equals the logical one.
                    if (e.NewItems?.Count > 0 && e.NewItems[0] is GraphicalUiElement movedItem)
                    {
                        int oldLogicalIndex = base.Items.IndexOf(movedItem);
                        if (oldLogicalIndex >= 0)
                        {
                            base.MoveItem(oldLogicalIndex, LogicalIndexOf(e.NewStartingIndex));
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    base.ClearItems();
                    foreach (var item in _innerCollection.OfType<GraphicalUiElement>())
                    {
                        base.Items.Add(item);
                    }
                    base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    break;
            }
        }
        finally
        {
            _isUpdatingFromInner = false;
        }
    }

    /// <summary>
    /// Throws an exception if the collection is read-only.
    /// </summary>
    private void ThrowIfReadOnly()
    {
        if (IsReadOnly)
        {
            if(this == _empty)
            {
                throw new InvalidOperationException(
                    "Cannot modify the empty collection. " +
                    "If this is in a Visual, did you create a proper Visual which has a renderable, such as ContainerRuntime?");
            }
            throw new NotSupportedException("Cannot modify a read-only collection.");
        }
    }

    /// <summary>
    /// Maps a logical index (position among this wrapper's GraphicalUiElement-only items) to the
    /// corresponding index in <see cref="_innerCollection"/>, which may also hold raw
    /// non-GraphicalUiElement items this wrapper doesn't mirror (e.g. a shape runtime's
    /// auto-wired stroke renderable, attached to the inner collection directly via
    /// <c>RenderableBase.Parent</c>'s own <c>Children.Add</c> before any user-added children
    /// exist). Inserting at the logical end must land after ALL inner items, mirrored or not -
    /// "add my new child" means "goes after everything currently there," not "goes right after
    /// the last mirrored item, ahead of trailing raw ones."
    /// </summary>
    private int ToInnerIndex(int logicalIndex) =>
        logicalIndex >= base.Items.Count ? _innerCollection.Count : _innerCollection.IndexOf(base.Items[logicalIndex]);

    /// <summary>
    /// Maps a logical move to the index the moved item must land on in <see cref="_innerCollection"/>
    /// once it has been lifted out of <paramref name="innerOldIndex"/> - both the inner collection's
    /// Move and <c>MoveWithoutNotification</c> insert into the post-removal list. The item lands
    /// directly before whichever mirrored item follows it in the new logical order, or directly
    /// after the last mirrored one when it is moving to the logical end. Unlike
    /// <see cref="ToInnerIndex"/>'s append, a move must NOT land after trailing raw
    /// non-GraphicalUiElement items - reordering mirrored children can't relocate items this
    /// wrapper doesn't own.
    /// </summary>
    private int ToInnerMoveTarget(int oldIndex, int newIndex, int innerOldIndex)
    {
        // Only a forward move reaches the logical end, so the neighbour to anchor on is the current
        // last mirrored item - never the moved one, since MoveItem short-circuits an unchanged index.
        bool isMovingToEnd = newIndex >= oldIndex && newIndex + 1 >= base.Items.Count;
        int neighbourIndex = isMovingToEnd
            ? base.Items.Count - 1
            : (newIndex < oldIndex ? newIndex : newIndex + 1);

        int innerNeighbourIndex = _innerCollection.IndexOf(base.Items[neighbourIndex]);
        int postRemovalIndex = innerNeighbourIndex > innerOldIndex
            ? innerNeighbourIndex - 1
            : innerNeighbourIndex;
        return isMovingToEnd ? postRemovalIndex + 1 : postRemovalIndex;
    }

    /// <summary>
    /// The inverse of <see cref="ToInnerIndex"/>: how many of the first <paramref name="innerIndex"/>
    /// items in <see cref="_innerCollection"/> are GraphicalUiElement (i.e. mirrored) - the logical
    /// position a newly-added item at that raw index should be mirrored to.
    /// </summary>
    private int LogicalIndexOf(int innerIndex)
    {
        int logicalIndex = 0;
        for (int i = 0; i < innerIndex; i++)
        {
            if (_innerCollection[i] is GraphicalUiElement)
            {
                logicalIndex++;
            }
        }
        return logicalIndex;
    }

    /// <summary>
    /// Inserts an item into the collection at the specified index.
    /// </summary>
    protected override void InsertItem(int index, GraphicalUiElement item)
    {
        ThrowIfReadOnly();

        if (_isUpdatingFromInner)
        {
            base.InsertItem(index, item);
            return;
        }

        _isUpdatingFromOuter = true;
        try
        {
            int innerIndex = ToInnerIndex(index);
            if (_innerNoReset != null)
            {
                _innerNoReset.InsertWithoutNotification(innerIndex, item);
            }
            else
            {
                _innerCollection.Insert(innerIndex, item);
            }
            base.InsertItem(index, item);
        }
        finally
        {
            _isUpdatingFromOuter = false;
        }
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    protected override void RemoveItem(int index)
    {
        ThrowIfReadOnly();

        if (_isUpdatingFromInner)
        {
            base.RemoveItem(index);
            return;
        }

        _isUpdatingFromOuter = true;
        try
        {
            // The item at `index` already exists in the inner collection - look up its actual
            // position rather than assuming logical index == inner index (see ToInnerIndex).
            int innerIndex = _innerCollection.IndexOf(base.Items[index]);
            if (_innerNoReset != null)
            {
                _innerNoReset.RemoveAtWithoutNotification(innerIndex);
            }
            else
            {
                _innerCollection.RemoveAt(innerIndex);
            }
            base.RemoveItem(index);
        }
        finally
        {
            _isUpdatingFromOuter = false;
        }
    }

    /// <summary>
    /// Replaces the item at the specified index.
    /// </summary>
    protected override void SetItem(int index, GraphicalUiElement item)
    {
        ThrowIfReadOnly();

        if (_isUpdatingFromInner)
        {
            base.SetItem(index, item);
            return;
        }

        _isUpdatingFromOuter = true;
        try
        {
            // The item currently at `index` already exists in the inner collection - look up its
            // actual position rather than assuming logical index == inner index.
            int innerIndex = _innerCollection.IndexOf(base.Items[index]);
            if (_innerNoReset != null)
            {
                _innerNoReset.SetWithoutNotification(innerIndex, item);
            }
            else
            {
                _innerCollection[innerIndex] = item;
            }
            base.SetItem(index, item);
        }
        finally
        {
            _isUpdatingFromOuter = false;
        }
    }

    /// <summary>
    /// Clears all items from the collection.
    /// </summary>
    protected override void ClearItems()
    {
        ThrowIfReadOnly();

        if (_isUpdatingFromInner)
        {
            base.ClearItems();
            return;
        }

        _isUpdatingFromOuter = true;
        try
        {
            // Equal counts means every inner item is mirrored here, so a wholesale clear is
            // both correct and cheaper. Otherwise the inner collection also holds raw
            // non-GraphicalUiElement renderables this wrapper never mirrored - a shape runtime's
            // auto-wired stroke (see ToInnerIndex) - and clearing wholesale would destroy the
            // shape's stroke along with the user's children, with nothing to re-create it.
            if (_innerCollection.Count == base.Items.Count)
            {
                if (_innerNoReset != null)
                {
                    _innerNoReset.ClearWithoutNotification();
                }
                else
                {
                    _innerCollection.Clear();
                }
            }
            else
            {
                for (int i = base.Items.Count - 1; i > -1; i--)
                {
                    int innerIndex = _innerCollection.IndexOf(base.Items[i]);
                    if (innerIndex > -1)
                    {
                        if (_innerNoReset != null)
                        {
                            _innerNoReset.RemoveAtWithoutNotification(innerIndex);
                        }
                        else
                        {
                            _innerCollection.RemoveAt(innerIndex);
                        }
                    }
                }
            }
            base.ClearItems();
        }
        finally
        {
            _isUpdatingFromOuter = false;
        }
    }

    /// <summary>
    /// Moves an item from one index to another.
    /// </summary>
    protected override void MoveItem(int oldIndex, int newIndex)
    {
        ThrowIfReadOnly();

        if (_isUpdatingFromInner || oldIndex == newIndex)
        {
            base.MoveItem(oldIndex, newIndex);
            return;
        }

        _isUpdatingFromOuter = true;
        try
        {
            int innerOldIndex = _innerCollection.IndexOf(base.Items[oldIndex]);
            int innerNewIndex = ToInnerMoveTarget(oldIndex, newIndex, innerOldIndex);
            if (_innerNoReset != null)
            {
                _innerNoReset.MoveWithoutNotification(innerOldIndex, innerNewIndex);
            }
            else
            {
                _innerCollection.Move(innerOldIndex, innerNewIndex);
            }
            base.MoveItem(oldIndex, newIndex);
        }
        finally
        {
            _isUpdatingFromOuter = false;
        }
    }
}