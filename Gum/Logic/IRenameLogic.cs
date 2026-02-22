using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Logic;
public interface IRenameLogic
{
    #region StateSave

    void RenameState(StateSave stateSave, StateSaveCategory category, string newName, bool applyRefactoringChanges = true);

    StateRenameChanges GetChangesForRenamedState(StateSave state, string oldName, IStateContainer? container, StateSaveCategory? category);

    void ApplyStateRenameChanges(StateRenameChanges changes, StateSave state);

    #endregion

    #region Category

    void AskToRenameStateCategory(StateSaveCategory category, IStateContainer owner);

    CategoryRenameChanges GetChangesForRenamedCategory(IStateContainer owner, StateSaveCategory category, string oldName);

    void ApplyCategoryRenameChanges(CategoryRenameChanges categoryChanges, IStateContainer owner, StateSaveCategory category, string oldName, string newName);

    #endregion

    #region Element

    GeneralResponse HandleRename(IInstanceContainer instanceContainer, InstanceSave? instance, string oldName, NameChangeAction action, bool askAboutRename = true);

    InstanceRenameChanges GetChangesForRenamedInstance(ElementSave containerElement, InstanceSave instance, string oldName);

    void ApplyInstanceRenameChanges(InstanceRenameChanges changes, string newName, string oldName, HashSet<ElementSave> elementsToSave);

    ElementRenameChanges GetChangesForRenamedElement(ElementSave elementSave, string oldName);

    void ApplyElementRenameChanges(ElementRenameChanges changes, ElementSave elementSave, string oldName);

    #endregion

    #region Variable

    VariableChangeResponse GetChangesForRenamedVariable(IStateContainer owner, string oldFullName, string oldStrippedOrExposedName);

    void ApplyVariableRenameChanges(VariableChangeResponse changes, string oldStrippedOrExposedName, string newStrippedOrExposedName, HashSet<ElementSave> elementsNeedingSave);

    #endregion
}
