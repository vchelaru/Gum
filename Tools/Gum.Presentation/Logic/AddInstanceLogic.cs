using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;

namespace Gum.Logic;

/// <inheritdoc cref="IAddInstanceLogic"/>
public class AddInstanceLogic : IAddInstanceLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IDialogService _dialogService;
    private readonly ICircularReferenceManager _circularReferenceManager;
    private readonly IUndoManager _undoManager;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly IAddDestinationTracker _addDestinationTracker;
    private readonly IWireframeObjectManager _wireframeObjectManager;

    public AddInstanceLogic(ISelectedState selectedState,
        IElementCommands elementCommands,
        IDialogService dialogService,
        ICircularReferenceManager circularReferenceManager,
        IUndoManager undoManager,
        ISetVariableLogic setVariableLogic,
        IAddDestinationTracker addDestinationTracker,
        IWireframeObjectManager wireframeObjectManager)
    {
        _wireframeObjectManager = wireframeObjectManager;
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _dialogService = dialogService;
        _circularReferenceManager = circularReferenceManager;
        _undoManager = undoManager;
        _setVariableLogic = setVariableLogic;
        _addDestinationTracker = addDestinationTracker;
    }

    /// <inheritdoc/>
    public InstanceSave? AddInstanceAtDestination(ElementSave elementToAdd, string? name = null)
    {
        object? container = _addDestinationTracker.Destination
            ?? (object?)_selectedState.SelectedInstance
            ?? _selectedState.SelectedElement;

        return AddInstance(elementToAdd, container, name);
    }

    /// <inheritdoc/>
    public InstanceSave? AddInstance(ElementSave elementToAdd, object? container, string? name = null, DropPosition? position = null)
    {
        if (GetErrorMessage(elementToAdd, container) is { } errorMessage)
        {
            _dialogService.ShowMessage(errorMessage);
            return null;
        }

        InstanceSave? newInstance = container switch
        {
            BehaviorSave behavior => AddToBehavior(elementToAdd, behavior, name),
            InstanceSave parent => AddToElement(elementToAdd, parent.ParentContainer, parent, name, position),
            _ => AddToElement(elementToAdd, (ElementSave)container!, null, name, position),
        };

        // After the add, so its selection of the new instance does not count as a user pick.
        _addDestinationTracker.Anchor(container);

        return newInstance;
    }

    private string? GetErrorMessage(ElementSave elementToAdd, object? container)
    {
        ElementSave? target = container switch
        {
            InstanceSave parent => parent.ParentContainer,
            ElementSave element => element,
            _ => null,
        };

        if (container is BehaviorSave)
        {
            return elementToAdd is ScreenSave
                ? "Screens cannot be added as required instances in behaviors"
                : null;
        }
        if (target == null)
        {
            return "No Screen or Component selected";
        }
        if (target is StandardElementSave)
        {
            return $"Standard type {target} cannot contain objects instances, so {elementToAdd} cannot be added here";
        }
        if (elementToAdd is ScreenSave)
        {
            return "Screens can't be added to other Screens or Components";
        }
        if (!_circularReferenceManager.CanTypeBeAddedToElement(target, elementToAdd.Name))
        {
            return $"Cannot add {elementToAdd.Name} to {target.Name} because it would create a circular reference";
        }
        if (target.IsSourceFileMissing)
        {
            return $"The source file for {target.Name} is missing, so it cannot be edited";
        }
        if (target == _selectedState.SelectedElement && _selectedState.SelectedStateSave != target.DefaultState)
        {
            return $"Cannot add instances to {target} while the {_selectedState.SelectedStateSave} state is selected. Select the Default state first.";
        }
        return null;
    }

    private InstanceSave? AddToElement(ElementSave elementToAdd, ElementSave target, InstanceSave? parent, string? name, DropPosition? position)
    {
        // One undo entry for the add and its parenting.
        using UndoLock undoLock = _undoManager.RequestLock();

        name ??= _elementCommands.GetUniqueNameForNewInstance(elementToAdd, target);

        // The target is selected first so plugins reacting to the add see it as the selected element
        // (a drag can start from another element's node).
        _selectedState.SelectedElement = target;

        string? parentName = parent == null ? null : GetParentName(parent);
        int? desiredIndex = position?.ResolveFlatIndex(target);

        InstanceSave? newInstance = _elementCommands.AddInstance(target, name, elementToAdd.Name, parentName, desiredIndex);

        if (newInstance != null && parentName != null)
        {
            _setVariableLogic.PropertyValueChanged("Parent", null, newInstance, target.DefaultState);
            // The add refreshes the wireframe before the Parent variable exists, so the child would
            // draw un-parented until the next refresh (#973).
            _wireframeObjectManager.RefreshAll(true, forceReloadTextures: false);
        }

        return newInstance;
    }

    private InstanceSave AddToBehavior(ElementSave elementToAdd, BehaviorSave behavior, string? name)
    {
        name ??= _elementCommands.GetUniqueNameForNewInstance(elementToAdd, behavior);

        // The undo snapshot is taken before any change; a lock held by a caller would block it later.
        _undoManager.RecordBehaviorState(behavior);

        _selectedState.SelectedBehavior = behavior;

        return _elementCommands.AddInstance(behavior, name, elementToAdd.Name);
    }

    /// <summary>
    /// The value of a child's Parent variable for <paramref name="parent"/>: the parent's name, plus
    /// its default child slot (e.g. a ScrollViewer's clip panel) when it has one.
    /// </summary>
    private string GetParentName(InstanceSave parent)
    {
        string defaultChild = ObjectFinder.Self.GetDefaultChildName(parent, _selectedState.SelectedStateSave);
        return string.IsNullOrEmpty(defaultChild) ? parent.Name : $"{parent.Name}.{defaultChild}";
    }
}
