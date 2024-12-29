using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.Plugins.InternalPlugins.StatePlugin.Views;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;

namespace Gum.Plugins.StatePlugin;

// This is new as of Oct 30, 2020
// I'd like to move all state logic to this plugin over time.
[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
public class MainStatePlugin : InternalPlugin
{
    #region Fields/Properties

    StateTreeView stateTreeView;
    StateTreeViewModel stateTreeViewModel;

    PluginTab newPluginTab;

    StateTreeViewRightClickService _stateTreeViewRightClickService;
    private HotkeyManager _hotkeyManager;
    private readonly ISelectedState _selectedState;

    #endregion

    #region Initialize

    public MainStatePlugin()
    {
        _stateTreeViewRightClickService = new StateTreeViewRightClickService(GumState.Self.SelectedState);
        _hotkeyManager = HotkeyManager.Self;
        _selectedState = GumState.Self.SelectedState;
    }

    public override void StartUp()
    {
        AssignEvents();

        stateTreeViewModel = new StateTreeViewModel(_stateTreeViewRightClickService);

        CreateNewStateTab();

        // State Tree ViewManager needs init before MenuStripManager
        StateTreeViewManager.Self.Initialize(
            _stateTreeViewRightClickService,
            _hotkeyManager);
    }

    private void AssignEvents()
    {
        this.StateWindowTreeNodeSelected += HandleStateSelected;
        this.TreeNodeSelected += HandleTreeNodeSelected;
        this.RefreshStateTreeView += HandleRefreshStateTreeView;
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.ReactToStateSaveCategorySelected += HandleStateSaveCategorySelected;
        this.StateRename += HandleStateRename;
        this.CategoryRename += HandleCategoryRename;
        this.BehaviorSelected += HandleBehaviorSelected;
        this.InstanceSelected += HandleInstanceSelected;
        this.ElementSelected += HandleElementSelected;
        this.StateMovedToCategory += HandleStateMovedToCategory;
        this.VariableSet += HandleVariableSet;
    }

    private void CreateNewStateTab()
    {
        stateTreeView = new StateTreeView(stateTreeViewModel, _stateTreeViewRightClickService, _hotkeyManager, _selectedState);
        _stateTreeViewRightClickService.NewMenuStrip = stateTreeView.TreeViewContextMenu;
        newPluginTab = GumCommands.Self.GuiCommands.AddControl(stateTreeView, "States", TabLocation.CenterTop);
    }

    #endregion

    #region Event Handlers

    private void HandleStateMovedToCategory(StateSave stateSave, StateSaveCategory newCategory, StateSaveCategory oldCategory)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();
        stateTreeViewModel.RefreshTo(GumState.Self.SelectedState.SelectedStateContainer, GumState.Self.SelectedState);
    }

    private void HandleElementSelected(ElementSave save)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();
        HandleRefreshStateTreeView();
        RefreshTabHeaders();
    }

    private void HandleInstanceSelected(ElementSave save1, InstanceSave save2)
    {
        RefreshUI(SelectedState.Self.SelectedStateContainer, SelectedState.Self);

        // A user could directly select an instance in
        // a different container such as going from a component
        // to a selected instance in a behavior. In that case we
        // still want to refresh the menu items.
        _stateTreeViewRightClickService.PopulateMenuStrip();
    }

    private void HandleBehaviorSelected(BehaviorSave behavior)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();
        HandleRefreshStateTreeView();
    }

    private void HandleCategoryRename(StateSaveCategory category, string arg2)
    {
        stateTreeViewModel.HandleRename(category);
    }

    private void HandleStateRename(StateSave save, string oldName)
    {
        stateTreeViewModel.HandleRename(save);
    }

    private void HandleStateSelected(StateSave state)
    {
        stateTreeViewModel.SetSelectedState(state);
    }

    private void HandleStateSaveCategorySelected(StateSaveCategory stateSaveCategory)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();

        stateTreeViewModel.SetSelectedStateSaveCategory(stateSaveCategory);
    }

    private void HandleRefreshStateTreeView()
    {
        RefreshUI(SelectedState.Self.SelectedStateContainer, SelectedState.Self);
    }

    private void HandleTreeNodeSelected(TreeNode node)
    {
        RefreshTabHeaders();

        var selectedState = SelectedState.Self;
        if (selectedState.SelectedBehavior == null &&
            selectedState.SelectedElement == null)
        {

            _stateTreeViewRightClickService.PopulateMenuStrip();
            HandleRefreshStateTreeView();
        }
    }

    private ISelectedState RefreshTabHeaders()
    {
        var selectedState = SelectedState.Self;
        var element = selectedState.SelectedElement;
        string desiredTitle = "States";
        if (element != null)
        {
            desiredTitle = $"{element.Name} States";
        }

        newPluginTab.Title = desiredTitle;
        return selectedState;
    }

    private void HandleStateSelected(TreeNode stateTreeNode)
    {
        var currentCategory = SelectedState.Self.SelectedStateCategorySave;
        var currentState = SelectedState.Self.SelectedStateSave;

        if (currentCategory != null && currentState != null)
        {
            PropagateVariableForCategorizedState(currentState);
        }
        else if (currentCategory != null)
        {
            foreach (var state in currentCategory.States)
            {
                PropagateVariableForCategorizedState(state);
            }
        }


    }

    private void HandleVariableSet(ElementSave elementSave, InstanceSave instance, string variableName, object oldValue)
    {
        // Do this to refresh the yellow highlights - We may not need to do more than this:
        stateTreeViewModel.RefreshTo(elementSave, GumState.Self.SelectedState);
    }
    #endregion

    private void PropagateVariableForCategorizedState(StateSave currentState)
    {
        foreach (var variable in currentState.Variables)
        {
            VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(variable.Name, 
                GumState.Self.SelectedState.SelectedElement, GumState.Self.SelectedState.SelectedStateCategorySave);
        }
    }

    IStateContainer mLastElementRefreshedTo;
    void RefreshUI(IStateContainer stateContainer, ISelectedState selectedState)
    {

        bool changed = stateContainer != mLastElementRefreshedTo;

        mLastElementRefreshedTo = stateContainer;

        StateSave lastStateSave = SelectedState.Self.SelectedStateSave;
        InstanceSave instance = SelectedState.Self.SelectedInstance;

        stateTreeViewModel.RefreshTo(stateContainer, selectedState);
    }

}
