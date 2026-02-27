using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Gui.Windows;
using Gum.Logic;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
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
    private readonly ISelectedState _selectedState;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IPluginManager _pluginManager;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IProjectManager _projectManager;
    private readonly IReferenceFinder _referenceFinder;

    public DeleteLogic(
        ISelectedState selectedState,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IPluginManager pluginManager,
        IWireframeObjectManager wireframeObjectManager,
        IProjectManager projectManager,
        IReferenceFinder referenceFinder)
    {
        _selectedState = selectedState;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _pluginManager = pluginManager;
        _wireframeObjectManager = wireframeObjectManager;
        _projectManager = projectManager;
        _referenceFinder = referenceFinder;
    }


    public void HandleDeleteCommand()
    {
        var handled = _pluginManager.TryHandleDelete();
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

        Array? objectsDeleted = null;
        DeleteOptionsWindow? optionsWindow = null;

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

                    // Fire DeleteConfirmed before removal so plugins can still find
                    // children via parent reference variables (e.g. "Child.Parent = thisInstance").
                    // RemoveInstanceFromElement destroys those references, breaking GetChildrenOf.
                    _pluginManager.DeleteConfirmed(optionsWindow, objectsDeleted);
                    objectsDeleted = null; // prevent double-firing at the end of DoDeletingLogic

                    var selectedElement = selectedElements.FirstOrDefault();
                    if (selectedElement != null)
                    {
                        RemoveInstanceFromElement(selectedInstance, selectedElement);
                    }
                    else if (selectedBehavior != null)
                    {
                        selectedBehavior.RequiredInstances.Remove(selectedInstance as BehaviorInstanceSave);
                    }



                    _pluginManager.InstanceDelete(selectedElement, selectedInstance);

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
                        RemoveElement(item);
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
                    RemoveBehavior(behavior);

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
            _pluginManager.DeleteConfirmed(optionsWindow, objectsDeleted);
        }
    }

    private void HandleMixedTypeDeletion(
        List<ElementSave>? elements = null,
        List<BehaviorSave>? behaviors = null,
        List<InstanceSave>? instances = null)
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

        // 1. Notify plugins before removal so they can still find children via
        //    parent reference variables (e.g. "Child.Parent = thisInstance").
        //    RemoveInstanceFromElement destroys those references, breaking GetChildrenOf.
        _pluginManager.DeleteConfirmed(optionsWindow, combinedArray);

        // 2. Remove instances (skip if parent element/behavior is also being deleted)
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

        // 3. Remove elements
        foreach (var element in elements)
        {
            RemoveElement(element);
        }

        // 4. Remove behaviors
        foreach (var behavior in behaviors)
        {
            RemoveBehavior(behavior);
        }

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
        List<InstanceSave>? instancesFromBase = null)
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
                optionsWindow.Message += $"  • {GetItemDisplayName(item, duplicateNames)}\n";
            }
        }

        if (instancesFromBase != null && instancesFromBase.Count > 0)
        {
            optionsWindow.Message += "\nThe following will NOT be deleted (defined in base):";
            foreach (var instance in instancesFromBase)
            {
                optionsWindow.Message += $"\n  • {instance.Name}";
            }
            optionsWindow.Message += "\n";
        }

        var elementItems = objectsToDelete.OfType<ElementSave>().ToList();
        bool multipleElements = elementItems.Count > 1;
        foreach (var element in elementItems)
        {
            ElementRenameChanges impactChanges = _referenceFinder.GetReferencesToElement(element, element.Name);
            string impactDetails = impactChanges.GetDeleteImpactDetails();
            if (!string.IsNullOrEmpty(impactDetails))
            {
                optionsWindow.Message += "\n";
                if (multipleElements)
                {
                    optionsWindow.Message += $"\nImpact of deleting {element.Name}:\n{impactDetails}";
                }
                else
                {
                    optionsWindow.Message += $"\n{impactDetails}";
                }
            }
        }

        optionsWindow.ObjectsToDelete = objectsToDelete;

        // do this in Loaded so it has height
        //_guiCommands.MoveToCursor(optionsWindow);

        _pluginManager.ShowDeleteDialog(optionsWindow, objectsToDelete);

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

    private void RefreshAndSaveAfterInstanceRemoval(ElementSave? selectedElement, BehaviorSave? behavior)
    {
        SaveElementAfterInstanceRemoval(selectedElement);
        SaveBehaviorAfterInstanceRemoval(behavior);
        PickSelectedInstanceAfterInstanceRemoval(selectedElement, behavior);
        RefreshAll();
    }

    private void SaveBehaviorAfterInstanceRemoval(BehaviorSave? behavior)
    {
        if (behavior != null)
        {
            _fileCommands.TryAutoSaveBehavior(behavior);
            _guiCommands.RefreshElementTreeView(behavior);
        }
    }

    private void SaveElementAfterInstanceRemoval(ElementSave? selectedElement)
    {
        if (selectedElement != null)
        {
            _fileCommands.TryAutoSaveElement(selectedElement);
            _guiCommands.RefreshElementTreeView(selectedElement);
        }
    }

    private void PickSelectedInstanceAfterInstanceRemoval(ElementSave? selectedElement, BehaviorSave? behavior)
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
        var stateCategoryListContainerToUse = _selectedState.SelectedStateContainer;

        var isRemovingSelectedCategory = _selectedState.SelectedStateCategorySave == category;

        stateCategoryListContainerToUse.Categories.Remove(category);

        if (_selectedState.SelectedElement != null)
        {
            var element = _selectedState.SelectedElement;

            foreach (var state in element.AllStates)
            {
                for (int i = state.Variables.Count - 1; i > -1; i--)
                {
                    var variable = state.Variables[i];

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

        _pluginManager.CategoryDelete(category);
    }

    public List<BehaviorSave> GetBehaviorsNeedingCategory(StateSaveCategory category, ComponentSave? componentSave)
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
        var stateCategory = _selectedState.SelectedStateCategorySave;
        var shouldSelectAfterRemoval = stateSave == _selectedState.SelectedStateSave;
        int index = stateCategory?.States.IndexOf(stateSave) ?? -1;

        RemoveState(stateSave, _selectedState.SelectedStateContainer);
        _pluginManager.StateDelete(stateSave);

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

    public void RemoveElement(ElementSave element)
    {
        var gps = _projectManager.GumProjectSave;
        var name = element.Name;
        var removed = false;

        if (element is ScreenSave asScreenSave)
        {
            RemoveElementReferencesFromList(name, gps.ScreenReferences);
            gps.Screens.Remove(asScreenSave);
            removed = true;
        }
        else if (element is ComponentSave asComponentSave)
        {
            RemoveElementReferencesFromList(name, gps.ComponentReferences);
            gps.Components.Remove(asComponentSave);
            removed = true;
        }

        if (removed)
        {
            if (_selectedState.SelectedElements.Contains(element))
            {
                _selectedState.SelectedElement = null;
            }
            _pluginManager.ElementDelete(element);
            _fileCommands.TryAutoSaveProject();
        }
    }

    private static void RemoveElementReferencesFromList(string name, List<ElementReference> references)
    {
        for (int i = 0; i < references.Count; i++)
        {
            if (references[i].Name == name)
            {
                references.RemoveAt(i);
                break;
            }
        }
    }

    public void RemoveBehavior(BehaviorSave behavior)
    {
        var behaviorName = behavior.Name;
        var gps = _projectManager.GumProjectSave;

        gps.BehaviorReferences.RemoveAll(item => item.Name == behaviorName);
        gps.Behaviors.Remove(behavior);

        var elementsReferencingBehavior = new List<ElementSave>();
        foreach (var element in ObjectFinder.Self.GumProjectSave.AllElements)
        {
            var matchingBehavior = element.Behaviors.FirstOrDefault(item => item.BehaviorName == behaviorName);
            if (matchingBehavior != null)
            {
                element.Behaviors.Remove(matchingBehavior);
                elementsReferencingBehavior.Add(element);
            }
        }

        _selectedState.SelectedBehavior = null;

        _pluginManager.BehaviorDeleted(behavior);

        _guiCommands.RefreshStateTreeView();
        _guiCommands.RefreshVariables();

        _fileCommands.TryAutoSaveProject();
        foreach (var element in elementsReferencingBehavior)
        {
            _fileCommands.TryAutoSaveElement(element);
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
            string? blocker = GetFolderDeletionBlocker(fullPath);

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
                message += $"  • {node.Text}\n";
            }
            message += "\n";
        }

        message += "Cannot be deleted:\n";
        foreach (var (node, reason) in blocked)
        {
            message += $"  • {node.Text} - {reason}\n";
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
            message += $"  • {node.Text}\n";
        }
        return message;
    }

    /// <summary>
    /// Returns null if the folder can be deleted (empty or doesn't exist on disk),
    /// or a reason string describing why it cannot be deleted.
    /// </summary>
    internal static string? GetFolderDeletionBlocker(string fullPath)
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

        string? filePart = null;
        if (fileCount == 1)
        {
            filePart = "a file";
        }
        else if (fileCount > 1)
        {
            filePart = fileCount + " files";
        }

        string? folderPart = null;
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
