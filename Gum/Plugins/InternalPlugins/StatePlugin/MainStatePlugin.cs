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
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.Logic;

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
    private readonly StateTreeViewRightClickService _stateTreeViewRightClickService;
    private readonly HotkeyManager _hotkeyManager;
    private readonly ISelectedState _selectedState;
    private readonly ObjectFinder _objectFinder;
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;
    private readonly CopyPasteLogic _copyPasteLogic;

    #endregion

    #region Initialize

    [ImportingConstructor]
    public MainStatePlugin(ISelectedState selectedState)
    {
        _selectedState = selectedState;
        var elementCommands = Locator.GetRequiredService<IElementCommands>();
        var editCommands = Locator.GetRequiredService<IEditCommands>();
        var dialogService = Locator.GetRequiredService<IDialogService>();
        _stateTreeViewRightClickService = new StateTreeViewRightClickService(
            _selectedState, 
            elementCommands, 
            editCommands, 
            dialogService, 
            _guiCommands, 
            _fileCommands);
        _hotkeyManager = Locator.GetRequiredService<HotkeyManager>();
        _objectFinder = ObjectFinder.Self;
        _variableInCategoryPropagationLogic = Locator.GetRequiredService<IVariableInCategoryPropagationLogic>();
        _copyPasteLogic = Locator.GetRequiredService<CopyPasteLogic>();

        stateTreeViewModel = new StateTreeViewModel(_stateTreeViewRightClickService,
            selectedState);
    }

    public override void StartUp()
    {
        AssignEvents();

        CreateNewStateTab();

        // State Tree ViewManager needs init before MenuStripManager
        StateTreeViewManager.Self.Initialize(
            _stateTreeViewRightClickService,
            _hotkeyManager);
    }

    private void AssignEvents()
    {
        this.TreeNodeSelected += HandleTreeNodeSelected;
        
        this.RefreshStateTreeView += HandleRefreshStateTreeView;
        
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.ReactToStateSaveCategorySelected += HandleStateSaveCategorySelected;
        
        this.StateRename += HandleStateRename;
        this.StateDelete += HandleStateDelete;
        this.StateMovedToCategory += HandleStateMovedToCategory;

        this.CategoryRename += HandleCategoryRename;
        this.BehaviorSelected += HandleBehaviorSelected;
        this.BehaviorReferenceSelected += HandleBehaviorReferenceSelected;
        this.InstanceSelected += HandleInstanceSelected;
        this.ElementSelected += HandleElementSelected;
        this.ElementDelete += HandleElementDeleted;
        this.VariableSet += HandleVariableSet;
    }


    private void HandleElementDeleted(ElementSave save)
    {
        RefreshUI(_selectedState.SelectedStateContainer);
    }

    private void CreateNewStateTab()
    {
        stateTreeView = new StateTreeView(
            stateTreeViewModel, 
            _stateTreeViewRightClickService, 
            _hotkeyManager, 
            _selectedState,
            _copyPasteLogic);
        _stateTreeViewRightClickService.SetMenuStrip(stateTreeView.TreeViewContextMenu, stateTreeView);
        
        newPluginTab = _tabManager.AddControl(stateTreeView, "States", TabLocation.CenterTop);
    }

    #endregion

    #region Event Handlers

    private void HandleStateMovedToCategory(StateSave stateSave, StateSaveCategory newCategory, StateSaveCategory oldCategory)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();
        stateTreeViewModel.RefreshTo(_selectedState.SelectedStateContainer, _selectedState, _objectFinder);
    }

    private void HandleElementSelected(ElementSave save)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();
        HandleRefreshStateTreeView();
        RefreshTabHeaders();
    }

    private void HandleInstanceSelected(ElementSave save1, InstanceSave save2)
    {
        RefreshUI(_selectedState.SelectedStateContainer);

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

    private void HandleBehaviorReferenceSelected(ElementBehaviorReference reference, ElementSave element)
    {
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


    private void HandleStateDelete(StateSave save)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();
        stateTreeViewModel.RefreshTo(_selectedState.SelectedStateContainer, _selectedState, _objectFinder);
    }

    private void HandleStateSelected(StateSave state)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();
        stateTreeViewModel.SetSelectedState(state);
        var currentCategory = _selectedState.SelectedStateCategorySave;
        var currentState = _selectedState.SelectedStateSave;

        if (currentCategory != null && currentState != null)
        {
            PropagateVariableForCategorizedState(currentState);
        }
        else if (currentCategory != null)
        {
            foreach (var item in currentCategory.States)
            {
                PropagateVariableForCategorizedState(item);
            }
        }
    }

    private void HandleStateSaveCategorySelected(StateSaveCategory stateSaveCategory)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();
        stateTreeViewModel.SetSelectedStateSaveCategory(stateSaveCategory);
    }

    private void HandleRefreshStateTreeView()
    {
        RefreshUI(_selectedState.SelectedStateContainer);
    }

    private void HandleTreeNodeSelected(TreeNode node)
    {
        RefreshTabHeaders();
        
        if (_selectedState.SelectedBehavior == null &&
            _selectedState.SelectedElement == null)
        {

            _stateTreeViewRightClickService.PopulateMenuStrip();
            HandleRefreshStateTreeView();
        }
    }

    private void RefreshTabHeaders()
    {
        var element = _selectedState.SelectedElement;
        string desiredTitle = "States";
        if (element != null)
        {
            desiredTitle = $"{element.Name} States";
        }

        newPluginTab.Title = desiredTitle;
    }

    private void HandleVariableSet(ElementSave elementSave, InstanceSave instance, string variableName, object oldValue)
    {
        // Do this to refresh the yellow highlights - We may not need to do more than this:
        stateTreeViewModel.RefreshTo(elementSave, _selectedState, _objectFinder);
    }
    #endregion

    private void PropagateVariableForCategorizedState(StateSave currentState)
    {
        foreach (var variable in currentState.Variables)
        {
            _variableInCategoryPropagationLogic.PropagateVariablesInCategory(variable.Name,
                _selectedState.SelectedElement, _selectedState.SelectedStateCategorySave);
        }
    }

    void RefreshUI(IStateContainer stateContainer)
    {
        stateTreeViewModel.RefreshTo(stateContainer, _selectedState, _objectFinder);
    }

}
