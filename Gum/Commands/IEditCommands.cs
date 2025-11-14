using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Commands;

public interface IEditCommands
{
    #region State

    void AskToDeleteState(StateSave stateSave, IStateContainer stateContainer);

    void AskToRenameState(StateSave stateSave, IStateContainer stateContainer);

    #endregion

    #region Category

    void MoveToCategory(string categoryNameToMoveTo, StateSave stateToMove, IStateContainer stateContainer);

    void RemoveStateCategory(StateSaveCategory category, IStateContainer stateCategoryListContainer);

    void AskToRenameStateCategory(StateSaveCategory category, ElementSave elementSave);

    #endregion


    #region Behavior

    void RemoveBehaviorVariable(BehaviorSave container, VariableSave variable);

    void AddBehavior();

    #endregion

    #region Element

    void DuplicateSelectedElement();

    void ShowCreateComponentFromInstancesDialog();

    #endregion

    void DeleteSelection();
}
