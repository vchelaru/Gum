using System.Collections.Generic;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The top-level names an element's animations take, so no instance, category or exposed variable shares one.
/// </summary>
public static class AnimationTopLevelNames
{
    /// <summary>
    /// Adds one name per animation in <paramref name="animations"/> to <paramref name="names"/>. Does nothing for null.
    /// </summary>
    public static void Add(ElementAnimationsSave? animations, List<TopLevelName> names)
    {
        if (animations == null)
        {
            return;
        }

        foreach (AnimationSave animation in animations.Animations)
        {
            names.Add(new TopLevelName(animation.Name, "Animation", animation));
        }
    }
}
