using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;

namespace Gum.Logic;

public class ReorderLogic
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
                    var element = _selectedState.SelectedElement;
                    var behavior = _selectedState.SelectedBehavior;
                    var nextSibling = siblingInstances[thisIndex + 1];
                    if(element != null)
                    {
                        // remove it before getting the new index, or else the removal could impact the
                        // index.
                        element.Instances.Remove(instance);
                        var nextSiblingIndexInContainer = element.Instances.IndexOf(nextSibling);
                        element.Instances.Insert(nextSiblingIndexInContainer + 1, instance);
                    }
                    else if(behavior != null && instance is BehaviorInstanceSave behaviorInstance)
                    {
                        behavior.RequiredInstances.Remove(behaviorInstance);
                        var nextSiblingIndexInContainer = behavior.RequiredInstances.IndexOf(nextSibling as BehaviorInstanceSave);
                        behavior.RequiredInstances.Insert(nextSiblingIndexInContainer + 1, behaviorInstance);
                    }
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
            // remove it before getting the new index, or else the removal could impact the
            // index.
            var siblingInstances = instance.GetSiblingsIncludingThis();
            var thisIndex = siblingInstances.IndexOf(instance);
            bool isFirst = thisIndex == 0;

            if (!isFirst)
            {
                using (_undoManager.RequestLock())
                {
                    var element = _selectedState.SelectedElement;
                    var behavior = _selectedState.SelectedBehavior;
                    var previousSibling = siblingInstances[thisIndex - 1];

                    if(element != null)
                    {
                        element.Instances.Remove(instance);
                        var previousSiblingIndexInContainer = element.Instances.IndexOf(previousSibling);
                        element.Instances.Insert(previousSiblingIndexInContainer, instance);
                    }
                    else if(behavior != null && instance is BehaviorInstanceSave behaviorInstance)
                    {
                        behavior.RequiredInstances.Remove(behaviorInstance);
                        var previousSiblingIndexInContainer = behavior.RequiredInstances.IndexOf(previousSibling as BehaviorInstanceSave);
                        behavior.RequiredInstances.Insert(previousSiblingIndexInContainer, behaviorInstance);
                    }

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
                var element = _selectedState.SelectedElement;
                var behavior = _selectedState.SelectedBehavior;
                if(element != null)
                {
                    // to bring to back, we're going to remove, then add (at the end)
                    element.Instances.Remove(instance);
                    element.Instances.Add(instance);
                }
                else if(behavior != null && instance is BehaviorInstanceSave behaviorInstance)
                {
                    behavior.RequiredInstances.Remove(behaviorInstance);
                    behavior.RequiredInstances.Add(behaviorInstance);
                }

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
                var element = _selectedState.SelectedElement;
                if(element != null)
                {
                    // to bring to back, we're going to remove, then insert at index 0
                    element.Instances.Remove(instance);
                    element.Instances.Insert(0, instance);
                }
                else if(_selectedState.SelectedBehavior is BehaviorSave behavior && instance is BehaviorInstanceSave behaviorInstance)
                {
                    behavior.RequiredInstances.Remove(behaviorInstance);
                    behavior.RequiredInstances.Insert(0, behaviorInstance);
                }

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
                var element = _selectedState.SelectedElement;
                var behavior = _selectedState.SelectedBehavior;
                if (element != null)
                {
                    element.Instances.Remove(whatToInsert);
                    int whereToInsert = element.Instances.IndexOf(whatToMoveInFrontOf) + 1;
                    element.Instances.Insert(whereToInsert, whatToInsert);
                    RefreshInResponseToReorder(whatToMoveInFrontOf);
                }
                else if(behavior != null && whatToInsert is BehaviorInstanceSave behaviorInstance)
                {
                    behavior.RequiredInstances.Remove(behaviorInstance);
                    int whereToInsert = behavior.RequiredInstances.IndexOf(whatToMoveInFrontOf as BehaviorInstanceSave) + 1;
                    behavior.RequiredInstances.Insert(whereToInsert, behaviorInstance);
                    RefreshInResponseToReorder(whatToMoveInFrontOf);
                }
            }
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
