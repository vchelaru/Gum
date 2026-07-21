using System;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;

namespace Gum.Managers;

/// <summary>
/// Owns the States tab's WPF-free reactions to selection/rename/delete/variable-set events and the
/// tab title text. Extracted from <c>MainStatePlugin</c> (issue #3926): none of this touches a WPF
/// type, but every method here used to be an instance method on the plugin itself, reading the
/// plugin's own private fields rather than taking its dependencies as constructor parameters, which
/// is what blocked the extraction until now (the same shape as <c>AnimationTabController</c>,
/// issue #3866).
///
/// <para>
/// The plugin still owns the real platform glue this class deliberately has no seam for:
/// constructing the WPF <c>StateTreeView</c> and registering it with the tab manager, and pushing
/// <see cref="TabTitleChanged"/>'s title onto the WPF-typed <c>PluginTab.Title</c>.
/// </para>
/// </summary>
public class StateTreeController
{
    private readonly IStateTreeViewRightClickService _rightClickService;
    private readonly ISelectedState _selectedState;
    private readonly ObjectFinder _objectFinder;
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;

    /// <summary>
    /// The states tree's view model. Constructed here (rather than by the plugin) since its only
    /// dependencies are this controller's own <see cref="IStateTreeViewRightClickService"/> and
    /// <see cref="ISelectedState"/>.
    /// </summary>
    public StateTreeViewModel ViewModel { get; }

    /// <summary>
    /// Raised whenever the desired States tab title changes (selected element changed), so the
    /// plugin can push it onto the WPF-typed <c>PluginTab.Title</c> - a real platform concern this
    /// class has no seam for.
    /// </summary>
    public event Action<string>? TabTitleChanged;

    public StateTreeController(
        IStateTreeViewRightClickService rightClickService,
        ISelectedState selectedState,
        ObjectFinder objectFinder,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic)
    {
        _rightClickService = rightClickService;
        _selectedState = selectedState;
        _objectFinder = objectFinder;
        _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;

        ViewModel = new StateTreeViewModel(rightClickService, selectedState);
    }

    public void HandleElementDeleted(ElementSave save) => RefreshUI(_selectedState.SelectedStateContainer);

    public void HandleStateMovedToCategory(StateSave stateSave, StateSaveCategory newCategory, StateSaveCategory oldCategory)
    {
        _rightClickService.PopulateContextMenu();
        ViewModel.RefreshTo(_selectedState.SelectedStateContainer, _selectedState, _objectFinder);
    }

    public void HandleElementSelected(ElementSave? save)
    {
        _rightClickService.PopulateContextMenu();
        HandleRefreshStateTreeView();
        RefreshTabHeaders();
    }

    public void HandleInstanceSelected(ElementSave save1, InstanceSave save2)
    {
        RefreshUI(_selectedState.SelectedStateContainer);

        // A user could directly select an instance in
        // a different container such as going from a component
        // to a selected instance in a behavior. In that case we
        // still want to refresh the menu items.
        _rightClickService.PopulateContextMenu();
    }

    public void HandleBehaviorSelected(BehaviorSave? behavior)
    {
        _rightClickService.PopulateContextMenu();
        HandleRefreshStateTreeView();
    }

    public void HandleBehaviorReferenceSelected(ElementBehaviorReference reference, ElementSave element)
    {
        HandleRefreshStateTreeView();
    }

    public void HandleCategoryRename(StateSaveCategory category, string oldName)
    {
        ViewModel.HandleRename(category);
    }

    public void HandleStateRename(StateSave save, string oldName)
    {
        ViewModel.HandleRename(save);
    }

    public void HandleStateDelete(StateSave save)
    {
        _rightClickService.PopulateContextMenu();
        ViewModel.RefreshTo(_selectedState.SelectedStateContainer, _selectedState, _objectFinder);
    }

    public void HandleStateSelected(StateSave? state)
    {
        _rightClickService.PopulateContextMenu();
        ViewModel.SetSelectedState(state);
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

    public void HandleStateSaveCategorySelected(StateSaveCategory? stateSaveCategory)
    {
        _rightClickService.PopulateContextMenu();
        ViewModel.SetSelectedStateSaveCategory(stateSaveCategory);
    }

    public void HandleRefreshStateTreeView()
    {
        RefreshUI(_selectedState.SelectedStateContainer);
    }

    public void HandleTreeNodeSelected()
    {
        RefreshTabHeaders();

        if (_selectedState.SelectedBehavior == null &&
            _selectedState.SelectedElement == null)
        {
            _rightClickService.PopulateContextMenu();
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

        TabTitleChanged?.Invoke(desiredTitle);
    }

    public void HandleVariableSet(ElementSave elementSave, InstanceSave? instance, string variableName, object? oldValue)
    {
        // Do this to refresh the yellow highlights - We may not need to do more than this:
        ViewModel.RefreshTo(elementSave, _selectedState, _objectFinder);
    }

    private void PropagateVariableForCategorizedState(StateSave currentState)
    {
        foreach (var variable in currentState.Variables)
        {
            _variableInCategoryPropagationLogic.PropagateVariablesInCategory(variable.Name,
                _selectedState.SelectedElement, _selectedState.SelectedStateCategorySave);
        }
    }

    private void RefreshUI(IStateContainer stateContainer)
    {
        ViewModel.RefreshTo(stateContainer, _selectedState, _objectFinder);
    }
}
