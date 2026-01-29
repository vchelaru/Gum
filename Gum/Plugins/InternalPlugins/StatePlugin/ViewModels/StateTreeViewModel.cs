using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Services;
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

    private readonly ISelectedState _selectedState;
    
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

    public StateTreeViewModel(
        StateTreeViewRightClickService stateTreeViewRightClickService, 
        ISelectedState selectedState)
    {
        _selectedState = selectedState;
        _stateTreeViewRightClickService = stateTreeViewRightClickService;
        Categories = new ObservableCollection<CategoryViewModel>();
        States = new ObservableCollection<StateViewModel>();

        PropertyChanged += HandlePropertyChanged;
    }

    private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
    }

    #endregion

    bool IsPushingChangesToGum = true;

    private void HandleItemVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {

        if(e.PropertyName == nameof(StateTreeViewItem.IsSelected))
        {
            if (!IsPushingChangesToGum) return;

            if (sender is StateViewModel stateVm && stateVm.IsSelected == true)
            {
                _selectedState.SelectedStateSave = stateVm.Data;
                // No need to do this, we have events that do this for us:
                //_stateTreeViewRightClickService.PopulateMenuStrip();
            }
            else if(sender is CategoryViewModel categoryVm && categoryVm.IsSelected)
            {
                // If a state was selected, we need to deselect everything and forcefully select the state:
                if(_selectedState.SelectedStateSave != null)
                {
                    _selectedState.SelectedStateSave = null;
                    _selectedState.SelectedStateCategorySave = null;
                }
                _selectedState.SelectedStateCategorySave = categoryVm.Data;
                // I don't think we need to do this anymore because we can rely on the plugin to respond to events
                //_stateTreeViewRightClickService.PopulateMenuStrip();
            }
        }
        
    }

    #region Refresh

    public void RefreshTo(IStateContainer stateContainer, ISelectedState selectedState, ObjectFinder objectFinder)
    {
        if(stateContainer != null)
        {
            IsPushingChangesToGum = false;
            var expandedNodes = Items.Where(item => item.IsExpanded).ToList();

            RemoveUnnecessaryNodes(stateContainer);
            AddMissingItems(stateContainer, selectedState);
            FixNodeOrderInCategory(stateContainer);
            RefreshStateVmBackground(selectedState, objectFinder);

            ApplyExpanded(expandedNodes);

            IsPushingChangesToGum = true;

        }
        else
        {
            Categories.Clear();
            States.Clear();
        }
    }

    private void ApplyExpanded(List<StateTreeViewItem> expandedNodes)
    {
        foreach (var item in expandedNodes)
        {
            if (item is CategoryViewModel categoryViewModel)
            {
                var categoryName = categoryViewModel.Data.Name;

                var category = Categories.FirstOrDefault(item => item.Data.Name == categoryName);
                if (category != null)
                {
                    category.IsExpanded = true;
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
                var categoryVm = Categories[i];
                        // Remove this so they don't push any changes to Gum
                categoryVm.PropertyChanged -= HandleItemVmPropertyChanged;
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
                        // Remove this so they don't push any changes to Gum
                        stateViewModel.PropertyChanged -= HandleItemVmPropertyChanged;
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
                i--;
            }
        }
    }

    public void AddMissingItems(IStateContainer stateContainer, ISelectedState selectedState)
    {
        foreach (var category in stateContainer.Categories)
        {
            if (Categories.Any(item => item.Data == category) == false)
            {
                var categoryVm = new CategoryViewModel() { Data = category };
                categoryVm.PropertyChanged += HandleItemVmPropertyChanged;

                categoryVm.IsSelected = selectedState.SelectedStateSave == null && selectedState.SelectedStateCategorySave == category;

                Categories.Add(categoryVm);
            }
        }


        foreach (var state in stateContainer.UncategorizedStates)
        {
            if (States.Any(item => item.Data == state) == false)
            {
                var stateVm = new StateViewModel() { Data = state };
                stateVm.IsSelected = selectedState.SelectedStateSave == state;
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
                        stateVm.IsSelected = selectedState.SelectedStateSave == state;
                        stateVm.PropertyChanged += HandleItemVmPropertyChanged;
                        categoryViewModel.States.Add(stateVm);
                    }
                }
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

    internal void RefreshStateVmBackground(ISelectedState selectedState, ObjectFinder objectFinder)
    {
        var instance = selectedState.SelectedInstance;
        var behaviorReference = selectedState.SelectedBehaviorReference;

        var behavior = behaviorReference != null ?
            objectFinder.GetBehavior(behaviorReference) :
            null;

        foreach(var categoryVm in Categories)
        {
            var matchingBehaviorCategory = behavior?.Categories.FirstOrDefault(item => item.Name == categoryVm.Data.Name);
            categoryVm.IsRequiredBySelectedBehavior = 
                matchingBehaviorCategory != null;

            foreach (var stateVm in categoryVm.States)
            {
                var state = stateVm.Data;
                
                stateVm.IsRequiredBySelectedBehavior =
                    matchingBehaviorCategory?.States.Any(item => item.Name == state.Name) == true;

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

    #endregion

    #region Methods

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


    #endregion

    #region Rename

    internal void HandleRename(StateSave state)
    {
        var stateVm = Categories.SelectMany(item => item.States).FirstOrDefault(item => item.Data == state);

        stateVm?.ForceRefreshTitle();
        _stateTreeViewRightClickService.PopulateMenuStrip();
    }

    internal void HandleRename(StateSaveCategory category)
    {
        var categoryVm = Categories.FirstOrDefault(item => item.Data == category);
        categoryVm?.ForceRefreshTitle();
        _stateTreeViewRightClickService.PopulateMenuStrip();
    }

    #endregion
}
