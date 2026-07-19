using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.PropertyGridHelpers;

public interface IVariableInCategoryPropagationLogic
{
    void PropagateVariablesInCategory(string memberName, ElementSave element, StateSaveCategory categoryToPropagate);

    void PropagateVariablesInCategory(string memberName, ElementSave element, List<StateSave> states);

    void AskRemoveVariableFromAllStatesInCategory(string variableName, StateSaveCategory stateCategory);

}
