using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The rules and the data change when an element's or an instance's base type changes: the tool's Base Type row and
/// <c>gumcli edit</c>'s <c>setBaseType</c>. Variables the new type lacks are removed
/// (<see cref="IBaseTypeVariableRemover"/>) and the elements that inherit follow (<see cref="IInheritanceSync"/>).
/// </summary>
public interface IBaseTypeChangeLogic
{
    /// <summary>
    /// Why <paramref name="element"/> cannot get <paramref name="newBaseType"/> as its base type, or null when it
    /// can. A Screen can derive only from another Screen or from nothing (empty); a Component needs a standard
    /// element or another Component; the new base must not create a circular reference.
    /// </summary>
    string? GetWhyCannotSetElementBaseType(ElementSave element, string newBaseType);

    /// <summary>
    /// Sets the base type of <paramref name="element"/> (empty clears it) and reacts as the tool does: removes the
    /// variables the new base lacks and replaces the instances inherited from the old base.
    /// </summary>
    void SetElementBaseType(ElementSave element, string newBaseType);

    /// <summary>
    /// Why <paramref name="instance"/> in <paramref name="container"/> cannot get <paramref name="newBaseType"/> as its
    /// type, or null when it can. An instance inherited from the container's base takes its type from there.
    /// </summary>
    string? GetWhyCannotSetInstanceBaseType(ElementSave container, InstanceSave instance, string newBaseType);

    /// <summary>
    /// Sets the type of <paramref name="instance"/> and reacts as the tool does: removes the variables the new type
    /// lacks and gives the same instance in every element inheriting from <paramref name="container"/> the type.
    /// </summary>
    /// <returns>The values other elements set for a removed exposed variable that were removed.</returns>
    IReadOnlyList<RemovedVariable> SetInstanceBaseType(ElementSave container, InstanceSave instance, string newBaseType);
}

/// <inheritdoc/>
public class BaseTypeChangeLogic : IBaseTypeChangeLogic
{
    private readonly IInstanceAddRules _instanceAddRules;
    private readonly IBaseTypeVariableRemover _variableRemover;
    private readonly IInheritanceSync _inheritanceSync;

    public BaseTypeChangeLogic(IInstanceAddRules instanceAddRules, IBaseTypeVariableRemover variableRemover,
        IInheritanceSync inheritanceSync)
    {
        _instanceAddRules = instanceAddRules;
        _variableRemover = variableRemover;
        _inheritanceSync = inheritanceSync;
    }

    /// <inheritdoc/>
    public string? GetWhyCannotSetElementBaseType(ElementSave element, string newBaseType)
    {
        if (element is StandardElementSave)
        {
            return $"{element.Name} is a standard element, which has no base type.";
        }
        if (element.IsSourceFileMissing)
        {
            return $"The source file for {element.Name} is missing, so it cannot be edited";
        }

        if (string.IsNullOrEmpty(newBaseType))
        {
            return element is ComponentSave
                ? $"{element.Name} is a Component, which needs a base type. Use Container for a plain one."
                : null;
        }

        if (ObjectFinder.Self.GetElementSave(newBaseType) is not { } newBase)
        {
            return $"The project has no Screen, Component or standard element named {newBaseType}.";
        }

        if (element is ScreenSave && newBase is not ScreenSave)
        {
            return $"A Screen can only derive from another Screen, and {newBaseType} is not one.";
        }
        // The Component standard element is the type of components themselves, not something to derive from.
        if (element is ComponentSave && (newBase is ScreenSave || newBase.Name == "Component"))
        {
            return newBase is ScreenSave
                ? $"A Component cannot derive from the Screen {newBaseType}."
                : "A Component cannot derive from the Component standard element. Use Container for a plain one.";
        }

        if (DerivesFrom(newBase, element))
        {
            return $"Setting {element.Name}'s base type to {newBaseType} would create a circular reference";
        }
        return null;
    }

    // Walks the base type names itself: ObjectFinder.GetBaseElement stops at an element already in a cycle, which is
    // the state the tool is in when it checks after assigning the typed base type.
    private static bool DerivesFrom(ElementSave start, ElementSave target)
    {
        HashSet<ElementSave> visited = new HashSet<ElementSave>();
        ElementSave? current = start;
        while (current != null && visited.Add(current))
        {
            if (current == target)
            {
                return true;
            }
            current = string.IsNullOrEmpty(current.BaseType) ? null : ObjectFinder.Self.GetElementSave(current.BaseType);
        }
        return false;
    }

    /// <inheritdoc/>
    public void SetElementBaseType(ElementSave element, string newBaseType)
    {
        element.BaseType = string.IsNullOrEmpty(newBaseType) ? null : newBaseType;

        _variableRemover.PruneElement(element);
        _inheritanceSync.ReplaceInheritedInstances(element);
    }

    /// <inheritdoc/>
    public string? GetWhyCannotSetInstanceBaseType(ElementSave container, InstanceSave instance, string newBaseType)
    {
        if (instance.DefinedByBase)
        {
            return InstancePropertyLogic.GetWhyInherited(container, instance);
        }
        if (string.IsNullOrEmpty(newBaseType))
        {
            return $"{instance.Name} needs a type.";
        }

        if (ObjectFinder.Self.GetElementSave(newBaseType) is not { } typeElement)
        {
            return $"The project has no Component or standard element named {newBaseType}.";
        }
        if (_instanceAddRules.GetWhyCannotAdd(typeElement, container) is { } whyNot)
        {
            return $"{instance.Name} cannot become a {newBaseType}: {whyNot}";
        }
        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<RemovedVariable> SetInstanceBaseType(ElementSave container, InstanceSave instance, string newBaseType)
    {
        instance.BaseType = newBaseType;

        IReadOnlyList<RemovedVariable> removed = _variableRemover.PruneInstance(container, instance);
        _inheritanceSync.SetInstanceBaseType(container, instance);
        return removed;
    }
}
