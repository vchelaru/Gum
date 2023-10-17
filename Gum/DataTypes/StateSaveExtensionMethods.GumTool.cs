using Gum.Plugins;
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
        public static void ReactToInstanceNameChange(this StateSave stateSave, InstanceSave instanceSave, string oldName, string newName)
        {
            foreach (VariableSave variable in stateSave.Variables)
            {
                if (variable.SourceObject == oldName)
                {
                    variable.Name = newName + "." + variable.Name.Substring((oldName + ".").Length);
                }

                if (variable.GetRootName() == "Parent" && variable.SetsValue && variable.Value is string valueAsString && !string.IsNullOrEmpty(valueAsString))
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

            PluginManager.Self.InstanceRename(instanceSave.ParentContainer, instanceSave, oldName);

        }

        public static void SetFrom(this StateSave stateSave, StateSave otherStateSave)
        {
            stateSave.Name = otherStateSave.Name;
            // We don't want to do this because the otherStateSave may not have a parent
            //stateSave.ParentContainer = otherStateSave.ParentContainer;

            stateSave.Variables.Clear();
            stateSave.VariableLists.Clear();

            foreach (VariableSave variable in otherStateSave.Variables)
            {
                stateSave.Variables.Add(FileManager.CloneSaveObject(variable));
            }

            foreach (VariableListSave variableList in otherStateSave.VariableLists)
            {
                stateSave.VariableLists.Add(FileManager.CloneSaveObject(variableList));
            }

#if GUM

            stateSave.FixEnumerations();
#endif
        }


    }
}
