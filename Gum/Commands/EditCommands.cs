using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Gui.Windows;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.Responses;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Gum.Commands;

public class EditCommands
{
    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IRenameLogic _renameLogic;
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IDialogService _dialogService;
    private readonly ProjectCommands _projectCommands;
    private readonly VariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;

    public EditCommands(ISelectedState selectedState, 
        INameVerifier nameVerifier,
        IRenameLogic renameLogic,
        IUndoManager undoManager,
        IDialogService dialogService,
        IFileCommands fileCommands,
        ProjectCommands projectCommands,
        IGuiCommands guiCommands,
        VariableInCategoryPropagationLogic variableInCategoryPropagationLogic)
    {
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _renameLogic = renameLogic;
        _undoManager = undoManager;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _projectCommands = projectCommands;
        _guiCommands = guiCommands;
        _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;
    }

    #region State

    public void AskToDeleteState(StateSave stateSave, IStateContainer stateContainer)
    {
        var deleteResponse = new DeleteResponse();
        deleteResponse.ShouldDelete = true;
        deleteResponse.ShouldShowMessage = false;

        var behaviorNeedingState = GetBehaviorsNeedingState(stateSave);

        if (behaviorNeedingState.Any())
        {
            deleteResponse.ShouldDelete = false;
            deleteResponse.ShouldShowMessage = true;
            string message =
                $"The state {stateSave.Name} cannot be removed because it is needed by the following behavior(s):";

            foreach (var behavior in behaviorNeedingState)
            {
                message += "\n" + behavior.Name;
            }

            deleteResponse.Message = message;

        }

        if (deleteResponse.ShouldDelete && stateSave.ParentContainer?.DefaultState == stateSave)
        {
            string message =
                "This state cannot be removed because it is the default state.";

            deleteResponse.ShouldDelete = false;
            deleteResponse.Message = message;
            deleteResponse.ShouldShowMessage = false;
        }

        if (deleteResponse.ShouldDelete)
        {
            deleteResponse = PluginManager.Self.GetDeleteStateResponse(stateSave, stateContainer);
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
            if (_dialogService.ShowYesNoMessage($"Are you sure you want to delete the state {stateSave.Name}?", "Delete state?"))
            {
                DeleteLogic.Self.Remove(stateSave);
            }
        }
    }

    internal void AskToRenameState(StateSave stateSave, IStateContainer stateContainer)
    {
        var behaviorNeedingState = GetBehaviorsNeedingState(stateSave);

        if (behaviorNeedingState.Any())
        {
            string message =
                $"The state {stateSave.Name} cannot be renamed because it is needed by the following behavior(s):";

            foreach (var behavior in behaviorNeedingState)
            {
                message += "\n" + behavior.Name;
            }

            _dialogService.ShowMessage(message);
        }
        else
        {
            string message = "Enter new state name";
            string title = "Rename state";
            GetUserStringOptions options = new(){InitialValue = _selectedState.SelectedStateSave.Name};
            if (_dialogService.GetUserString(message, title, options) is { } result)
            {
                var category = stateContainer.Categories.FirstOrDefault(item => item.States.Contains(stateSave));

                using var undoLock = _undoManager.RequestLock();
                _renameLogic.RenameState(stateSave, category, result);
            }
        }
    }

    public void MoveToCategory(string categoryNameToMoveTo, StateSave stateToMove, IStateContainer stateContainer)
    {
        var newCategory = stateContainer.Categories
            .FirstOrDefault(item => item.Name == categoryNameToMoveTo);

        var oldCategory = stateContainer.Categories
            .FirstOrDefault(item => item.States.Contains(stateToMove));
        ////////////////////Early Out //////////////////////
        if (stateToMove == null || categoryNameToMoveTo == null || oldCategory == null)
        {
            return;
        }
        var behaviorsNeedingState = GetBehaviorsNeedingState(stateToMove);
        if (behaviorsNeedingState.Count > 0)
        {
            string message =
                $"The state {stateToMove.Name} cannot be moved to a different category because it is needed by the following behavior(s):";

            foreach (var behaviorNeedingState in behaviorsNeedingState)
            {
                message += "\n" + behaviorNeedingState.Name;
            }

            _dialogService.ShowMessage(message);
            return;
        }

        //////////////////End Early Out /////////////////////




        oldCategory.States.Remove(stateToMove);
        newCategory.States.Add(stateToMove);

        _guiCommands.RefreshStateTreeView();
        _selectedState.SelectedStateSave = stateToMove;

        // make sure to propagate all variables in this new state and
        // also move all existing variables to the new state (use the first)
        if (stateContainer is ElementSave element)
        {
            foreach (var variable in stateToMove.Variables)
            {
                _variableInCategoryPropagationLogic.PropagateVariablesInCategory(variable.Name,
                    element, _selectedState.SelectedStateCategorySave);
            }


            var firstState = newCategory.States.FirstOrDefault();
            if (firstState != stateToMove)
            {
                foreach (var variable in firstState.Variables)
                {
                    _variableInCategoryPropagationLogic.PropagateVariablesInCategory(variable.Name,
                        element, _selectedState.SelectedStateCategorySave);
                }
            }
        }

        PluginManager.Self.StateMovedToCategory(stateToMove, newCategory, oldCategory);

        if (stateContainer is BehaviorSave behavior)
        {
            _fileCommands.TryAutoSaveBehavior(behavior);

        }
        else if (stateContainer is ElementSave asElement)
        {
            _fileCommands.TryAutoSaveElement(asElement);
        }
    }


    #endregion

    #region Category

    public void RemoveStateCategory(StateSaveCategory category, IStateContainer stateCategoryListContainer)
    {
        DeleteLogic.Self.RemoveStateCategory(category, stateCategoryListContainer);
    }


    internal void AskToRenameStateCategory(StateSaveCategory category, ElementSave elementSave)
    {
        using var undoLock = _undoManager.RequestLock();
        _renameLogic.AskToRenameStateCategory(category, elementSave);
    }


    #endregion

    #region Behavior

    private List<BehaviorSave> GetBehaviorsNeedingState(StateSave stateSave)
    {
        List<BehaviorSave> toReturn = new List<BehaviorSave>();
        // Try to get the parent container from the state...
        var element = stateSave.ParentContainer;
        if (element == null)
        {
            // ... if we can't find it for some reason, assume it's the current element (is this bad?)
            element = _selectedState.SelectedElement;
        }

        var componentSave = element as ComponentSave;

        if (element != null)
        {
            // uncategorized states can't come from behaviors:
            bool isUncategorized = element.States.Contains(stateSave);
            StateSaveCategory elementCategory = null;

            if (!isUncategorized)
            {
                elementCategory = element.Categories.FirstOrDefault(item => item.States.Contains(stateSave));
            }

            if (elementCategory != null)
            {
                var allBehaviorsNeedingCategory = DeleteLogic.Self.GetBehaviorsNeedingCategory(elementCategory, componentSave);

                foreach (var behavior in allBehaviorsNeedingCategory)
                {
                    var behaviorCategory = behavior.Categories.First(item => item.Name == elementCategory.Name);

                    bool isStateReferencedInCategory = behaviorCategory.States.Any(item => item.Name == stateSave.Name);

                    if (isStateReferencedInCategory)
                    {
                        toReturn.Add(behavior);
                    }
                }
            }
        }

        return toReturn;
    }

    public void RemoveBehaviorVariable(BehaviorSave container, VariableSave variable)
    {
        container.RequiredVariables.Variables.Remove(variable);
        _fileCommands.TryAutoSaveBehavior(container);
        _guiCommands.RefreshVariables();
    }

    public void AddBehavior()
    {
        if (GumState.Self.ProjectState.NeedsToSaveProject)
        {
            _dialogService.ShowMessage("You must first save the project before adding a new component");
            return;
        }

        string message = "Enter new behavior name:";
        string title = "Add behavior";
        GetUserStringOptions options = new()
        {
            Validator = x => _nameVerifier.IsBehaviorNameValid(x, null, out string whyNotValid) ? null : whyNotValid
        };

        if (_dialogService.GetUserString(message, title, options) is { } name)
        {
            var behavior = new BehaviorSave();
            behavior.Name = name;

            ProjectManager.Self.GumProjectSave.BehaviorReferences.Add(new BehaviorReference { Name = name });
            ProjectManager.Self.GumProjectSave.BehaviorReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            ProjectManager.Self.GumProjectSave.Behaviors.Add(behavior);
            ProjectManager.Self.GumProjectSave.Behaviors.Sort((first, second) => first.Name.CompareTo(second.Name));

            PluginManager.Self.BehaviorCreated(behavior);

            _selectedState.SelectedBehavior = behavior;

            _fileCommands.TryAutoSaveProject();
            _fileCommands.TryAutoSaveBehavior(behavior);
        }
    }

    #endregion

    #region Element

    public void DuplicateSelectedElement()
    {
        var element = _selectedState.SelectedElement;

        if (element == null)
        {
            _dialogService.ShowMessage("You must first save the project before adding a new component");
        }
        else if (element is StandardElementSave)
        {
            _dialogService.ShowMessage("Standard Elements cannot be duplicated");
        }
        else if (element is ScreenSave)
        {
            string message = "Enter new Screen name:";
            string title = "Duplicate Screen";

            GetUserStringOptions options = new()
            {
                InitialValue = element.Name + "Copy",
                Validator = n =>
                    _nameVerifier.IsElementNameValid(n, null, null, out string whyNotValid)
                        ? null
                        : whyNotValid
            };

            // todo - handle folders... do we support folders?

            if (_dialogService.GetUserString(message, title, options) is { } name)
            {
                var newScreen = (element as ScreenSave).Clone();
                newScreen.Name = name;
                newScreen.Initialize(null);
                StandardElementsManagerGumTool.Self.FixCustomTypeConverters(newScreen);

                _projectCommands.AddScreen(newScreen);

                PluginManager.Self.ElementDuplicate(element, newScreen);
            }
        }
        else if (element is ComponentSave)
        {
            string message = "Enter new Component name:";
            string title = "Duplicate Component";

            FilePath filePath = element.Name;
            var nameWithoutPath = filePath.FileNameNoPath;

            string folder = null;
            if (element.Name.Contains("/"))
            {
                folder = element.Name.Substring(0, element.Name.LastIndexOf('/'));
            }

            GetUserStringOptions options = new()
            {
                InitialValue = nameWithoutPath + "Copy",
                Validator = n =>
                    _nameVerifier.IsElementNameValid(n, folder, null, out string whyNotValid)
                        ? null
                        : whyNotValid
            };

            if (_dialogService.GetUserString(message, title, options) is { } name)
            {
                var newComponent = (element as ComponentSave).Clone();
                if (!string.IsNullOrEmpty(folder))
                {
                    folder += "/";
                }
                newComponent.Name = folder + name;
                newComponent.Initialize(null);
                StandardElementsManagerGumTool.Self.FixCustomTypeConverters(newComponent);

                _projectCommands.AddComponent(newComponent);

                PluginManager.Self.ElementDuplicate(element, newComponent);
            }
        }

    }

    public void ShowCreateComponentFromInstancesDialog()
    {
        var element = _selectedState.SelectedElement;
        var instances = _selectedState.SelectedInstances.ToList();
        if (instances == null || instances.Count == 0 || element == null)
        {
            _dialogService.ShowMessage("You must first save the project before adding a new component");
        }
        else if (instances is List<InstanceSave>)
        {
            FilePath filePath = element.Name;
            var nameWithoutPath = filePath.FileNameNoPath;

            string message = "Name of the new component";
            string title = "Create a component";

            GetUserStringOptions options = new()
            {
                InitialValue = $"{nameWithoutPath}Component",
                Validator = x => _nameVerifier.IsComponentNameAlreadyUsed(x) ? "A component with this name already exists!" : null
            };
            //tiwcw.Option = $"Replace {nameWithoutPath} and all children with an instance of the new component";

            if (_dialogService.GetUserString(message, title, options) is { } name)
            {
                //bool replace = tiwcw.Checked

                string whyNotValid;
                _nameVerifier.IsElementNameValid(name, "", null, out whyNotValid);

                if (string.IsNullOrEmpty(whyNotValid))
                {
                    ComponentSave componentSave = new ComponentSave();
                    componentSave.BaseType = "Container";
                    string folder = null;
                    if (!string.IsNullOrEmpty(folder))
                    {
                        folder += "/";
                    }
                    componentSave.Name = folder + name;

                    StateSave defaultState;

                    // Clone instances
                    foreach (var instance in instances)
                    {
                        // Clone will fail if we are cloning an InstanceSave
                        // in a behavior because its type is BehaviorInstanceSave.
                        // Therefore, we will just manually create a copy:
                        //var instanceSave = instance.Clone();
                        //var instanceSave = instance.Clone();
                        var instanceSave = new InstanceSave();
                        instanceSave.Name = instance.Name;
                        instanceSave.BaseType = instance.BaseType;
                        instanceSave.DefinedByBase = instance.DefinedByBase;
                        instanceSave.Locked = instance.Locked;
                        instanceSave.ParentContainer = componentSave;

                        componentSave.Instances.Add(instanceSave);
                    }

                    // Clone states
                    foreach (var state in element.States)
                    {
                        if (element.DefaultState == state)
                        {
                            defaultState = state.Clone();

                            componentSave.Initialize(defaultState);
                            continue;
                        }
                        componentSave.States.Add(state.Clone());
                    }

                    StandardElementsManagerGumTool.Self.FixCustomTypeConverters(componentSave);
                    _projectCommands.AddComponent(componentSave);

                }
                else
                {
                    _dialogService.ShowMessage($"Invalid name for new component: {whyNotValid}");
                    ShowCreateComponentFromInstancesDialog();
                }
            }
        }
    }

    #endregion

    public void DeleteSelection()
    {
        DeleteLogic.Self.HandleDeleteCommand();
    }
}
