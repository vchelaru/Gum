using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.PropertyGridHelpers.Converters;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.Controls;

namespace Gum.Managers
{
    public class StandardElementsManagerGumTool
    {
        public static StandardElementsManagerGumTool Self { get; private set; }
        public StandardElementsManagerGumTool()
        {
            Self = Self switch
            {
                { } => throw new NotImplementedException(),
                _ => this
            };
        }
        
        public void Initialize()
        {
            var defaultStates =
                StandardElementsManager.Self.DefaultStates;
            defaultStates["Container"].Variables.First(item => item.Name == "ContainedType").CustomTypeConverter =
                new AvailableContainedTypeConverter();

            defaultStates["Component"].Variables
                .Add(new VariableSave { SetsValue = true, Type = "State", Value = null, Name = "State", CustomTypeConverter = new AvailableStatesConverter(null) });

            foreach (var state in StandardElementsManager.Self.DefaultStates.Values)
            {
                foreach(var variable in state.Variables)
                {
                    if (variable.Type == "float" && variable.Name == "Rotation")
                    {
                        MakeDegreesAngle(variable);
                    }
                    else if (variable.Type == "string" &&  variable.Name == "Parent")
                    {
                        variable.CustomTypeConverter = new AvailableInstancesConverter() { IncludeScreenBounds = true };
                        variable.PropertiesToSetOnDisplayer["IsEditable"] = true;
                    }
                    else if(variable.Type == "State" && variable.Name == "State")
                    {
                        variable.Category = "States and Visibility";
                        variable.CustomTypeConverter = new AvailableStatesConverter(null);
                    }
                    else if(variable.Name == "Red" ||
                        variable.Name == "Green" ||
                        variable.Name == "Blue" ||
                        variable.Name == "Alpha")
                    {
                        variable.PropertiesToSetOnDisplayer["MinValue"] = 0.0;
                        variable.PropertiesToSetOnDisplayer["MaxValue"] = 255.0;
                        variable.PreferredDisplayer = typeof(SliderDisplay);
                    }
                }
            }




            RefreshStateVariablesThroughPlugins();

        }

        public void FixCustomTypeConverters(GumProjectSave project)
        {
            foreach(var screen in project.Screens)
            {
                FixCustomTypeConverters(screen);
            }
            foreach(var component in project.Components)
            {
                FixCustomTypeConverters(component);
            }
        }

        public void FixCustomTypeConverters(ElementSave elementSave)
        {
            foreach (var stateSaveCategory in elementSave.Categories)
            {
                VariableSave foundVariable = elementSave.DefaultState.Variables.FirstOrDefault(item => item.Name == stateSaveCategory.Name + "State");

                if (foundVariable != null)
                {
                    foundVariable.CustomTypeConverter = new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(stateSaveCategory.Name);
                }
            }
        }

        public static void MakeDegreesAngle(VariableSave variableSave)
        {
            variableSave.PreferredDisplayer = typeof(AngleSelectorDisplay);
            variableSave.PropertiesToSetOnDisplayer[nameof(AngleSelectorDisplay.TypeToPushToInstance)] = AngleType.Degrees;
        }

        public void RefreshStateVariablesThroughPlugins()
        {
#if GUM
            foreach (var kvp in StandardElementsManager.Self.DefaultStates)
            {
                PluginManager.Self.ModifyDefaultStandardState(kvp.Key, kvp.Value);
            }
#endif
        }



    }
}
