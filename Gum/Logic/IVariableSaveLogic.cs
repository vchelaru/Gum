using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Logic;

/// <summary>
/// Determines whether variables should be included when building the Variables tab for an element or instance.
/// </summary>
public interface IVariableSaveLogic
{
    /// <summary>
    /// Returns whether the given variable should be active (visible/editable) for the current selection context.
    /// </summary>
    bool GetIfVariableIsActive(VariableSave defaultVariable, ElementSave container, InstanceSave? currentInstance);

    /// <summary>
    /// Returns whether the given variable list should be included based on the container's base type.
    /// </summary>
    bool GetShouldIncludeBasedOnBaseType(VariableListSave variableList, ElementSave container, StandardElementSave rootElementSave);

    /// <summary>
    /// Returns whether a variable is hidden from instances of the given element, walking the inheritance chain.
    /// </summary>
    bool IsVariableHiddenForInstance(string variableName, InstanceSave instance);
}
