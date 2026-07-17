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
            foreach (VariableSave variable in stateSave.Variables)
            {
                if (variable.SourceObject == oldName)
                {
                    variable.Name = newName + "." + variable.Name.Substring((oldName + ".").Length);
                }

                // Variables whose value is an instance name (Parent, and a Sprite's render-target
                // source) must have that value rewritten when the referenced instance is renamed, or
                // the reference goes stale and silently resolves to nothing.
                var rootName = variable.GetRootName();
                if ((rootName == "Parent" || rootName == "RenderTargetTextureSource") && variable.SetsValue && variable.Value is string valueAsString && !string.IsNullOrEmpty(valueAsString))
                {
                    if (valueAsString == oldName)
                    {
                        variable.Value = newName;
                    }
                    else if(valueAsString.StartsWith(oldName + "."))
                    {
                        var afterDot = valueAsString.Substring(oldName.Length + 1);
                        variable.Value = newName + "." + afterDot;
                    }
                }
            }

            foreach (VariableListSave variableList in stateSave.VariableLists)
            {
                if (variableList.SourceObject == oldName)
                {
                    if (variableList.SourceObject == oldName)
                    {
                        variableList.Name = newName + "." + variableList.Name.Substring((oldName + ".").Length);
                    }
                }
            }

            pluginManager.InstanceRename(instanceSave.ParentContainer, instanceSave, oldName);

        }
    }
}
