using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Answers whether an element's type defines a variable, the "does the type have it" half of the tool's
/// <c>IVariableSaveLogic.GetIfVariableIsActive</c> without the selection and plugin exclusions that only
/// decide what the Variables tab shows. A base type change prunes by it, and <c>gumcli edit</c> sets only
/// variables it defines.
/// </summary>
public interface IVariableDefinedByTypeLogic
{
    /// <summary>
    /// Returns whether <paramref name="typeElement"/>'s type defines a variable named <paramref name="variableName"/>
    /// (no instance prefix): a variable of its root standard element, of Component or Screen, a custom or exposed
    /// variable declared along its base chain, or the selector of a category along that chain.
    /// </summary>
    /// <param name="variableName">The variable name without an instance prefix.</param>
    /// <param name="typeElement">The element whose type is asked: an element for its own variables, or the
    /// element an instance is a type of for the instance's variables.</param>
    bool GetIfVariableIsDefinedByType(string variableName, ElementSave typeElement);

    /// <summary>
    /// The variable list counterpart of <see cref="GetIfVariableIsDefinedByType"/>.
    /// </summary>
    bool GetIfVariableListIsDefinedByType(string variableListName, ElementSave typeElement);

    /// <summary>
    /// Lists every variable <see cref="GetIfVariableIsDefinedByType"/> accepts for <paramref name="typeElement"/>,
    /// each name once, with its type.
    /// </summary>
    IEnumerable<DefinedVariable> GetVariablesDefinedByType(ElementSave typeElement);
}

/// <summary>
/// A variable an element's type defines, as <see cref="IVariableDefinedByTypeLogic.GetVariablesDefinedByType"/> lists it.
/// </summary>
/// <param name="Name">The variable name without an instance prefix.</param>
/// <param name="Type">The variable's type, such as <c>float</c> or an enum name; a category selector's type is the category name.</param>
/// <param name="Category">The category a selector variable (<c>{Category}State</c>) picks a state of, or null.</param>
public record DefinedVariable(string Name, string Type, StateSaveCategory? Category);
