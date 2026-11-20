using System.Linq;
using Gum.DataTypes;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Finds an element by name among a project's Screens, Components and standard elements, the set
/// <c>gumcli edit</c> and <c>gumcli describe</c> work on.
/// </summary>
public static class ProjectElementFinder
{
    /// <summary>
    /// The Screen, Component or standard element named <paramref name="name"/>, or null when there is none.
    /// </summary>
    public static ElementSave? Find(GumProjectSave project, string name) =>
        (ElementSave?)project.Screens.FirstOrDefault(item => item.Name == name)
            ?? (ElementSave?)project.Components.FirstOrDefault(item => item.Name == name)
            ?? project.StandardElements.FirstOrDefault(item => item.Name == name);
}
