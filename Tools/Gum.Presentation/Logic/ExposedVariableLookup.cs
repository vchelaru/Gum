using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Logic;

/// <summary>
/// Finds the variables an element exposes from one of its instances, following the BaseType chain so an
/// exposure declared on a base component also counts on the components derived from it (#7650).
/// </summary>
public static class ExposedVariableLookup
{
    /// <summary>
    /// Default-state variables of <paramref name="owner"/> and its base types that expose a variable of
    /// <paramref name="instance"/> (<see cref="VariableSave.ExposedAsName"/> set). When several elements in the
    /// chain expose the same variable, only the most derived one is returned.
    /// </summary>
    public static List<VariableSave> GetExposures(ElementSave owner, InstanceSave instance)
    {
        List<VariableSave> result = new List<VariableSave>();
        HashSet<string> seenNames = new HashSet<string>();
        IEnumerable<ElementSave> chain = new[] { owner }.Concat(ObjectFinder.Self.GetBaseElements(owner));
        foreach (ElementSave chainElement in chain)
        {
            StateSave? defaultState = chainElement.DefaultState;
            if (defaultState == null)
            {
                continue;
            }
            foreach (VariableSave variable in defaultState.Variables)
            {
                if (!string.IsNullOrEmpty(variable.ExposedAsName) &&
                    variable.SourceObject == instance.Name &&
                    seenNames.Add(variable.Name))
                {
                    result.Add(variable);
                }
            }
        }
        return result;
    }

    /// <summary>The exposure of one variable of <paramref name="instance"/> by root name, or null.</summary>
    public static VariableSave? GetExposure(ElementSave owner, InstanceSave instance, string rootName) =>
        GetExposures(owner, instance).FirstOrDefault(item => item.GetRootName() == rootName);
}
