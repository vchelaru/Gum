using Gum.Commands;
using Gum.Expressions;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Services;

using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using GumRuntime;
using HarfBuzzSharp;
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

[Export(typeof(PluginBase))]
public class MainVariableGridPlugin : PriorityPlugin
{
    PropertyGridManager _propertyGridManager;
    private readonly IVariableReferenceLogic _variableReferenceLogic;
    private readonly ISelectedState _selectedState;
    private readonly VariableGridSelectionCoordinator _selectionCoordinator = new();

    public MainVariableGridPlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _propertyGridManager = PropertyGridManager.Self;
        _variableReferenceLogic = Locator.GetRequiredService<IVariableReferenceLogic>();
        GumExpressionService.Initialize();
    }

    public override void StartUp()
    {
        AssignEvents();
    }

    private void AssignEvents()
    {
        this.TreeNodeSelected += HandleTreeNodeSelected;
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.ReactToStateSaveCategorySelected += MainVariableGridPlugin_ReactToStateSaveCategorySelected;
        this.ReactToCustomStateSaveSelected += HandleCustomStateSelected;
        this.StateMovedToCategory += HandleStateMovedToCategory;
        this.InstanceSelected += HandleInstanceSelected;
        this.ElementSelected += HandleElementSelected;
        this.ElementDelete += HandleElementDeleted;
        this.ElementRename += HandleElementRenamed;
        this.BehaviorSelected += HandleBehaviorSelected;
        this.VariableSelected += HandleVariableSelected;
        this.RefreshVariableView += HandleRefreshVariableView;
        this.AfterUndo += HandleAfterUndo;
        this.VariableSet += HandleVariableSet;
    }

    private void HandleElementRenamed(ElementSave save, string arg2)
    {
        _propertyGridManager.RefreshVariablesDataGridValues();
    }

    private void HandleVariableSet(ElementSave element, InstanceSave? instance, string strippedName, object? oldValue)
    {
        _propertyGridManager.HandleVariableSet(element, instance, strippedName, oldValue);
    }

    private void HandleElementDeleted(ElementSave save)
    {
        _propertyGridManager.RefreshEntireGrid(force:false);
    }

    private void HandleAfterUndo()
    {
        // An undo can result in variables added or removed, so let's
        // do a full refresh
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleVariableSelected(IStateContainer container, VariableSave save)
    {

    }

    private void HandleElementSelected(ElementSave? save)
    {
        _selectionCoordinator.Reset();
        // when an element is selected, so is a state. States
        // also refresh the grid so we don't need to also refresh
        // here.
        //_propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleBehaviorSelected(BehaviorSave? save)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleInstanceSelected(ElementSave save1, InstanceSave save2)
    {
        if (!_selectionCoordinator.ShouldRefreshOnInstanceSelected(save2))
        {
            // HandleStateSelected already refreshed the grid for this instance
            // during the synchronous selection cascade — no need to refresh again.
            return;
        }

        _propertyGridManager.RefreshEntireGrid(
            // When an instance is selected in a new component, the state and instance are both
            // selected. Don't force it here because if so, it forces a double select on the instance
            // which is raised after the state.
            force: false);
    }

    private void MainVariableGridPlugin_ReactToStateSaveCategorySelected(StateSaveCategory? obj)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);

    }

    private void HandleStateMovedToCategory(StateSave save, StateSaveCategory category1, StateSaveCategory category2)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleStateSelected(StateSave? save)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
        // Record which instance the grid was just refreshed for, so that if
        // HandleInstanceSelected fires next in the same synchronous cascade for
        // that same instance it can skip its refresh. A later instance change
        // (e.g. user clicks a sibling instance after a state-pane click) won't
        // match and will refresh — see issue #2615.
        _selectionCoordinator.OnStateRefreshed(_selectedState.SelectedInstance);
    }

    private void HandleCustomStateSelected(StateSave? save)
    {
        // custom states are states where an animation is playing. This slows down
        // the animation considerably so let's not do it:
        //PropertyGridManager.Self.RefreshVariablesDataGridValues();
    }

    private void HandleTreeNodeSelected(TreeNode? node)
    {
        _selectionCoordinator.Reset();
        var selectedState = _selectedState;
        var shouldShowButton = (selectedState.SelectedBehavior != null ||
            selectedState.SelectedComponent != null ||
            selectedState.SelectedScreen != null);
        if(shouldShowButton)
        {
            shouldShowButton = _selectedState.SelectedInstance == null;
        }
        _propertyGridManager.VariableViewModel.AddVariableButtonVisibility =
            shouldShowButton.ToVisibility();

        if(selectedState.SelectedBehavior == null && selectedState.SelectedInstance == null && selectedState.SelectedElement == null)
        {
            _propertyGridManager.RefreshEntireGrid(force: true);
        }
    }

    private void HandleRefreshVariableView(bool force)
    {
        _propertyGridManager.RefreshEntireGrid(force);
    }
}
