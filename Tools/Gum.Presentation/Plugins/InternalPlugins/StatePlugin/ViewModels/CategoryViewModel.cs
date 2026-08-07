using CommunityToolkit.Mvvm.Input;
using Gum.DataTypes.Variables;
using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;

public abstract class StateTreeViewItem : ViewModel
{
    public abstract object DataAsObject { get; }
    public abstract string Title { get; }
    public bool IsSelected
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsExpanded
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsRequiredBySelectedBehavior
    {
        get => Get<bool>();
        set => Set(value);
    }

    public override string ToString() => DataAsObject?.ToString();


    internal void ForceRefreshTitle() => NotifyPropertyChanged(nameof(Title));
}

public class CategoryViewModel : StateTreeViewItem
{
    public StateSaveCategory Data { get; set; }
    public override object DataAsObject => Data;

    public ObservableCollection<StateViewModel> States { get; set; } = new ObservableCollection<StateViewModel>();

    public override string Title => Data?.Name;

    /// <summary>
    /// Raised when the user clicks this category's "+" button. The owning <see cref="StateTreeViewModel"/>
    /// subscribes to this to select the category and show the Add State dialog, since this view model
    /// has no dependencies of its own to do that directly.
    /// </summary>
    public event EventHandler? AddStateRequested;

    public RelayCommand AddStateCommand { get; }

    public CategoryViewModel()
    {
        AddStateCommand = new RelayCommand(() => AddStateRequested?.Invoke(this, EventArgs.Empty));
    }
}
