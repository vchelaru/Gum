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
    public bool GetIfVariableIsDefinedByType(string variableName, ElementSave typeElement) =>
        GetCandidates(typeElement).Any(candidate => candidate.Name == variableName);

    /// <inheritdoc/>
    public bool GetIfVariableListIsDefinedByType(string variableListName, ElementSave typeElement) =>
        GetDefaultStatesDefiningType(typeElement).Any(state => state.VariableLists.Any(list => list.Name == variableListName));

    /// <inheritdoc/>
    public IEnumerable<DefinedVariable> GetVariablesDefinedByType(ElementSave typeElement)
    {
        HashSet<string> listed = new HashSet<string>();
        foreach (DefinedVariable candidate in GetCandidates(typeElement))
        {
            if (listed.Add(candidate.Name))
            {
                yield return candidate;
            }
        }
    }

    /// <summary>
    /// Every variable the type defines, in lookup order; a name can repeat (a standard variable is in both the
    /// project's file and the built-in defaults).
    /// </summary>
    private static IEnumerable<DefinedVariable> GetCandidates(ElementSave typeElement)
    {
        yield return new DefinedVariable("State", "State", null);

        foreach (ElementSave inChain in GetSelfAndBaseElements(typeElement))
        {
            foreach (StateSaveCategory category in inChain.Categories)
            {
                yield return new DefinedVariable(category.Name + "State", category.Name, category);
            }

            // A custom variable is declared by the element that owns it. An exposed variable is declared by name
            // on the element that exposes it, and a derived element or an instance of it uses that name.
            foreach (VariableSave variable in inChain.DefaultState?.Variables ?? Enumerable.Empty<VariableSave>())
            {
                if (string.IsNullOrEmpty(variable.SourceObject))
                {
                    if (variable.IsCustomVariable)
                    {
                        yield return new DefinedVariable(variable.Name, variable.Type ?? "", null);
                    }
                }
                else if (!string.IsNullOrEmpty(variable.ExposedAsName))
                {
                    yield return new DefinedVariable(variable.ExposedAsName, variable.Type ?? "", null);
                }
            }
        }

        foreach (StateSave state in GetDefaultStatesDefiningType(typeElement))
        {
            foreach (VariableSave variable in state.Variables)
            {
                yield return new DefinedVariable(variable.Name, variable.Type ?? "", null);
            }
        }

        // The Forms properties of the type's behaviors (a Slider's Minimum), the Variables tab's Behavior rows.
        // Last, so a name a visual variable also has stays that variable.
        foreach (VariableSave declaration in FormsBehaviorChain.GetFormsPropertyDeclarations(typeElement))
        {
            yield return new DefinedVariable(declaration.Name, declaration.Type ?? "", null);
        }
    }

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
