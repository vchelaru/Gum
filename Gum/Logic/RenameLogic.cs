using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.ToolStates;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.Plugins;
using Gum.Managers;
using Gum.Undo;
using CommonFormsAndControls;
using Gum.Controls;
using System.Drawing;
using System.Windows.Documents.DocumentStructures;
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;
using DialogResult = System.Windows.Forms.DialogResult;
using Gum.Plugins.InternalPlugins.VariableGrid;

namespace Gum.Logic;

#region Enums

public enum NameChangeAction
{
    Move,
    Rename
}

#endregion

#region VariableChange Class

public class VariableChange
{
    public IStateContainer Container;
    public StateSaveCategory Category;
    public StateSave State;
    public VariableSave Variable;
    public object NewValue;

}

public enum SideOfEquals
{
    Left,
    Right,
    Both
}
public class VariableReferenceChange
{
    public ElementSave Container;
    public VariableListSave VariableReferenceList;
    public int LineIndex;
    public SideOfEquals ChangedSide;
}

public class VariableChangeResponse
{
    public List<VariableChange> VariableChanges = new List<VariableChange>();
    public List<VariableReferenceChange> VariableReferenceChanges = new List<VariableReferenceChange>();
}

#endregion

#region ElementRenameChanges Class

public class ElementRenameChanges
{
    // Screens or components where BaseType == oldName
    public List<ElementSave> ElementsWithBaseTypeReference = new();
    // Instances in any element where BaseType == oldName
    public List<(ElementSave Container, InstanceSave Instance)> InstancesWithBaseTypeReference = new();
    // ContainedType variables whose value == oldName
    public List<(ElementSave Container, VariableSave Variable)> ContainedTypeVariableReferences = new();
    // VariableReferences list entries whose right-hand side references oldName
    public List<VariableReferenceChange> VariableReferenceChanges = new();
}

#endregion

#region StateRenameChanges Class

public class StateRenameChanges
{
    // Variables in referencing elements whose value == oldStateName and which need updating
    public List<(ElementSave Container, VariableSave Variable)> VariablesToUpdate = new();
}

#endregion

#region CategoryRenameChanges Class

public class CategoryRenameChanges
{
    // Variables in referencing elements/components whose Type matches the old category name
    public List<VariableChange> VariableChanges = new();
}

#endregion

public class RenameLogic : IRenameLogic
{
    static bool isRenamingXmlFile;

    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IDeleteLogic _deleteLogic;
    private readonly IProjectManager _projectManager;
    private readonly IProjectState _projectState;
    private readonly IPluginManager _pluginManager;
    private readonly IStandardElementsManagerGumTool _standardElementsManagerGumTool;

    public RenameLogic(ISelectedState selectedState,
        INameVerifier nameVerifier,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IDeleteLogic deleteLogic,
        IProjectManager projectManager,
        IProjectState projectState,
        IPluginManager pluginManager,
        IStandardElementsManagerGumTool standardElementsManagerGumTool)
    {
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _deleteLogic = deleteLogic;
        _projectManager = projectManager;
        _projectState = projectState;
        _pluginManager = pluginManager;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
    }

    #region StateSave

    public void RenameState(StateSave stateSave, StateSaveCategory category, string newName)
    {
        if (!_nameVerifier.IsStateNameValid(newName, category, stateSave, out string whyNotValid))
        {
            _dialogService.ShowMessage(whyNotValid);
        }
        else
        {
            string oldName = stateSave.Name;

            var container = ObjectFinder.Self.GetStateContainerOf(stateSave);
            var stateChanges = GetChangesForRenamedState(stateSave, oldName, container, category);

            stateSave.Name = newName;
            _guiCommands.RefreshStateTreeView();
            // I don't think we need to save the project when renaming a state:
            //_fileCommands.TryAutoSaveProject();

            // Renaming the state should refresh the property grid
            // because it displays the state name at the top
            _guiCommands.RefreshVariables(force: true);

            _pluginManager.StateRename(stateSave, oldName);

            ApplyStateRenameChanges(stateChanges, stateSave);

            _fileCommands.TryAutoSaveCurrentObject();
        }
    }

    public StateRenameChanges GetChangesForRenamedState(
        StateSave state, string oldName, IStateContainer? container, StateSaveCategory? category)
    {
        var changes = new StateRenameChanges();

        if (container is ElementSave elementSave)
        {
            string variableName = category != null ? category.Name + "State" : "State";

            List<InstanceSave> instances = new List<InstanceSave>();
            ObjectFinder.Self.GetElementsReferencing(elementSave, foundInstances: instances);

            foreach (var instance in instances)
            {
                var parentOfInstance = instance.ParentContainer;
                var variableNameToLookFor = $"{instance.Name}.{variableName}";

                if(parentOfInstance != null)
                {
                    var variablesToFix = parentOfInstance.AllStates
                        .SelectMany(item => item.Variables)
                        .Where(item => item.Name == variableNameToLookFor)
                        .Where(item => (string?)item.Value == oldName);

                    foreach (var variable in variablesToFix)
                    {
                        changes.VariablesToUpdate.Add((parentOfInstance, variable));
                    }
                }
            }
        }

        return changes;
    }

    public void ApplyStateRenameChanges(StateRenameChanges changes, StateSave state)
    {
        var elementsToSave = new HashSet<ElementSave>();

        foreach (var (container, variable) in changes.VariablesToUpdate)
        {
            variable.Value = state.Name;
            elementsToSave.Add(container);
        }

        foreach (var elementToSave in elementsToSave)
        {
            _fileCommands.TryAutoSaveElement(elementToSave);
        }
    }

    #endregion

    #region Category

    public void AskToRenameStateCategory(StateSaveCategory category, ElementSave elementSave)
    {
        // This category can only be renamed if no behaviors require it
        var behaviorsNeedingCategory = _deleteLogic.GetBehaviorsNeedingCategory(category, elementSave as ComponentSave);

        if (behaviorsNeedingCategory.Any())
        {
            string message =
                $"The category {category.Name} cannot be renamed because it is needed by the following behavior(s):";

            foreach (var behavior in behaviorsNeedingCategory)
            {
                message += "\n" + behavior.Name;
            }

            _dialogService.ShowMessage(message);
        }
        else
        {
            string message = "Enter new category name";
            string title = "New Category";

            GetUserStringOptions options = new() { InitialValue = category.Name };
            string oldName = category.Name;
            var changes = GetChangesForRenamedCategory(elementSave, category, oldName);

            if (changes.VariableChanges.Count > 0)
            {
                message += "\n\nThe following variables will be affected:";
                foreach (var change in changes.VariableChanges)
                {
                    var containerDisplay = change.Container is ElementSave changeElementSave
                        ? changeElementSave.Name
                        : change.Container.ToString();

                    message += $"\n  {change.Variable.Name} in {containerDisplay}";
                }
            }


            if (_dialogService.GetUserString(message, title, options) is { } newName)
            {
                ApplyCategoryRenameChanges(changes, elementSave, category, oldName, newName);
            }
        }
    }

    public void ApplyCategoryRenameChanges(CategoryRenameChanges categoryChanges, IStateContainer owner, StateSaveCategory category, string oldName, string newName)
    {
        // Gather self-referencing state variables in the owner element before mutating anything
        var ownerAsElement = owner as ElementSave;
        var selfVarsToUpdate = ownerAsElement?.AllStates
            .SelectMany(state => state.Variables)
            .Where(v => v.Type == oldName + "State")
            .ToList();

        category.Name = newName;

        HashSet<ElementSave> elementsWithChangedVariables = new HashSet<ElementSave>();

        foreach (var change in categoryChanges.VariableChanges)
        {
            var containerElement = change.Container as ElementSave;
            if (containerElement != null)
            {
                elementsWithChangedVariables.Add(change.Container as ElementSave);
            }
            change.Variable.Type = newName;
            if (change.Variable.GetRootName() == $"{oldName}State")
            {
                if (string.IsNullOrEmpty(change.Variable.SourceObject))
                {
                    change.Variable.Name = $"{newName}State";
                }
                else
                {
                    change.Variable.Name = $"{change.Variable.SourceObject}.{newName}State";
                }
            }
        }

        if (selfVarsToUpdate != null)
        {
            foreach (var variable in selfVarsToUpdate)
            {
                variable.Name = category.Name + "State";
                variable.Type = category.Name + "State";

#if GUM
                variable.CustomTypeConverter =
                    new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(category.Name, _selectedState);
#endif
            }

            foreach (var state in ownerAsElement!.AllStates)
            {
                state.Variables.Sort((first, second) => first.Name.CompareTo(second.Name));
            }
        }

        _guiCommands.RefreshStateTreeView();
        // I don't think we need to save the project when renaming a state:
        //_fileCommands.TryAutoSaveProject();

        _pluginManager.CategoryRename(category, oldName);

        _fileCommands.TryAutoSaveCurrentObject();

        if (owner is ElementSave ownerAsElementSave)
        {
            _standardElementsManagerGumTool.FixCustomTypeConverters(ownerAsElementSave);
        }

        foreach (var item in elementsWithChangedVariables)
        {
            _standardElementsManagerGumTool.FixCustomTypeConverters(item);

            _fileCommands.TryAutoSaveElement(item);
        }
    }

    public CategoryRenameChanges GetChangesForRenamedCategory(IStateContainer owner, StateSaveCategory category, string oldName)
    {
        var changes = new CategoryRenameChanges();

        var project = _projectState.GumProjectSave;

        var ownerAsElement = owner as ElementSave;

        List<ElementSave>? inheritingElements = new List<ElementSave>();
        if (ownerAsElement != null)
        {
            inheritingElements.Add(ownerAsElement);
            inheritingElements.AddRange(ObjectFinder.Self.GetElementsInheritingFrom(ownerAsElement));
        }

        // This currently only handles categories in elements, not behaviors
        foreach (var screen in project.Screens)
        {
            RenameReferencesInElement(category, oldName, inheritingElements, screen);
        }

        foreach (var component in project.Components)
        {
            RenameReferencesInElement(category, oldName, inheritingElements, component);
        }

        // Standards cannot include instances so no need to loop through them

        return changes;

        void RenameReferencesInElement(StateSaveCategory changedCategory, string oldName, ICollection<ElementSave> inheritingElements, ElementSave element)
        {
            // If the element inherits from the owner of the category and if the screen has a variable of this type, change it.
            foreach (var state in element.AllStates)
            {
                foreach (var variable in state.Variables)
                {
                    var variableType = variable.Type;

                    if (variableType == oldName)
                    {
                        if (string.IsNullOrEmpty(variable.SourceObject))
                        {
                            if (inheritingElements.Contains(element))
                            {
                                AddVariableToList(element, changes.VariableChanges, state, variable);
                            }
                        }
                        else
                        {
                            // only do it if the instance is in the inheritance chain
                            var instance = element.GetInstance(variable.SourceObject);
                            if (instance != null)
                            {
                                var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                                if (inheritingElements?.Contains(instanceElement) == true)
                                {
                                    AddVariableToList(element, changes.VariableChanges, state, variable);
                                }
                            }
                        }
                    }
                }
            }

            static void AddVariableToList(ElementSave element, List<VariableChange> toReturn, StateSave state, VariableSave variable)
            {
                var category = element.Categories.FirstOrDefault(item => item.States.Contains(state));

                toReturn.Add(new VariableChange
                {
                    Container = element,

                    Category = category,
                    State = state,
                    Variable = variable
                });
            }
        }
    }

    #endregion

    #region Element

    public ElementRenameChanges GetChangesForRenamedElement(ElementSave elementSave, string oldName)
    {
        var changes = new ElementRenameChanges();
        var project = _projectState.GumProjectSave;

        string qualifiedOldName = elementSave switch
        {
            ScreenSave => $"Screens/{oldName}",
            ComponentSave => $"Components/{oldName}",
            StandardElementSave => $"Standards/{oldName}",
            _ => oldName
        };

        foreach (var screen in project.Screens)
        {
            CollectChangesInElement(screen, oldName, qualifiedOldName, changes);
        }

        foreach (var component in project.Components)
        {
            CollectChangesInElement(component, oldName, qualifiedOldName, changes);
        }

        return changes;

        static void CollectChangesInElement(ElementSave element, string oldName, string qualifiedOldName, ElementRenameChanges changes)
        {
            if (element.BaseType == oldName)
            {
                changes.ElementsWithBaseTypeReference.Add(element);
            }

            foreach (var instance in element.Instances)
            {
                if (instance.BaseType == oldName)
                {
                    changes.InstancesWithBaseTypeReference.Add((element, instance));
                }
            }

            foreach (var variable in element.DefaultState.Variables.Where(v => v.GetRootName() == "ContainedType"))
            {
                if (variable.Value as string == oldName)
                {
                    changes.ContainedTypeVariableReferences.Add((element, variable));
                }
            }

            foreach (var state in element.AllStates)
            {
                foreach (var variableList in state.VariableLists)
                {
                    if (variableList.GetRootName() != "VariableReferences") continue;

                    for (int i = 0; i < variableList.ValueAsIList.Count; i++)
                    {
                        var line = variableList.ValueAsIList[i];
                        if (line is not string asString || asString.StartsWith("//") || !asString.Contains("="))
                            continue;

                        var right = asString.Substring(asString.IndexOf("=") + 1).Trim();

                        if (right.StartsWith(qualifiedOldName + "."))
                        {
                            changes.VariableReferenceChanges.Add(new VariableReferenceChange
                            {
                                Container = element,
                                VariableReferenceList = variableList,
                                LineIndex = i,
                                ChangedSide = SideOfEquals.Right
                            });
                        }
                    }
                }
            }
        }
    }

    public void ApplyElementRenameChanges(ElementRenameChanges changes, ElementSave elementSave, string oldName)
    {
        var containersToSave = new HashSet<ElementSave>();

        foreach (var element in changes.ElementsWithBaseTypeReference)
        {
            element.BaseType = elementSave.Name;
            containersToSave.Add(element);
        }

        foreach (var (container, instance) in changes.InstancesWithBaseTypeReference)
        {
            instance.BaseType = elementSave.Name;
            containersToSave.Add(container);
        }

        foreach (var (container, variable) in changes.ContainedTypeVariableReferences)
        {
            variable.Value = elementSave.Name;
            containersToSave.Add(container);
        }

        string qualifiedOldName = elementSave switch
        {
            ScreenSave => $"Screens/{oldName}",
            ComponentSave => $"Components/{oldName}",
            StandardElementSave => $"Standards/{oldName}",
            _ => oldName
        };

        string qualifiedNewName = elementSave switch
        {
            ScreenSave => $"Screens/{elementSave.Name}",
            ComponentSave => $"Components/{elementSave.Name}",
            StandardElementSave => $"Standards/{elementSave.Name}",
            _ => elementSave.Name
        };

        foreach (var referenceChange in changes.VariableReferenceChanges)
        {
            var variableList = referenceChange.VariableReferenceList;
            var oldLine = variableList.ValueAsIList[referenceChange.LineIndex]?.ToString();
            if (oldLine == null) continue;

            var equalIndex = oldLine.IndexOf("=");
            if (equalIndex < 0) continue;

            var left = oldLine.Substring(0, equalIndex).Trim();
            var right = oldLine.Substring(equalIndex + 1).Trim();

            if (right.StartsWith(qualifiedOldName + "."))
            {
                right = qualifiedNewName + right.Substring(qualifiedOldName.Length);
            }

            variableList.ValueAsIList[referenceChange.LineIndex] = $"{left} = {right}";
            containersToSave.Add(referenceChange.Container);
        }

        foreach (var container in containersToSave)
        {
            _fileCommands.TryAutoSaveElement(container);
        }
    }

    public GeneralResponse HandleRename(IInstanceContainer instanceContainer, InstanceSave? instance, string oldName, NameChangeAction action, bool askAboutRename = true)
    {
        GeneralResponse toReturn = new GeneralResponse();
        toReturn.Succeeded = false;

        try
        {
            isRenamingXmlFile = instance == null;

            bool shouldContinue = true;

            shouldContinue = ValidateWithPopup(instanceContainer, instance, shouldContinue);


            var elementSave = instanceContainer as ElementSave;
            shouldContinue = AskIfToRenameElement(oldName, askAboutRename, action, shouldContinue);

            if (shouldContinue)
            {
                if (elementSave != null)
                {
                    RenameAllReferencesTo(elementSave, instance, oldName);
                }

                // Even though this gets called from the PropertyGrid methods which eventually
                // save this object, we want to force a save here to make sure it worked.  If it
                // does, then we're safe to delete the old files.
                _fileCommands.TryAutoSaveObject(instanceContainer);

                if (isRenamingXmlFile)
                {
                    RenameXml(elementSave, oldName);
                }

                _guiCommands.RefreshElementTreeView(instanceContainer);
            }

            if (!shouldContinue && isRenamingXmlFile)
            {
                elementSave.Name = oldName;
            }
            else if (!shouldContinue && instance != null)
            {
                instance.Name = oldName;
            }
            toReturn.Succeeded = shouldContinue;
        }
        catch (Exception e)
        {
            _dialogService.ShowMessage("Error renaming instance container " + instanceContainer.ToString() + "\n\n" + e.ToString());
            toReturn.Succeeded = false;
        }
        finally
        {
            isRenamingXmlFile = false;
        }
        return toReturn;
    }

    private void RenameXml(ElementSave elementSave, string oldName)
    {
        // If we got here that means all went okay, so we should delete the old files
        var oldXml = elementSave.GetFullPathXmlFile(oldName);
        var newXml = elementSave.GetFullPathXmlFile();

        // Delete the XML.
        // If the file doesn't
        // exist, no biggie - we
        // were going to delete it
        // anyway.
        if (oldXml.Exists())
        {
            System.IO.File.Delete(oldXml.FullPath);
        }

        _pluginManager.ElementRename(elementSave, oldName);

        _fileCommands.TryAutoSaveProject();

        var oldDirectory = oldXml.GetDirectoryContainingThis();
        var newDirectory = newXml.GetDirectoryContainingThis();

        bool didMoveToNewDirectory = oldDirectory != newDirectory;

        if (didMoveToNewDirectory)
        {
            // refresh the entire tree view because the node is moving:
            _guiCommands.RefreshElementTreeView();
        }
        else
        {
            _guiCommands.RefreshElementTreeView(elementSave);
        }
    }

    private void RenameAllReferencesTo(ElementSave elementSave, InstanceSave instance, string oldName)
    {
        var project = _projectManager.GumProjectSave;
        // Tell the GumProjectSave to react to the rename.
        // This changes the names of the ElementSave references.
        project.ReactToRenamed(elementSave, instance, oldName);

        project.SortElementAndBehaviors();

        _fileCommands.TryAutoSaveProject();

        if (instance == null)
        {
            var elementChanges = GetChangesForRenamedElement(elementSave, oldName);
            ApplyElementRenameChanges(elementChanges, elementSave, oldName);
        }
        if (instance != null)
        {
            string newName = instance.Name;

            if (_selectedState.SelectedElement != null)
            {
                foreach (StateSave stateSave in _selectedState.SelectedElement.AllStates)
                {
                    stateSave.ReactToInstanceNameChange(instance, oldName, newName);
                }

                foreach (var eventSave in _selectedState.SelectedElement.Events)
                {
                    if (eventSave.GetSourceObject() == oldName)
                    {
                        eventSave.Name = instance.Name + "." + eventSave.GetRootName();
                    }
                }

                var renamedDefaultChildContainer = false;
                foreach (var state in _selectedState.SelectedElement.AllStates)
                {
                    var variable = state.Variables.FirstOrDefault(item => item.Name == "DefaultChildContainer");

                    if (variable?.Value as string != null)
                    {
                        var value = variable.Value as string;
                        if (value == oldName)
                        {
                            variable.Value = newName;
                            renamedDefaultChildContainer = true;
                        }
                    }
                }

                if (renamedDefaultChildContainer)
                {
                    var elementsToConsider = ObjectFinder.Self.GetElementsReferencing(elementSave);

                    foreach (var elementToCheckParent in elementsToConsider)
                    {
                        var shouldSaveElement = false;

                        foreach (var state in elementToCheckParent.AllStates)
                        {
                            foreach (var variable in state.Variables)
                            {
                                if (variable.GetRootName() == "Parent" && (variable.Value as string)?.Contains(".") == true)
                                {
                                    var value = variable.Value as string;
                                    var valueBeforeDot = value.Substring(0, value.IndexOf("."));
                                    var valueAfterDot = value.Substring(value.IndexOf(".") + 1);
                                    if (valueAfterDot == oldName)
                                    {
                                        // let's be safe, see if the instance is of the type elementSave
                                        var parentInstance = elementToCheckParent.GetInstance(valueBeforeDot);
                                        var parentInstanceElement = ObjectFinder.Self.GetElementSave(parentInstance);
                                        if (parentInstanceElement == elementSave)
                                        {
                                            variable.Value = parentInstance.Name + "." + newName;
                                            shouldSaveElement = true;
                                        }
                                    }
                                }
                            }
                        }
                        if (shouldSaveElement)
                        {
                            _fileCommands.TryAutoSaveElement(elementToCheckParent);
                        }

                    }
                }

            }
        }
    }

    private bool AskIfToRenameElement(string oldName, bool askAboutRename, NameChangeAction action, bool shouldContinue)
    {
        if (shouldContinue && isRenamingXmlFile && askAboutRename)
        {
            string moveOrRename;
            if (action == NameChangeAction.Move)
            {
                moveOrRename = "move";
            }
            else
            {
                moveOrRename = "rename";
            }

            string message = $"Are you sure you want to {moveOrRename} {oldName}?\n\n" +
                "This will change the file name for " + oldName + " which may break " +
                "external references to this object.";

            shouldContinue = _dialogService.ShowYesNoMessage(message, "Rename Object and File?");
        }

        return shouldContinue;
    }

    private bool ValidateWithPopup(IInstanceContainer instanceContainer, InstanceSave instance, bool shouldContinue)
    {
        string whyNot;
        if (instance != null)
        {
            if (_nameVerifier.IsInstanceNameValid(instance.Name, instance, instanceContainer, out whyNot) == false)
            {
                _dialogService.ShowMessage(whyNot);
                shouldContinue = false;
            }
        }
        else if (instanceContainer is ElementSave elementSave and not StandardElementSave)
        {
            // Prevent failures if a "\" is used instead of a "/" for the folder separator
            var nameWithoutFolder = elementSave.Name.Replace('\\', '/');
            string? folder = null;

            if (nameWithoutFolder.Contains('/'))
            {
                var lastIndexOfSlash = nameWithoutFolder.LastIndexOf('/');
                folder = nameWithoutFolder.Substring(0, lastIndexOfSlash);
                nameWithoutFolder = nameWithoutFolder.Substring(lastIndexOfSlash + 1);
            }

            if (_nameVerifier.IsElementNameValid(nameWithoutFolder, folder, elementSave, out whyNot) == false)
            {
                _dialogService.ShowMessage(whyNot);
                shouldContinue = false;
            }
        }

        return shouldContinue;
    }

    // public void HandleRename(ElementSave containerElement, EventSave eventSave, string oldName)
    // {
    //     List<ElementSave> elements = new List<ElementSave>();
    //     elements.AddRange(_projectManager.GumProjectSave.Screens);
    //     elements.AddRange(_projectManager.GumProjectSave.Components);
    //
    //     foreach (var possibleElement in elements)
    //     {
    //         foreach (var instance in possibleElement.Instances.Where(item => item.IsOfType(containerElement.Name)))
    //         {
    //             foreach (var eventToRename in possibleElement.Events.Where(item => item.GetSourceObject() == instance.Name))
    //             {
    //                 if (eventToRename.GetRootName() == oldName)
    //                 {
    //                     eventToRename.Name = instance.Name + "." + eventSave.ExposedAsName;
    //                 }
    //             }
    //
    //         }
    //
    //     }
    //
    //
    //
    // }


    #endregion

    #region Variable

    public VariableChangeResponse GetChangesForRenamedVariable(IStateContainer owner, string oldFullName, string oldStrippedOrExposedName)
    {
        List<VariableChange> variableChanges = new List<VariableChange>();
        List<VariableReferenceChange> variableReferenceChanges = new List<VariableReferenceChange>();

        var project = _projectState.GumProjectSave;

        var changedVariableOwnerElement = owner as ElementSave;

        // consider:
        // Inheritance
        // Instances using this

        List<ElementSave>? inheritingElements = new List<ElementSave>();
        if (changedVariableOwnerElement != null)
        {
            inheritingElements.AddRange(ObjectFinder.Self.GetElementsInheritingFrom(changedVariableOwnerElement));
        }

        foreach (var item in inheritingElements)
        {
            foreach (var state in item.AllStates)
            {
                foreach (var variable in state.Variables)
                {
                    if (variable.ExposedAsName == oldStrippedOrExposedName)
                    {
                        variableChanges.Add(new VariableChange
                        {
                            Container = item,
                            Category = item.Categories.FirstOrDefault(item => item.States.Contains(state)),
                            State = state,
                            Variable = variable
                        });
                    }
                }

            }
        }

        foreach (var element in project.AllElements)
        {
            foreach (var state in element.AllStates)
            {
                foreach (var variable in state.Variables)
                {
                    if (variable.GetRootName() == oldStrippedOrExposedName && !string.IsNullOrEmpty(variable.SourceObject))
                    {
                        var instance = element.GetInstance(variable.SourceObject);
                        if (instance != null)
                        {
                            var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                            if (inheritingElements.Contains(instanceElement) || instanceElement == changedVariableOwnerElement)
                            {
                                variableChanges.Add(new VariableChange
                                {
                                    Container = element,
                                    Category = element.Categories.FirstOrDefault(item => item.States.Contains(state)),
                                    State = state,
                                    Variable = variable
                                });
                            }
                        }
                    }
                }
                foreach (var variableList in state.VariableLists)
                {
                    if (variableList.GetRootName() == "VariableReferences")
                    {
                        ElementSave rootLeftSideElement = null;
                        InstanceSave leftSideInstance = null;
                        if (string.IsNullOrEmpty(variableList.SourceObject))
                        {
                            rootLeftSideElement = element;
                        }
                        else
                        {
                            leftSideInstance = element.GetInstance(variableList.SourceObject);
                            if (leftSideInstance != null)
                            {
                                rootLeftSideElement = ObjectFinder.Self.GetElementSave(leftSideInstance);
                            }
                        }
                        InstanceSave instanceLeft = element.GetInstance(variableList.SourceObject);


                        // loop through the items and see if any are using this:
                        for (int i = 0; i < variableList.ValueAsIList.Count; i++)
                        {
                            var line = variableList.ValueAsIList[i];

                            if (line is not string asString || asString.StartsWith("//") || asString.Contains("=") == false || asString.Contains(oldStrippedOrExposedName) == false)
                            {
                                continue;
                            }

                            var right = asString.Substring(asString.IndexOf("=") + 1).Trim();
                            var leftSide = asString.Substring(0, asString.IndexOf("=")).Trim();


                            var matchesLeft = false;
                            if (leftSide == oldStrippedOrExposedName)
                            {
                                string oldSourceObject = null;
                                if (oldFullName.Contains("."))
                                {
                                    oldSourceObject = oldFullName.Substring(0, oldFullName.IndexOf("."));
                                }
                                // See if the element that contains the left side variable is a match...
                                matchesLeft = changedVariableOwnerElement == rootLeftSideElement || inheritingElements.Contains(rootLeftSideElement) ||
                                    // or if we are in the same instance as the one that owns the variable reference...
                                    (element == changedVariableOwnerElement && variableList.SourceObject == oldSourceObject);
                            }

                            var stateContainingRightSideVariable = state;
                            GumRuntime.ElementSaveExtensions.GetRightSideAndState(ref right, ref stateContainingRightSideVariable);
                            var matchesRight = false;
                            if (right == oldStrippedOrExposedName || right.EndsWith("." + oldStrippedOrExposedName))
                            {
                                // see if the owner of the right side is this element or an inheriting element:
                                // finish here....
                                var rightSideOwner = stateContainingRightSideVariable.ParentContainer;

                                matchesRight = changedVariableOwnerElement == rightSideOwner || inheritingElements.Contains(rightSideOwner);
                            }


                            if (matchesLeft || matchesRight)
                            {
                                // we have a match!!
                                var referenceChange = new VariableReferenceChange();
                                referenceChange.Container = element;
                                referenceChange.VariableReferenceList = variableList;
                                referenceChange.LineIndex = i;
                                referenceChange.ChangedSide = (matchesLeft && matchesRight) ? SideOfEquals.Both
                                    : matchesLeft ? SideOfEquals.Left
                                    : SideOfEquals.Right;
                                variableReferenceChanges.Add(referenceChange);
                            }
                        }
                    }
                }
            }
        }

        return new VariableChangeResponse
        {
            VariableChanges = variableChanges,
            VariableReferenceChanges = variableReferenceChanges
        };
    }

    public void ApplyVariableRenameChanges(VariableChangeResponse changes,
        string oldStrippedOrExposedName, string newStrippedOrExposedName,
        HashSet<ElementSave> elementsNeedingSave)
    {
        foreach (var change in changes.VariableChanges)
        {
            if (change.Container is ElementSave element)
                elementsNeedingSave.Add(element);

            if (change.Variable.ExposedAsName == oldStrippedOrExposedName)
            {
                change.Variable.ExposedAsName = newStrippedOrExposedName;
            }
            else if (change.Variable.GetRootName() == oldStrippedOrExposedName)
            {
                var prefix = change.Variable.SourceObject != null
                    ? change.Variable.SourceObject + "."
                    : string.Empty;
                change.Variable.Name = prefix + newStrippedOrExposedName;
            }
        }

        foreach (var referenceChange in changes.VariableReferenceChanges)
        {
            if (referenceChange.Container != null)
                elementsNeedingSave.Add(referenceChange.Container);

            var variableList = referenceChange.VariableReferenceList;
            var oldLine = variableList.ValueAsIList[referenceChange.LineIndex]?.ToString();
            if (oldLine == null) continue;

            var leftAndRight = oldLine.Split('=').Select(item => item.Trim()).ToArray();
            if (leftAndRight.Length < 2) continue;

            if (referenceChange.ChangedSide is SideOfEquals.Left or SideOfEquals.Both)
            {
                if (leftAndRight[0] == oldStrippedOrExposedName)
                    leftAndRight[0] = newStrippedOrExposedName;
            }

            if (referenceChange.ChangedSide is SideOfEquals.Right or SideOfEquals.Both)
            {
                if (leftAndRight[1] == oldStrippedOrExposedName)
                {
                    leftAndRight[1] = newStrippedOrExposedName;
                }
                else if (leftAndRight[1].EndsWith("." + oldStrippedOrExposedName))
                {
                    var newLength = leftAndRight[1].Length - oldStrippedOrExposedName.Length;
                    leftAndRight[1] = leftAndRight[1].Substring(0, newLength) + newStrippedOrExposedName;
                }
            }

            variableList.ValueAsIList[referenceChange.LineIndex] = $"{leftAndRight[0]}={leftAndRight[1]}";
        }
    }


    #endregion
}
