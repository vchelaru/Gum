using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The rules an animation's name must meet that need only the other animations' names. The tool's animation name
/// validator and the CLI's animation operations both apply them.
/// </summary>
public static class AnimationNameRules
{
    /// <summary>
    /// Why <paramref name="name"/> cannot name an animation next to <paramref name="otherAnimationNames"/>, or null
    /// when it can. The comparison ignores case.
    /// </summary>
    public static string? GetWhyInvalid(string? name, IEnumerable<string> otherAnimationNames)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "Animation names cannot be empty";
        }
        if (name.Contains(' '))
        {
            return "Animation names cannot have spaces";
        }
        if (otherAnimationNames.Any(item => item.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
        {
            return $"The name \"{name}\" is already being used.";
        }
        return null;
    }
}
