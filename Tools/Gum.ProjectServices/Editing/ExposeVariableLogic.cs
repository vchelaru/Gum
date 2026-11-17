using System;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Exposes an instance's variable on its element: the data change behind the tool's Expose Variable, without
/// the name prompt, undo, plugin events or saving.
/// </summary>
public interface IExposeVariableLogic
{
    /// <summary>
    /// The type of a variable nobody has set on <paramref name="instance"/>, found on its base type, through the
    /// variable's root, or as a Forms property one of its behaviors declares. Null when none define it, which
    /// means the variable cannot be exposed.
    /// </summary>
    string? GetUnauthoredVariableType(InstanceSave instance, string rootVariableName, ElementSave parentContainer);

    /// <summary>
    /// Sets <paramref name="exposedName"/> as the exposed name of <paramref name="instance"/>'s
    /// <paramref name="rootVariableName"/>, adding the variable to the default state, without setting a value, when
    /// nothing has set it yet.
    /// </summary>
    /// <exception cref="InvalidOperationException">The variable's type cannot be found.</exception>
    VariableSave Expose(ElementSave element, InstanceSave instance, string rootVariableName, string exposedName);
}

/// <inheritdoc/>
public class ExposeVariableLogic : IExposeVariableLogic
{
    /// <inheritdoc/>
    public string? GetUnauthoredVariableType(InstanceSave instance, string rootVariableName, ElementSave parentContainer)
    {
        // An instance of a missing type has no base variables.
        ElementSave? elementForInstance = ObjectFinder.Self.GetElementSave(instance.BaseType);

        VariableSave? variable = elementForInstance?.GetDefaultStateOrThrow().GetVariableSave(rootVariableName)
            ?? ObjectFinder.Self.GetRootVariable(instance.Name + "." + rootVariableName, parentContainer);

        if (variable != null)
        {
            return variable.Type;
        }

        return elementForInstance == null
            ? null
            : FormsBehaviorChain.GetFormsPropertyDeclarations(elementForInstance).FirstOrDefault(item => item.Name == rootVariableName)?.Type;
    }

    /// <inheritdoc/>
    public VariableSave Expose(ElementSave element, InstanceSave instance, string rootVariableName, string exposedName)
    {
        string fullVariableName = instance.Name + "." + rootVariableName;
        StateSave defaultState = element.GetDefaultStateOrThrow();
        VariableSave? variable = defaultState.GetVariableSave(fullVariableName);

        if (variable == null)
        {
            // The instance belongs to the element, so it is its own parent container here.
            string variableType = GetUnauthoredVariableType(instance, rootVariableName, instance.ParentContainer ?? element)
                ?? throw new InvalidOperationException($"Error getting root variable for {fullVariableName} in {element}");

            defaultState.SetValue(fullVariableName, null, instance, variableType);

            // SetValue just added the variable.
            variable = defaultState.GetVariableSave(fullVariableName)!;
            variable.SetsValue = false;
        }

        variable.ExposedAsName = exposedName;
        return variable;
    }
}
