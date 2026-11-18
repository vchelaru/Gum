using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using System.Linq;
using Gum.Commands;
using Gum.Services.Dialogs;
using System.Collections.Generic;
using Gum.ProjectServices.Editing;

namespace Gum.PropertyGridHelpers;

public class VariableInCategoryPropagationLogic : IVariableInCategoryPropagationLogic
{
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IDialogService _dialogService;
    private readonly IPluginManager _pluginManager;
    private readonly ICategoryVariablePropagator _propagator;

    public VariableInCategoryPropagationLogic(IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IDialogService dialogService,
        IPluginManager pluginManager,
        ICategoryVariablePropagator propagator)
    {
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _dialogService = dialogService;
        _pluginManager = pluginManager;
        _propagator = propagator;
    }

    public void PropagateVariablesInCategory(string memberName, ElementSave? element, StateSaveCategory? categoryToPropagate)
    {
        /////////////////////Early Out//////////////////////////
        if (categoryToPropagate == null)
        {
            return;
        }
        ///////////////////End Early Out////////////////////////

        PropagateVariablesInCategory(memberName, element, categoryToPropagate.States);
    }

    public void PropagateAllVariablesInCategory(ElementSave? element, StateSaveCategory? category)
    {
        if (element == null || category == null)
        {
            return;
        }

        PrintAdditions(_propagator.PropagateAllVariablesInCategory(element, category));
    }

    public void PropagateVariablesInCategory(string memberName, ElementSave? element, List<StateSave> states)
    {
        if (element == null)
        {
            return;
        }

        PrintAdditions(_propagator.PropagateVariablesInCategory(memberName, element, states));
    }

    private void PrintAdditions(IReadOnlyList<PropagatedVariable> added)
    {
        foreach (PropagatedVariable addition in added)
        {
            _guiCommands.PrintOutput($"Adding {addition.Name} to {addition.State.Name}");
        }
    }


    public void AskRemoveVariableFromAllStatesInCategory(string variableName, StateSaveCategory stateCategory)
    {
        string message =
            $"Are you sure you want to remove {variableName} from all states in {stateCategory.Name}? The following states will be impacted:\n";

        foreach (var state in stateCategory.States.Where(item =>
            item.Variables.Any(variable => variable.Name == variableName) ||
            item.VariableLists.Any(variableList => variableList.Name == variableName)))
        {
            message += $"\n{state.Name}";
        }

        if (_dialogService.ShowYesNoMessage(message, "Remove Variables?"))
        {
            using(_undoManager.RequestLock())
            {
                foreach (var state in stateCategory.States)
                {
                    var foundVariable = state.Variables.FirstOrDefault(item => item.Name == variableName);

                    if (foundVariable != null)
                    {
                        state.Variables.Remove(foundVariable);
                    }
                    else
                    {
                        // it's probably a list:
                        var foundVariableList = state.VariableLists.FirstOrDefault(item => item.Name == variableName);
                        if(foundVariableList != null)
                        {
                            state.VariableLists.Remove(foundVariableList);
                        }
                    }
                }

                // save everything
                _fileCommands.TryAutoSaveCurrentElement();
                _guiCommands.RefreshStateTreeView();
                // no selection has changed, but we want to force refresh here because we know
                // we really need a refresh - something was removed.
                _guiCommands.RefreshVariables(force:true);

                _pluginManager.VariableRemovedFromCategory(variableName, stateCategory);
            }
        }
    }

}
