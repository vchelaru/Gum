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
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.Services.Dialogs;
using WpfDataUi.DataTypes;

namespace Gum.Services;

#region Interface

public interface IEditVariableService
{
    VariableEditMode GetAvailableEditModeFor(VariableSave variableSave, IStateContainer stateCategoryListContainer);

    void ShowEditVariableWindow(VariableSave variable, IStateContainer container);

    void TryAddEditVariableOptions(InstanceMember instanceMember, VariableSave variableSave, IStateContainer stateListCategoryContainer);
}
#endregion

#region Enums

public enum VariableEditMode
{
    None,
    ExposedName,
    FullEdit
}

#endregion

public class EditVariableService : IEditVariableService
{
    private readonly IRenameLogic _renameLogic;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;

    public EditVariableService(IRenameLogic renameLogic, 
        IDialogService dialogService, 
        IGuiCommands guiCommands,
        IFileCommands fileCommands)
    {
        _renameLogic = renameLogic;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
    }

    public void TryAddEditVariableOptions(InstanceMember instanceMember, VariableSave variableSave, IStateContainer stateListCategoryContainer)
    {
        if (CanEditVariable(variableSave, stateListCategoryContainer))
        {
            instanceMember.ContextMenuEvents.Add($"Edit Variable [{variableSave.Name}]", (sender, e) =>
            {
                ShowEditVariableWindow(variableSave, stateListCategoryContainer);
            });
        }
    }

    bool CanEditVariable(VariableSave variableSave, IStateContainer stateListCategoryContainer)
    {
        return GetAvailableEditModeFor(variableSave, stateListCategoryContainer) != VariableEditMode.None;
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



        var changes = _renameLogic.GetVariableChangesForRenamedVariable(container, variable.Name, variable.ExposedAsName);
        string changesDetails = GetChangesDetails(changes);

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

    private static string GetChangesDetails(VariableChangeResponse changes)
    {
        var variableChanges = changes.VariableChanges;

        var changesDetails = string.Empty;

        if (variableChanges.Count > 0)
        {
            if (!string.IsNullOrEmpty(changesDetails))
            {
                changesDetails += "\n\n";
            }
            changesDetails += "This will also rename the following variables:";
            foreach (var change in variableChanges)
            {
                var containerName = change.Container.ToString();
                if (change.Container is ElementSave elementSave)
                {
                    containerName = elementSave.Name;
                }
                changesDetails += $"\n{change.Variable.Name} in {containerName}";
            }
        }
        var variableReferenceChanges = changes.VariableReferenceChanges;
        if (variableReferenceChanges.Count > 0)
        {
            if(!string.IsNullOrEmpty(changesDetails))
            {
                changesDetails += "\n\n";
            }
            changesDetails += "This will also modify the following variable references:";
            foreach (var change in variableReferenceChanges)
            {
                // just in case something changes this on a separate thread, let's be safe:
                try
                {
                    var line = change.VariableReferenceList.ValueAsIList[change.LineIndex];
                    changesDetails += $"\n{line} in {change.Container.Name}";

                }
                catch { }
            }
        }

        return changesDetails;
    }

    private void RenameExposedVariable(VariableSave variable, string newName, IStateContainer container, VariableChangeResponse changeResponse)
    {
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
        var vm = Locator.GetRequiredService<AddVariableViewModel>();
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
                _renameLogic.GetVariableChangesForRenamedVariable(container, variable.Name, variable.Name);
            string changesDetails = GetChangesDetails(changes);

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
