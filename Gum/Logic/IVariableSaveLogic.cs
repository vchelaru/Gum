using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.ComponentModel;

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

    /// <summary>
    /// Returns the <see cref="TypeConverter"/> that should be used to edit/display the given variable
    /// in the Variables tab (classified by file/font/guide/state/animation-chain/exposed-variable, or
    /// ultimately its runtime type). Implemented by delegating to the injected
    /// <c>IVariableTypeConverterProvider</c> — the actual narrow seam over
    /// <c>VariableSaveExtensionMethodsGumTool.GetTypeConverter</c> (Locator-resolving, tool-only) —
    /// so headless callers (e.g. the relocated <c>ElementSaveDisplayer</c>) don't need to reference
    /// that tool-only extension method directly.
    /// </summary>
    TypeConverter GetTypeConverter(VariableSave defaultVariable, ElementSave? container);
}
