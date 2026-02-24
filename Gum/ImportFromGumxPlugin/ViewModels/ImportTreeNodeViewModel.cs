using Gum.Mvvm;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ImportFromGumxPlugin.ViewModels;

public class ImportTreeNodeViewModel : ViewModel
{
    private InclusionState _inclusionState;
    private bool _suppressChildNotifications;

    public string DisplayName { get; }
    public string FullName { get; }
    public bool IsFolder { get; }
    public bool IsLeaf => !IsFolder;
    public ElementItemType? ElementType { get; }
    public ObservableCollection<ImportTreeNodeViewModel> Children { get; }

    public string? AutoIncludedReason
    {
        get => Get<string>();
        set => Set(value);
    }

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
                return _inclusionState switch
                {
                    InclusionState.Explicit => true,
                    InclusionState.AutoIncluded => null,
                    _ => false
                };
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
                    return null;
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
