﻿using CommonFormsAndControls;
using Gum.DataTypes.Variables;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gum.Services;

internal interface IExposeVariableService
{
    void HandleExposeVariableClick(InstanceSave instanceSave, VariableSave variableSave, string rootVariableName);

}

internal class ExposeVariableService : IExposeVariableService
{
    public void HandleExposeVariableClick(InstanceSave instanceSave, VariableSave variableSave, string rootVariableName)
    {
        if (instanceSave == null)
        {
            MessageBox.Show("Cannot expose variables on components or screens, only on instances");
            return;
        }

        // Update June 1, 2017
        // This code used to expose
        // a variable on whatever state
        // was selected; however, exposed
        // variables should be exposed on the
        // default state or else Gum breaks
        //StateSave currentStateSave = SelectedState.Self.SelectedStateSave;
        StateSave stateToExposeOn = SelectedState.Self.SelectedElement.DefaultState;

        if (variableSave == null)
        {
            // This variable hasn't been assigned yet.  Let's make a new variable with a null value

            string variableName = instanceSave.Name + "." + rootVariableName;
            string rawVariableName = rootVariableName;

            ElementSave elementForInstance = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
            var variableInDefault = elementForInstance.DefaultState.GetVariableSave(rawVariableName);
            while (variableInDefault == null && !string.IsNullOrEmpty(elementForInstance.BaseType))
            {
                elementForInstance = ObjectFinder.Self.GetElementSave(elementForInstance.BaseType);
                if (elementForInstance?.DefaultState == null)
                {
                    break;
                }
                variableInDefault = elementForInstance.DefaultState.GetVariableSave(rawVariableName);
            }

            if (variableInDefault != null)
            {
                string variableType = variableInDefault.Type;

                stateToExposeOn.SetValue(variableName, null, instanceSave, variableType);

                // Now the variable should be created so we can access it
                variableSave = stateToExposeOn.GetVariableSave(variableName);
                // Since it's newly-created, there is no value being set:
                variableSave.SetsValue = false;
            }
        }

        if (variableSave == null)
        {
            MessageBox.Show("This variable cannot be exposed.");
        }
        else
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter variable name:";
            tiw.Title = "Expose variable";
            // We want to use the name without the dots.
            // So something like TextInstance.Text would be
            // TextInstanceText
            tiw.Result = variableSave.Name.Replace(".", "");

            DialogResult result = tiw.ShowDialog();

            if (result == DialogResult.OK)
            {
                string whyNot;
                if (!NameVerifier.Self.IsVariableNameValid(tiw.Result, SelectedState.Self.SelectedElement, variableSave, out whyNot))
                {
                    MessageBox.Show(whyNot);
                }
                else
                {
                    var elementSave = SelectedState.Self.SelectedElement;
                    // if there is an inactive variable,
                    // we should get rid of it:
                    var existingVariable = SelectedState.Self.SelectedElement.GetVariableFromThisOrBase(tiw.Result);

                    // there's a variable but we shouldn't consider it
                    // unless it's "Active" - inactive variables may be
                    // leftovers from a type change


                    if (existingVariable != null)
                    {
                        var isActive = VariableSaveLogic.GetIfVariableIsActive(existingVariable, elementSave, null);
                        if (isActive == false)
                        {
                            // gotta remove the variable:
                            if (elementSave.DefaultState.Variables.Contains(existingVariable))
                            {
                                // We may need to worry about inheritance...eventually
                                elementSave.DefaultState.Variables.Remove(existingVariable);
                            }
                        }

                    }

                    variableSave.ExposedAsName = tiw.Result;

                    PluginManager.Self.VariableAdd(elementSave, tiw.Result);

                    GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                    GumCommands.Self.GuiCommands.RefreshVariables(force: true);
                }
            }
        }
    }
}
