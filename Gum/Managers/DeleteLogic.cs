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
        // Check for mixed-type selections (e.g., instance + behavior, component + behavior)
        // by reading directly from the tree view's selected nodes. SelectedState enforces
        // mutual exclusion between elements, behaviors, and instances, so we bypass it here.
        var allSelectedTags = _selectedState.SelectedTreeNodes
            .Select(n => n.Tag)
            .Where(t => t != null)
            .ToList();

        var elementsFromTree = allSelectedTags.OfType<ElementSave>()
            .Where(e => e is not StandardElementSave).ToList();
        var behaviorsFromTree = allSelectedTags.OfType<BehaviorSave>().ToList();
        var instancesFromTree = allSelectedTags.OfType<InstanceSave>().ToList();

        // Folder nodes have Tag == null so they are not in allSelectedTags.
        // Extract them separately for multi-folder delete support.
        var folderNodes = _selectedState.SelectedTreeNodes
            .Where(n => n.IsScreensFolderTreeNode() || n.IsComponentsFolderTreeNode())
            .ToList();

        int typesPresent = (elementsFromTree.Count > 0 ? 1 : 0)
            + (behaviorsFromTree.Count > 0 ? 1 : 0)
            + (instancesFromTree.Count > 0 ? 1 : 0);

        if (typesPresent > 1)
        {
            HandleMixedTypeDeletion(elementsFromTree, behaviorsFromTree, instancesFromTree);
            return;
        }

        Array objectsDeleted = null;
        DeleteOptionsWindow optionsWindow = null;

        var selectedElements = _selectedState.SelectedElements;
        var selectedInstance = _selectedState.SelectedInstance;
        var selectedBehavior = _selectedState.SelectedBehavior;

        var selectedStateContainer = (IStateContainer)selectedElements.FirstOrDefault() ?? selectedBehavior;

        if (_selectedState.SelectedInstances.Count() > 1)
        {
            HandleMixedTypeDeletion(
                instances: _selectedState.SelectedInstances.ToList());
        }
        else if (selectedInstance != null)
        {
            var array = new InstanceSave[] { selectedInstance };
            objectsDeleted = array;

            if (selectedInstance.DefinedByBase)
            {
                _dialogService.ShowMessage($"The instance {selectedInstance.Name} cannot be deleted because it is defined in a base object.", "Delete Instance");
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

                    var selectedElement = selectedElements.FirstOrDefault();
                    if (selectedElement != null)
                    {
                        RemoveInstanceFromElement(selectedInstance, selectedElement);
                    }
                    else if (selectedBehavior != null)
                    {
                        selectedBehavior.RequiredInstances.Remove(selectedInstance as BehaviorInstanceSave);
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
        else if (folderNodes.Count > 0)
        {
            DeleteFolders(folderNodes);
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

    private void HandleMixedTypeDeletion(
        List<ElementSave> elements = null,
        List<BehaviorSave> behaviors = null,
        List<InstanceSave> instances = null)
    {
        elements ??= new List<ElementSave>();
        behaviors ??= new List<BehaviorSave>();
        instances ??= new List<InstanceSave>();

        // Filter out DefinedByBase instances
        var deletableInstances = instances.Where(i => !i.DefinedByBase).ToList();
        var instancesFromBase = instances.Except(deletableInstances).ToList();

        var combinedList = new List<object>();
        combinedList.AddRange(elements);
        combinedList.AddRange(behaviors);
        combinedList.AddRange(deletableInstances);

        if (combinedList.Count == 0)
        {
            if (instances.Count > 0)
            {
                _dialogService.ShowMessage(
                    "All selected instances are defined in a base object, so cannot be deleted",
                    "Delete Instances");
            }
            return;
        }

        var combinedArray = combinedList.ToArray();
        var result = ShowDeleteDialog(combinedArray, out var optionsWindow, instancesFromBase);

        if (result != true) return;

        // Cache behavior parents before removal (GetBehaviorContainerOf won't find them after)
        var affectedBehaviorParents = deletableInstances
            .Select(i => ObjectFinder.Self.GetBehaviorContainerOf(i))
            .Where(b => b != null && !behaviors.Contains(b))
            .Distinct()
            .ToList();

        // 1. Remove instances (skip if parent element/behavior is also being deleted)
        foreach (var instance in deletableInstances)
        {
            var parentElement = instance.ParentContainer;
            var parentBehavior = ObjectFinder.Self.GetBehaviorContainerOf(instance);

            if (parentElement != null && !elements.Contains(parentElement))
            {
                RemoveInstanceFromElement(instance, parentElement);
            }
            else if (parentBehavior != null && !behaviors.Contains(parentBehavior)
                && instance is BehaviorInstanceSave behaviorInstance)
            {
                parentBehavior.RequiredInstances.Remove(behaviorInstance);
            }
        }

        // 2. Remove elements
        foreach (var element in elements)
        {
            _projectCommands.RemoveElement(element);
        }

        // 3. Remove behaviors
        foreach (var behavior in behaviors)
        {
            _projectCommands.RemoveBehavior(behavior);
        }

        // 4. Notify plugins (handles XML file deletion, children deletion, etc.)
        PluginManager.Self.DeleteConfirm(optionsWindow, combinedArray);

        // 5. Update selection to avoid stale references
        if (elements.Count > 0)
        {
            _selectedState.SelectedElement = null;
        }
        if (deletableInstances.Count > 0)
        {
            DeselectInstances(deletableInstances);
            _selectedState.SelectedInstance = null;
        }

        // 6. Refresh and save for instance parents that were not themselves deleted
        if (deletableInstances.Count > 0)
        {
            var affectedElementParents = deletableInstances
                .Select(i => i.ParentContainer)
                .Where(e => e != null && !elements.Contains(e))
                .Distinct()
                .ToList();

            foreach (var parent in affectedElementParents)
            {
                SaveElementAfterInstanceRemoval(parent);
            }

            foreach (var behavior in affectedBehaviorParents)
            {
                SaveBehaviorAfterInstanceRemoval(behavior);
            }
        }

        _wireframeObjectManager.RefreshAll(true);
    }

    bool? ShowDeleteDialog(Array objectsToDelete, out DeleteOptionsWindow optionsWindow,
        List<InstanceSave> instancesFromBase = null)
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

        // Collect the short name of every item, then find which names appear more than once
        var names = new List<string>();
        foreach (var item in objectsToDelete)
        {
            if (item != null) names.Add(GetShortName(item));
        }
        var duplicateNames = new HashSet<string>(
            names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key));

        foreach (var item in objectsToDelete)
        {
            if (item != null)
            {
                // I tried a tab, but the spacing was too big
                optionsWindow.Message += $"  - {GetItemDisplayName(item, duplicateNames)}\n";
            }
        }

        if (instancesFromBase != null && instancesFromBase.Count > 0)
        {
            optionsWindow.Message += "\nThe following will NOT be deleted (defined in base):";
            foreach (var instance in instancesFromBase)
            {
                optionsWindow.Message += $"\n  - {instance.Name}";
            }
            optionsWindow.Message += "\n";
        }

        optionsWindow.ObjectsToDelete = objectsToDelete;

        // do this in Loaded so it has height
        //_guiCommands.MoveToCursor(optionsWindow);

        PluginManager.Self.ShowDeleteDialog(optionsWindow, objectsToDelete);

        var result = optionsWindow.ShowDialog();


        return result;
    }

    private static string GetShortName(object item)
    {
        if (item is InstanceSave instance) return instance.Name;
        if (item is ElementSave element) return element.Name;
        if (item is BehaviorSave behavior) return behavior.Name;
        return item.ToString() ?? "";
    }

    private static string GetItemDisplayName(object item, HashSet<string> duplicateNames)
    {
        if (item is InstanceSave instanceSave)
        {
            if (duplicateNames.Contains(instanceSave.Name))
            {
                var parent = instanceSave.ParentContainer;
                var typePrefix = parent is ScreenSave ? "Screens" : "Components";
                return $"{typePrefix}/{parent?.Name}/{instanceSave.Name}";
            }
            return instanceSave.Name;
        }
        if (item is ScreenSave screenSave)
        {
            return duplicateNames.Contains(screenSave.Name)
                ? $"Screens/{screenSave.Name}" : screenSave.Name;
        }
        if (item is ElementSave elementSave)
        {
            return duplicateNames.Contains(elementSave.Name)
                ? $"Components/{elementSave.Name}" : elementSave.Name;
        }
        if (item is BehaviorSave behaviorSave)
        {
            return duplicateNames.Contains(behaviorSave.Name)
                ? $"Behaviors/{behaviorSave.Name}" : behaviorSave.Name;
        }
        return item.ToString() ?? "";
    }

    private void RefreshAndSaveAfterInstanceRemoval(ElementSave selectedElement, BehaviorSave behavior)
    {
        SaveElementAfterInstanceRemoval(selectedElement);
        SaveBehaviorAfterInstanceRemoval(behavior);
        PickSelectedInstanceAfterInstanceRemoval(selectedElement, behavior);
        RefreshAll();
    }

    private void SaveBehaviorAfterInstanceRemoval(BehaviorSave behavior)
    {
        if (behavior != null)
        {
            _fileCommands.TryAutoSaveBehavior(behavior);
            _guiCommands.RefreshElementTreeView(behavior);
        }
    }

    private void SaveElementAfterInstanceRemoval(ElementSave selectedElement)
    {
        if (selectedElement != null)
        {
            _fileCommands.TryAutoSaveElement(selectedElement);
            _guiCommands.RefreshElementTreeView(selectedElement);
        }
    }

    private void PickSelectedInstanceAfterInstanceRemoval(ElementSave selectedElement, BehaviorSave behavior)
    {
        _selectedState.SelectedInstance = null;
        if (selectedElement != null)
        {
            _selectedState.SelectedElement = selectedElement;
        }
        else if (behavior != null)
        {
            _selectedState.SelectedBehavior = behavior;
        }
    }

    private void DeselectInstances(IEnumerable<InstanceSave> instancesToDeselect)
    {
        var newSelection = _selectedState.SelectedInstances.ToList()
            .Except(instancesToDeselect);
        _selectedState.SelectedInstances = newSelection;
    }

    private void RefreshAll()
    {
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
                _dialogService.ShowMessage(deleteResponse.Message, "Delete Category");
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

        RemoveInstanceFromElement(instanceToRemove, elementToRemoveFrom);

        _pluginManager.InstanceDelete(elementToRemoveFrom, instanceToRemove);

        if (_selectedState.SelectedInstance == instanceToRemove)
        {
            _selectedState.SelectedInstance = null;
        }
    }

    private void RemoveInstanceFromElement(InstanceSave instance, ElementSave element)
    {
        element.Instances.Remove(instance);
        element.Events.RemoveAll(item => item.GetSourceObject() == instance.Name);
        RemoveParentReferencesToInstance(instance, element);
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
            RemoveInstanceFromElement(instance, elementToRemoveFrom);
        }


        _pluginManager.InstancesDelete(elementToRemoveFrom, instances.ToArray());

        DeselectInstances(instances);
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

    private void DeleteFolder(ITreeNode treeNode)
    {
        DeleteFolders(new List<ITreeNode> { treeNode });
    }

    public void DeleteFolders(List<ITreeNode> folderNodes)
    {
        if (folderNodes.Count == 0) return;

        var (deletable, blocked, needsRefresh) = ClassifyFolders(folderNodes);

        // If any folder is blocked, show error and abort the entire operation
        if (blocked.Count > 0)
        {
            string blockedTitle = folderNodes.Count == 1 ? "Delete Folder" : "Delete Folders";
            _dialogService.ShowMessage(BuildBlockedMessage(folderNodes, deletable, blocked), blockedTitle);
            return;
        }

        // All folders are deletable (or non-existent). Confirm with user.
        if (deletable.Count > 0)
        {
            string title = deletable.Count == 1 ? "Delete Folder" : "Delete Folders";
            bool result = _dialogService.ShowYesNoMessage(
                BuildConfirmationMessage(deletable), title);

            if (result)
            {
                foreach (var (node, fullPath) in deletable)
                {
                    try
                    {
                        FileManager.DeleteDirectory(fullPath);
                    }
                    catch (Exception exception)
                    {
                        _guiCommands.PrintOutput(
                            $"Exception attempting to delete folder {node.Text}:\n{exception}");
                        _dialogService.ShowMessage(
                            "Could not delete folder " + node.Text
                            + "\nSee the output tab for more info",
                            "Delete Folder");
                    }
                }
                needsRefresh = true;
            }
        }

        if (needsRefresh)
        {
            _guiCommands.RefreshElementTreeView();
        }
    }

    private static (List<(ITreeNode node, string fullPath)> deletable,
        List<(ITreeNode node, string reason)> blocked,
        bool needsRefresh) ClassifyFolders(List<ITreeNode> folderNodes)
    {
        var deletable = new List<(ITreeNode node, string fullPath)>();
        var blocked = new List<(ITreeNode node, string reason)>();
        bool needsRefresh = false;

        foreach (var node in folderNodes)
        {
            string fullPath = node.GetFullFilePath().FullPath;
            string blocker = GetFolderDeletionBlocker(fullPath);

            if (blocker != null)
            {
                blocked.Add((node, blocker));
            }
            else if (!System.IO.Directory.Exists(fullPath))
            {
                // Stale node - doesn't exist on disk, will be cleaned up by tree refresh
                needsRefresh = true;
            }
            else
            {
                deletable.Add((node, fullPath));
            }
        }

        return (deletable, blocked, needsRefresh);
    }

    private static string BuildBlockedMessage(
        List<ITreeNode> folderNodes,
        List<(ITreeNode node, string fullPath)> deletable,
        List<(ITreeNode node, string reason)> blocked)
    {
        if (folderNodes.Count == 1)
        {
            return "Cannot delete this folder, it currently " + blocked[0].reason + ".";
        }

        string message = "";
        if (deletable.Count > 0)
        {
            message += "Can be deleted:\n";
            foreach (var (node, _) in deletable)
            {
                message += $"  - {node.Text}\n";
            }
            message += "\n";
        }

        message += "Cannot be deleted:\n";
        foreach (var (node, reason) in blocked)
        {
            message += $"  - {node.Text} - {reason}\n";
        }
        return message;
    }

    private static string BuildConfirmationMessage(
        List<(ITreeNode node, string fullPath)> deletable)
    {
        if (deletable.Count == 1)
        {
            return "Delete folder " + deletable[0].node.Text + "?";
        }

        string message = "Delete " + deletable.Count + " folders?\n";
        foreach (var (node, _) in deletable)
        {
            message += $"  - {node.Text}\n";
        }
        return message;
    }

    /// <summary>
    /// Returns null if the folder can be deleted (empty or doesn't exist on disk),
    /// or a reason string describing why it cannot be deleted.
    /// </summary>
    internal static string GetFolderDeletionBlocker(string fullPath)
    {
        if (!System.IO.Directory.Exists(fullPath))
        {
            return null;
        }

        int fileCount = System.IO.Directory.GetFiles(fullPath).Length;
        int folderCount = System.IO.Directory.GetDirectories(fullPath).Length;

        if (fileCount == 0 && folderCount == 0)
        {
            return null;
        }

        string filePart = null;
        if (fileCount == 1)
        {
            filePart = "a file";
        }
        else if (fileCount > 1)
        {
            filePart = fileCount + " files";
        }

        string folderPart = null;
        if (folderCount == 1)
        {
            folderPart = "a folder";
        }
        else if (folderCount > 1)
        {
            folderPart = folderCount + " folders";
        }

        if (filePart != null && folderPart != null)
        {
            return "contains " + filePart + " and " + folderPart;
        }
        else if (filePart != null)
        {
            return "contains " + filePart;
        }
        else
        {
            return "contains " + folderPart;
        }
    }
}
