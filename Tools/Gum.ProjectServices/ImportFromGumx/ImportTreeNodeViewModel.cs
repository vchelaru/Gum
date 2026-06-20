using Gum.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ImportFromGumxPlugin.ViewModels;

public class ImportTreeNodeViewModel : ViewModel
{
    private InclusionState _inclusionState;
    private bool _suppressChildNotifications;
    private IReadOnlyList<StandardDiffRowViewModel>? _standardDiffRows;

    public string DisplayName { get; }
    public string FullName { get; }
    public bool IsFolder { get; }
    public bool IsLeaf => !IsFolder;
    public ElementItemType? ElementType { get; }
    public ObservableCollection<ImportTreeNodeViewModel> Children { get; }

    /// <summary>
    /// Per-row diff detail for a flagged Standard (#2779). Null on non-standard rows
    /// and on standards that match the destination. The view's template selector keys
    /// off <see cref="HasStandardDiffRows"/> to show an expander.
    /// </summary>
    public IReadOnlyList<StandardDiffRowViewModel>? StandardDiffRows
    {
        get => _standardDiffRows;
        set
        {
            if (!ReferenceEquals(_standardDiffRows, value))
            {
                _standardDiffRows = value;
                NotifyPropertyChanged(nameof(StandardDiffRows));
                NotifyPropertyChanged(nameof(HasStandardDiffRows));
            }
        }
    }

    /// <summary>True when this row has at least one diff entry to display.</summary>
    public bool HasStandardDiffRows => _standardDiffRows != null && _standardDiffRows.Count > 0;

    /// <summary>Whether the "Details..." button next to the checkbox is shown. True iff there are diff rows.</summary>
    [DependsOn(nameof(StandardDiffRows))]
    public bool IsDetailsButtonVisible => HasStandardDiffRows;

    public InclusionState InclusionState
    {
        get => _inclusionState;
        set
        {
            if (_inclusionState != value)
            {
                _inclusionState = value;
                NotifyPropertyChanged(nameof(InclusionState));
                NotifyPropertyChanged(nameof(IsChecked));
            }
        }
    }

    public bool? IsChecked
    {
        get
        {
            if (!IsFolder)
            {
                return _inclusionState == InclusionState.Explicit;
            }

            if (Children.Count == 0)
            {
                return false;
            }

            bool? first = Children[0].IsChecked;
            foreach (ImportTreeNodeViewModel child in Children)
            {
                if (child.IsChecked != first)
                {
                    return false;
                }
            }
            return first;
        }
        set
        {
            if (!IsFolder)
            {
                if (value == true)
                {
                    InclusionState = InclusionState.Explicit;
                }
                else
                {
                    InclusionState = InclusionState.NotIncluded;
                }
            }
            else
            {
                _suppressChildNotifications = true;
                foreach (ImportTreeNodeViewModel child in Children)
                {
                    child.IsChecked = value;
                }
                _suppressChildNotifications = false;
                NotifyPropertyChanged(nameof(IsChecked));
            }
        }
    }

    // Folder constructor
    public ImportTreeNodeViewModel(string displayName, string fullName)
    {
        DisplayName = displayName;
        FullName = fullName;
        IsFolder = true;
        ElementType = null;
        Children = new ObservableCollection<ImportTreeNodeViewModel>();
        Children.CollectionChanged += OnChildrenChanged;
    }

    // Leaf constructor
    public ImportTreeNodeViewModel(string displayName, string fullName, ElementItemType elementType)
    {
        DisplayName = displayName;
        FullName = fullName;
        IsFolder = false;
        ElementType = elementType;
        Children = new ObservableCollection<ImportTreeNodeViewModel>();
        Children.CollectionChanged += OnChildrenChanged;
    }

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ImportTreeNodeViewModel child in e.NewItems)
            {
                child.PropertyChanged += OnChildPropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (ImportTreeNodeViewModel child in e.OldItems)
            {
                child.PropertyChanged -= OnChildPropertyChanged;
            }
        }
        NotifyPropertyChanged(nameof(IsChecked));
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsChecked) && !_suppressChildNotifications)
        {
            NotifyPropertyChanged(nameof(IsChecked));
        }
    }
}
