using Gum.DataTypes;

namespace Gum.Logic;

/// <summary>
/// Answers whether an element's type defines a variable, the "does the type have it" half of
/// <see cref="IVariableSaveLogic.GetIfVariableIsActive"/> without the selection and plugin exclusions that only
/// decide what the Variables tab shows. A base type change prunes by it (<see cref="IBaseTypeVariablePruner"/>).
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
}
