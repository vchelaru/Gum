using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Removes custom variables: the data change behind the tool's Delete Variable.
/// </summary>
public interface IVariableRemovalLogic
{
    /// <summary>
    /// Why the custom variable <paramref name="variable"/> cannot be removed from <paramref name="container"/>, or
    /// null when it can. A variable reference that uses it, or an inheriting element exposing a different variable
    /// under the same name, blocks the remove. Values other elements set for it do not block; they are returned in
    /// <paramref name="cascadingOverrides"/> for the caller to remove. See ADR 0016.
    /// </summary>
    string? GetWhyCannotRemoveCustomVariable(VariableSave variable, IStateContainer container,
        out List<VariableChange> cascadingOverrides);

    /// <summary>
    /// Removes the custom variable from the element's default state or the behavior's required variables.
    /// </summary>
    void RemoveCustomVariable(VariableSave variable, IStateContainer container);
}

/// <inheritdoc/>
public class VariableRemovalLogic : IVariableRemovalLogic
{
    private readonly IReferenceFinder _referenceFinder;

    public VariableRemovalLogic(IReferenceFinder referenceFinder)
    {
        _referenceFinder = referenceFinder;
    }

    /// <inheritdoc/>
    public string? GetWhyCannotRemoveCustomVariable(VariableSave variable, IStateContainer container,
        out List<VariableChange> cascadingOverrides)
    {
        cascadingOverrides = new List<VariableChange>();

        bool isVariableContained = container switch
        {
            ElementSave element => element.GetDefaultStateOrThrow().Variables.Contains(variable),
            BehaviorSave behavior => behavior.RequiredVariables.Variables.Contains(variable),
            _ => false
        };
        if (!isVariableContained)
        {
            return $"The variable {variable} is not contained in {container}";
        }

        VariableChangeResponse references =
            _referenceFinder.GetReferencesToVariable(container, variable.Name, variable.GetRootName());

        // A VariableReferences binding, or an inheriting element exposing a variable of its own under the same
        // name, is not a simple "restore this value" case, so it blocks instead of cascading.
        List<VariableChange> blockingChanges = references.VariableChanges.Where(c => !c.IsRemovableValue).ToList();
        if (blockingChanges.Count > 0 || references.VariableReferenceChanges.Count > 0)
        {
            VariableChangeResponse blocking = new VariableChangeResponse();
            blocking.VariableChanges.AddRange(blockingChanges);
            blocking.VariableReferenceChanges.AddRange(references.VariableReferenceChanges);

            return $"Cannot delete variable {variable.Name} because it is referenced by other elements.\n\n{blocking.GetChangesDetails()}";
        }

        cascadingOverrides = references.VariableChanges.Where(c => c.IsRemovableValue).ToList();
        return null;
    }

    /// <inheritdoc/>
    public void RemoveCustomVariable(VariableSave variable, IStateContainer container)
    {
        if (container is ElementSave element)
        {
            element.GetDefaultStateOrThrow().Variables.Remove(variable);
        }
        else if (container is BehaviorSave behavior)
        {
            behavior.RequiredVariables.Variables.Remove(variable);
        }
    }
}
