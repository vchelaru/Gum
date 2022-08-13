using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.CirclePlugin
{
    [Export(typeof(PluginBase))]
    public class MainCirclePlugin : InternalPlugin
    {
        public override void StartUp()
        {
            this.VariableSet += HandleVariableSet;
        }

        private void HandleVariableSet(ElementSave element, InstanceSave instance, string variable, object oldValue)
        {
            var state = SelectedState.Self.SelectedStateSave;

            var shouldRefreshGrid = false;

            if(instance != null)
            {
                // todo - inheritance form circle
                if(instance.BaseType == "Circle")
                {
                    // Not investigating this yet, but if we apply width it hops to a different value
                    if( variable == "Height")
                    {
                        var qualifiedVariableName = $"{instance.Name}.Radius";

                        var newValue = state.GetValue($"{instance.Name}.{variable}");
                        object oldRadiusValue = null;

                        if(newValue == null)
                        {
                            var radiusVariable = state.GetVariableSave(qualifiedVariableName);
                            if(radiusVariable != null)
                            {
                                oldRadiusValue = radiusVariable.Value;
                                state.Variables.Remove(radiusVariable);
                                shouldRefreshGrid = true;
                            }

                        }
                        else
                        {
                            var newRadius = ((float)newValue) / 2.0f;

                            oldRadiusValue = state.GetValue(qualifiedVariableName);

                            state.SetValue(qualifiedVariableName, newRadius, "float");
                            shouldRefreshGrid = true;

                        }

                        // todo - call SetValue
                    }
                }
            }
            // todo - handle element

            //if(shouldRefreshGrid)
            //{
            //    GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
            //}
        }
    }
}
