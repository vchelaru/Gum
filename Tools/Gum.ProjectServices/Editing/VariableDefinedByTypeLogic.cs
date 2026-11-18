using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <inheritdoc/>
public class VariableDefinedByTypeLogic : IVariableDefinedByTypeLogic
{
    /// <inheritdoc/>
    public bool GetIfVariableIsDefinedByType(string variableName, ElementSave typeElement)
    {
        if (variableName == "State")
        {
            return true;
        }

        bool isCategorySelector = variableName.Length > "State".Length && variableName.EndsWith("State");
        foreach (ElementSave inChain in GetSelfAndBaseElements(typeElement))
        {
            if (isCategorySelector && inChain.Categories.Any(category => category.Name + "State" == variableName))
            {
                return true;
            }

            // A custom variable is declared by the element that owns it. An exposed variable is declared by name
            // on the element that exposes it, and a derived element or an instance of it uses that name.
            if (inChain.DefaultState?.Variables.Any(variable => string.IsNullOrEmpty(variable.SourceObject)
                    ? variable.IsCustomVariable && variable.Name == variableName
                    : variable.ExposedAsName == variableName) == true)
            {
                return true;
            }
        }

        return GetDefaultStatesDefiningType(typeElement).Any(state => state.Variables.Any(variable => variable.Name == variableName));
    }

    /// <inheritdoc/>
    public bool GetIfVariableListIsDefinedByType(string variableListName, ElementSave typeElement) =>
        GetDefaultStatesDefiningType(typeElement).Any(state => state.VariableLists.Any(list => list.Name == variableListName));

    private static IEnumerable<ElementSave> GetSelfAndBaseElements(ElementSave element)
    {
        yield return element;
        foreach (ElementSave baseElement in ObjectFinder.Self.GetBaseElements(element))
        {
            yield return baseElement;
        }
    }

    /// <summary>
    /// The default states that list what an element of this type has: its root standard element's (the project's
    /// file and the built-in defaults, since a project can predate a variable) and, for a Component or Screen,
    /// that kind's built-in defaults.
    /// </summary>
    private static IEnumerable<StateSave> GetDefaultStatesDefiningType(ElementSave typeElement)
    {
        StandardElementSave? rootStandard = ObjectFinder.Self.GetRootStandardElementSave(typeElement);
        if (rootStandard != null)
        {
            if (rootStandard.DefaultState != null)
            {
                yield return rootStandard.DefaultState;
            }
            if (StandardElementsManager.Self.TryGetDefaultStateFor(rootStandard.Name, throwExceptionOnMissing: false) is { } builtIn)
            {
                yield return builtIn;
            }
        }

        string? kindName = typeElement switch
        {
            ComponentSave => "Component",
            ScreenSave => "Screen",
            _ => null
        };
        if (kindName != null && StandardElementsManager.Self.TryGetDefaultStateFor(kindName, throwExceptionOnMissing: false) is { } kindDefaults)
        {
            yield return kindDefaults;
        }
    }
}
