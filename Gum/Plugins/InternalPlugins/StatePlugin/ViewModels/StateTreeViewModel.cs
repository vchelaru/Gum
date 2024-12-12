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
    #region Fields/Properties

    public StateStackingMode StateStackingMode
    {
        get => Get<StateStackingMode>();
        set => Set(value);
    }

    [DependsOn(nameof(StateStackingMode))]
    public bool IsSingleStateSelected
    {
        get => StateStackingMode == StateStackingMode.SingleState;
        set
        {
            if (value) StateStackingMode = StateStackingMode.SingleState;
        }
    }

    [DependsOn(nameof(StateStackingMode))]
    public bool IsCombinedStateSelected
    {
        get => StateStackingMode == StateStackingMode.CombineStates;
        set
        {
            if (value) StateStackingMode = StateStackingMode.CombineStates;
        }
    }

    [DependsOn(nameof(Categories))]
    [DependsOn(nameof(States))]
    public IEnumerable<StateTreeViewItem> Items => Categories.Concat<StateTreeViewItem>(States);

    private readonly StateTreeViewRightClickService _stateTreeViewRightClickService;

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


    #endregion

    #region Initialize

    public StateTreeViewModel(StateTreeViewRightClickService stateTreeViewRightClickService)
    {
        _stateTreeViewRightClickService = stateTreeViewRightClickService;
        Categories = new ObservableCollection<CategoryViewModel>();
        States = new ObservableCollection<StateViewModel>();

        PropertyChanged += HandlePropertyChanged;
    }

    private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StateStackingMode))
        {
            GumState.Self.SelectedState.StateStackingMode = StateStackingMode;
        }
    }

    #endregion

    private void HandleItemVmPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(StateTreeViewItem.IsSelected))
        {
            if (sender is StateViewModel stateVm && stateVm.IsSelected == true)
            {
                GumState.Self.SelectedState.SelectedStateSave = stateVm.Data;
            }
            else if(sender is CategoryViewModel categoryVm && categoryVm.IsSelected)
            {
                // If a state was selected, we need to deselect everything and forcefully select the state:
                if(GumState.Self.SelectedState.SelectedStateSave != null)
                {
                    GumState.Self.SelectedState.SelectedStateSave = null;
                    GumState.Self.SelectedState.SelectedStateCategorySave = null;
                }
                GumState.Self.SelectedState.SelectedStateCategorySave = categoryVm.Data;
            }
            _stateTreeViewRightClickService.PopulateMenuStrip();
        }
        
    }

    #region Methods

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
            foreach (var category in Categories)
            {
                foundState = category.States.FirstOrDefault(item => item.Data == stateSave);
                if (foundState != null)
                {
                    category.IsExpanded = true;
                    break;
                }
            }
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
            foundState.IsSelected = true;
        }
    }

    public void SetSelectedStateSaveCategory(StateSaveCategory category)
    {
        var foundCategory = Categories.FirstOrDefault(item => item.Data == category);

        foreach(var state in States)
        {
            if (state.IsSelected)
            {
                state.IsSelected = false;
            }
        }

        foreach (var categoryVm in Categories)
        {
            if (categoryVm.IsSelected && categoryVm != foundCategory)
            {
                categoryVm.IsSelected = false;
            }
            foreach(var state in categoryVm.States)
            {
                if (state.IsSelected)
                {
                    state.IsSelected = false;
                }
            }
        }
        if (foundCategory != null)
        {
            foundCategory.IsSelected = true;
        }
    }

    public void AddMissingItems(IStateContainer stateContainer)
    {
        foreach (var category in stateContainer.Categories)
        {
            if (Categories.Any(item => item.Data == category) == false)
            {
                var categoryVm = new CategoryViewModel() { Data = category };
                categoryVm.PropertyChanged += HandleItemVmPropertyChanged;
                Categories.Add(categoryVm);
            }
        }


        foreach (var state in stateContainer.UncategorizedStates)
        {
            if (States.Any(item => item.Data == state) == false)
            {
                var stateVm = new StateViewModel() { Data = state };
                stateVm.PropertyChanged += HandleItemVmPropertyChanged;
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
                        stateVm.PropertyChanged += HandleItemVmPropertyChanged;
                        categoryViewModel.States.Add(stateVm);
                    }
                }
            }
        }


    }

    public void RemoveUnnecessaryNodes(IStateContainer stateContainer)
    {

        for (int i = 0; i < Categories.Count; i++)
        {
            if (stateContainer.Categories.Contains(Categories[i].Data) == false)
            {
                Categories.RemoveAt(i);
                i--;
            }
            else
            {
                var categoryViewModel = Categories[i];
                var category = categoryViewModel.Data;
                for (int j = 0; j < categoryViewModel.States.Count; j++)
                {
                    var stateViewModel = categoryViewModel.States[j];

                    if (category.States.Contains(stateViewModel.Data) == false)
                    {
                        Categories[i].States.RemoveAt(j);
                        j--;
                    }
                }
            }
        }
        for (int i = 0; i < States.Count; i++)
        {
            var stateViewModel = States[i];
            if (stateContainer.UncategorizedStates.Contains(stateViewModel.Data) == false)
            {
                States.RemoveAt(i);
            }
        }
    }

    public void FixNodeOrderInCategory(IStateContainer stateContainer)
    {
        for (int categoryIndex = 0; categoryIndex < stateContainer.Categories.Count(); categoryIndex++)
        {
            var categoryViewModel = Categories[categoryIndex];
            var category = stateContainer.Categories.ElementAt(categoryIndex);
            if (categoryViewModel.Data != category)
            {
                var categoryToMove = Categories.FirstOrDefault(item => item.Data == category);
                var oldIndex = Categories.IndexOf(categoryToMove);
                Categories.Move(oldIndex, categoryIndex);
            }
        }

        for (int categoryIndex = 0; categoryIndex < stateContainer.Categories.Count(); categoryIndex++)
        {
            var categoryViewModel = Categories[categoryIndex];
            var category = stateContainer.Categories.ElementAt(categoryIndex);
            for (int stateIndex = 0; stateIndex < category.States.Count; stateIndex++)
            {
                var state = category.States[stateIndex];
                if (categoryViewModel.States[stateIndex].Data != state)
                {
                    var itemToMove = categoryViewModel.States.FirstOrDefault(item => item.Data == state);
                    if(itemToMove != null)
                    {
                        var oldIndex = categoryViewModel.States.IndexOf(itemToMove);
                        categoryViewModel.States.Move(oldIndex, stateIndex);
                    }
                }
            }

        }
    }


    #endregion

    #region Rename

    internal void HandleRename(StateSave state)
    {
        var stateVm = Categories.SelectMany(item => item.States).FirstOrDefault(item => item.Data == state);

        stateVm?.ForceRefreshTitle();
    }

    internal void HandleRename(StateSaveCategory category)
    {
        var categoryVm = Categories.FirstOrDefault(item => item.Data == category);
        categoryVm?.ForceRefreshTitle();
    }

    #endregion
}
