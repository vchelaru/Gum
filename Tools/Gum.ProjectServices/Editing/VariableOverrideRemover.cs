using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// A variable a removal took out of a state, so the tool can record it for undo, save its element and notify plugins.
/// </summary>
/// <param name="Container">The element whose state held the variable.</param>
/// <param name="State">The state the variable was removed from.</param>
/// <param name="Variable">The removed variable.</param>
public sealed record RemovedVariable(ElementSave Container, StateSave State, VariableSave Variable);

/// <summary>
/// Removes values that other elements set for a variable that is going away (a deleted custom variable, an
/// un-exposed variable, the exposed variables of a removed instance), on their instances or, for an inheriting
/// element, on itself. See ADR 0016.
/// </summary>
public interface IVariableOverrideRemover
{
    /// <summary>
    /// Removes each override from its state and returns what was removed. An override whose container is not an
    /// element, or an instance value whose instance no longer exists, is skipped.
    /// </summary>
    IReadOnlyList<RemovedVariable> Remove(IEnumerable<VariableChange> overrides);
}

/// <inheritdoc/>
public class VariableOverrideRemover : IVariableOverrideRemover
{
    /// <inheritdoc/>
    public IReadOnlyList<RemovedVariable> Remove(IEnumerable<VariableChange> overrides)
    {
        List<RemovedVariable> removed = new List<RemovedVariable>();
        foreach (VariableChange change in overrides)
        {
            if (change.Container is not ElementSave element)
            {
                continue;
            }

            // An inheriting element's own value may name no instance, or one it inherits without a copy.
            bool hasInstance = !string.IsNullOrEmpty(change.Variable.SourceObject)
                && element.GetInstance(change.Variable.SourceObject) != null;
            if (!hasInstance && !change.IsInheritingElementValue)
            {
                continue;
            }

            change.State.Variables.Remove(change.Variable);
            removed.Add(new RemovedVariable(element, change.State, change.Variable));
        }
        return removed;
    }
}
