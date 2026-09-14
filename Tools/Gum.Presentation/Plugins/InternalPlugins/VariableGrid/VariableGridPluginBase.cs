using System;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// The Variables tab plugin: refreshes <see cref="PropertyGridManager"/> in response to selection,
/// undo, variable, and element events. Each head exports a subclass (named
/// <c>MainVariableGridPlugin</c>) so MEF discovers it in that head's assembly; the subclasses add
/// nothing.
/// </summary>
public abstract class VariableGridPluginBase : PluginBase, IPriorityPlugin
{
    private readonly PropertyGridManager _propertyGridManager;
    private readonly IVariableReferenceLogic _variableReferenceLogic;
    private readonly ISelectedState _selectedState;
    private readonly VariableGridSelectionCoordinator _selectionCoordinator;

    /// <summary>Creates the plugin over the shared grid manager.</summary>
    protected VariableGridPluginBase(ISelectedState selectedState, PropertyGridManager propertyGridManager, IVariableReferenceLogic variableReferenceLogic)
    {
        _selectedState = selectedState;
        _propertyGridManager = propertyGridManager;
        _variableReferenceLogic = variableReferenceLogic;
        _selectionCoordinator = new VariableGridSelectionCoordinator();
        GumExpressionService.Initialize();
    }

    /// <inheritdoc/>
    public override string FriendlyName => GetType().Name;

    /// <inheritdoc/>
    public override Version Version => new Version();

    /// <inheritdoc/>
    public override bool ShutDown(PluginShutDownReason shutDownReason) => false;

    /// <inheritdoc/>
    public override void StartUp()
    {
        AssignEvents();
    }

    private void AssignEvents()
    {
        this.TreeNodeSelected += HandleTreeNodeSelected;
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.ReactToStateSaveCategorySelected += HandleStateSaveCategorySelected;
        this.StateMovedToCategory += HandleStateMovedToCategory;
        this.InstanceSelected += HandleInstanceSelected;
        this.ElementSelected += HandleElementSelected;
        this.ElementDelete += HandleElementDeleted;
        this.ElementRename += HandleElementRenamed;
        this.BehaviorSelected += HandleBehaviorSelected;
        this.RefreshVariableView += HandleRefreshVariableView;
        this.AfterUndo += HandleAfterUndo;
        this.VariableSet += HandleVariableSet;
        this.FocusVariableFilter += HandleFocusVariableFilter;
    }

    private void HandleFocusVariableFilter()
    {
        _propertyGridManager.FocusVariableFilter();
    }

    private void HandleElementRenamed(ElementSave save, string oldName)
    {
        _propertyGridManager.RefreshVariablesDataGridValues();
    }

    private void HandleVariableSet(ElementSave element, InstanceSave? instance, string strippedName, object? oldValue)
    {
        _propertyGridManager.HandleVariableSet(element, instance, strippedName, oldValue);
    }

    private void HandleElementDeleted(ElementSave save)
    {
        _propertyGridManager.RefreshEntireGrid(force: false);
    }

    private void HandleAfterUndo()
    {
        // An undo can result in variables added or removed, so do a full refresh.
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleElementSelected(ElementSave? save)
    {
        _selectionCoordinator.Reset();
        // Selecting an element also selects a state, and the state refresh covers the grid.
    }

    private void HandleBehaviorSelected(BehaviorSave? save)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        // Auto-selecting a new instance (e.g. right-click Add Object on an already-selected
        // Screen) only raises InstanceSelected, not TreeNodeSelected - see issue #4067.
        _propertyGridManager.VariableViewModel.IsAddVariableButtonVisible =
            AddVariableButtonVisibilityLogic.ShouldShow(_selectedState);

        if (!_selectionCoordinator.ShouldRefreshOnInstanceSelected(instance))
        {
            // HandleStateSelected already refreshed the grid for this instance
            // during the synchronous selection cascade.
            return;
        }

        // Not forced: when an instance is selected in a new component, the state and instance are
        // both selected, and forcing here would refresh the instance twice.
        _propertyGridManager.RefreshEntireGrid(force: false);
    }

    private void HandleStateSaveCategorySelected(StateSaveCategory? category)
    {
        _propertyGridManager.RefreshEntireGrid(force: true);
    }

    private void HandleStateMovedToCategory(StateSave state, StateSaveCategory newCategory, StateSaveCategory oldCategory)
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
        // match and will refresh - see issue #2615.
        _selectionCoordinator.OnStateRefreshed(_selectedState.SelectedInstance);
    }

    private void HandleTreeNodeSelected(ITreeNode? node)
    {
        _selectionCoordinator.Reset();
        _propertyGridManager.VariableViewModel.IsAddVariableButtonVisible =
            AddVariableButtonVisibilityLogic.ShouldShow(_selectedState);

        if (_selectedState.SelectedBehavior == null && _selectedState.SelectedInstance == null && _selectedState.SelectedElement == null)
        {
            _propertyGridManager.RefreshEntireGrid(force: true);
        }
    }

    private void HandleRefreshVariableView(bool force)
    {
        _propertyGridManager.RefreshEntireGrid(force);
    }
}

/// <summary>
/// Wires the VariableExcluded/VariableSet plugin events to <see cref="ExclusionsLogic"/>, which owns
/// the variable-exclusion decisions. Each head exports a subclass (named <c>ExclusionsPlugin</c>).
/// </summary>
public abstract class ExclusionsPluginBase : PluginBase, IPriorityPlugin
{
    private readonly ExclusionsLogic _logic;

    /// <summary>Creates the plugin.</summary>
    protected ExclusionsPluginBase(ISelectedState selectedState, Commands.IGuiCommands guiCommands)
    {
        _logic = new ExclusionsLogic(selectedState, guiCommands);
    }

    /// <inheritdoc/>
    public override string FriendlyName => GetType().Name;

    /// <inheritdoc/>
    public override Version Version => new Version();

    /// <inheritdoc/>
    public override bool ShutDown(PluginShutDownReason shutDownReason) => false;

    /// <inheritdoc/>
    public override void StartUp()
    {
        this.VariableExcluded += _logic.GetIfVariableIsExcluded;
        this.VariableSet += _logic.HandleVariableSet;
    }
}
