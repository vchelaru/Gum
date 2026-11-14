using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System;
using System.Collections.Generic;

namespace Gum.ProjectServices;

/// <summary>
/// The behaviors an element has through itself and its <c>BaseType</c> chain. A component derived from a
/// Forms control (<c>Controls/ButtonStandard</c>) is that control at runtime, so the runtime, the code generator
/// and the tool all read the whole chain through this one class instead of the element's own
/// <see cref="ElementSave.Behaviors"/>.
/// </summary>
public static class FormsBehaviorChain
{
    /// <summary>
    /// The behaviors of <paramref name="element"/> and every base element, nearest first, each behavior once.
    /// A base cycle ends the walk.
    /// </summary>
    public static IEnumerable<BehaviorSave> GetBehaviors(ElementSave element)
    {
        HashSet<BehaviorSave> seenBehaviors = new HashSet<BehaviorSave>();
        HashSet<ElementSave> visited = new HashSet<ElementSave>();
        ElementSave? current = element;

        while (current != null && visited.Add(current))
        {
            foreach (ElementBehaviorReference reference in current.Behaviors)
            {
                BehaviorSave? behavior = ObjectFinder.Self.GetBehavior(reference);
                if (behavior != null && seenBehaviors.Add(behavior))
                {
                    yield return behavior;
                }
            }

            current = ObjectFinder.Self.GetBaseElement(current);
        }
    }

    /// <summary>
    /// The FormsProperty declarations of every behavior on <paramref name="element"/> and its base chain, in the
    /// order the runtime applier visits them, the nearest element winning a repeated name.
    /// </summary>
    public static IEnumerable<VariableSave> GetFormsPropertyDeclarations(ElementSave element)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (BehaviorSave behavior in GetBehaviors(element))
        {
            foreach (VariableSave declaration in behavior.FormsProperties)
            {
                if (!string.IsNullOrEmpty(declaration.Name) && seen.Add(declaration.Name))
                {
                    yield return declaration;
                }
            }
        }
    }

    /// <summary>
    /// The declaration of the FormsProperty named <paramref name="name"/> on <paramref name="element"/> and its
    /// base chain, the nearest winning, or null. Same answer as searching <see cref="GetFormsPropertyDeclarations"/>
    /// but with plain loops and no collections, since a live reference update asks it on every re-evaluation.
    /// </summary>
    public static VariableSave? FindFormsPropertyDeclaration(ElementSave element, string name)
    {
        ElementSave? current = element;
        for (int depth = 0; current != null && depth < 32; depth++)
        {
            List<ElementBehaviorReference> references = current.Behaviors;
            for (int i = 0; i < references.Count; i++)
            {
                BehaviorSave? behavior = ObjectFinder.Self.GetBehavior(references[i]);
                if (behavior == null)
                {
                    continue;
                }
                List<VariableSave> properties = behavior.FormsProperties;
                for (int j = 0; j < properties.Count; j++)
                {
                    if (properties[j].Name == name)
                    {
                        return properties[j];
                    }
                }
            }
            current = ObjectFinder.Self.GetBaseElement(current);
        }
        return null;
    }

    /// <summary>
    /// A name resolver for the Forms properties of a component's behaviors, used where the instance (or
    /// the project's stored data) holds no value. It checks, in order, the component's own default state
    /// (when an <paramref name="instance"/> is given) and then the behavior declarations on the component's
    /// base chain, returning the declared <c>Value</c>. Together with the instance override the caller reads
    /// first, these are the three tiers of Forms-property defaults: instance override, component default,
    /// behavior declaration (see the <c>gum-forms-behaviors</c> skill). Skipping the component-default tier
    /// resolved an instance whose value equalled a stale declared default as if nothing had changed (#4904).
    /// Names are bare (<c>Minimum</c>) or qualified with the instance name (<c>Slider.Minimum</c>); a name
    /// qualified with another instance of the container is answered from that instance's component.
    /// </summary>
    public static Func<string, object?> BuildFormsPropertyDefaultsFallback(ComponentSave component, InstanceSave? instance)
    {
        string? instancePrefix = instance != null ? instance.Name + "." : null;
        StateSave? componentDefaultState = instance != null ? component.DefaultState : null;
        ElementSave? container = instance?.ParentContainer;
        return name =>
        {
            // "Other.Prop" (from a FindAncestor line): the declared default of another instance's Forms property.
            int dotIndex = name.IndexOf('.');
            if (container != null && instancePrefix != null && dotIndex > 0 && !name.StartsWith(instancePrefix))
            {
                InstanceSave? other = FindInstanceInElementOrBase(container, name.Substring(0, dotIndex));
                if (other != null && ObjectFinder.Self.GetElementSave(other.BaseType) is ComponentSave otherComponent)
                {
                    return BuildFormsPropertyDefaultsFallback(otherComponent, other)(name);
                }
            }

            string lookupName = instancePrefix != null && name.StartsWith(instancePrefix)
                ? name.Substring(instancePrefix.Length)
                : name;

            object? componentValue = componentDefaultState?.GetValue(lookupName);
            if (componentValue != null)
            {
                return componentValue;
            }

            foreach (VariableSave declaration in GetFormsPropertyDeclarations(component))
            {
                if (declaration.Name == lookupName)
                {
                    return declaration.Value;
                }
            }

            return null;
        };
    }

    /// <summary>The instance named <paramref name="name"/> in <paramref name="container"/> or one of its base elements.</summary>
    public static InstanceSave? FindInstanceInElementOrBase(ElementSave container, string name)
    {
        ElementSave? current = container;
        HashSet<ElementSave> visited = new HashSet<ElementSave>();
        while (current != null && visited.Add(current))
        {
            foreach (InstanceSave instance in current.Instances)
            {
                if (instance.Name == name)
                {
                    return instance;
                }
            }
            current = ObjectFinder.Self.GetBaseElement(current);
        }
        return null;
    }
}
