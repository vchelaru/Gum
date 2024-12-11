using Gum.Mvvm;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;

public class StateTreeViewModel : ViewModel
{
    [DependsOn(nameof(Categories))]
    [DependsOn(nameof(States))]
    public IEnumerable<StateTreeViewItem> Items => Categories.Concat<StateTreeViewItem>(States);

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => Get<ObservableCollection<CategoryViewModel>>();
        set => Set(value);
    }
    public ObservableCollection<StateViewModel> States
    {
        get => Get<ObservableCollection<StateViewModel>>();
        set => Set(value);
    }

    public StateTreeViewItem SelectedItem
    {
        get => Get<StateTreeViewItem>();
        set => Set(value);
    }

    public StateTreeViewModel()
    {
        Categories = new ObservableCollection<CategoryViewModel>();
        States = new ObservableCollection<StateViewModel>();

        PropertyChanged += HandlePropertyChanged;

    }

    private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch(e.PropertyName)
        {
            case nameof(SelectedItem):
                PushSelectionToGum();
                break;
        }
    }

    private void PushSelectionToGum()
    {
        if (SelectedItem is CategoryViewModel categoryViewModel)
        {
            GumState.Self.SelectedState.SelectedStateCategorySave = categoryViewModel.Data;
        }
        else if (SelectedItem is StateViewModel stateViewModel)
        {
            GumState.Self.SelectedState.SelectedStateSave = stateViewModel.Data;
        }
    }

    internal void RefreshBackgroundToVariables()
    {
        var instance = GumState.Self.SelectedState.SelectedInstance;

        foreach(var categoryVm in Categories)
        {
            foreach (var stateVm in categoryVm.States)
            {
                var state = stateVm.Data;
                if (instance != null)
                {
                    stateVm.IncludesVariablesForSelectedInstance =
                        state.Variables.Any(item => item.SourceObject == instance.Name);
                }
                else
                {
                    stateVm.IncludesVariablesForSelectedInstance = 
                        state.Variables.Any(item => string.IsNullOrEmpty(item.SourceObject));
                }
            }
        }
    }
}
