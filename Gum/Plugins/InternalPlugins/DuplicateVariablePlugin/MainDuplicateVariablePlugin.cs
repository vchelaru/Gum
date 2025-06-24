using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.BaseClasses;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Gum.Services;
using Gum.Services.Dialogs;
using GumCommon;

namespace Gum.Plugins.InternalPlugins.DuplicateVariablePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainDuplicateVariablePlugin : InternalPlugin
    {
        private readonly IDialogService _dialogService;
        
        public MainDuplicateVariablePlugin()
        {
            _dialogService = Locator.GetRequiredService<IDialogService>();
        }
        
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

            HashSet<string> GetStateVariableDuplicates(StateSave state)
            {
                HashSet<string> foundDuplicateVariabless = new HashSet<string>();
                HashSet<string> variables = new HashSet<string>();
                
                foreach(var variable in state.Variables)
                {
                    var name = variable.Name;

                    if(variables.Contains(name))
                    {
                        foundDuplicateVariabless.Add(name);
                    }
                    else
                    {
                        variables.Add(name);
                    }
                }
                return foundDuplicateVariabless;
            }

            HashSet<string> GetStateVariableListDuplicates(StateSave state)
            {
                HashSet<string> foundDuplicateVariabless = new HashSet<string>();
                HashSet<string> variables = new HashSet<string>();

                foreach (var variable in state.VariableLists)
                {
                    var name = variable.Name;

                    if (variables.Contains(name))
                    {
                        foundDuplicateVariabless.Add(name);
                    }
                    else
                    {
                        variables.Add(name);
                    }
                }
                return foundDuplicateVariabless;
            }


            var duplicateVariables = GetStateVariableDuplicates(element.DefaultState);

            var duplicateVariableLists = GetStateVariableListDuplicates(element.DefaultState);

            string message = string.Empty;
            if (duplicateVariables.Count > 0) 
            {
                message += $"The default state for {element} has the following duplicate variables. Open the XML file and correct these:";
                foreach(var variable in duplicateVariables)
                {
                    message += "\n" + variable;
                }
                message += "\n";
            }

            if(duplicateVariableLists.Count > 0)
            {
                message += $"The default state for {element} has the following duplicate variable lists. Open the XML file and correct these:";
                foreach (var variable in duplicateVariableLists)
                {
                    message += "\n" + variable;
                }
                message += "\n";

            }


            if(!string.IsNullOrEmpty(message))
            {
                _dialogService.ShowMessage(message);
            }

        }
    }
}
