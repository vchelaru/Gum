using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Keeps the elements that derive from a component in sync with it: the data change behind the tool's
/// inheritance reactions to an instance added, removed, renamed, reordered or retyped, and to an element's base
/// type changing. Each method returns the elements it changed so the caller can save and refresh them.
/// </summary>
public interface IInheritanceSync
{
    /// <summary>
    /// Adds a <c>DefinedByBase</c> copy of <paramref name="instance"/> to every element inheriting from
    /// <paramref name="container"/> that lacks one, at the position that mirrors the base.
    /// </summary>
    IReadOnlyList<ElementSave> AddInstance(ElementSave container, InstanceSave instance);

    /// <summary>
    /// Removes the instance named like <paramref name="instance"/> from every element inheriting from
    /// <paramref name="container"/>. Returns every inheriting element.
    /// </summary>
    IReadOnlyList<ElementSave> RemoveInstance(ElementSave container, InstanceSave instance);

    /// <summary>
    /// Renames the instance called <paramref name="oldName"/> in every element inheriting from
    /// <paramref name="container"/> to <paramref name="instance"/>'s name.
    /// </summary>
    IReadOnlyList<ElementSave> RenameInstance(ElementSave container, InstanceSave instance, string oldName);

    /// <summary>
    /// Moves the instance in every element inheriting from its container so it keeps the base's relative order.
    /// </summary>
    IReadOnlyList<ElementSave> ReorderInstance(InstanceSave instance);

    /// <summary>
    /// Gives the instance named like <paramref name="instance"/> in every element inheriting from
    /// <paramref name="container"/> the same base type.
    /// </summary>
    IReadOnlyList<ElementSave> SetInstanceBaseType(ElementSave container, InstanceSave instance);

    /// <summary>
    /// Replaces the instances <paramref name="element"/> inherited with copies of the ones its current base type
    /// has, marked <c>DefinedByBase</c>. Does nothing for a standard base type.
    /// </summary>
    void ReplaceInheritedInstances(ElementSave element);
}

/// <inheritdoc/>
public class InheritanceSync : IInheritanceSync
{
    /// <inheritdoc/>
    public IReadOnlyList<ElementSave> AddInstance(ElementSave container, InstanceSave instance)
    {
        List<ElementSave> changed = new List<ElementSave>();
        foreach (ElementSave inheritingElement in ObjectFinder.Self.GetElementsInheritingFrom(container))
        {
            if (inheritingElement.GetInstance(instance.Name) != null)
            {
                continue;
            }

            InstanceSave clone = instance.Clone();
            clone.DefinedByBase = true;
            clone.ParentContainer = inheritingElement;
            inheritingElement.Instances.Add(clone);

            // An inheriting element's base type always exists.
            ElementSave directBase = ObjectFinder.Self.GetElementSave(inheritingElement.BaseType)!;
            AdjustInstance(directBase, inheritingElement, clone.Name);
            changed.Add(inheritingElement);
        }
        return changed;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ElementSave> RemoveInstance(ElementSave container, InstanceSave instance)
    {
        List<ElementSave> inheriting = ObjectFinder.Self.GetElementsInheritingFrom(container).ToList();
        foreach (ElementSave inheritingElement in inheriting)
        {
            inheritingElement.Instances.RemoveAll(item => item.Name == instance.Name);
        }
        return inheriting;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ElementSave> RenameInstance(ElementSave container, InstanceSave instance, string oldName)
    {
        List<ElementSave> changed = new List<ElementSave>();
        foreach (ElementSave inheritingElement in ObjectFinder.Self.GetElementsInheritingFrom(container))
        {
            if (inheritingElement.GetInstance(oldName) is { } toRename)
            {
                toRename.Name = instance.Name;
                // What the inheriting element set on the inherited instance is keyed by its name.
                new RenameReferenceApplier().RenameInstanceInContainer(inheritingElement, toRename, oldName);
                changed.Add(inheritingElement);
            }
        }
        return changed;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ElementSave> ReorderInstance(InstanceSave instance)
    {
        List<ElementSave> changed = new List<ElementSave>();
        if (instance.ParentContainer is { } baseElement)
        {
            foreach (ElementSave inheritingElement in ObjectFinder.Self.GetElementsInheritingFrom(baseElement))
            {
                if (AdjustInstance(baseElement, inheritingElement, instance.Name))
                {
                    changed.Add(inheritingElement);
                }
            }
        }
        return changed;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ElementSave> SetInstanceBaseType(ElementSave container, InstanceSave instance)
    {
        List<ElementSave> changed = new List<ElementSave>();
        foreach (ElementSave inheritingElement in ObjectFinder.Self.GetElementsInheritingFrom(container))
        {
            if (inheritingElement.GetInstance(instance.Name) is { } toChange)
            {
                toChange.BaseType = instance.BaseType;
                changed.Add(inheritingElement);
            }
        }
        return changed;
    }

    /// <inheritdoc/>
    public void ReplaceInheritedInstances(ElementSave element)
    {
        element.Instances.RemoveAll(item => item.DefinedByBase);

        string? newValue = element.BaseType;
        if (string.IsNullOrEmpty(newValue) || StandardElementsManager.Self.IsDefaultType(newValue))
        {
            return;
        }

        if (ObjectFinder.Self.GetElementSave(newValue) is not { } baseElement)
        {
            return;
        }

        foreach (InstanceSave instance in baseElement.Instances)
        {
            if (element.Instances.FirstOrDefault(item => item.Name == instance.Name) is { } alreadyExists)
            {
                alreadyExists.DefinedByBase = true;
            }
            else
            {
                InstanceSave derivedInstance = instance.Clone();
                derivedInstance.DefinedByBase = true;
                element.Instances.Add(derivedInstance);
            }
        }
        element.Initialize(new StateSave());
    }

    private static bool AdjustInstance(ElementSave baseElement, ElementSave derivedElement, string instanceName)
    {
        InstanceSave? instanceInBase = baseElement.GetInstance(instanceName);
        // Without the instance in the derived element there is nothing to move; going on would
        // insert null into its instance list.
        if (derivedElement.GetInstance(instanceName) is not { } instanceInDerived)
        {
            return false;
        }

        int indexInBase = IndexOf(baseElement.Instances, instanceInBase);
        string? nameOfObjectBefore = null;
        if (indexInBase > 0)
        {
            nameOfObjectBefore = baseElement.Instances[indexInBase - 1].Name;
        }

        string? nameOfObjectAfter = null;
        if (indexInBase < baseElement.Instances.Count - 1)
        {
            nameOfObjectAfter = baseElement.Instances[indexInBase + 1].Name;
        }

        int exclusiveLowerIndexInDerived = -1;
        if (nameOfObjectBefore != null)
        {
            exclusiveLowerIndexInDerived = IndexOf(derivedElement.Instances, derivedElement.GetInstance(nameOfObjectBefore));
        }

        int exclusiveUpperIndexInDerived = derivedElement.Instances.Count;
        if (nameOfObjectAfter != null)
        {
            exclusiveUpperIndexInDerived = IndexOf(derivedElement.Instances, derivedElement.GetInstance(nameOfObjectAfter));
        }

        int currentDerivedIndex = derivedElement.Instances.IndexOf(instanceInDerived);

        int desiredIndex = System.Math.Min(currentDerivedIndex, exclusiveUpperIndexInDerived - 1);
        desiredIndex = System.Math.Max(desiredIndex, exclusiveLowerIndexInDerived + 1);

        if (desiredIndex == currentDerivedIndex)
        {
            return false;
        }

        derivedElement.Instances.Remove(instanceInDerived);
        derivedElement.Instances.Insert(currentDerivedIndex < desiredIndex ? desiredIndex - 1 : desiredIndex, instanceInDerived);
        return true;
    }

    // List.IndexOf returns -1 for a missing (null) instance, the same as for one not in the list.
    private static int IndexOf(List<InstanceSave> instances, InstanceSave? instance) =>
        instance == null ? -1 : instances.IndexOf(instance);
}
