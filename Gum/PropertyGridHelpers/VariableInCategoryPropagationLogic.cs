using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using System.Linq;
using System.Windows.Forms;
using Gum.Commands;
using Gum.Services.Dialogs;
using System.Collections.Generic;

namespace Gum.PropertyGridHelpers;

public class VariableInCategoryPropagationLogic : IVariableInCategoryPropagationLogic
{
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IDialogService _dialogService;

    public VariableInCategoryPropagationLogic(IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IDialogService dialogService)
    {
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _dialogService = dialogService;
    }

    public void PropagateVariablesInCategory(string memberName, ElementSave element, StateSaveCategory categoryToPropagate)
    {
        /////////////////////Early Out//////////////////////////
        if (categoryToPropagate == null)
        {
            return;
        }
        ///////////////////End Early Out////////////////////////

        PropagateVariablesInCategory(memberName, element, categoryToPropagate.States);
    }

    public void PropagateVariablesInCategory(string memberName, ElementSave element, List<StateSave> states)
    { 
        var defaultState = element.DefaultState;
        var defaultVariable = defaultState.GetVariableSave(memberName);
        if (defaultVariable == null)
        {
            defaultVariable = defaultState.GetVariableRecursive(memberName);
        }
        if(defaultVariable == null)
        {
            defaultVariable = ObjectFinder.Self.GetRootVariable(memberName, element);
        }
        var defaultVariableList = defaultState.GetVariableListSave(memberName);
        if(defaultVariableList == null && defaultVariable == null)
        {
            defaultVariableList = defaultState.GetVariableListRecursive(memberName);
        }

        // If the user is setting a variable that is a categorized state, the
        // default may be null. If so, then we need to select the first value
        // as the default so that values are always set:
        if(defaultVariable != null && defaultVariable.Value == null)
        {
            var variableContainer = element;


            var sourceObjectName = VariableSave.GetSourceObject(memberName);

            if(!string.IsNullOrEmpty(sourceObjectName))
            {
                var nos = variableContainer.GetInstance(sourceObjectName);

                if(nos != null)
                {
                    variableContainer = ObjectFinder.Self.GetElementSave(nos);
                }
            }

            StateSaveCategory category = null;
            var isState = false;
            
            if(variableContainer != null)
            {
                isState = defaultVariable.IsState(variableContainer, out _, out category);
            }

            if(isState)
            {
                // we're going to assign a value on the variable, but we don't want to modify the original one so, 
                // let's clone it:
                defaultVariable = defaultVariable.Clone();
                if(category != null)
                {
                    defaultVariable.Value = category.States.FirstOrDefault()?.Name;
                }
                else
                {
                    defaultVariable.Value = variableContainer.DefaultState?.Name;

                }

            }
        }
        // variable lists cannot be states, so no need to do anything here:
        if(defaultVariableList != null && defaultVariableList.ValueAsIList == null)
        {
            // do nothing...
        }

        var defaultValue = defaultVariable?.Value ?? defaultVariableList?.ValueAsIList;

        if(defaultValue == null)
        {
            defaultValue = element.DefaultState.GetValueRecursive(memberName);
        }

        foreach (var state in states)
        {

            if(defaultVariable != null)
            {
                var existingVariable = state.GetVariableSave(memberName);
                if (existingVariable == null)
                {
                    if(defaultVariable != null)
                    {
                        VariableSave newVariable = defaultVariable.Clone();
                        newVariable.Value = defaultValue;
                        newVariable.SetsValue = true;
                        newVariable.Name = memberName;

                        state.Variables.Add(newVariable);

                        _guiCommands.PrintOutput(
                            $"Adding {memberName} to {state.Name}");
                    }
                }
                else if (existingVariable.SetsValue == false)
                {
                    existingVariable.SetsValue = true;
                }
            }
            else if(defaultVariableList != null)
            {
                var existingVariableList = state.GetVariableListSave(memberName);
                if(existingVariableList == null)
                {
                    if(defaultVariableList != null)
                    {
                        var newVariableList = defaultVariableList.Clone();
                        // handled by clone:
                        //newVariableList.ValueAsIList = defaultVariableList.ValueAsIList;
                        newVariableList.Name = memberName;
                        state.VariableLists.Add(newVariableList);
                        _guiCommands.PrintOutput(
                            $"Adding {memberName} to {state.Name}");
                    }
                }
            }
        }
    }


    public void AskRemoveVariableFromAllStatesInCategory(string variableName, StateSaveCategory stateCategory)
    {
        string message =
            $"Are you sure you want to remove {variableName} from all states in {stateCategory.Name}? The following states will be impacted:\n";

        foreach (var state in stateCategory.States)
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

                PluginManager.Self.VariableRemovedFromCategory(variableName, stateCategory);
            }
        }
    }

}
