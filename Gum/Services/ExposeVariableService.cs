using CommonFormsAndControls;
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
using Gum.Commands;
using Gum.Services.Dialogs;
using Gum.Undo;
using ToolsUtilities;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Gum.Services;

#region IExposeVariableService Interface

public interface IExposeVariableService
{
    OptionallyAttemptedGeneralResponse<VariableSave> HandleExposeVariableClick(InstanceSave instanceSave, string rootVariableName);
    void HandleUnexposeVariableClick(VariableSave variableSave, ElementSave elementSave);
}

#endregion

internal class ExposeVariableService : IExposeVariableService
{
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IRenameLogic _renameLogic;
    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IDialogService _dialogService;

    public ExposeVariableService(
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IRenameLogic renameLogic,
        ISelectedState selectedState,
        INameVerifier nameVerifier,
        IDialogService dialogService)
    {
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _renameLogic = renameLogic;
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _dialogService = dialogService;
    }

    public OptionallyAttemptedGeneralResponse<VariableSave> HandleExposeVariableClick(InstanceSave instanceSave, string rootVariableName)
    {
        // find the variable if it exists:
        var parentElement = instanceSave.ParentContainer;
        var variableSave = parentElement?.DefaultState.GetVariableSave(
            $"{instanceSave.Name}.{rootVariableName}");

        var canExpose = GetIfCanExpose(instanceSave, variableSave, rootVariableName);

        if(canExpose.Succeeded == false)
        {
            // show message
            _dialogService.ShowMessage(canExpose.Message);
            return OptionallyAttemptedGeneralResponse<VariableSave>.SuccessfulWithoutAttempt;
        }

        var tiw = new TextInputWindow();
        tiw.Message = "Enter variable name:";
        tiw.Title = "Expose variable";
        // We want to use the name without the dots.
        // So something like TextInstance.Text would be
        // TextInstanceText
        var fullVariableName = instanceSave.Name + "." + rootVariableName;
        tiw.Result = fullVariableName.Replace(".", "").Replace(" ", "");

        DialogResult result = tiw.ShowDialog();

        var toReturn = new OptionallyAttemptedGeneralResponse<VariableSave>();
        toReturn.DidAttempt = true;
        toReturn.Succeeded = false;

        if (result == DialogResult.OK)
        {
            string whyNot;
            if (!_nameVerifier.IsVariableNameValid(tiw.Result, _selectedState.SelectedElement, variableSave, out whyNot))
            {
                toReturn.Message = whyNot;
                MessageBox.Show(whyNot);

            }
            else
            {
                var elementSave = _selectedState.SelectedElement;
                // if there is an inactive variable,
                // we should get rid of it:
                var existingVariable = _selectedState.SelectedElement.GetVariableFromThisOrBase(tiw.Result);

                // there's a variable but we shouldn't consider it
                // unless it's "Active" - inactive variables may be
                // leftovers from a type change

                using var undoLocl = _undoManager.RequestLock();
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

                if(variableSave == null)
                {
                    StateSave stateToExposeOn = _selectedState.SelectedElement.DefaultState;

                    var variableInDefault = ObjectFinder.Self.GetRootVariable(fullVariableName, instanceSave.ParentContainer);

                    if(variableInDefault == null)
                    {
                        throw new Exception($"Error getting root variable for {fullVariableName} in {instanceSave.ParentContainer}");
                    }

                    string variableType = variableInDefault.Type;
                    stateToExposeOn.SetValue(fullVariableName, null, instanceSave, variableType);

                    variableSave = stateToExposeOn.GetVariableSave(fullVariableName);

                    // Not sure if we need this, but setting SetsValue to false matches the old behavior when
                    // this code used to be part of validation
                    variableSave.SetsValue = false;
                }

                variableSave.ExposedAsName = tiw.Result;

                PluginManager.Self.VariableAdd(elementSave, tiw.Result);

                _fileCommands.TryAutoSaveCurrentElement();
                _guiCommands.RefreshVariables(force: true);
                toReturn.Data = variableSave;
                toReturn.Succeeded = true;
            }
        }
        return toReturn;
    }

    private GeneralResponse GetIfCanExpose(InstanceSave instanceSave, VariableSave variableSave, string rootVariableName)
    {
        if (instanceSave == null)
        {
            return GeneralResponse.UnsuccessfulWith("Cannot expose variables on components or screens, only on instances");
        }

        // Update June 1, 2017
        // This code used to expose
        // a variable on whatever state
        // was selected; however, exposed
        // variables should be exposed on the
        // default state or else Gum breaks
        //StateSave currentStateSave = _selectedState.SelectedStateSave;
        StateSave stateToExposeOn = _selectedState.SelectedElement.DefaultState;

        if (variableSave == null)
        {
            // This variable hasn't been assigned yet.  Let's make a new variable with a null value

            string variableName = instanceSave.Name + "." + rootVariableName;
            string rawVariableName = rootVariableName;

            ElementSave elementForInstance = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
            var variableInDefault = elementForInstance.DefaultState.GetVariableSave(rawVariableName);

            if(variableInDefault == null)
            {
                variableInDefault = ObjectFinder.Self.GetRootVariable(variableName, instanceSave.ParentContainer);
            }

            if (variableInDefault == null)
            {
                return GeneralResponse.UnsuccessfulWith("This variable cannot be exposed.");
            }
        }


        // if the variable is used on a left-side, it should not be exposable:
        // Update Jan 29, 2025
        // Actually yes, expose it
        // but make it read-only.
        // https://github.com/vchelaru/Gum/issues/521
        //var selectedElement = _selectedState.SelectedElement;

        //var fullVariableName = instanceSave.Name + "." + rootVariableName;

        //var renames = _renameLogic.GetVariableChangesForRenamedVariable(selectedElement, fullVariableName, rootVariableName);

        //var variableReferences = renames.VariableReferenceChanges
        //    .Where(item => item.Container == selectedElement && item.ChangedSide == SideOfEquals.Left)
        //    .ToArray();

        //if (variableReferences.Length > 0)
        //{
        //    var firstRename = variableReferences[0];
        //    string message = $"Cannot expose variable {fullVariableName} because it is assigned in a variable reference:\n\n" +
        //        $"{firstRename.VariableReferenceList.ValueAsIList[firstRename.LineIndex]}";

        //    return GeneralResponse.UnsuccessfulWith(message);
        //}

        return GeneralResponse.SuccessfulResponse;
    }

    public void HandleUnexposeVariableClick(VariableSave variableSave, ElementSave elementSave)
    {
        // do we want to support undos? I think so....?


        var response = GetIfCanUnexposeVariable(variableSave, elementSave);
        if (response.Succeeded == false)
        {
            _dialogService.ShowMessage(response.Message);
            return;
        }

        using var undoLock = _undoManager.RequestLock();

        var oldExposedName = variableSave.ExposedAsName;
        variableSave.ExposedAsName = null;

        PluginManager.Self.VariableDelete(elementSave, oldExposedName);
        _fileCommands.TryAutoSaveCurrentElement();
        _guiCommands.RefreshVariables(force: true);
    }

    private GeneralResponse GetIfCanUnexposeVariable(VariableSave variableSave, ElementSave elementSave)
    {
        var renames = _renameLogic.GetVariableChangesForRenamedVariable(elementSave, variableSave.Name, variableSave.ExposedAsName);

        if (renames.VariableReferenceChanges.Count > 0)
        {
            string message = $"Cannot unexpose variable {variableSave.ExposedAsName} because it is referenced by:\n\n";
            foreach (var item in renames.VariableReferenceChanges)
            {
                message += $"{item.VariableReferenceList.ValueAsIList[item.LineIndex]} in {item.VariableReferenceList.Name} ({item.Container})\n";
            }
            return GeneralResponse.UnsuccessfulWith(message);
        }

        return GeneralResponse.SuccessfulResponse;
    }
}
