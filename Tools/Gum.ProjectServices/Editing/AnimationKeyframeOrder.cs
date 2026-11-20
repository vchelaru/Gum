using System;
using System.Collections.Generic;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Where a new animation keyframe goes in a list kept in time order. The tool's keyframe list and the CLI's
/// animation operations both insert through it.
/// </summary>
public static class AnimationKeyframeOrder
{
    /// <summary>
    /// The index to insert a keyframe at <paramref name="time"/>: after every keyframe at the same time, before the
    /// first later one.
    /// </summary>
    public static int GetInsertIndex<T>(IReadOnlyList<T> keyframes, Func<T, float> timeOf, float time)
    {
        int index = keyframes.Count;
        while (index > 0 && timeOf(keyframes[index - 1]) > time)
        {
            index--;
        }
        return index;
    }
}
