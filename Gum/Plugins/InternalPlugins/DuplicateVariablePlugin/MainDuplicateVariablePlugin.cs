using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.DuplicateVariablePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainDuplicateVariablePlugin : InternalPlugin
    {
        public override void StartUp()
        {
            this.ElementSelected += HandleElementSelected;
        }

        private void HandleElementSelected(ElementSave element)
        {
            //////////////Early Out/////////////////////
            if(element == null)
            {
                return; 
            }
            ///////////End Early Out///////////////////
            
            StringBuilder stringBuilder= new StringBuilder();

            HashSet<string> AddStateDuplicatesToStringBuilder(StateSave state)
            {
                HashSet<string> foundDuplicates = new HashSet<string>();
                HashSet<string> variables = new HashSet<string>();
                
                foreach(var variable in state.Variables)
                {
                    var name = variable.Name;

                    if(variables.Contains(name))
                    {
                        foundDuplicates.Add(name);
                    }
                    else
                    {
                        variables.Add(name);
                    }
                }
                return foundDuplicates;
            }

            var duplicates = AddStateDuplicatesToStringBuilder(element.DefaultState);

            if(duplicates.Count > 0) 
            {
                var message = $"The default state for {element} has the following duplicate variables. Open the XML file and correct these:";
                foreach(var variable in duplicates)
                {
                    message += "\n" + variable;
                }

                GumCommands.Self.GuiCommands.ShowMessage(message);

            }

        }
    }
}
