using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;

namespace Gum.ProjectServices;

/// <summary>
/// Supplies an element's animation data (its deserialized <c>.ganx</c>) to headless consumers such
/// as the error checker, decoupling them from how and where the data is loaded (disk, in-memory
/// cache, etc.). Implementations may cache; callers should not assume a fresh disk read per call.
/// </summary>
public interface IElementAnimationsProvider
{
    /// <summary>
    /// Returns the animations defined for <paramref name="element"/> in the given project, or
    /// <c>null</c> if the element has no animation data.
    /// </summary>
    ElementAnimationsSave? GetAnimationsFor(ElementSave element, GumProjectSave project);
}
