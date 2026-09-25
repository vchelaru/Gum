using Gum.DataTypes.Variables;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using System;
using Gum.Commands;
using Gum.Services.Dialogs;
using Gum.Undo;
using ToolsUtilities;

namespace Gum.Services;

public class ExposeVariableService : IExposeVariableService
{
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IRenameLogic _renameLogic;
    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IDialogService _dialogService;
    private readonly IVariableSaveLogic _variableSaveLogic;
    private readonly IPluginManager _pluginManager;

    public ExposeVariableService(
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IRenameLogic renameLogic,
        ISelectedState selectedState,
        INameVerifier nameVerifier,
        IDialogService dialogService,
        IVariableSaveLogic variableSaveLogic,
        IPluginManager pluginManager)
    {
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _renameLogic = renameLogic;
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _dialogService = dialogService;
        _variableSaveLogic = variableSaveLogic;
        _pluginManager = pluginManager;
    }

    public OptionallyAttemptedGeneralResponse<VariableSave> HandleExposeVariableClick(InstanceSave instanceSave, string rootVariableName)
    {
        // find the variable if it exists:
        var parentElement = instanceSave.ParentContainer;
        var variableSave = parentElement?.GetDefaultStateOrThrow().GetVariableSave(
            $"{instanceSave.Name}.{rootVariableName}");
        var elementSave = _selectedState.SelectedElement;

        var canExpose = GetIfCanExpose(instanceSave, elementSave, variableSave, rootVariableName);

        // GetIfCanExpose fails when there is no selected element.
        if(canExpose.Succeeded == false || elementSave == null)
        {
            // show message
            _dialogService.ShowMessage(canExpose.Message);
            return OptionallyAttemptedGeneralResponse<VariableSave>.SuccessfulWithoutAttempt;
        }

        string message = "Enter variable name:";
        string title = "Expose variable";
        // We want to use the name without the dots.
        // So something like TextInstance.Text would be
        // TextInstanceText
        var fullVariableName = instanceSave.Name + "." + rootVariableName;

        GetUserStringOptions options = new()
        {
            InitialValue = fullVariableName.Replace(".", "").Replace(" ", ""),
            Validator = v =>
                _nameVerifier.IsVariableNameValid(v ?? "", elementSave, variableSave, out string? whyNot)
                    ? null
                    : whyNot
        };

        if (_dialogService.GetUserString(message, title, options) is { } result)
        {
            return ApplyExposure(elementSave, instanceSave, rootVariableName, variableSave, result);
        }

        // User cancelled the prompt: attempted, but nothing exposed.
        return new OptionallyAttemptedGeneralResponse<VariableSave> { DidAttempt = true, Succeeded = false };
    }

    public OptionallyAttemptedGeneralResponse<VariableSave> ExposeVariable(InstanceSave instanceSave, string rootVariableName, string exposedName)
    {
        var parentElement = instanceSave.ParentContainer;
        var variableSave = parentElement?.GetDefaultStateOrThrow().GetVariableSave(
            $"{instanceSave.Name}.{rootVariableName}");
        var elementSave = _selectedState.SelectedElement;

        var canExpose = GetIfCanExpose(instanceSave, elementSave, variableSave, rootVariableName);

        // GetIfCanExpose fails when there is no selected element.
        if (canExpose.Succeeded == false || elementSave == null)
        {
            _dialogService.ShowMessage(canExpose.Message);
            return OptionallyAttemptedGeneralResponse<VariableSave>.SuccessfulWithoutAttempt;
        }

        return ApplyExposure(elementSave, instanceSave, rootVariableName, variableSave, exposedName);
    }

    /// <summary>
    /// Performs the actual exposure once the final exposed name is known. Shared by the prompt-driven
    /// <see cref="HandleExposeVariableClick"/> and the name-supplied <see cref="ExposeVariable"/>.
    /// </summary>
    private OptionallyAttemptedGeneralResponse<VariableSave> ApplyExposure(ElementSave elementSave, InstanceSave instanceSave,
        string rootVariableName, VariableSave? variableSave, string exposedName)
    {
        var fullVariableName = instanceSave.Name + "." + rootVariableName;

        // if there is an inactive variable, we should get rid of it:
        var existingVariable = GetVariableFromThisOrBase(elementSave, exposedName);

        // there's a variable but we shouldn't consider it
        // unless it's "Active" - inactive variables may be
        // leftovers from a type change

        using var undoLock = _undoManager.RequestLock();
        if (existingVariable != null)
        {
            var isActive = _variableSaveLogic.GetIfVariableIsActive(existingVariable, elementSave, null);
            if (isActive == false)
            {
                // gotta remove the variable:
                if (elementSave.GetDefaultStateOrThrow().Variables.Contains(existingVariable))
                {
                    // We may need to worry about inheritance...eventually
                    elementSave.GetDefaultStateOrThrow().Variables.Remove(existingVariable);
                }
            }
        }

        if (variableSave == null)
        {
            StateSave stateToExposeOn = elementSave.GetDefaultStateOrThrow();

            // GetIfCanExpose only succeeds for an instance in an element.
            var variableInDefault = ObjectFinder.Self.GetRootVariable(fullVariableName, instanceSave.ParentContainer!);

            if (variableInDefault == null)
            {
                throw new Exception($"Error getting root variable for {fullVariableName} in {instanceSave.ParentContainer}");
            }

            string variableType = variableInDefault.Type;
            stateToExposeOn.SetValue(fullVariableName, null, instanceSave, variableType);

            // SetValue just added the variable.
            variableSave = stateToExposeOn.GetVariableSave(fullVariableName)!;

            // Not sure if we need this, but setting SetsValue to false matches the old behavior when
            // this code used to be part of validation
            variableSave.SetsValue = false;
        }

        variableSave.ExposedAsName = exposedName;

        _pluginManager.VariableAdd(elementSave, exposedName);

        _fileCommands.TryAutoSaveCurrentElement();
        _guiCommands.RefreshVariables(force: true);

        return new OptionallyAttemptedGeneralResponse<VariableSave>
        {
            DidAttempt = true,
            Succeeded = true,
            Data = variableSave,
        };
    }

    /// <summary>
    /// Returns the variable from the element's currently-selected state if <paramref name="element"/>
    /// is the currently selected element and a state is selected, otherwise from its default state.
    /// Headless replacement for the tool-only <c>ElementSaveExtensionMethodsGumTool.GetVariableFromThisOrBase</c>,
    /// which resolves <see cref="ISelectedState"/> via <c>Locator</c> instead of taking it as a dependency.
    /// </summary>
    private VariableSave? GetVariableFromThisOrBase(ElementSave element, string variable)
    {
        var stateToPullFrom = (element == _selectedState.SelectedElement && _selectedState.SelectedStateSave != null)
            ? _selectedState.SelectedStateSave
            : element.GetDefaultStateOrThrow();

        return stateToPullFrom.GetVariableRecursive(variable);
    }

    private GeneralResponse GetIfCanExpose(InstanceSave instanceSave, ElementSave? selectedElement, VariableSave? variableSave, string rootVariableName)
    {
        if (instanceSave == null)
        {
            return GeneralResponse.UnsuccessfulWith("Cannot expose variables on components or screens, only on instances");
        }

        // A behavior's instances have no element to expose the variable on.
        if (instanceSave.ParentContainer is not { } parentContainer || selectedElement == null)
        {
            return GeneralResponse.UnsuccessfulWith("Variables can only be exposed on instances in a component or screen");
        }

        if (variableSave == null)
        {
            // This variable hasn't been assigned yet.  Let's make a new variable with a null value

            string variableName = instanceSave.Name + "." + rootVariableName;
            string rawVariableName = rootVariableName;

            // An instance of a missing type has no base variables.
            ElementSave? elementForInstance = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
            var variableInDefault = elementForInstance?.GetDefaultStateOrThrow().GetVariableSave(rawVariableName);

            if(variableInDefault == null)
            {
                variableInDefault = ObjectFinder.Self.GetRootVariable(variableName, parentContainer);
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

        //var renames = _renameLogic.GetChangesForRenamedVariable(selectedElement, fullVariableName, rootVariableName);

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


        // The un-expose menu item only shows for an exposed variable.
        if (variableSave.ExposedAsName is not { } oldExposedName)
        {
            return;
        }

        var response = GetIfCanUnexposeVariable(variableSave, oldExposedName, elementSave);
        if (response.Succeeded == false)
        {
            _dialogService.ShowMessage(response.Message);
            return;
        }

        using var undoLock = _undoManager.RequestLock();

        variableSave.ExposedAsName = null;

        _pluginManager.VariableDelete(elementSave, oldExposedName);
        _fileCommands.TryAutoSaveCurrentElement();
        _guiCommands.RefreshVariables(force: true);
    }

    private GeneralResponse GetIfCanUnexposeVariable(VariableSave variableSave, string exposedName, ElementSave elementSave)
    {
        var renames = _renameLogic.GetChangesForRenamedVariable(elementSave, variableSave.Name, exposedName);

        if (renames.VariableReferenceChanges.Count > 0)
        {
            string message = $"Cannot unexpose variable {exposedName} because it is referenced by:\n\n";
            foreach (var item in renames.VariableReferenceChanges)
            {
                message += $"{item.VariableReferenceList.ValueAsIList[item.LineIndex]} in {item.VariableReferenceList.Name} ({item.Container})\n";
            }
            return GeneralResponse.UnsuccessfulWith(message);
        }

        return GeneralResponse.SuccessfulResponse;
    }
}
