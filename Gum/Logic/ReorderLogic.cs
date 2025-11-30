using Gum.Commands;
using Gum.DataTypes;
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

    public ReorderLogic(ISelectedState selectedState,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands)
    {
        _selectedState = _selectedState;
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
    }
    
    public void MoveSelectedInstanceForward()
    {
        var instance = _selectedState.SelectedInstance;
        var element = _selectedState.SelectedElement;

        if (instance != null)
        {
            var siblingInstances = instance.GetSiblingsIncludingThis();
            var thisIndex = siblingInstances.IndexOf(instance);
            bool isLast = thisIndex == siblingInstances.Count - 1;

            if (!isLast)
            {
                using (_undoManager.RequestLock())
                {

                    // remove it before getting the new index, or else the removal could impact the
                    // index.
                    element.Instances.Remove(instance);
                    var nextSibling = siblingInstances[thisIndex + 1];

                    var nextSiblingIndexInContainer = element.Instances.IndexOf(nextSibling);

                    element.Instances.Insert(nextSiblingIndexInContainer + 1, instance);
                    RefreshInResponseToReorder(instance);
                }
            }
        }
    }

    public void MoveSelectedInstanceBackward()
    {
        var instance = _selectedState.SelectedInstance;
        var element = _selectedState.SelectedElement;

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

                    element.Instances.Remove(instance);
                    var previousSibling = siblingInstances[thisIndex - 1];

                    var previousSiblingIndexInContainer = element.Instances.IndexOf(previousSibling);

                    element.Instances.Insert(previousSiblingIndexInContainer, instance);
                    RefreshInResponseToReorder(instance);
                }
            }
        }
    }

    public void MoveSelectedInstanceToFront()
    {
        InstanceSave instance = _selectedState.SelectedInstance;
        ElementSave element = _selectedState.SelectedElement;

        if (instance != null)
        {
            using (_undoManager.RequestLock())
            {

                // to bring to back, we're going to remove, then add (at the end)
                element.Instances.Remove(instance);
                element.Instances.Add(instance);

                RefreshInResponseToReorder(instance);
            }
        }
    }

    public void MoveSelectedInstanceToBack()
    {
        InstanceSave instance = _selectedState.SelectedInstance;
        ElementSave element = _selectedState.SelectedElement;

        if (instance != null)
        {
            using (_undoManager.RequestLock())
            {

                // to bring to back, we're going to remove, then insert at index 0
                element.Instances.Remove(instance);
                element.Instances.Insert(0, instance);

                RefreshInResponseToReorder(instance);
            }
        }
    }

    public void MoveSelectedInstanceInFrontOf(InstanceSave whatToMoveInFrontOf)
    {
        var element = _selectedState.SelectedElement;
        var whatToInsert = _selectedState.SelectedInstance;
        if (whatToInsert != null)
        {
            using (_undoManager.RequestLock())
            {

                element.Instances.Remove(whatToInsert);
                int whereToInsert = element.Instances.IndexOf(whatToMoveInFrontOf) + 1;

                element.Instances.Insert(whereToInsert, whatToInsert);

                RefreshInResponseToReorder(whatToMoveInFrontOf);
                _fileCommands.TryAutoSaveElement(element);
            }
        }
    }
    private void RefreshInResponseToReorder(InstanceSave instance)
    {
        var element = _selectedState.SelectedElement;

        _guiCommands.RefreshElementTreeView(element);

        _fileCommands.TryAutoSaveCurrentElement();

        PluginManager.Self.InstanceReordered(instance);
    }
}
