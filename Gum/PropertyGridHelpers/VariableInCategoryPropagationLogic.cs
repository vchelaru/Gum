using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gum.PropertyGridHelpers
{
    public class VariableInCategoryPropagationLogic : Singleton<VariableInCategoryPropagationLogic> 
    {
        public void PropagateVariablesInCategory(string changedMember)
        {
            var currentCategory = SelectedState.Self.SelectedStateCategorySave;
            /////////////////////Early Out//////////////////////////
            if (currentCategory == null)
            {
                return;
            }
            ///////////////////End Early Out////////////////////////

            var defaultState = SelectedState.Self.SelectedElement.DefaultState;
            var defaultVariable = defaultState.GetVariableSave(changedMember);
            if (defaultVariable == null)
            {
                defaultVariable = defaultState.GetVariableRecursive(changedMember);
            }

            // If the user is setting a variable that is a categorized state, the
            // default may be null. If so, then we need to select the first value
            // as the default so that values are always set:
            if(defaultVariable != null && defaultVariable.Value == null)
            {
                var variableContainer = SelectedState.Self.SelectedElement;


                var sourceObjectName = VariableSave.GetSourceObject(changedMember);

                if(!string.IsNullOrEmpty(sourceObjectName))
                {
                    var nos = variableContainer.GetInstance(sourceObjectName);

                    if(nos != null)
                    {
                        variableContainer = ObjectFinder.Self.GetElementSave(nos);
                    }
                }

                ElementSave categoryContainer;
                StateSaveCategory category;
                var isState = defaultVariable.IsState(variableContainer, out categoryContainer, out category);

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

            var defaultValue = defaultVariable.Value;

            foreach (var state in currentCategory.States)
            {
                var existingVariable = state.GetVariableSave(changedMember);

                if (existingVariable == null)
                {
                    VariableSave newVariable = defaultVariable.Clone();
                    newVariable.Value = defaultValue;
                    newVariable.SetsValue = true;
                    newVariable.Name = changedMember;

                    state.Variables.Add(newVariable);

                    GumCommands.Self.GuiCommands.PrintOutput(
                        $"Adding {changedMember} to {currentCategory.Name}/{state.Name}");
                }
                else if (existingVariable.SetsValue == false)
                {
                    existingVariable.SetsValue = true;
                }
            }
        }


        public void AskRemoveVariableFromAllStatesInCategory(string variableName, StateSaveCategory stateCategory)
        {
            string message =
                $"Are you sure you want to remove {variableName} from all states in {stateCategory.Name}? The following categories will be impacted:\n";

            foreach (var state in stateCategory.States)
            {
                message += $"\n{state.Name}";
            }

            var result = MessageBox.Show(message, "Remove Variables?", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                foreach (var state in stateCategory.States)
                {
                    var foundVariable = state.Variables.FirstOrDefault(item => item.Name == variableName);

                    if (foundVariable != null)
                    {
                        state.Variables.Remove(foundVariable);
                    }
                }

                // save everything
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                GumCommands.Self.GuiCommands.RefreshStateTreeView();
                // no selection has changed, but we want to force refresh here because we know
                // we really need a refresh - something was removed.
                GumCommands.Self.GuiCommands.RefreshPropertyGrid(force:true);
            }
        }

    }
}
