using Gum.DataTypes.Variables;
using Gum.PropertyGridHelpers.Converters;
using Gum.Wireframe;
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
            StandardElementsManager.Self.DefaultStates["Container"].Variables.First(item => item.Name == "Contained Type").CustomTypeConverter =
                new AvailableContainedTypeConverter();

            foreach(var state in StandardElementsManager.Self.DefaultStates.Values)
            {
                foreach(var variable in state.Variables)
                {
                    if(variable.Type == "float" && variable.Name == "Rotation")
                    {
                        MakeDegreesAngle(variable);
                    }
                }
            }
        }

        private static void AddParentVariables(StateSave stateSave)
        {
            VariableSave variableSave = new VariableSave();
            variableSave.SetsValue = true;
            variableSave.Type = "string";
            variableSave.Name = "Parent";
            variableSave.Category = "Parent";
            variableSave.CanOnlyBeSetInDefaultState = true;
            variableSave.CustomTypeConverter = new AvailableInstancesConverter() { IncludeScreenBounds = true };

            variableSave.PropertiesToSetOnDisplayer["IsEditable"] = true;

            stateSave.Variables.Add(variableSave);

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = nameof(GraphicalUiElement.IgnoredByParentSize), Category = "Parent" });
        }

        public static void MakeDegreesAngle(VariableSave variableSave)
        {
            variableSave.PreferredDisplayer = typeof(AngleSelectorDisplay);
            variableSave.PropertiesToSetOnDisplayer[nameof(AngleSelectorDisplay.TypeToPushToInstance)] = AngleType.Degrees;
        }



    }
}
