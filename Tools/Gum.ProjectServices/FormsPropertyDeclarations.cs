using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System;
using System.Collections.Generic;

namespace Gum.ProjectServices;

/// <summary>
/// Resolves the Forms properties that the behaviors on an element declare.
/// </summary>
public static class FormsPropertyDeclarations
{
    /// <summary>
    /// The FormsProperty declarations of every behavior on <paramref name="element"/> and its base chain, in the
    /// order the runtime applier visits them, the nearest element winning a repeated name.
    /// </summary>
    public static IEnumerable<VariableSave> GetFor(ElementSave element)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        HashSet<ElementSave> visited = new HashSet<ElementSave>();
        ElementSave? current = element;

        while (current != null && visited.Add(current))
        {
            foreach (ElementBehaviorReference reference in current.Behaviors)
            {
                BehaviorSave? behavior = ObjectFinder.Self.GetBehavior(reference);
                if (behavior == null)
                {
                    continue;
                }

                foreach (VariableSave declaration in behavior.FormsProperties)
                {
                    if (!string.IsNullOrEmpty(declaration.Name) && seen.Add(declaration.Name))
                    {
                        yield return declaration;
                    }
                }
            }

            current = ObjectFinder.Self.GetBaseElement(current);
        }
    }
}
