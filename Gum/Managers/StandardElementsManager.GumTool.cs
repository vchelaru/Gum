using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.PropertyGridHelpers.Converters;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.Controls;

namespace Gum.Managers
{
    public class StandardElementsManagerGumTool : Singleton<StandardElementsManagerGumTool>
    {
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
                SetPreferredDisplayers(state);
            }




            RefreshStateVariablesThroughPlugins();

        }

        public static void SetPreferredDisplayers(StateSave state)
        {
            foreach (var variable in state.Variables)
            {
                if (variable.Type == "float" && 
                    (variable.Name == "Rotation"
                     || variable.Name == "StartAngle"
                     || variable.Name == "SweepAngle"))
                {
                    MakeDegreesAngle(variable);
                }
                else if (variable.Type == nameof(TextOverflowVerticalMode))
                {
                    variable.PreferredDisplayer = typeof(TextOverflowVerticalModeControl);
                }
                else if (variable.Type == nameof(TextOverflowHorizontalMode))
                {
                    variable.PreferredDisplayer = typeof(TextOverflowHorizontalModeControl);
                }
                else if (variable.Type == nameof(ChildrenLayout))
                {
                    variable.PreferredDisplayer = typeof(ChildrenLayoutControl);
                }
                else if (variable.Type == nameof(DimensionUnitType))
                {
                    if (variable.Name == "WidthUnits")
                    {
                        variable.PreferredDisplayer = typeof(WidthUnitsControl);
                    }
                    else if (variable.Name == "HeightUnits")
                    {
                        variable.PreferredDisplayer = typeof(HeightUnitsControl);
                    }
                }
                else if (variable.Type == nameof(PositionUnitType))
                {
                    if (variable.Name == "XUnits" || variable.Name == "GradientX1Units" || variable.Name == "GradientX2Units")
                    {
                        variable.PreferredDisplayer = typeof(XUnitsControl);
                    }
                    else if (variable.Name == "YUnits" || variable.Name == "GradientY1Units" || variable.Name == "GradientY2Units")
                    {
                        variable.PreferredDisplayer = typeof(YUnitsControl);
                    }
                }
                else if (variable.Type == nameof(VerticalAlignment))
                {
                    if (variable.Name == "VerticalAlignment")
                    {
                        variable.PreferredDisplayer = typeof(TextVerticalAlignmentControl);
                    }
                    else if (variable.Name == "YOrigin")
                    {
                        variable.PreferredDisplayer = typeof(YOriginControl);
                    }
                }
                else if (variable.Type == nameof(HorizontalAlignment))
                {
                    if (variable.Name == "HorizontalAlignment")
                    {
                        variable.PreferredDisplayer = typeof(TextHorizontalAlignmentControl);
                    }
                    else if (variable.Name == "XOrigin")
                    {
                        variable.PreferredDisplayer = typeof(XOriginControl);
                    }
                }
                else if (variable.Type == "string" && variable.Name == "Parent")
                {
                    variable.CustomTypeConverter = new AvailableInstancesConverter() { IncludeScreenBounds = true };
                    variable.PropertiesToSetOnDisplayer["IsEditable"] = true;
                }
                else if (variable.Type == "State" && variable.Name == "State")
                {
                    variable.Category = "States and Visibility";
                    variable.CustomTypeConverter = new AvailableStatesConverter(null);
                }
                else if (variable.Name == "Red" ||
                    variable.Name == "Green" ||
                    variable.Name == "Blue" ||
                    variable.Name == "Alpha"||
                    variable.Name == "Red1" ||
                    variable.Name == "Green1" ||
                    variable.Name == "Blue1" ||
                    variable.Name == "Alpha1" ||
                    variable.Name == "Red2" ||
                    variable.Name == "Green2" ||
                    variable.Name == "Blue2" ||
                    variable.Name == "Alpha2" 
                    )
                {
                    variable.PropertiesToSetOnDisplayer["MinValue"] = 0.0;
                    variable.PropertiesToSetOnDisplayer["MaxValue"] = 255.0;
                    variable.PreferredDisplayer = typeof(SliderDisplay);
                }
            }
            foreach (var variableList in state.VariableLists)
            {
                if (variableList.Name == "VariableReferences")
                {
                    variableList.PreferredDisplayer = typeof(StringListTextBoxDisplay);
                }
            }
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
                VariableSave? foundVariable = elementSave.DefaultState.Variables.FirstOrDefault(item => item.Name == stateSaveCategory.Name + "State");

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
