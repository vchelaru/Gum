using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Managers;
public interface INameVerifier
{
    bool IsFolderNameValid(string folderName, out string whyNotValid);

    bool IsElementNameValid(string componentNameWithoutFolder, string folderName, ElementSave elementSave, out string whyNotValid);

    bool IsCategoryNameValid(string name, IStateContainer categoryContainer, out string whyNotValid);

    bool IsStateNameValid(string name, StateSaveCategory category, StateSave stateSave, out string whyNotValid);

    bool IsInstanceNameValid(string instanceName, InstanceSave instanceSave, IInstanceContainer instanceContainer, out string whyNotValid);

    bool IsVariableNameValid(string variableName, ElementSave elementSave, VariableSave variableSave, out string whyNotValid);

    bool IsBehaviorNameValid(string behaviorName, BehaviorSave behaviorSave, out string whyNotValid);

    bool IsComponentNameAlreadyUsed(string name);

    bool IsValidCSharpName(string name, out string whyNotValid, out CommonValidationError commonValidationError);


    bool IsNameValidAndroidFile(string name, out string whyNotValid);

    bool IsNameValidCommon(string name, out string whyNotValid, out CommonValidationError commonValidationError);
}
