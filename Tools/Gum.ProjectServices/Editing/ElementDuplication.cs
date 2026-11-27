using Gum.DataTypes;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The data change behind the tool's Duplicate of a Screen or Component, also used by <c>gumcli edit</c>'s
/// <c>duplicateComponent</c>: a full copy of the element (instances, states, categories, behaviors, variables and
/// variable references, unchanged) under a new name, initialized so its states and instances point at the copy.
/// The caller adds it to the project and copies the element's animation and code settings files.
/// </summary>
public static class ElementDuplication
{
    /// <summary>Copies <paramref name="source"/> as a Component named <paramref name="newName"/> (folder included).</summary>
    public static ComponentSave CopyComponent(ComponentSave source, string newName) =>
        Initialize(source.Clone(), newName);

    /// <summary>Copies <paramref name="source"/> as a Screen named <paramref name="newName"/> (folder included).</summary>
    public static ScreenSave CopyScreen(ScreenSave source, string newName) =>
        Initialize(source.Clone(), newName);

    private static T Initialize<T>(T copy, string newName) where T : ElementSave
    {
        copy.Name = newName;
        copy.Initialize(null);
        return copy;
    }
}
