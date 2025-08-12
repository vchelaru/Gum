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

    void RenameState(StateSave stateSave, StateSaveCategory category, string newName);

    #endregion

    #region Category

    void AskToRenameStateCategory(StateSaveCategory category, ElementSave elementSave);

    #endregion

    #region Element

    GeneralResponse HandleRename(IInstanceContainer instanceContainer, InstanceSave instance, string oldName, NameChangeAction action, bool askAboutRename = true);



    #endregion

    #region Variable

    VariableChangeResponse GetVariableChangesForRenamedVariable(IStateContainer owner, string oldFullName, string oldStrippedOrExposedName);

    #endregion
}
