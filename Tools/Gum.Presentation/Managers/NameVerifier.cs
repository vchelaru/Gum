using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.ProjectServices;
using Gum.ProjectServices.Editing;
using Gum.ToolStates;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
namespace Gum.Managers;

public class NameVerifier : INameVerifier
{
    #region Fields/Properties

    private readonly IVariableSaveLogic _variableSaveLogic;
    private readonly ISelectedState _selectedState;

    // The rules that need no tool service; the CLI runs the same ones.
    private readonly NameRules _rules = new NameRules();
    private readonly IElementNameValidator _validator;

    #endregion

    #region Folder

    public NameVerifier(IVariableSaveLogic variableSaveLogic, Plugins.IPluginManager pluginManager, ISelectedState selectedState)
    {
        _variableSaveLogic = variableSaveLogic;
        _selectedState = selectedState;
        _validator = new ElementNameValidator(pluginManager);
    }

    public bool IsFolderNameValid(string? folderName, out string whyNotValid)
    {
        IsNameValidCommon(folderName, out whyNotValid, out _);
        return string.IsNullOrEmpty(whyNotValid);
    }

    #endregion

    public bool IsElementNameValid(string? componentNameWithoutFolder, string? folderName, ElementSave? elementSave, [NotNullWhen(false)] out string? whyNotValid) =>
        _validator.IsElementNameValid(componentNameWithoutFolder, folderName, elementSave, out whyNotValid);

    public bool IsCategoryNameValid(string? name, IStateContainer categoryContainer, [NotNullWhen(false)] out string? whyNotValid, StateSaveCategory? categoryToIgnore = null) =>
        _validator.IsCategoryNameValid(name, categoryContainer, out whyNotValid, categoryToIgnore);

    public bool IsStateNameValid(string? name, StateSaveCategory? category, StateSave? stateSave, [NotNullWhen(false)] out string? whyNotValid) =>
        _validator.IsStateNameValid(name, category, stateSave, out whyNotValid);

    public bool IsInstanceNameValid(string? instanceName, InstanceSave? instanceSave, IInstanceContainer? instanceContainer, [NotNullWhen(false)] out string? whyNotValid) =>
        _validator.IsInstanceNameValid(instanceName, instanceSave, instanceContainer, out whyNotValid);

    public bool IsNameValidTopLevel(string name, ElementSave element, object? objectToIgnore, [NotNullWhen(false)] out string? whyNotValid) =>
        _validator.IsNameValidTopLevel(name, element, objectToIgnore, out whyNotValid);

    public bool IsVariableNameValid(string variableName, ElementSave? elementSave, VariableSave? variableSave, [NotNullWhen(false)] out string? whyNotValid)
    {
        if (!_validator.IsVariableNameValid(variableName, elementSave, variableSave, out whyNotValid))
        {
            return false;
        }

        var existingVariable = elementSave != null ? GetVariableFromThisOrBase(elementSave, variableName) : null;
        // there's a variable but we shouldn't consider it
        // unless it's "Active" - inactive variables may be
        // leftovers from a type change in a project saved by an earlier Gum
        // A state can hold its own copy of the edited variable, so match by name as well as by reference.
        var isVariableBeingEdited = variableSave != null && existingVariable != null &&
            (existingVariable == variableSave || existingVariable.Name == variableSave.Name);
        if (existingVariable != null && elementSave != null && !isVariableBeingEdited)
        {
            var isActive = _variableSaveLogic.GetIfVariableIsActive(existingVariable,
                elementSave, null);
            if (isActive)
            {
                whyNotValid = $"The variable name {variableName} is already used";
            }
        }
        return string.IsNullOrEmpty(whyNotValid);
    }

    public bool IsBehaviorNameValid(string? behaviorName, BehaviorSave? behaviorSave, [NotNullWhen(false)] out string? whyNotValid)
    {
        // Behaviors are files named after them, so a duplicate (compared as elements are) would overwrite the other's file.
        IEnumerable<BehaviorSave> allBehaviors = ObjectFinder.Self.GumProjectSave?.Behaviors ?? Enumerable.Empty<BehaviorSave>();
        return _rules.IsBehaviorNameValid(behaviorName, behaviorSave, allBehaviors, out whyNotValid);
    }

    public bool IsNameValidCommon([NotNullWhen(true)] string? name, out string whyNotValid, out CommonValidationError commonValidationError)
    {
        return _rules.IsNameValidCommon(name, out whyNotValid, out commonValidationError);
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

    public bool IsValidCSharpName(string name, [NotNullWhen(false)] out string? whyNotValid, out CommonValidationError commonValidationError)
    {
        return _rules.IsValidCSharpName(name, out whyNotValid, out commonValidationError);
    }

    public bool IsComponentNameAlreadyUsed(string name)
    {
        return ObjectFinder.Self.GetComponent(name) != null;
    }

    public bool IsNameValidAndroidFile(string name, [NotNullWhen(false)] out string? whyNotValid)
    {
        return _rules.IsNameValidAndroidFile(name, out whyNotValid);
    }
}
