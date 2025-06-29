﻿using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using System.ComponentModel.Composition;

namespace Gum.Plugins.CirclePlugin
{
    [Export(typeof(PluginBase))]
    public class MainCirclePlugin : InternalPlugin
    {
        private readonly ISelectedState _selectedState;

        public MainCirclePlugin()
        {
            _selectedState = Locator.GetRequiredService<ISelectedState>();
        }
        
        public override void StartUp()
        {
            this.VariableSet += HandleVariableSet;
        }

        private void HandleVariableSet(ElementSave element, InstanceSave instance, string variable, object oldValue)
        {
            var state = _selectedState.SelectedStateSave;

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
