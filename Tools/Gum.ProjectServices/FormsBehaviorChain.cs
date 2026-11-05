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
}
