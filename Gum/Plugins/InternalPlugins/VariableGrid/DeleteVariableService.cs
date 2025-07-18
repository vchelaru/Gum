using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

public interface IDeleteVariableService
{
    bool CanDeleteVariable(VariableSave variable);
    void DeleteVariable(VariableSave variable, IStateContainer stateContainer);
}
public class DeleteVariableService : IDeleteVariableService
{
    private readonly UndoManager _undoManager;
    private readonly FileCommands _fileCommands;
    private readonly GuiCommands _guiCommands;
    private readonly RenameLogic _renameLogic;
    private readonly IDialogService _dialogService;

    public DeleteVariableService(FileCommands fileCommands)
    {
        _undoManager = Locator.GetRequiredService<UndoManager>();
        _fileCommands = fileCommands;
        _guiCommands = Locator.GetRequiredService<GuiCommands>();
        _renameLogic = Locator.GetRequiredService<RenameLogic>();
        _dialogService = Locator.GetRequiredService<IDialogService>();
    }

    public bool CanDeleteVariable(VariableSave variable)
    {
        return variable.IsCustomVariable;
    }

    public void DeleteVariable(VariableSave variable, IStateContainer stateContainer)
    {



        var response = GetIfCanDeleteVariable(variable, stateContainer);

        if(response.Succeeded == false)
        {
            _dialogService.ShowMessage(response.Message);
        }
        else
        {

            using var undoLock = _undoManager.RequestLock();
            var elementSave = stateContainer as ElementSave;
            if(elementSave != null)
            {
                elementSave.DefaultState.Variables.Remove(variable);
                _fileCommands.TryAutoSaveElement(elementSave);
            }
            else if(stateContainer is BehaviorSave behavior)
            {
                behavior.RequiredVariables.Variables.Remove(variable);
                _fileCommands.TryAutoSaveObject(behavior);
            }

            _guiCommands.RefreshVariables(force: true);

            PluginManager.Self.VariableDelete(elementSave, variable.Name);
        }
    }

    private GeneralResponse GetIfCanDeleteVariable(VariableSave variable, IStateContainer stateContainer)
    {
        var isVariableContained = false;

        if (stateContainer is ElementSave elementSave)
        {
            isVariableContained =
                elementSave.DefaultState.Variables.Contains(variable);
        }
        else if (stateContainer is BehaviorSave behaviorSave)
        {
            isVariableContained = behaviorSave.RequiredVariables.Variables.Contains(variable);
        }

        if(!isVariableContained)
        {
            return GeneralResponse.UnsuccessfulWith($"The variable {variable} is not contained in {stateContainer}");
        }

        var renames = _renameLogic.GetVariableChangesForRenamedVariable(stateContainer, variable.Name, variable.GetRootName());

        if (renames.VariableReferenceChanges.Count > 0)
        {
            string message = $"Cannot delete variable {variable} because it is referenced by:\n\n";
            foreach(var item in renames.VariableReferenceChanges)
            {
                message += $"{item.VariableReferenceList.ValueAsIList[item.LineIndex]} in {item.VariableReferenceList.Name} ({item.Container})\n";
            }
            return GeneralResponse.UnsuccessfulWith(message);
        }

        return GeneralResponse.SuccessfulResponse;
    }
}