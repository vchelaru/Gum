using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.DataTypes;

namespace Gum.Services;


internal interface IEditVariableService
{
    void TryAddEditVariableOptions(InstanceMember instanceMember, VariableSave variableSave, IStateCategoryListContainer stateListCategoryContainer);
}


internal class EditVariableService : IEditVariableService
{
    public void TryAddEditVariableOptions(InstanceMember instanceMember, VariableSave variableSave, IStateCategoryListContainer stateListCategoryContainer)
    {
        if(ShouldAddEditVariableOptions(variableSave, stateListCategoryContainer))
        {
            instanceMember.ContextMenuEvents.Add("Edit Variable", (sender, e) =>
            {
                GumCommands.Self.GuiCommands.ShowEditVariableWindow(variableSave);
            });
        }
    }

    bool ShouldAddEditVariableOptions(VariableSave variableSave, IStateCategoryListContainer stateListCategoryContainer)
    {
        if(variableSave == null)
        {
            return false;
        }

        var behaviorSave = stateListCategoryContainer as BehaviorSave;

        // for now only edit variables inside of behaviors:
        if (behaviorSave == null)
        {
            return false;
        }


        return true;
    }
}
