using CommonFormsAndControls;
using ExCSS;
using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Plugins.VariableGrid;
using Gum.ToolCommands;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.Services.Dialogs;
using Gum.Undo;

namespace Gum.Services;

public class EditVariableService : IEditVariableService
{
    private readonly IRenameLogic _renameLogic;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IUndoManager _undoManager;
    private readonly Func<AddVariableViewModel> _addVariableViewModelFactory;

    public EditVariableService(IRenameLogic renameLogic,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IUndoManager undoManager,
        Func<AddVariableViewModel> addVariableViewModelFactory)
    {
        _renameLogic = renameLogic;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _undoManager = undoManager;
        _addVariableViewModelFactory = addVariableViewModelFactory;
    }

    /// <summary>
    /// Returns the context-menu label for editing the given variable, or null when the variable
    /// offers no edit action. The wording reflects the available edit mode: an exposed variable can
    /// only be renamed ("Rename Variable [...]"), while a custom or behavior variable opens the full
    /// Add/Edit Variable dialog ("Edit Variable [...]").
    /// </summary>
    public string? GetEditVariableMenuLabel(VariableSave variableSave, IStateContainer stateListCategoryContainer)
    {
        var editMode = GetAvailableEditModeFor(variableSave, stateListCategoryContainer);
        return editMode switch
        {
            // Exposing only renames the exposed name; the full type/value edit is not available, so
            // don't promise "Edit", and show the exposed name (what's actually being renamed).
            VariableEditMode.ExposedName => $"Rename Variable [{variableSave.ExposedAsName}]",
            VariableEditMode.FullEdit => $"Edit Variable [{variableSave.Name}]",
            _ => null
        };
    }

    public VariableEditMode GetAvailableEditModeFor(VariableSave variableSave, IStateContainer stateCategoryListContainer)
    {
        if (variableSave == null)
        {
            return VariableEditMode.None;
        }

        var behaviorSave = stateCategoryListContainer as BehaviorSave;
        // for now only edit variables inside of behaviors:
        if (behaviorSave != null)
        {
            return VariableEditMode.FullEdit;
        }

        if(variableSave.IsCustomVariable)
        {
            return VariableEditMode.FullEdit;
        }

        if (stateCategoryListContainer is ElementSave elementSave)
        {
            //var rootVariable = ObjectFinder.Self.GetRootVariable(variableSave.Name, stateListCategoryContainer as ElementSave);

            var isExposed = !string.IsNullOrEmpty(variableSave.ExposedAsName);

            return isExposed ? VariableEditMode.ExposedName : VariableEditMode.None;
        }

        return VariableEditMode.None;
    }


    public void ShowEditVariableWindow(VariableSave variable, IStateContainer container)
    {
        var editmode = GetAvailableEditModeFor(variable, container);

        if (editmode == VariableEditMode.ExposedName)
        {
            ShowEditExposedUi(variable, container);
        }
        else if (editmode == VariableEditMode.FullEdit)
        {
            ShowFullEditUi(variable, container);
        }

    }

    private void ShowEditExposedUi(VariableSave variable, IStateContainer container)
    {
        string message = "Enter desired exposed variable name.";
        string title = "Edit Variable Name";



        var changes = _renameLogic.GetChangesForRenamedVariable(container, variable.Name, variable.ExposedAsName);
        string changesDetails = changes.GetChangesDetails();

        if(!string.IsNullOrEmpty(changesDetails))
        {
            message += "\n\n" + changesDetails;
        }

        GetUserStringOptions options = new() { InitialValue = variable.ExposedAsName };

        if (_dialogService.GetUserString(message, title, options) is { } result)
        {
            RenameExposedVariable(variable, result, container, changes);
        }
    }

    private void RenameExposedVariable(VariableSave variable, string newName, IStateContainer container, VariableChangeResponse changeResponse)
    {
        using var undoLock = _undoManager.RequestLock();

        var variableChanges = changeResponse.VariableChanges;

        var oldName = variable.ExposedAsName;

        variable.ExposedAsName = newName;

        HashSet<ElementSave> changedElements = new HashSet<ElementSave>();

        if(container is ElementSave containerElement)
        {
            changedElements.Add(containerElement);
        }

        foreach (var change in variableChanges)
        {
            var element = change.Container as ElementSave;
            if (element != null)
            {
                changedElements.Add(element);
            }

            if(change.Variable.ExposedAsName == oldName)
            {
                change.Variable.ExposedAsName = newName;
            }
            else if(change.Variable.GetRootName() == oldName)
            {
                var prefix = string.Empty;
                if(change.Variable.SourceObject != null)
                {
                    prefix = change.Variable.SourceObject + ".";
                }

                change.Variable.Name = prefix + newName;
            }
        }

        // We can re-use the logic in the AddVariableViewModel:
        var vm = _addVariableViewModelFactory();
        vm.RenameType = RenameType.ExposedName;
        vm.ApplyVariableReferenceChanges(changeResponse, newName, oldName, changedElements);

        _guiCommands.RefreshVariables(force:true);
        foreach(var element in changedElements)
        {
            _fileCommands.TryAutoSaveElement(element);
        }

    }

    private void ShowFullEditUi(VariableSave variable, IStateContainer container)
    {
        _dialogService.Show<AddVariableViewModel>(vm =>
        {
            var changes =
                _renameLogic.GetChangesForRenamedVariable(container, variable.Name, variable.Name);
            string changesDetails = changes.GetChangesDetails();

            vm.RenameType = RenameType.NormalName;
            vm.Variable = variable;
            vm.Element = container as ElementSave;
            vm.SelectedItem = variable.Type;
            vm.EnteredName = variable.Name;
            vm.VariableChangeResponse = changes;
            vm.DetailText = changesDetails;
            vm.Title = "Edit Variable";
        });
    }
}
