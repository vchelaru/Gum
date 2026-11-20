using Gum.DataTypes;
using Gum.ProjectServices.Editing;
using Gum.Services;

namespace Gum.Logic;

/// <summary>
/// Removes the variables a new base type cannot have, after the base type of an element or an instance changed.
/// </summary>
public interface IBaseTypeVariablePruner
{
    /// <inheritdoc cref="IBaseTypeVariableRemover.PruneElement"/>
    void PruneElement(ElementSave element);

    /// <summary>
    /// Removes from every state of <paramref name="container"/> the variables, variable lists and
    /// <c>VariableReferences</c> lines on <paramref name="instance"/> that the instance's new type does not define
    /// (<see cref="IBaseTypeVariableRemover"/>), and records the values other elements set for a pruned exposed
    /// variable for undo, so the caller must hold an undo lock.
    /// </summary>
    void PruneInstance(ElementSave container, InstanceSave instance);
}

/// <inheritdoc/>
public class BaseTypeVariablePruner : IBaseTypeVariablePruner
{
    private readonly IBaseTypeVariableRemover _remover;
    private readonly IInstanceOverrideRemover _instanceOverrideRemover;

    public BaseTypeVariablePruner(IBaseTypeVariableRemover remover, IInstanceOverrideRemover instanceOverrideRemover)
    {
        _remover = remover;
        _instanceOverrideRemover = instanceOverrideRemover;
    }

    /// <inheritdoc/>
    public void PruneElement(ElementSave element) => _remover.PruneElement(element);

    /// <inheritdoc/>
    public void PruneInstance(ElementSave container, InstanceSave instance) =>
        _instanceOverrideRemover.RecordRemoved(_remover.PruneInstance(container, instance));
}
