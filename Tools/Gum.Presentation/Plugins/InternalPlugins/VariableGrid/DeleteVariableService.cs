using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

public class DeleteVariableService : IDeleteVariableService
{
    private readonly IUndoManager _undoManager;
    private readonly IFileCommands _fileCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IRenameLogic _renameLogic;
    private readonly IDialogService _dialogService;
    private readonly IPluginManager _pluginManager;
    private readonly IInstanceOverrideRemover _instanceOverrideRemover;

    public DeleteVariableService(IUndoManager undoManager,
        IFileCommands fileCommands,
        IGuiCommands guiCommands,
        IRenameLogic renameLogic,
        IDialogService dialogService,
        IPluginManager pluginManager,
        IInstanceOverrideRemover instanceOverrideRemover)
    {
        _undoManager = undoManager;
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _renameLogic = renameLogic;
        _dialogService = dialogService;
        _pluginManager = pluginManager;
        _instanceOverrideRemover = instanceOverrideRemover;
    }

    public bool CanDeleteVariable(VariableSave variable)
    {
        return variable.IsCustomVariable;
    }

    public void DeleteVariable(VariableSave variable, IStateContainer stateContainer)
    {
        var response = GetIfCanDeleteVariable(variable, stateContainer, out var cascadingInstanceOverrides);

        if(response.Succeeded == false)
        {
            _dialogService.ShowMessage(response.Message);
            return;
        }

        var elementSave = stateContainer as ElementSave;

        using (_undoManager.RequestLock())
        {
            if(elementSave != null)
            {
                elementSave.GetDefaultStateOrThrow().Variables.Remove(variable);
                _fileCommands.TryAutoSaveElement(elementSave);
            }
            else if(stateContainer is BehaviorSave behavior)
            {
                behavior.RequiredVariables.Variables.Remove(variable);
                _fileCommands.TryAutoSaveObject(behavior);
            }

            // Instance-level value overrides elsewhere in the project are cascaded. See ADR 0016.
            _instanceOverrideRemover.RemoveAndRecordForUndo(cascadingInstanceOverrides);
        }

        _guiCommands.RefreshVariables(force: true);

        _pluginManager.VariableDelete(elementSave, variable.Name);
    }

    /// <summary>
    /// Values other elements set for the variable, on their instances or on an inheriting element itself,
    /// are cascaded rather than blocking - they're returned via <paramref name="cascadingInstanceOverrides"/>
    /// for the caller to remove and record for undo. Anything else that reuses the same rename-impact
    /// lookup - a VariableReferences binding, or an inheriting element exposing a variable of its own
    /// under the same name - still blocks: those aren't a simple "restore this value" case, so silently
    /// deleting through them would leave a dangling reference or destroy an unrelated exposed-variable
    /// declaration. <see cref="VariableChange.IsRemovableValue"/> tells the two apart. See ADR 0016.
    /// </summary>
    private GeneralResponse GetIfCanDeleteVariable(VariableSave variable, IStateContainer stateContainer,
        out List<VariableChange> cascadingInstanceOverrides)
    {
        cascadingInstanceOverrides = new List<VariableChange>();

        var isVariableContained = false;

        if (stateContainer is ElementSave elementSave)
        {
            isVariableContained =
                elementSave.GetDefaultStateOrThrow().Variables.Contains(variable);
        }
        else if (stateContainer is BehaviorSave behaviorSave)
        {
            isVariableContained = behaviorSave.RequiredVariables.Variables.Contains(variable);
        }

        if(!isVariableContained)
        {
            return GeneralResponse.UnsuccessfulWith($"The variable {variable} is not contained in {stateContainer}");
        }

        var renames = _renameLogic.GetChangesForRenamedVariable(stateContainer, variable.Name, variable.GetRootName());

        var blockingChanges = renames.VariableChanges.Where(c => !c.IsRemovableValue).ToList();

        if (blockingChanges.Count > 0 || renames.VariableReferenceChanges.Count > 0)
        {
            var blockingResponse = new VariableChangeResponse();
            blockingResponse.VariableChanges.AddRange(blockingChanges);
            blockingResponse.VariableReferenceChanges.AddRange(renames.VariableReferenceChanges);

            return GeneralResponse.UnsuccessfulWith(
                $"Cannot delete variable {variable.Name} because it is referenced by other elements.\n\n{blockingResponse.GetChangesDetails()}");
        }

        cascadingInstanceOverrides = renames.VariableChanges.Where(c => c.IsRemovableValue).ToList();

        return GeneralResponse.SuccessfulResponse;
    }
}