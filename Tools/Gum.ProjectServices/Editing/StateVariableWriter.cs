using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Writes a variable's value into a state: the data change behind setting a value in the tool's Variables tab,
/// without the reactions (unit conversion, font and file handling, references) the tool runs afterwards.
/// </summary>
public interface IStateVariableWriter
{
    /// <summary>
    /// The type of <paramref name="variableName"/> as <paramref name="state"/> sees it: from the state or the
    /// element's base chain, else from the variable's root definition. Null when nothing defines it.
    /// </summary>
    string? ResolveVariableType(ElementSave element, StateSave state, string variableName);

    /// <summary>
    /// Sets <paramref name="variableName"/> to <paramref name="value"/> in <paramref name="state"/>, typed by
    /// <see cref="ResolveVariableType"/> (or <paramref name="fallbackType"/> when that finds nothing). A new entry for
    /// a variable exposed on this element or a base keeps its exposed name.
    /// </summary>
    /// <param name="element">The element that owns <paramref name="state"/>.</param>
    /// <param name="state">The state to write into.</param>
    /// <param name="variableName">The variable name, prefixed with the instance name for an instance variable.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="instance">The instance the variable belongs to, or null for the element's own variable.</param>
    /// <param name="fallbackType">The type to use when nothing defines the variable.</param>
    void SetVariable(ElementSave element, StateSave state, string variableName, object? value, InstanceSave? instance, string? fallbackType);
}

/// <inheritdoc/>
public class StateVariableWriter : IStateVariableWriter
{
    /// <inheritdoc/>
    public string? ResolveVariableType(ElementSave element, StateSave state, string variableName)
    {
        string? variableType = state.GetVariableRecursive(variableName)?.Type;
        if (string.IsNullOrEmpty(variableType))
        {
            variableType = ObjectFinder.Self.GetRootVariable(variableName, element)?.Type;
        }
        if (string.IsNullOrEmpty(variableType))
        {
            variableType = GetFormsPropertyType(element, variableName);
        }
        return variableType;
    }

    // A Forms property is declared by a behavior, not stored on any element, so its type comes from the declaration.
    private static string? GetFormsPropertyType(ElementSave element, string variableName)
    {
        string? instanceName = VariableSave.GetSourceObject(variableName);
        ElementSave? typeElement = string.IsNullOrEmpty(instanceName)
            ? element
            : ObjectFinder.Self.GetElementSave(element.GetInstance(instanceName)?.BaseType);
        string? declaredType = typeElement == null
            ? null
            : FormsBehaviorChain.FindFormsPropertyDeclaration(typeElement, VariableSave.GetRootName(variableName))?.Type;
        return string.IsNullOrEmpty(declaredType) ? null : declaredType;
    }

    /// <inheritdoc/>
    public void SetVariable(ElementSave element, StateSave state, string variableName, object? value, InstanceSave? instance, string? fallbackType)
    {
        VariableSave? existingVariable = state.GetVariableRecursive(variableName);
        VariableSave? variableDefinedInThisOrBase = !string.IsNullOrEmpty(existingVariable?.ExposedAsName)
            ? GetVariableDefinedInThisOrBase(element, state, variableName)
            : null;
        string? variableType = ResolveVariableType(element, state, variableName) ?? fallbackType;

        // Read the existing variable before setting, or else we'd get the variable we just set.
        state.SetValue(variableName, value, instance, variableType);

        if (existingVariable?.ExposedAsName is { Length: > 0 } exposedAsName && variableDefinedInThisOrBase != null &&
            state.GetVariableSave(variableName) is { } variable)
        {
            variable.ExposedAsName = exposedAsName;
        }
    }

    /// <summary>
    /// The variable as declared in <paramref name="state"/>, the element's default state, or a base element's
    /// default state; the exposed name is carried only when the variable is defined in one of those.
    /// </summary>
    private static VariableSave? GetVariableDefinedInThisOrBase(ElementSave element, StateSave state, string variableName)
    {
        VariableSave? variable = state.GetVariableSave(variableName);
        if (variable == null && state != element.DefaultState)
        {
            variable = element.GetDefaultStateOrThrow().GetVariableSave(variableName);
        }

        if (variable == null)
        {
            foreach (ElementSave baseElement in ObjectFinder.Self.GetBaseElements(element))
            {
                variable = baseElement.GetDefaultStateOrThrow().GetVariableSave(variableName);
                if (variable != null)
                {
                    break;
                }
            }
        }

        return variable;
    }
}
