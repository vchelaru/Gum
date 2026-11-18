using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Removes an instance from a Screen or Component: the data change behind the tool's instance delete.
/// </summary>
public interface IInstanceRemovalLogic
{
    /// <summary>
    /// Why <paramref name="instance"/> cannot be removed, or null when it can. An instance defined in a base
    /// element cannot be removed from a derived one.
    /// </summary>
    string? GetWhyCannotRemove(InstanceSave instance);

    /// <summary>
    /// Removes <paramref name="instance"/>, its events and every variable in <paramref name="element"/> that sets
    /// or names it, plus the values other elements set on the variables the element exposes from it. Returns those
    /// values from other elements.
    /// </summary>
    IReadOnlyList<RemovedVariable> RemoveInstance(InstanceSave instance, ElementSave element);

    /// <summary>
    /// Removes the variables and variable lists in <paramref name="element"/>'s states that set a value on
    /// <paramref name="instance"/>, and the Parent and render target source variables that name it.
    /// </summary>
    void RemoveReferencesToInstance(InstanceSave instance, ElementSave element);
}

/// <inheritdoc/>
public class InstanceRemovalLogic : IInstanceRemovalLogic
{
    private readonly IReferenceFinder _referenceFinder;
    private readonly IVariableOverrideRemover _overrideRemover;

    public InstanceRemovalLogic(IReferenceFinder referenceFinder, IVariableOverrideRemover overrideRemover)
    {
        _referenceFinder = referenceFinder;
        _overrideRemover = overrideRemover;
    }

    /// <inheritdoc/>
    public string? GetWhyCannotRemove(InstanceSave instance) => instance.DefinedByBase
        ? $"The instance {instance.Name} cannot be deleted because it is defined in a base object."
        : null;

    /// <inheritdoc/>
    public IReadOnlyList<RemovedVariable> RemoveInstance(InstanceSave instance, ElementSave element)
    {
        // Must run before the instance's exposed variables are removed below.
        IReadOnlyList<RemovedVariable> removedOverrides = RemoveValuesSetOnExposedVariablesOf(instance, element);
        element.Instances.Remove(instance);
        element.Events.RemoveAll(item => item.GetSourceObject() == instance.Name);
        RemoveReferencesToInstance(instance, element);
        return removedOverrides;
    }

    /// <summary>
    /// Removing an instance removes the variables the element exposes from it, so the values other elements set
    /// for those exposed names (RowA.Caption, a derived component's own value) would be orphans.
    /// </summary>
    private IReadOnlyList<RemovedVariable> RemoveValuesSetOnExposedVariablesOf(InstanceSave instance, ElementSave element)
    {
        List<VariableChange> valuesToClear = new List<VariableChange>();
        foreach (VariableSave exposed in element.GetDefaultStateOrThrow().Variables
            .Where(v => v.SourceObject == instance.Name && !string.IsNullOrEmpty(v.ExposedAsName))
            .ToList())
        {
            VariableChangeResponse references =
                _referenceFinder.GetReferencesToVariable(element, exposed.Name, exposed.ExposedAsName!);
            valuesToClear.AddRange(references.VariableChanges.Where(c => c.IsRemovableValue));
        }

        return _overrideRemover.Remove(valuesToClear);
    }

    /// <inheritdoc/>
    public void RemoveReferencesToInstance(InstanceSave instance, ElementSave element)
    {
        foreach (StateSave state in element.AllStates)
        {
            for (int i = state.Variables.Count - 1; i > -1; i--)
            {
                VariableSave variable = state.Variables[i];

                if (variable.SourceObject == instance.Name)
                {
                    // Sets a value on the removed object.
                    state.Variables.RemoveAt(i);
                }
                else if ((variable.GetRootName() == "Parent" || variable.GetRootName() == "RenderTargetTextureSource") &&
                         variable.Value is string valueAsString &&
                         (valueAsString == instance.Name || valueAsString.StartsWith(instance.Name + ".")))
                {
                    // Names the removed object: a Parent assignment (or a dotted reference like "Parent.Container")
                    // or a Sprite's render-target source.
                    state.Variables.RemoveAt(i);
                }
            }
            for (int i = state.VariableLists.Count - 1; i > -1; i--)
            {
                if (state.VariableLists[i].SourceObject == instance.Name)
                {
                    state.VariableLists.RemoveAt(i);
                }
            }
        }
    }
}
