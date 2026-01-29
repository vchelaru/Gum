using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Gum.Behaviors;

public static class SelectedItemsBehavior
{
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(IList),
            typeof(SelectedItemsBehavior),
            new PropertyMetadata(null, OnSelectedItemsChanged));

    public static void SetSelectedItems(DependencyObject d, IList? value) => d.SetValue(SelectedItemsProperty, value);
    public static IList? GetSelectedItems(DependencyObject d) => (IList?)d.GetValue(SelectedItemsProperty);

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(SelectedItemsBehavior), new PropertyMetadata(false));

    private static bool GetIsUpdating(DependencyObject d) => (bool)d.GetValue(IsUpdatingProperty);
    private static void SetIsUpdating(DependencyObject d, bool v) => d.SetValue(IsUpdatingProperty, v);

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Selector selector)
            throw new InvalidOperationException("SelectedItemsBehavior can only be attached to Selector-derived controls.");

        // Unhook old events if rebinding
        selector.SelectionChanged -= Selector_SelectionChanged;
        if (e.OldValue is INotifyCollectionChanged oldObs)
            oldObs.CollectionChanged -= (_, __) => { /* no-op placeholder */ };

        // Hook selection change
        selector.SelectionChanged += Selector_SelectionChanged;

        // If VM collection is observable, mirror changes into UI
        if (e.NewValue is INotifyCollectionChanged newObs)
        {
            newObs.CollectionChanged += (s, _) => SyncFromSource(selector, s as IList);
        }

        // Initial sync from source to UI
        SyncFromSource(selector, e.NewValue as IList);
    }

    private static IList GetUiSelectedItems(Selector selector)
    {
        // ListBox/ListView have SelectedItems; DataGrid (MultiSelector) too
        if (selector is ListBox lb) return lb.SelectedItems;
        if (selector is MultiSelector ms) return ms.SelectedItems;
        throw new NotSupportedException($"{selector.GetType().Name} doesn’t expose SelectedItems.");
    }

    private static IEnumerable GetItems(ItemsControl itemsControl) =>
        itemsControl.Items.SourceCollection ?? itemsControl.Items;

    private static void SyncFromSource(Selector selector, IList? source)
    {
        if (source is null || GetIsUpdating(selector)) return;
        try
        {
            SetIsUpdating(selector, true);
            var ui = GetUiSelectedItems(selector);
            ui.Clear();
            // Only select items that are present in the ItemsControl
            var set = new HashSet<object>(new ReferenceEqualityComparer());
            foreach (var item in GetItems((ItemsControl)selector))
                set.Add(item);
            foreach (var item in source)
                if (set.Contains(item)) ui.Add(item);
        }
        finally { SetIsUpdating(selector, false); }
    }

    private static void Selector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not Selector selector) return;
        var target = GetSelectedItems(selector);
        if (target is null || GetIsUpdating(selector)) return;

        try
        {
            SetIsUpdating(selector, true);
            foreach (var removed in e.RemovedItems) target.Remove(removed);
            foreach (var added in e.AddedItems) if (!target.Contains(added)) target.Add(added);
        }
        finally { SetIsUpdating(selector, false); }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}