using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.Editing;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
namespace Gum.Managers;

public class NameVerifier : INameVerifier
{
    #region Fields/Properties

    // The rules that need no tool service; the CLI runs the same ones.
    private readonly NameRules _rules = new NameRules();
    private readonly IElementNameValidator _validator;

    #endregion

    #region Folder

    public NameVerifier(Plugins.IPluginManager pluginManager)
    {
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

    public bool IsVariableNameValid(string variableName, ElementSave? elementSave, VariableSave? variableSave, [NotNullWhen(false)] out string? whyNotValid) =>
        _validator.IsVariableNameValid(variableName, elementSave, variableSave, out whyNotValid);

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
