using Gum.DataTypes.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.VariableGrid;
public class SubtextLogic
{
    public void GetDefaultSubtext(VariableSave defaultVariable, string subtext, 
        InstanceSavePropertyDescriptor property, 
        ElementSave elementSave, 
        InstanceSave instanceSave)
    {
        property.Subtext = subtext;
        if (!string.IsNullOrEmpty(defaultVariable?.DetailText))
        {
            property.Subtext += "\n" + defaultVariable.DetailText;
        }

        if (defaultVariable.Name == "HasEvents")
        {
            var element = elementSave;
            if (instanceSave != null)
            {
                element = ObjectFinder.Self.GetElementSave(instanceSave);
            }

            if (element is StandardElementSave)
            {
                if (!string.IsNullOrEmpty(property.Subtext))
                {
                    property.Subtext += "\n";
                }
                property.Subtext += "Assigns Cursor.WindowOver only if events are assigned at runtime";
            }
        }
    }

    public string GetSubtextForCurrentState(StateSave stateSave, string variableName)
    {
        var root = ObjectFinder.Self.GetRootVariable(variableName, stateSave.ParentContainer);
        if(root?.Name == "YUnits")
        {
            var rfv = new RecursiveVariableFinder(stateSave);
            var value = rfv.GetValue< PositionUnitType?>(variableName);

            if(value is PositionUnitType.PercentageHeight)
            {
                return "Parents with a Height Units of Depends on Children will ignore this instance";

            }
        }
        if (root?.Name == "XUnits")
        {
            var rfv = new RecursiveVariableFinder(stateSave);
            var value = rfv.GetValue<PositionUnitType?>(variableName);

            if (value is PositionUnitType.PercentageWidth)
            {
                return "Parents with a Width Units of Depends on Children will ignore this instance";

            }
        }


        return "";
    }
}
