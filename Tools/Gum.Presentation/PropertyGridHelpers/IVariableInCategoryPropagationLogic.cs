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
    void PropagateVariablesInCategory(string memberName, ElementSave? element, StateSaveCategory? categoryToPropagate);

    void PropagateVariablesInCategory(string memberName, ElementSave? element, List<StateSave> states);

    /// <summary>
    /// Propagates every variable and variable list that any state in the category sets to all the
    /// category's states, so a state that was just added or moved in is consistent with its siblings.
    /// </summary>
    void PropagateAllVariablesInCategory(ElementSave? element, StateSaveCategory? category);

    void AskRemoveVariableFromAllStatesInCategory(string variableName, StateSaveCategory stateCategory);

}
