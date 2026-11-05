using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Managers;

namespace Gum.Wireframe.Editors;

/// <summary>
/// Decides when a selected instance needs a full-bounds outline. The resize handles normally show
/// where a selection is, but a component can hide Width and/or Height from its instances, which
/// leaves the instance with no sign that it is selected.
/// </summary>
public static class SelectionOutlineRule
{
    /// <summary>
    /// True when exactly one unlocked instance is selected and its element hides Width or Height
    /// (on the element itself or anywhere up its base types). Locked instances already get their own
    /// dashed outline, and with several objects selected the handles cover the group.
    /// </summary>
    public static bool ShouldShow(IReadOnlyCollection<object?> selectedTags, IObjectFinder objectFinder)
    {
        if (selectedTags.Count != 1)
        {
            return false;
        }

        foreach (object? tag in selectedTags)
        {
            if (tag is not InstanceSave { Locked: false } instance)
            {
                return false;
            }

            ElementSave? element = objectFinder.GetElementSave(instance);
            return element != null
                && (objectFinder.IsVariableHiddenRecursively(element, "Width")
                    || objectFinder.IsVariableHiddenRecursively(element, "Height"));
        }

        return false;
    }
}
