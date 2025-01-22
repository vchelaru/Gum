using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

public interface IDeleteVariableLogic
{
    bool CanDeleteVariable(VariableSave variable);
    void DeleteVariable(VariableSave variable, IStateContainer stateContainer);
}
public class DeleteVariableLogic : IDeleteVariableLogic
{
    private readonly UndoManager _undoManager;
    private readonly FileCommands _fileCommands;
    private readonly GuiCommands _guiCommands;
    private readonly RenameLogic _renameLogic;

    public DeleteVariableLogic(UndoManager undoManager, FileCommands fileCommands, GuiCommands guiCommands, 
        RenameLogic renameLogic)
    {
        _undoManager = undoManager;
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _renameLogic = renameLogic;
    }

    public bool CanDeleteVariable(VariableSave variable)
    {
        return variable.IsCustomVariable;
    }

    public void DeleteVariable(VariableSave variable, IStateContainer stateContainer)
    {
        if(stateContainer is ElementSave elementSave && 
            elementSave.DefaultState.Variables.Contains(variable))
        {
            var response = GetIfCanDeleteVariable(variable, stateContainer);

            if(response.Succeeded == false)
            {
                _guiCommands.ShowMessage(response.Message);
            }
            else
            {

                using var undoLock = _undoManager.RequestLock();
                elementSave.DefaultState.Variables.Remove(variable);

                _fileCommands.TryAutoSaveElement(elementSave);
                _guiCommands.RefreshVariables(force: true);
            }

        }
    }

    private GeneralResponse GetIfCanDeleteVariable(VariableSave variable, IStateContainer stateContainer)
    {
        var renames = _renameLogic.GetVariableChangesForRenamedVariable(stateContainer, variable, variable.GetRootName());

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