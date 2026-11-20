using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Finds the state an animation keyframe names. A keyframe's state name is <c>Category/State</c>, or a bare name
/// for an uncategorized state.
/// </summary>
public static class AnimationStateLookup
{
    /// <summary>
    /// Whether <paramref name="element"/> has the state <paramref name="categorizedStateName"/> names.
    /// </summary>
    public static bool StateExists(string categorizedStateName, ElementSave element)
    {
        int slashIndex = categorizedStateName.IndexOf('/');
        if (slashIndex >= 0)
        {
            string categoryName = categorizedStateName.Substring(0, slashIndex);
            string stateName = categorizedStateName.Substring(slashIndex + 1);
            StateSaveCategory? category = element.Categories.FirstOrDefault(item => item.Name == categoryName);
            return category != null && category.States.Any(item => item.Name == stateName);
        }

        return element.States.Any(item => item.Name == categorizedStateName);
    }
}
