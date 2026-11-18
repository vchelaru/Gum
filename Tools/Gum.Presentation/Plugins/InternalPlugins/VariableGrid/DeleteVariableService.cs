using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.ProjectServices.Editing;
using Gum.Undo;
using System.Collections.Generic;
using Gum.Services;
using Gum.Services.Dialogs;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

public class DeleteVariableService : IDeleteVariableService
{
    private readonly IUndoManager _undoManager;
    private readonly IFileCommands _fileCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IDialogService _dialogService;
    private readonly IPluginManager _pluginManager;
    private readonly IInstanceOverrideRemover _instanceOverrideRemover;
    private readonly IVariableRemovalLogic _variableRemovalLogic;

    public DeleteVariableService(IUndoManager undoManager,
        IFileCommands fileCommands,
        IGuiCommands guiCommands,
        IDialogService dialogService,
        IPluginManager pluginManager,
        IInstanceOverrideRemover instanceOverrideRemover,
        IVariableRemovalLogic variableRemovalLogic)
    {
        _undoManager = undoManager;
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _dialogService = dialogService;
        _pluginManager = pluginManager;
        _instanceOverrideRemover = instanceOverrideRemover;
        _variableRemovalLogic = variableRemovalLogic;
    }

    public bool CanDeleteVariable(VariableSave variable)
    {
        return variable.IsCustomVariable;
    }

    public void DeleteVariable(VariableSave variable, IStateContainer stateContainer)
    {
        string? whyNot = _variableRemovalLogic.GetWhyCannotRemoveCustomVariable(variable, stateContainer,
            out List<VariableChange> cascadingInstanceOverrides);

        if (whyNot != null)
        {
            _dialogService.ShowMessage(whyNot);
            return;
        }

        var elementSave = stateContainer as ElementSave;

        using (_undoManager.RequestLock())
        {
            _variableRemovalLogic.RemoveCustomVariable(variable, stateContainer);
            if (elementSave != null)
            {
                _fileCommands.TryAutoSaveElement(elementSave);
            }
            else if (stateContainer is BehaviorSave behavior)
            {
                _fileCommands.TryAutoSaveObject(behavior);
            }

            // Instance-level value overrides elsewhere in the project are cascaded. See ADR 0016.
            _instanceOverrideRemover.RemoveAndRecordForUndo(cascadingInstanceOverrides);
        }

        _guiCommands.RefreshVariables(force: true);

        _pluginManager.VariableDelete(elementSave, variable.Name);
    }
}
