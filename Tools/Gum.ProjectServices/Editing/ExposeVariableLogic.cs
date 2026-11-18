using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Exposes an instance's variable on its element and un-exposes it: the data change behind the tool's Expose
/// Variable and Un-expose, without the prompts, undo, plugin events or saving.
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

    /// <summary>
    /// Why the variable exposed as <paramref name="exposedName"/> cannot be un-exposed, or null when it can.
    /// <paramref name="references"/> are the uses of the exposed name; a variable reference among them blocks.
    /// Values other elements set through the exposed name do not block; they are returned in
    /// <paramref name="valuesToClear"/> for the caller to remove or keep.
    /// </summary>
    string? GetWhyCannotUnexpose(VariableChangeResponse references, string exposedName, out List<VariableChange> valuesToClear);

    /// <summary>
    /// Clears the exposed name of <paramref name="variable"/>, then removes it from <paramref name="element"/> when
    /// it is a valueless shader parameter placeholder nothing else names. Removing the values other elements set
    /// through the exposed name is the caller's choice.
    /// </summary>
    void Unexpose(ElementSave element, VariableSave variable);
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

    /// <inheritdoc/>
    public string? GetWhyCannotUnexpose(VariableChangeResponse references, string exposedName, out List<VariableChange> valuesToClear)
    {
        valuesToClear = new List<VariableChange>();
        if (references.VariableReferenceChanges.Count > 0)
        {
            string message = $"Cannot unexpose variable {exposedName} because it is referenced by:\n\n";
            foreach (VariableReferenceChange item in references.VariableReferenceChanges)
            {
                message += $"{item.VariableReferenceList.ValueAsIList[item.LineIndex]} in {item.VariableReferenceList.Name} ({item.Container})\n";
            }
            return message;
        }

        valuesToClear = references.VariableChanges.Where(change => change.IsRemovableValue).ToList();
        return null;
    }

    /// <inheritdoc/>
    public void Unexpose(ElementSave element, VariableSave variable)
    {
        variable.ExposedAsName = null;
        RemoveUnnamedShaderPlaceholder(variable, element);
    }

    /// <summary>
    /// Removes the marked, valueless shader parameter entry that exposing added once nothing names the parameter any
    /// more: no value in any state, no variable reference assigning it, and no exposure. A parameter that is only
    /// saved because something names it must not stay saved after that is gone.
    /// </summary>
    private void RemoveUnnamedShaderPlaceholder(VariableSave variable, ElementSave element)
    {
        if (!global::RenderingLibrary.Graphics.ShaderParameterMarker.TryParse(variable.ShaderParameter, out _) ||
            variable.SetsValue || variable.Value != null || !string.IsNullOrEmpty(variable.ExposedAsName))
        {
            return;
        }

        string referencesName = variable.SourceObject + ".VariableReferences";
        string rootName = variable.GetRootName();
        foreach (StateSave state in element.AllStates)
        {
            foreach (VariableSave other in state.Variables)
            {
                if (other != variable && other.Name == variable.Name && (other.SetsValue || !string.IsNullOrEmpty(other.ExposedAsName)))
                {
                    return;
                }
            }

            foreach (VariableListSave list in state.VariableLists)
            {
                if (list.Name != referencesName || list.ValueAsIList == null)
                {
                    continue;
                }
                foreach (object? line in list.ValueAsIList)
                {
                    string? text = line as string;
                    if (text == null || text.TrimStart().StartsWith("//"))
                    {
                        continue;
                    }
                    int equals = text.IndexOf('=');
                    if (equals > 0 && text.Substring(0, equals).Trim() == rootName)
                    {
                        return;
                    }
                }
            }
        }

        foreach (StateSave state in element.AllStates)
        {
            state.Variables.Remove(variable);
        }
    }
}
