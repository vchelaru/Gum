using Gum.DataTypes.Variables;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.StatePlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public string? GetDefaultSubtext(VariableSave defaultVariable,
        string? subtext,
        string propertyName,
        ElementSave elementSave,
        InstanceSave? instanceSave)
    {
        string? result = subtext;
        if (!string.IsNullOrEmpty(defaultVariable.DetailText))
        {
            // No leading newline when nothing sits above the description, or the row shows a blank line first.
            result = string.IsNullOrEmpty(result)
                ? defaultVariable.DetailText
                : result + "\n" + defaultVariable.DetailText;
        }

        return AppendSetBy(result, GetCategoriesSettingVariable(defaultVariable, propertyName, elementSave, instanceSave));
    }

    /// <summary>Adds the "Set by X, Y" line to <paramref name="text"/> when any category sets the variable.</summary>
    public static string? AppendSetBy(string? text, List<StateSaveCategory> settingCategories)
    {
        if(settingCategories.Count > 0)
        {
            if(!string.IsNullOrEmpty(text))
            {
                text += "\n";
            }
            text += "Set by " + string.Join(", ", settingCategories.Select(category => category.Name));
        }

        return text;
    }

    /// <summary>
    /// The categories, in category order, in which any state sets the variable: the ones the row's
    /// "Set by X, Y" line names. Parameters are as for <see cref="GetDefaultSubtext(VariableSave, string?, string, ElementSave, InstanceSave?)"/>.
    /// </summary>
    public List<StateSaveCategory> GetCategoriesSettingVariable(VariableSave defaultVariable,
        string propertyName,
        ElementSave elementSave,
        InstanceSave? instanceSave)
    {
        return GetCategoriesSetting(defaultVariable.Name, propertyName, elementSave, instanceSave,
            (defaultState, name) => defaultState.GetVariableSave(name)?.Name,
            state => state.Variables.Select(variable => variable.Name));
    }

    /// <summary>
    /// <see cref="GetCategoriesSettingVariable(VariableSave, string, ElementSave, InstanceSave?)"/> for a list variable.
    /// </summary>
    public List<StateSaveCategory> GetCategoriesSettingVariable(VariableListSave defaultVariable,
        string propertyName,
        ElementSave elementSave,
        InstanceSave? instanceSave)
    {
        return GetCategoriesSetting(defaultVariable.Name, propertyName, elementSave, instanceSave,
            (defaultState, name) => defaultState.GetVariableListSave(name)?.Name,
            state => state.VariableLists.Select(list => list.Name));
    }

    private static List<StateSaveCategory> GetCategoriesSetting(string defaultVariableName,
        string propertyName,
        ElementSave elementSave,
        InstanceSave? instanceSave,
        Func<StateSave, string, string?> findNameInDefaultState,
        Func<StateSave, IEnumerable<string>> getNamesSetByState)
    {
        List<StateSaveCategory> result = new List<StateSaveCategory>();
        var variableOwner = instanceSave == null ? elementSave : instanceSave.ParentContainer;

        if(variableOwner != null)
        {
            var topLevelVariableName = instanceSave == null ? defaultVariableName
                : $"{instanceSave.Name}.{defaultVariableName}";
            // The lookup is keyed by the (possibly instance-qualified, possibly exposed) name the row shows.
            var nameToSearchFor = instanceSave == null ? propertyName : instanceSave.Name + "." + propertyName;
            var nameInOwner = findNameInDefaultState(variableOwner.GetDefaultStateOrThrow(), nameToSearchFor);

            foreach (var category in variableOwner.Categories)
            {
                // Any state counts, so a variable set only by Disabled still names the category.
                if (category.States.Any(state => getNamesSetByState(state)
                    .Any(name => name == nameInOwner || name == topLevelVariableName)))
                {
                    result.Add(category);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// The palette name of the first colored category, in category order, that sets the variable, or
    /// null when none does. The row's left-edge bar uses it.
    /// </summary>
    public string? GetCategoryColorName(VariableSave defaultVariable,
        string propertyName,
        ElementSave elementSave,
        InstanceSave? instanceSave) =>
        CategoryColors.GetFirstName(GetCategoriesSettingVariable(defaultVariable, propertyName, elementSave, instanceSave));

    /// <summary><see cref="GetCategoryColorName(VariableSave, string, ElementSave, InstanceSave?)"/> for a list variable.</summary>
    public string? GetCategoryColorName(VariableListSave defaultVariable,
        string propertyName,
        ElementSave elementSave,
        InstanceSave? instanceSave) =>
        CategoryColors.GetFirstName(GetCategoriesSettingVariable(defaultVariable, propertyName, elementSave, instanceSave));


    Dictionary<string, Func<StateSave, string, string>> GetSubtextCalls = new()
    {
        {"XUnits", GetXUnitsSubtext },
        {"YUnits", GetYUnitsSubtext }
    };

    public bool HasSubtextFunctionFor(StateSave stateSave, string variableName)
    {
        // A behavior's states have no element, and none of their variables have subtext.
        var root = stateSave.ParentContainer is { } element
            ? ObjectFinder.Self.GetRootVariable(variableName, element)
            : null;
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
        var root = stateSave.ParentContainer is { } element
            ? ObjectFinder.Self.GetRootVariable(variableName, element)
            : null;

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
