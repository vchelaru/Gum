using CommonFormsAndControls.Forms;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Gui.Windows;
using Gum.Plugins;
using Gum.Responses;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ToolsUtilities;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Gum.Managers;

public class DeleteLogic : IDeleteLogic
{
    private readonly ProjectCommands _projectCommands;
    private readonly ISelectedState _selectedState;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly PluginManager _pluginManager;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IProjectManager _projectManager;

    public DeleteLogic(
        ProjectCommands projectCommands,
        ISelectedState selectedState,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        PluginManager pluginManager,
        IWireframeObjectManager wireframeObjectManager,
        IProjectManager projectManager)
    {
        _projectCommands = projectCommands;
        _selectedState = selectedState;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _pluginManager = pluginManager;
        _wireframeObjectManager = wireframeObjectManager;
        _projectManager = projectManager;
    }


    public void HandleDeleteCommand()
    {
        var handled = PluginManager.Self.TryHandleDelete();
        if (!handled)
        {
            DoDeletingLogic();
        }

    }

    private void DoDeletingLogic()
    {
        Array objectsDeleted = null;
        DeleteOptionsWindow optionsWindow = null;

        var selectedElements = _selectedState.SelectedElements;
        var selectedInstance = _selectedState.SelectedInstance;
        var selectedBehavior = _selectedState.SelectedBehavior;

        var selectedStateContainer = (IStateContainer)selectedElements.FirstOrDefault() ?? selectedBehavior;

        if (_selectedState.SelectedInstances.Count() > 1)
        {
            AskToDeleteInstances(_selectedState.SelectedInstances);
        }
        else if (selectedInstance != null)
        {
            var array = new InstanceSave[] { selectedInstance };
            objectsDeleted = array;

            if (selectedInstance.DefinedByBase)
            {
                _dialogService.ShowMessage($"The instance {selectedInstance.Name} cannot be deleted becuase it is defined in a base object.");
            }
            else
            {
                //DialogResult result =
                //    MessageBox.Show("Are you sure you'd like to delete " + instance.Name + "?", "Delete instance?", MessageBoxButtons.YesNo);
                var result = ShowDeleteDialog(array, out optionsWindow);


                if (result == true)
                {
                    var siblings = selectedInstance.GetSiblingsIncludingThis();
                    var parentInstance = selectedInstance.GetParentInstance();

                    // This will delete all references to this, meaning, all 
                    // instances attached to the deleted object will be detached, 
                    // but we don't want that, we want to only do that if the user wants to do it, which 
                    // will be handled in a plugin
                    //Gum.ToolCommands.ElementCommands.Self.RemoveInstance(instance, selectedElement);
                    var instanceName = selectedInstance.Name;
                    var selectedElement = selectedElements.FirstOrDefault();
                    if (selectedElement != null)
                    {
                        selectedElement.Instances.Remove(selectedInstance);
                        selectedElement.Events.RemoveAll(item => item.GetSourceObject() == instanceName);
                    }
                    else if (selectedBehavior != null)
                    {
                        selectedBehavior.RequiredInstances.Remove(selectedInstance as BehaviorInstanceSave);
                    }

                    // March 17, 2019
                    // Let's also delete
                    // any variables referencing
                    // this object
                    foreach (var state in selectedStateContainer.AllStates)
                    {
                        state.Variables.RemoveAll(item => item.SourceObject == instanceName);
                        state.VariableLists.RemoveAll(item => item.SourceObject == instanceName);
                    }



                    PluginManager.Self.InstanceDelete(selectedElement, selectedInstance);

                    var deletedSelection = _selectedState.SelectedInstance == selectedInstance;

                    RefreshAndSaveAfterInstanceRemoval(selectedElement, selectedBehavior);

                    if (deletedSelection)
                    {
                        var index = siblings.IndexOf(selectedInstance);
                        if (index + 1 < siblings.Count)
                        {
                            _selectedState.SelectedInstance = siblings[index + 1];
                        }
                        else if (index > 0)
                        {
                            _selectedState.SelectedInstance = siblings[index - 1];
                        }
                        else
                        {
                            // no siblings so select the container or null if none exists:
                            _selectedState.SelectedInstance = parentInstance;
                        }
                    }
                }
                else
                {
                    objectsDeleted = null;
                }
            }
        }
        else if (_selectedState.SelectedElements.Count() > 0)
        {
            var array = _selectedState.SelectedElements
                .Where(item => item is not StandardElementSave).ToArray();

            if (array.Length > 0)
            {

                var result = ShowDeleteDialog(array, out optionsWindow);

                if (result == true)
                {
                    objectsDeleted = array;

                    foreach (var item in array)
                    {
                        _projectCommands.RemoveElement(item);
                    }
                    _selectedState.SelectedElement = null;
                }
            }
        }
        else if (_selectedState.SelectedBehaviors.Count() > 0)
        {
            var array = _selectedState.SelectedBehaviors.ToArray();

            var result = ShowDeleteDialog(array, out optionsWindow);

            if (result == true)
            {
                objectsDeleted = array;
                foreach(var item in array)
                {
                    // We need to remove the reference
                    var behavior = item;
                    _projectCommands.RemoveBehavior(behavior);

                }
            }
        }
        else if(_selectedState.SelectedTreeNode?.IsScreensFolderTreeNode() == true ||
            _selectedState.SelectedTreeNode?.IsComponentsFolderTreeNode() == true)
        {
            DeleteFolder(_selectedState.SelectedTreeNode);
        }

        var shouldDelete = objectsDeleted?.Length > 0;

        if (shouldDelete && selectedInstance != null)
        {
            shouldDelete = selectedInstance.DefinedByBase == false;
        }

        if (shouldDelete)
        {
            PluginManager.Self.DeleteConfirm(optionsWindow, objectsDeleted);
        }
    }

    //bool? ShowDeleteMultiple(Array array)
    //{
    //    if(array.Length == 1)
    //    {
    //        ShowDeleteDialog(array[0]);
    //    }
    //}
    bool? ShowDeleteDialog(Array objectsToDelete, out DeleteOptionsWindow optionsWindow)
    {

        string titleText;

        titleText = "Delete?";
        if (objectsToDelete.Length == 1)
        {
            var objectToDelete = objectsToDelete.GetValue(0);
            if (objectToDelete is ComponentSave)
            {
                titleText = "Delete Component?";
            }
            else if (objectToDelete is ScreenSave)
            {
                titleText = "Delete Screen?";
            }
            else if (objectToDelete is InstanceSave)
            {
                titleText = "Delete Instance?";
            }
            else if (objectToDelete is BehaviorSave)
            {
                titleText = "Delete Behavior?";
            }
        }

        optionsWindow = new DeleteOptionsWindow();
        optionsWindow.Title = titleText;
        optionsWindow.Message = "Are you sure you want to delete:\n";
        foreach (var item in objectsToDelete)
        {
            if(item != null)
            {
                string itemDisplay = item is InstanceSave instanceSave 
                    ? instanceSave.Name 
                    : item.ToString() ?? "";
                // I tried a tab, but the spacing was too big
                optionsWindow.Message += $"  •{itemDisplay}\n";
            }

        }

        optionsWindow.ObjectsToDelete = objectsToDelete;

        // do this in Loaded so it has height
        //_guiCommands.MoveToCursor(optionsWindow);

        PluginManager.Self.ShowDeleteDialog(optionsWindow, objectsToDelete);

        var result = optionsWindow.ShowDialog();


        return result;
    }

    private void AskToDeleteInstances(IEnumerable<InstanceSave> instances)
    {
        var deletableInstances = instances.Where(item => item.DefinedByBase == false).ToArray();
        var instancesFromBase = instances.Except(deletableInstances).ToArray();

        if (instancesFromBase.Any())
        {
            // Show warning about instances from base
            string message;
            if (deletableInstances.Any())
            {
                // has both
                message = "The following instances will be deleted:";
                foreach (var instance in deletableInstances)
                {
                    message += "\n" + instance.Name;
                }

                message += "\n\nThe following instances will NOT be deleted because they are defined in a base object:";
                foreach (var instance in instancesFromBase)
                {
                    message += "\n" + instance.Name;
                }
            }
            else
            {
                // only from base
                message = "All selected instances are defined in a base object, so cannot be deleted";
                _dialogService.ShowMessage(message);
                return;
            }

            // Show the message as part of the delete dialog by appending to the dialog later
            // For now, show the warning separately if there are mixed instances
            _dialogService.ShowMessage(message);
        }

        if (deletableInstances.Any())
        {
            DeleteOptionsWindow optionsWindow = null;
            var result = ShowDeleteDialog(deletableInstances, out optionsWindow);

            if (result == true)
            {
                ElementSave selectedElement = _selectedState.SelectedElement;

                // Just in case the argument is a reference to the selected instances:
                var instancesToRemove = deletableInstances.ToList();

                // Remove instances from collection and their owned variables,
                // but DO NOT remove parent references yet - let the plugin handle that
                foreach (var instance in instancesToRemove)
                {
                    selectedElement.Instances.Remove(instance);
                    selectedElement.Events.RemoveAll(item => item.GetSourceObject() == instance.Name);
                }

                // Remove variables owned by the instances
                foreach (var state in selectedElement.AllStates)
                {
                    foreach (var instance in instancesToRemove)
                    {
                        state.Variables.RemoveAll(item => item.SourceObject == instance.Name);
                        state.VariableLists.RemoveAll(item => item.SourceObject == instance.Name);
                    }
                }

                // Let plugin handle children deletion (this will call RemoveParentReferencesToInstance as needed)
                PluginManager.Self.DeleteConfirm(optionsWindow, deletableInstances);

                // Clear selection
                var newSelection = _selectedState.SelectedInstances.ToList()
                    .Except(instancesToRemove);
                _selectedState.SelectedInstances = newSelection;

                // Then refresh and save
                RefreshAndSaveAfterInstanceRemoval(selectedElement, null);
            }
        }
    }

    private void RefreshAndSaveAfterInstanceRemoval(ElementSave selectedElement, BehaviorSave behavior)
    {
        if (selectedElement != null)
        {
            _fileCommands.TryAutoSaveElement(selectedElement);
        }
        else if (behavior != null)
        {
            _fileCommands.TryAutoSaveBehavior(behavior);
        }

        ElementSave elementToReselect = selectedElement;
        BehaviorSave behaviorToReselect = behavior;


        _selectedState.SelectedInstance = null;
        if (selectedElement != null)
        {
            _selectedState.SelectedElement = elementToReselect;
            _guiCommands.RefreshElementTreeView(selectedElement);
        }
        else if (behavior != null)
        {
            _selectedState.SelectedBehavior = behaviorToReselect;
            _guiCommands.RefreshElementTreeView(behavior);
        }

        _wireframeObjectManager.RefreshAll(true);
    }

    public void RemoveStateCategory(StateSaveCategory category, IStateContainer stateCategoryListContainer)
    {
        var deleteResponse = new DeleteResponse();
        deleteResponse.ShouldDelete = true;
        deleteResponse.ShouldShowMessage = false;

        // This category can only be removed if no behaviors require it
        var behaviorsNeedingCategory = GetBehaviorsNeedingCategory(category, stateCategoryListContainer as ComponentSave);

        if (behaviorsNeedingCategory.Any())
        {
            deleteResponse.ShouldDelete = false;
            deleteResponse.ShouldShowMessage = true;

            string message =
                $"The category {category.Name} cannot be removed because it is needed by the following behavior(s):";

            foreach (var behavior in behaviorsNeedingCategory)
            {
                message += "\n" + behavior.Name;
            }

            deleteResponse.Message = message;
        }


        if (deleteResponse.ShouldDelete)
        {
            deleteResponse = PluginManager.Self.GetDeleteStateCategoryResponse(category, stateCategoryListContainer);
        }

        if (deleteResponse.ShouldDelete == false)
        {
            if (deleteResponse.ShouldShowMessage)
            {
                _dialogService.ShowMessage(deleteResponse.Message);
            }
        }
        else
        {
            var response = _dialogService.ShowYesNoMessage($"Are you sure you want to delete the category {category.Name}?", "Delete category?");

            if (response)
            {
                Remove(category);
            }

        }
    }

    private void Remove(StateSaveCategory category)
    {
        var stateCategoryListContainer =
            _selectedState.SelectedStateContainer;

        var isRemovingSelectedCategory = _selectedState.SelectedStateCategorySave == category;

        stateCategoryListContainer.Categories.Remove(category);

        if (_selectedState.SelectedElement != null)
        {
            var element = _selectedState.SelectedElement;

            foreach (var state in element.AllStates)
            {
                for (int i = state.Variables.Count - 1; i > -1; i--)
                {
                    var variable = state.Variables[i];

                    // Modern Gum now has the type match the state
                    //if(variable.Type == category.Name + "State")
                    if (variable.Type == category.Name)
                    {
                        state.Variables.RemoveAt(i);
                    }
                }
            }

            var elementReferences = ObjectFinder.Self.GetElementReferencesToThis(element);

            foreach (var reference in elementReferences)
            {
                if (reference.ReferenceType == ReferenceType.InstanceOfType)
                {
                    var shouldSave = false;

                    var ownerOfInstance = reference.OwnerOfReferencingObject;
                    var instance = reference.ReferencingObject as InstanceSave;

                    var variableToRemove = $"{instance.Name}.{category.Name}State";

                    foreach (var state in ownerOfInstance.AllStates)
                    {
                        var numberRemoved = state.Variables.RemoveAll(item => item.Name == variableToRemove);

                        if (numberRemoved > 0)
                        {
                            shouldSave = true;
                        }
                    }

                    if (shouldSave)
                    {
                        _fileCommands.TryAutoSaveElement(ownerOfInstance);
                    }
                }
            }
        }

        if (isRemovingSelectedCategory)
        {
            if (_selectedState.SelectedElement != null)
            {
                _selectedState.SelectedStateSave = _selectedState.SelectedElement.DefaultState;
            }
        }

        _fileCommands.TryAutoSaveCurrentElement();

        _guiCommands.RefreshStateTreeView();
        _guiCommands.RefreshVariables();
        _wireframeObjectManager.RefreshAll(true);

        PluginManager.Self.CategoryDelete(category);
    }

    public List<BehaviorSave> GetBehaviorsNeedingCategory(StateSaveCategory category, ComponentSave componentSave)
    {
        List<BehaviorSave> behaviors = new List<BehaviorSave>();

        if (componentSave != null)
        {
            var behaviorNames = componentSave.Behaviors.Select(item => item.BehaviorName);

            foreach (var behavior in _projectManager.GumProjectSave.Behaviors.Where(item => behaviorNames.Contains(item.Name)))
            {
                bool needsCategory = behavior.Categories.Any(item => item.Name == category.Name);

                if (needsCategory)
                {
                    behaviors.Add(behavior);
                }
            }
        }

        return behaviors;
    }

    public void Remove(StateSave stateSave)
    {
        bool shouldProgress = TryAskForRemovalConfirmation(stateSave, _selectedState.SelectedElement);
        if (shouldProgress)
        {
            var stateCategory = _selectedState.SelectedStateCategorySave;
            var shouldSelectAfterRemoval = stateSave == _selectedState.SelectedStateSave;
            int index = stateCategory?.States.IndexOf(stateSave) ?? -1;

            RemoveState(stateSave, _selectedState.SelectedStateContainer);
            PluginManager.Self.StateDelete(stateSave);

            _guiCommands.RefreshVariables();
            _wireframeObjectManager.RefreshAll(true);

            if (shouldSelectAfterRemoval)
            {
                int? newIndex = null;
                if (index != -1)
                {
                    if (index < stateCategory.States.Count)
                    {
                        newIndex = index;
                    }
                    else if (stateCategory.States.Count > 0)
                    {
                        newIndex = stateCategory.States.Count - 1;
                    }
                }

                if (newIndex == null && stateCategory != null)
                {
                    _selectedState.SelectedStateCategorySave = stateCategory;
                    _selectedState.SelectedStateSave = null;
                }
                else if (newIndex != null)
                {
                    _selectedState.SelectedStateSave = stateCategory.States[newIndex.Value];
                }
                else if (_selectedState.SelectedElement != null)
                {
                    _selectedState.SelectedStateSave = _selectedState.SelectedElement.DefaultState;
                }
                else
                {
                    _selectedState.SelectedStateSave = null;
                }
            }
        }
    }


    private bool TryAskForRemovalConfirmation(StateSave stateSave, ElementSave elementSave)
    {
        bool shouldContinue = true;
        // See if the element is used anywhere

        List<InstanceSave> foundInstances = new List<InstanceSave>();

        if (elementSave != null)
        {
            ObjectFinder.Self.GetElementsReferencing(elementSave, null, foundInstances);
        }

        foreach (var instance in foundInstances)
        {
            // We don't want to go recursively, just top level because
            // I *think* that the lists will include copies of the instances
            // recursively
            ElementSave parent = instance.ParentContainer;

            string variableToLookFor = instance.Name + ".State";

            // loop through all of the states to see if any of the parents' states
            // reference the state that is being removed.
            foreach (var stateInContainer in parent.AllStates)
            {
                var foundVariable = stateInContainer.Variables.FirstOrDefault(item => item.Name == variableToLookFor);

                if (foundVariable != null && foundVariable.Value == stateSave.Name)
                {
                    string message = "The state " + stateSave.Name + " is used in the element " +
                        elementSave + " in its state " + stateInContainer + ".\n  What would you like to do?";

                    DialogChoices<string> choices = new()
                    {
                        ["do-nothing"] = "Do nothing - project may be in an invalid state",
                        ["make-default"] = "Change variable to default"
                    };

                    string? result = _dialogService.ShowChoices(message, choices);

                    // eventually will want to add a cancel option

                    if (result == "make-default")
                    {
                        foundVariable.Value = "Default";
                    }
                }
            }
        }

        return shouldContinue;
    }

    /// <summary>
    /// Removes the argument instance from the argument elementToRemoveFrom, and detaches any
    /// object that was attached to this parent.
    /// </summary>
    /// <param name="instanceToRemove">The instance to remove.</param>
    /// <param name="elementToRemoveFrom">The element to remove from.</param>
    public void RemoveInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom)
    {
        if (!elementToRemoveFrom.Instances.Contains(instanceToRemove))
        {
            throw new Exception("Could not find the instance " + instanceToRemove.Name + " in " + elementToRemoveFrom.Name);
        }

        elementToRemoveFrom.Instances.Remove(instanceToRemove);

        RemoveParentReferencesToInstance(instanceToRemove, elementToRemoveFrom);

        elementToRemoveFrom.Events.RemoveAll(item => item.GetSourceObject() == instanceToRemove.Name);


        _pluginManager.InstanceDelete(elementToRemoveFrom, instanceToRemove);

        if (_selectedState.SelectedInstance == instanceToRemove)
        {
            _selectedState.SelectedInstance = null;
        }
    }

    public void RemoveParentReferencesToInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom)
    {
        foreach (StateSave stateSave in elementToRemoveFrom.AllStates)
        {
            for (int i = stateSave.Variables.Count - 1; i > -1; i--)
            {
                var variable = stateSave.Variables[i];

                if (variable.SourceObject == instanceToRemove.Name)
                {
                    // this is a variable that assigns a value on the removed object. The object
                    // is gone, so the variable should be removed too.
                    stateSave.Variables.RemoveAt(i);
                }
                else if (variable.GetRootName() == "Parent" && variable.Value is string valueAsString &&
                         (valueAsString == instanceToRemove.Name || valueAsString.StartsWith(instanceToRemove.Name + ".")))
                {
                    // This is a variable that assigns the Parent to the removed object (or a dotted reference like "Parent.Container").
                    // Since the object is gone, the parent value shouldn't be assigned anymore.
                    stateSave.Variables.RemoveAt(i);
                }
            }
            for (int i = stateSave.VariableLists.Count - 1; i > -1; i--)
            {
                if (stateSave.VariableLists[i].SourceObject == instanceToRemove.Name)
                {
                    stateSave.VariableLists.RemoveAt(i);
                }
            }
        }
    }


    public void RemoveInstances(List<InstanceSave> instances, ElementSave elementToRemoveFrom)
    {
        foreach (var instance in instances)
        {
            elementToRemoveFrom.Instances.Remove(instance);
            RemoveParentReferencesToInstance(instance, elementToRemoveFrom);
            elementToRemoveFrom.Events.RemoveAll(item => item.GetSourceObject() == instance.Name);
        }


        _pluginManager.InstancesDelete(elementToRemoveFrom, instances.ToArray());

        var newSelection = _selectedState.SelectedInstances.ToList()
            .Except(instances);
        _selectedState.SelectedInstances = newSelection;
    }

    public void RemoveState(StateSave stateSave, IStateContainer elementToRemoveFrom)
    {

        elementToRemoveFrom.UncategorizedStates.Remove(stateSave);

        foreach (var category in elementToRemoveFrom.Categories.Where(item => item.States.Contains(stateSave)))
        {
            category.States.Remove(stateSave);
        }

        if (elementToRemoveFrom is BehaviorSave behaviorSave)
        {
            _fileCommands.TryAutoSaveBehavior(behaviorSave);
        }
        else if (elementToRemoveFrom is ElementSave elementSave)
        {
            _fileCommands.TryAutoSaveElement(elementSave);
        }
    }

    public void DeleteFolder(ITreeNode treeNode)
    {
        string fullFile = treeNode.GetFullFilePath().FullPath;

        // Initially we won't allow deleting of the entire
        // folder because the user may have to make decisions
        // about what to do with Screens or Components contained
        // in the folder.

        if (!System.IO.Directory.Exists(fullFile))
        {
            // It doesn't exist, so let's just refresh the UI for this and it will go away
            _guiCommands.RefreshElementTreeView();
        }
        else
        {
            string[] files = System.IO.Directory.GetFiles(fullFile);
            string[] directories = System.IO.Directory.GetDirectories(fullFile);

            if (files != null && files.Length > 0)
            {
                _dialogService.ShowMessage("Cannot delete this folder, it currently contains " + files.Length + " files.");
            }
            else if (directories != null && directories.Length > 0)
            {
                _dialogService.ShowMessage("Cannot delete this folder, it currently contains " + directories.Length + " directories.");
            }

            else
            {
                bool result = _dialogService.ShowYesNoMessage("Delete folder " + treeNode.Text + "?", "Delete");

                if (result)
                {
                    try
                    {
                        FileManager.DeleteDirectory(fullFile);
                        _guiCommands.RefreshElementTreeView();
                    }
                    catch (Exception exception)
                    {
                        _guiCommands.PrintOutput($"Exception attempting to delete folder:\n{exception}");
                        _dialogService.ShowMessage("Could not delete folder\nSee the output tab for more info");
                    }
                }
            }
        }
    }
}
