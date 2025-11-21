using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Manager
{
    internal class CustomVariableManager
    {
        public static ViewModels.CodeWindowViewModel ViewModel;

        public static void HandleAddAndRemoveVariablesForType(string type, StateSave stateSave)
        {
            // this controls whether IsXamarinFormsControl and IsOverride variables exist.
            // These were added to support maui/xamforms which are currently not supported, so
            // we are killing these for now
            if(false)
            {
                bool Has(string variableName) => stateSave.Variables.Any(item => item.Name == variableName);

                if(!Has("IsXamarinFormsControl"))
                {
                    stateSave.Variables.Add(new VariableSave 
                    { 
                        SetsValue = true, Type = "bool", Category = "Xamarin Forms", 
                            // If we set the value
                            // explicitly, then new
                            // pages will have a hard
                            // `false` value set. This
                            // is problematic because pages
                            // often inherit from base pages,
                            // so a hard value will override the
                            // default derived value.
                            //Value = false, 
                            Name = "IsXamarinFormsControl" 
                    });
                }
                if(!Has("IsOverrideInCodeGen"))
                {
                    stateSave.Variables.Add(new VariableSave 
                    { 
                        SetsValue = true, Type = "bool", Category = "Xamarin Forms", Value = false, Name = "IsOverrideInCodeGen" 
                    });
                }

                // Vic says - it tried adding support here, but this is only called on standard elements, not entities.

                //if(type == "StyledEditor")
                //{
                //    stateSave.Variables.Add(new VariableSave
                //    {
                //        SetsValue = true,
                //        Type = "bool",
                //        Category = "Xamarin Forms",
                //        Value = false,
                //        Name = "CapitalizeWord"
                //    });
                //}
            }
            else
            {
                stateSave.Variables.RemoveAll(item => 
                    item.Name == "IsXamarinFormsControl" ||
                    item.Name == "IsOverrideInCodeGen"
                );
            }
        }
    }
}
