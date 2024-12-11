using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
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

    public void SetSelectedState(StateSave stateSave)
    {
        var foundState = States.FirstOrDefault(item => item.Data == stateSave);
        if (foundState == null)
        {
            foundState = Categories.SelectMany(item => item.States).FirstOrDefault(item => item.Data == stateSave);
        }


        foreach(var category in Categories)
        {
            foreach (var state in category.States)
            {
                if (state.IsSelected && state != foundState)
                {
                    state.IsSelected = false;
                }
            }
        }
        foreach(var state in States)
        {
            if (state.IsSelected && state != foundState)
            {
                state.IsSelected = false;
            }
        }

        if (foundState != null)
        {

            SelectedItem = foundState;
            foundState.IsSelected = true;
        }
        else
        {
            SelectedItem = null;
        }
    }

    public void AddMissingItems(IStateContainer stateContainer)
    {
        foreach (var category in stateContainer.Categories)
        {
            if (Categories.Any(item => item.Data == category) == false)
            {
                Categories.Add(new CategoryViewModel() { Data = category });
            }
        }


        foreach (var state in stateContainer.UncategorizedStates)
        {
            if (States.Any(item => item.Data == state) == false)
            {
                var stateVm = new StateViewModel() { Data = state };
                stateVm.PropertyChanged += HandleStateVmPropertyChanged;
                States.Add(stateVm);
            }
        }


        foreach (var category in stateContainer.Categories)
        {
            foreach (var state in category.States)
            {
                var categoryViewModel = Categories.FirstOrDefault(item => item.Data == category);
                if (categoryViewModel != null)
                {
                    var stateViewModel = categoryViewModel.States.FirstOrDefault(item => item.Data == state);

                    if (stateViewModel == null)
                    {
                        var stateVm = new StateViewModel() { Data = state };
                        stateVm.PropertyChanged += HandleStateVmPropertyChanged;
                        categoryViewModel.States.Add(stateVm);
                    }
                }
            }
        }


    }

    private void HandleStateVmPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(StateTreeViewItem.IsSelected))
        {
            if (sender is StateViewModel stateVm && stateVm.IsSelected == true)
            {
                GumState.Self.SelectedState.SelectedStateSave = stateVm.Data;
            }
            else if(sender is CategoryViewModel categoryVm && categoryVm.IsSelected)
            {
                //GumState.Self.SelectedState.SelectedStateCategorySave = categoryVm.Data;
            }
        }
    }
}
