using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Gum.Managers;
public interface INameVerifier
{
    bool IsFolderNameValid(string? folderName, out string whyNotValid);

    bool IsElementNameValid(string? componentNameWithoutFolder, string? folderName, ElementSave? elementSave, [NotNullWhen(false)] out string? whyNotValid);

    bool IsCategoryNameValid(string? name, IStateContainer categoryContainer, [NotNullWhen(false)] out string? whyNotValid, StateSaveCategory? categoryToIgnore = null);

    bool IsStateNameValid(string? name, StateSaveCategory? category, StateSave? stateSave, [NotNullWhen(false)] out string? whyNotValid);

    bool IsInstanceNameValid(string? instanceName, InstanceSave? instanceSave, IInstanceContainer? instanceContainer, [NotNullWhen(false)] out string? whyNotValid);

    bool IsNameValidTopLevel(string name, ElementSave element, object? objectToIgnore, [NotNullWhen(false)] out string? whyNotValid);

    bool IsVariableNameValid(string variableName, ElementSave? elementSave, VariableSave? variableSave, [NotNullWhen(false)] out string? whyNotValid);

    bool IsBehaviorNameValid(string? behaviorName, BehaviorSave? behaviorSave, [NotNullWhen(false)] out string? whyNotValid);

    bool IsComponentNameAlreadyUsed(string name);

    bool IsValidCSharpName(string name, [NotNullWhen(false)] out string? whyNotValid, out CommonValidationError commonValidationError);


    bool IsNameValidAndroidFile(string name, [NotNullWhen(false)] out string? whyNotValid);

    bool IsNameValidCommon(string? name, out string whyNotValid, out CommonValidationError commonValidationError);
}
