using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.ToolStates;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices.Editing;
using Gum.Undo;
using Gum.Commands;
using Gum.Services.Dialogs;
using Gum.Plugins.InternalPlugins.VariableGrid;
using ToolsUtilities;

namespace Gum.Logic;

public class RenameLogic : IRenameLogic, IUndoRenameLogic
{
    static bool isRenamingXmlFile;

    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IDeleteLogic _deleteLogic;
    private readonly IRenameProjectProvider _renameProjectProvider;
    private readonly IRenamePluginNotifier _renamePluginNotifier;
    private readonly IStandardElementsManagerGumTool _standardElementsManagerGumTool;
    private readonly IReferenceFinder _referenceFinder;
    // Lazy: UndoManager depends on this class through IUndoRenameLogic.
    private readonly Lazy<IUndoManager> _undoManager;
    private readonly BehaviorRenamer _behaviorRenamer;
    private readonly IRenameReferenceApplier _applier = new RenameReferenceApplier();

    public RenameLogic(ISelectedState selectedState,
        INameVerifier nameVerifier,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IDeleteLogic deleteLogic,
        IRenameProjectProvider renameProjectProvider,
        IRenamePluginNotifier renamePluginNotifier,
        IStandardElementsManagerGumTool standardElementsManagerGumTool,
        IReferenceFinder referenceFinder,
        Lazy<IUndoManager> undoManager)
    {
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _deleteLogic = deleteLogic;
        _renameProjectProvider = renameProjectProvider;
        _renamePluginNotifier = renamePluginNotifier;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
        _referenceFinder = referenceFinder;
        _undoManager = undoManager;
        _behaviorRenamer = new BehaviorRenamer(referenceFinder,
            () => renameProjectProvider.GumProjectSave ?? throw new InvalidOperationException("No Gum project is loaded."),
            fileCommands, guiCommands);
    }

    #region Behavior

    public void RenameBehavior(BehaviorSave behavior, string newName) => _behaviorRenamer.Rename(behavior, newName);

    #endregion

    #region StateSave

    public void RenameState(StateSave stateSave, StateSaveCategory? category, string newName, bool applyRefactoringChanges = true)
    {
        if (!_nameVerifier.IsStateNameValid(newName, category, stateSave, out string? whyNotValid))
        {
            _dialogService.ShowMessage(whyNotValid);
        }
        else
        {
            string oldName = stateSave.Name;

            var container = ObjectFinder.Self.GetStateContainerOf(stateSave);
            var stateChanges = applyRefactoringChanges
                ? GetChangesForRenamedState(stateSave, oldName, container, category)
                : new StateReferences();

            stateSave.Name = newName;
            _guiCommands.RefreshStateTreeView();
            // I don't think we need to save the project when renaming a state:
            //_fileCommands.TryAutoSaveProject();

            // Renaming the state should refresh the property grid
            // because it displays the state name at the top
            _guiCommands.RefreshVariables(force: true);

            _renamePluginNotifier.StateRename(stateSave, oldName);

            ApplyStateReferences(stateChanges, stateSave);

            _fileCommands.TryAutoSaveCurrentObject();
        }
    }

    public StateReferences GetChangesForRenamedState(
        StateSave state, string oldName, IStateContainer? container, StateSaveCategory? category)
    {
        return _referenceFinder.GetReferencesToState(state, oldName, container, category);
    }

    public void ApplyStateReferences(StateReferences changes, StateSave state)
    {
        var elementsToSave = new HashSet<ElementSave>();
        var observer = new CrossElementChangeObserver();

        _applier.ApplyStateReferences(changes, state, elementsToSave, observer);

        _undoManager.Value.RecordCrossElementVariableChanges(observer.Changes);

        foreach (var elementToSave in elementsToSave)
        {
            _fileCommands.TryAutoSaveElement(elementToSave);
        }
    }

    #endregion

    #region Category

    public void AskToRenameStateCategory(StateSaveCategory category, IStateContainer owner)
    {
        if (owner is ElementSave elementSave)
        {
            // This category can only be renamed if no behaviors require it
            var behaviorsNeedingCategory = _deleteLogic.GetBehaviorsNeedingCategory(category, elementSave as ComponentSave);

            if (ProjectRenamer.GetWhyCategoryCannotBeRenamed(category, behaviorsNeedingCategory) is { } whyNot)
            {
                _dialogService.ShowMessage(whyNot);
                return;
            }

            string elemMessage = "Enter new category name";
            string elemTitle = "New Category";

            GetUserStringOptions elemOptions = new()
            {
                InitialValue = category.Name,
                Validator = v => _nameVerifier.IsCategoryNameValid(v, elementSave, out string? whyNotValid, category) ? null : whyNotValid
            };
            string elemOldName = category.Name;
            var elemChanges = GetChangesForRenamedCategory(elementSave, category, elemOldName);

            var elemChangesDetails = elemChanges.GetChangesDetails();
            if (!string.IsNullOrEmpty(elemChangesDetails))
            {
                elemMessage += "\n\n" + elemChangesDetails;
            }

            if (_dialogService.GetUserString(elemMessage, elemTitle, elemOptions) is { } elemNewName)
            {
                ApplyCategoryReferences(elemChanges, elementSave, category, elemOldName, elemNewName);
            }
        }
        else if (owner is BehaviorSave behaviorSave)
        {
            string message = "Enter new category name\n\nNote: Renaming states/categories in behaviors does not update the components that use this behavior.";
            string title = "New Category";
            string oldName = category.Name;

            GetUserStringOptions options = new()
            {
                InitialValue = category.Name,
                Validator = v => _nameVerifier.IsCategoryNameValid(v, behaviorSave, out string? whyNotValid, category) ? null : whyNotValid
            };

            if (_dialogService.GetUserString(message, title, options) is { } newName)
            {
                ApplyCategoryReferences(new CategoryReferences(), behaviorSave, category, oldName, newName);
            }
        }
    }

    public void ApplyCategoryReferences(CategoryReferences categoryChanges, IStateContainer owner, StateSaveCategory category, string oldName, string newName)
    {
        var elementsWithChangedVariables = new HashSet<ElementSave>();
        var observer = new CrossElementChangeObserver();

        var selfVariables = _applier.ApplyCategoryReferences(categoryChanges, owner, category, oldName, newName,
            elementsWithChangedVariables, observer);

        foreach (var variable in selfVariables)
        {
            variable.CustomTypeConverter =
                new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(category.Name, _selectedState);
        }

        _undoManager.Value.RecordCrossElementVariableChanges(observer.Changes);

        _guiCommands.RefreshStateTreeView();
        // I don't think we need to save the project when renaming a state:
        //_fileCommands.TryAutoSaveProject();

        _renamePluginNotifier.CategoryRename(category, oldName);

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

    public CategoryReferences GetChangesForRenamedCategory(IStateContainer owner, StateSaveCategory category, string oldName)
    {
        return _referenceFinder.GetReferencesToStateCategory(owner, category, oldName);
    }

    #endregion

    #region Instance


    public InstanceReferences GetChangesForRenamedInstance(ElementSave containerElement, InstanceSave instance, string oldName)
    {
        return _referenceFinder.GetReferencesToInstance(containerElement, instance, oldName);
    }

    public void ApplyInstanceReferences(InstanceReferences changes, string newName, string oldName, HashSet<ElementSave> elementsToSave)
    {
        var observer = new CrossElementChangeObserver();

        _applier.ApplyInstanceReferences(changes, newName, oldName, elementsToSave, observer);

        // Recorded into the rename's undo action (HandleRename holds the lock); the renamed element's
        // own lines are also restored by its snapshot, which leaves those changes nothing to replay.
        _undoManager.Value.RecordCrossElementVariableChanges(observer.Changes);
    }

    #endregion

    #region Element

    public ElementReferences GetChangesForRenamedElement(ElementSave elementSave, string oldName)
    {
        return _referenceFinder.GetReferencesToElement(elementSave, oldName);
    }

    public void ApplyElementReferences(ElementReferences changes, ElementSave elementSave, string oldName)
    {
        var containersToSave = new HashSet<ElementSave>();

        _applier.ApplyElementReferences(changes, elementSave, oldName, containersToSave);

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

            ElementReferences? elementRenameChanges = null;
            if (isRenamingXmlFile && elementSave != null)
            {
                elementRenameChanges = GetChangesForRenamedElement(elementSave, oldName);
            }

            shouldContinue = AskIfToRenameElement(oldName, askAboutRename, action, shouldContinue, elementRenameChanges);

            InstanceReferences? instanceRenameChanges = null;
            if (!isRenamingXmlFile && elementSave != null && instance != null)
            {
                instanceRenameChanges = GetChangesForRenamedInstance(elementSave, instance, oldName);
            }

            shouldContinue = AskToRenameInstance(instance, oldName, askAboutRename, shouldContinue, instanceRenameChanges);

            if (shouldContinue)
            {
                // An instance rename rewrites references on other elements; the lock gathers those
                // into the same undo action as the rename (#5248). The undo system replays element
                // renames through this method, so only instance renames take it.
                using var undoLock = instance != null ? _undoManager.Value.RequestLock() : null;

                if (elementSave != null)
                {
                    RenameAllReferencesTo(elementSave, instance, oldName);
                }

                if (instanceRenameChanges != null && instance != null)
                {
                    var elementsToSave = new HashSet<ElementSave>();
                    ApplyInstanceReferences(instanceRenameChanges, instance.Name, oldName, elementsToSave);
                    foreach (var element in elementsToSave)
                        _fileCommands.TryAutoSaveElement(element);
                }

                // Even though this gets called from the PropertyGrid methods which eventually
                // save this object, we want to force a save here to make sure it worked.  If it
                // does, then we're safe to delete the old files.
                _fileCommands.TryAutoSaveObject(instanceContainer);

                // Only an element's own file is renamed; a behavior is renamed by EditCommands.
                if (isRenamingXmlFile && elementSave != null)
                {
                    RenameXml(elementSave, oldName);
                }

                _guiCommands.RefreshElementTreeView(instanceContainer);
            }

            if (!shouldContinue && isRenamingXmlFile && elementSave != null)
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
        var oldXml = _fileCommands.GetFullPathXmlFile(elementSave, oldName);
        var newXml = _fileCommands.GetFullFileName(elementSave);

        // A rename that only changes casing (e.g. "GameMenuScreens/Foo" -> "gamemenuscreens/Foo")
        // points oldXml and newXml at the SAME physical file on a case-insensitive filesystem
        // (Windows/macOS). TryAutoSaveObject already wrote the current content there under the new
        // name, so deleting "the old file" (below) would delete that just-saved content outright.
        // File.Move corrects the on-disk casing instead.
        // Both are null only for a project that was never saved, which has no files to move.
        bool isSameFileDifferentCase = oldXml != null && newXml != null &&
            oldXml.FullPath != newXml.FullPath &&
            string.Equals(oldXml.FullPath, newXml.FullPath, StringComparison.OrdinalIgnoreCase);

        if (isSameFileDifferentCase && oldXml != null && newXml != null)
        {
            if (oldXml.Exists())
            {
                System.IO.File.Move(oldXml.FullPath, newXml.FullPath, overwrite: true);
            }
        }
        // Delete the XML.
        // If the file doesn't
        // exist, no biggie - we
        // were going to delete it
        // anyway.
        else if (oldXml?.Exists() == true)
        {
            System.IO.File.Delete(oldXml.FullPath);
        }

        _renamePluginNotifier.ElementRename(elementSave, oldName);

        _fileCommands.TryAutoSaveProject();

        var oldDirectory = oldXml?.GetDirectoryContainingThis();
        var newDirectory = newXml?.GetDirectoryContainingThis();

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

    private void RenameAllReferencesTo(ElementSave elementSave, InstanceSave? instance, string oldName)
    {
        // Renames only happen while a project is open.
        var project = _renameProjectProvider.GumProjectSave
            ?? throw new InvalidOperationException("No Gum project is loaded.");
        // Tell the GumProjectSave to react to the rename.
        // This changes the names of the ElementSave references.
        project.ReactToRenamed(elementSave, instance, oldName);

        project.SortElementAndBehaviors();

        _fileCommands.TryAutoSaveProject();

        if (instance == null)
        {
            var elementChanges = GetChangesForRenamedElement(elementSave, oldName);
            ApplyElementReferences(elementChanges, elementSave, oldName);
        }
        if (instance != null)
        {
            string newName = instance.Name;

            if (_selectedState.SelectedElement != null)
            {
                foreach (StateSave stateSave in _selectedState.SelectedElement.AllStates)
                {
                    stateSave.ReactToInstanceNameChange(instance, oldName, newName, _renamePluginNotifier);
                }

                // Events and DefaultChildContainer; the variables were renamed state by state above.
                _applier.RenameInstanceEventsAndChildContainer(_selectedState.SelectedElement, instance, oldName);
                // Parent values in other elements that point at the renamed instance are rewritten
                // by ApplyInstanceReferences, which also records them for undo.
            }
        }
    }

    private bool AskIfToRenameElement(string oldName, bool askAboutRename, NameChangeAction action, bool shouldContinue, ElementReferences? elementRenameChanges = null)
    {
        if (shouldContinue && isRenamingXmlFile && askAboutRename)
        {
            string moveOrRename = action == NameChangeAction.Move ? "move" : "rename";

            string message = $"Are you sure you want to {moveOrRename} {oldName}?\n\n" +
                "This will change the file name for " + oldName + " which may break " +
                "external references to this object.";

            if (elementRenameChanges != null)
            {
                var changesDetails = elementRenameChanges.GetChangesDetails();
                if (!string.IsNullOrEmpty(changesDetails))
                {
                    message += "\n\n" + changesDetails;
                }
            }

            shouldContinue = _dialogService.ShowYesNoMessage(message, "Rename Object and File?");
        }

        return shouldContinue;
    }

    private bool AskToRenameInstance(InstanceSave? instance, string oldName, bool askAboutRename, bool shouldContinue, InstanceReferences? instanceRenameChanges)
    {
        if (shouldContinue && !isRenamingXmlFile && instance != null && askAboutRename)
        {
            var changesDetails = instanceRenameChanges?.GetChangesDetails(includeVariablesWithinElement: false);

            // Only confirm if the rename has ripple effects outside the element itself.
            // Pure renames with no external impact don't need user confirmation.
            if (!string.IsNullOrEmpty(changesDetails))
            {
                string message = $"Are you sure you want to rename {oldName} to {instance.Name}?\n\n{changesDetails}";
                shouldContinue = _dialogService.ShowYesNoMessage(message, "Rename Instance?");
            }
        }

        return shouldContinue;
    }

    private bool ValidateWithPopup(IInstanceContainer instanceContainer, InstanceSave? instance, bool shouldContinue)
    {
        string? whyNot;
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
        return _referenceFinder.GetReferencesToVariable(owner, oldFullName, oldStrippedOrExposedName);
    }

    public void ApplyVariableRenameChanges(VariableChangeResponse changes,
        string oldStrippedOrExposedName, string newStrippedOrExposedName,
        HashSet<ElementSave> elementsNeedingSave) =>
        _applier.ApplyVariableRenameChanges(changes, oldStrippedOrExposedName, newStrippedOrExposedName, elementsNeedingSave);

    public void ApplyVariableReferenceRenames(IEnumerable<VariableReferenceChange> referenceChanges,
        string oldStrippedOrExposedName, string newStrippedOrExposedName,
        HashSet<ElementSave> elementsNeedingSave) =>
        _applier.ApplyVariableReferenceRenames(referenceChanges, oldStrippedOrExposedName, newStrippedOrExposedName, elementsNeedingSave);

    #endregion
}
