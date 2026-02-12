using Gum.DataTypes.Variables;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExCSS;

namespace Gum.Plugins.InternalPlugins.VariableGrid;
public class SubtextLogic
{
    /// <summary>
    /// Returns the subtext for a property based on the initial subtext plus any additional
    /// subtext as determined by the method.
    /// </summary>
    /// <param name="defaultVariable">The default variable, which is the variable as defined in the StandardElementsManager. This may not have the same name as the
    /// actual variable being displayed if it's exposed</param>
    /// <param name="subtext"></param>
    /// <param name="propertyName">The root name of the property being displayed</param>
    /// <param name="elementSave">The element if no instance is specified, or the base type of the argument instance</param>
    /// <param name="instanceSave"></param>
    public string GetDefaultSubtext(VariableSave defaultVariable,
        string subtext,
        string propertyName,
        ElementSave elementSave,
        InstanceSave instanceSave)
    {
        string result = subtext;
        if (!string.IsNullOrEmpty(defaultVariable?.DetailText))
        {
            result += "\n" + defaultVariable.DetailText;
        }

        string? categoryString = null;
        var variableOwner = instanceSave == null ? elementSave : instanceSave.ParentContainer;

        var topLevelVariableName = instanceSave == null ? defaultVariable.Name
            : $"{instanceSave.Name}.{defaultVariable.Name}";


        if(variableOwner != null)
        {
            var nameToSearchFor = instanceSave == null ? propertyName : instanceSave.Name + "." + propertyName;
            var variableInOwner = variableOwner.DefaultState.GetVariableSave(nameToSearchFor);


            foreach (var category in variableOwner.Categories)
            {
                if(category.States.Count > 0)
                {
                    var firstState = category.States[0];

                    foreach(var variable in firstState.Variables)
                    {
                        if(variable.Name == variableInOwner?.Name || variable.Name == topLevelVariableName)
                        {
                            if(categoryString == null)
                            {
                                categoryString = $"Set by {category.Name}";
                            }
                            else
                            {
                                categoryString += $", {category.Name}";
                            }
                            break;
                        }
                    }
                }
            }
        }

        if(categoryString != null)
        {
            if(!string.IsNullOrEmpty(result))
            {
                result += "\n";
            }
            result += categoryString;
        }

        return result;
    }


    Dictionary<string, Func<StateSave, string, string>> GetSubtextCalls = new()
    {
        {"XUnits", GetXUnitsSubtext },
        {"YUnits", GetYUnitsSubtext }
    };

    public bool HasSubtextFunctionFor(StateSave stateSave, string variableName)
    {
        var root = ObjectFinder.Self.GetRootVariable(variableName, stateSave.ParentContainer);
        return root != null && GetSubtextCalls.ContainsKey(root.Name);
    }

    public string GetSubtextForCurrentState(StateSave stateSave, string variableName)
    {
#if DEBUG
        if(stateSave == null)
        {
            throw new ArgumentNullException(nameof(stateSave));
        }
#endif
        var root = ObjectFinder.Self.GetRootVariable(variableName, stateSave.ParentContainer);

        if(root != null && GetSubtextCalls.ContainsKey(root.Name))
        {
            return GetSubtextCalls[root.Name](stateSave, variableName);
        }

        return "";
    }

    private static string GetXUnitsSubtext(StateSave stateSave, string variableName)
    {
        var rfv = new RecursiveVariableFinder(stateSave);
        var value = rfv.GetValue<PositionUnitType?>(variableName);

        if (value is PositionUnitType.PercentageWidth)
        {
            return "Parents with a Width Units of Depends on Children will ignore this instance";

        }
        else
        {
            return string.Empty;
        }
    }

    private static string GetYUnitsSubtext(StateSave stateSave, string variableName)
    {
        var rfv = new RecursiveVariableFinder(stateSave);
        var value = rfv.GetValue<PositionUnitType?>(variableName);

        if (value is PositionUnitType.PercentageHeight)
        {
            return "Parents with a Height Units of Depends on Children will ignore this instance";
        }
        else
        {
            return string.Empty;
        }
    }
}
