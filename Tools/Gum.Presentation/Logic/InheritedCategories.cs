using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Logic;

/// <summary>The state categories an element has, own and inherited through its BaseType chain.</summary>
public static class InheritedCategories
{
    /// <summary>
    /// The element's own categories followed by those of its base types, nearest base first. A category
    /// whose name a more derived element already defines is skipped, since the derived one replaces it.
    /// </summary>
    public static IEnumerable<StateSaveCategory> GetCategories(ElementSave element)
    {
        HashSet<string> seenNames = new HashSet<string>();
        IEnumerable<ElementSave> chain = new[] { element }.Concat(ObjectFinder.Self.GetBaseElements(element));
        foreach (ElementSave chainElement in chain)
        {
            foreach (StateSaveCategory category in chainElement.Categories)
            {
                if (seenNames.Add(category.Name))
                {
                    yield return category;
                }
            }
        }
    }
}
