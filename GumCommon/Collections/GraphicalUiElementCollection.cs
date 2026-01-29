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
/// A wrapper collection that presents an ObservableCollection<IRenderableIpso> as ObservableCollection<GraphicalUiElement>.
/// Maintains bidirectional synchronization between the inner collection and this wrapper.
/// </summary>
public class GraphicalUiElementCollection : ObservableCollectionNoReset<GraphicalUiElement>
{
    private static readonly GraphicalUiElementCollection _empty = new GraphicalUiElementCollection();

    /// <summary>
    /// Gets a read-only empty collection that can be safely returned when no children exist.
    /// </summary>
    public static GraphicalUiElementCollection Empty => _empty;

    private readonly ObservableCollection<IRenderableIpso> _innerCollection;
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
        _innerCollection = null; // No backing collection for empty instance
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
                        int index = e.NewStartingIndex;
                        foreach (var item in e.NewItems)
                        {
                            if (item is GraphicalUiElement gue)
                            {
                                base.InsertItem(index++, gue);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        // Remove items at the specified index
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            base.RemoveAt(e.OldStartingIndex);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.NewItems != null && e.NewItems.Count > 0)
                    {
                        int index = e.NewStartingIndex;
                        foreach (var item in e.NewItems)
                        {
                            if (item is GraphicalUiElement gue)
                            {
                                base.SetItem(index++, gue);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    var movedItem = base.Items[e.OldStartingIndex];
                    base.RemoveAt(e.OldStartingIndex);
                    base.InsertItem(e.NewStartingIndex, movedItem);
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
            throw new NotSupportedException("Cannot modify a read-only collection.");
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
            _innerCollection.Insert(index, item);
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
            _innerCollection.RemoveAt(index);
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
            _innerCollection[index] = item;
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
            _innerCollection.Clear();
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

        if (_isUpdatingFromInner)
        {
            base.MoveItem(oldIndex, newIndex);
            return;
        }

        _isUpdatingFromOuter = true;
        try
        {
            _innerCollection.Move(oldIndex, newIndex);
            base.MoveItem(oldIndex, newIndex);
        }
        finally
        {
            _isUpdatingFromOuter = false;
        }
    }
}