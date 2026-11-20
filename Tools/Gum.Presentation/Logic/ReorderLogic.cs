using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices.Editing;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;

namespace Gum.Logic;

public class ReorderLogic : IReorderLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IPluginManager _pluginManager;

    public ReorderLogic(ISelectedState selectedState,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IPluginManager pluginManager)
    {
        _selectedState = selectedState;
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _pluginManager = pluginManager;
    }
    
    public void MoveSelectedInstanceForward()
    {
        var instance = _selectedState.SelectedInstance;

        if (instance != null)
        {
            var siblingInstances = instance.GetSiblingsIncludingThis();
            var thisIndex = siblingInstances.IndexOf(instance);
            bool isLast = thisIndex == siblingInstances.Count - 1;

            if (!isLast)
            {
                using (_undoManager.RequestLock())
                {
                    Move(instance, ReorderPlacement.After, siblingInstances[thisIndex + 1]);
                    RefreshInResponseToReorder(instance);
                }
            }
        }
    }

    public void MoveSelectedInstanceBackward()
    {
        var instance = _selectedState.SelectedInstance;

        if (instance != null)
        {
            var siblingInstances = instance.GetSiblingsIncludingThis();
            var thisIndex = siblingInstances.IndexOf(instance);
            bool isFirst = thisIndex == 0;

            if (!isFirst)
            {
                using (_undoManager.RequestLock())
                {
                    Move(instance, ReorderPlacement.Before, siblingInstances[thisIndex - 1]);
                    RefreshInResponseToReorder(instance);
                }
            }
        }
    }

    public void MoveSelectedInstanceToFront()
    {
        var instance = _selectedState.SelectedInstance;

        if (instance != null)
        {
            using (_undoManager.RequestLock())
            {
                Move(instance, ReorderPlacement.ToEnd, null);
                RefreshInResponseToReorder(instance);
            }
        }
    }

    public void MoveSelectedInstanceToBack()
    {
        var instance = _selectedState.SelectedInstance;

        if (instance != null)
        {
            using (_undoManager.RequestLock())
            {
                Move(instance, ReorderPlacement.ToStart, null);
                RefreshInResponseToReorder(instance);
            }
        }
    }

    public void MoveSelectedInstanceInFrontOf(InstanceSave whatToMoveInFrontOf)
    {
        var whatToInsert = _selectedState.SelectedInstance;
        if (whatToInsert != null)
        {
            using (_undoManager.RequestLock())
            {
                Move(whatToInsert, ReorderPlacement.After, whatToMoveInFrontOf);
                RefreshInResponseToReorder(whatToMoveInFrontOf);
            }
        }
    }

    /// <summary>
    /// Moves the instance within the selected element's instances, or the selected behavior's required instances.
    /// </summary>
    private void Move(InstanceSave instance, ReorderPlacement placement, InstanceSave? anchor)
    {
        var element = _selectedState.SelectedElement;
        var behavior = _selectedState.SelectedBehavior;
        if (element != null)
        {
            ListMover.Move(element.Instances, instance, placement, anchor);
        }
        else if (behavior != null && instance is BehaviorInstanceSave behaviorInstance)
        {
            ListMover.Move(behavior.RequiredInstances, behaviorInstance, placement, anchor as BehaviorInstanceSave);
        }
    }

    public void RefreshInResponseToReorder(InstanceSave instance)
    {
        var instanceContainer = _selectedState.SelectedInstanceContainer;
        if(instanceContainer != null)
        {
            _guiCommands.RefreshElementTreeView(instanceContainer);
        }

        _fileCommands.TryAutoSaveCurrentObject();

        _pluginManager.InstanceReordered(instance);
    }
}
