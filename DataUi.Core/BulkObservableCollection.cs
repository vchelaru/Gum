using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WpfDataUi;

/// <summary>
/// An ObservableCollection that supports replacing all items with a single Reset notification,
/// avoiding the per-item CollectionChanged notifications that ObservableCollection fires by default.
/// </summary>
public class BulkObservableCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Clears all items and adds the new items, firing a single Reset CollectionChanged notification
    /// instead of individual Add/Remove notifications per item.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> newItems)
    {
        Items.Clear();
        foreach (var item in newItems)
            Items.Add(item);
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
