using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The kind of element <see cref="ProjectElementFinder.List"/> reports.
/// </summary>
public enum ListedElementKind
{
    Screen,
    Component,
    Standard,
    Behavior
}

/// <summary>
/// One entry of a project's element listing.
/// </summary>
/// <param name="Name">The element's name, including its folder.</param>
/// <param name="Kind">Whether it is a Screen, Component, standard element or behavior.</param>
/// <param name="BaseType">The element's base type, or null when it has none (standards and behaviors never do).</param>
public record ListedElement(string Name, ListedElementKind Kind, string? BaseType);

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

    /// <summary>
    /// Every Screen, Component and standard element of <paramref name="project"/>, then its behaviors, each group by name.
    /// </summary>
    public static IReadOnlyList<ListedElement> List(GumProjectSave project) =>
        project.Screens.Select(item => ToListed(item, ListedElementKind.Screen)).OrderBy(item => item.Name)
            .Concat(project.Components.Select(item => ToListed(item, ListedElementKind.Component)).OrderBy(item => item.Name))
            .Concat(project.StandardElements.Select(item => ToListed(item, ListedElementKind.Standard)).OrderBy(item => item.Name))
            .Concat(project.BehaviorReferences.Select(item => new ListedElement(item.Name, ListedElementKind.Behavior, null)).OrderBy(item => item.Name))
            .ToList();

    private static ListedElement ToListed(ElementSave element, ListedElementKind kind) =>
        new ListedElement(element.Name, kind, string.IsNullOrEmpty(element.BaseType) ? null : element.BaseType);
}
