using Gum.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.DataTypes.Variables
{
    internal static class StateSaveExtensionMethodsGumTool
    {
        public static void ReactToInstanceNameChange(this StateSave stateSave, InstanceSave instanceSave, string oldName, string newName, IRenamePluginNotifier pluginManager)
        {
            new Gum.ProjectServices.Editing.RenameReferenceApplier().RenameInstanceInState(stateSave, oldName, newName);

            pluginManager.InstanceRename(instanceSave.ParentContainer, instanceSave, oldName);

        }
    }
}
