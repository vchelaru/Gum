using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Removes Screens and Components from a project: the data change behind the tool's element delete. Deleting the
/// element's file is left to the caller, after the project saves.
/// </summary>
public interface IElementRemovalLogic
{
    /// <summary>
    /// Removes <paramref name="element"/> and its reference from <paramref name="project"/>. Returns false, and
    /// changes nothing, for a standard element, which is never removed.
    /// </summary>
    bool RemoveElement(GumProjectSave project, ElementSave element);
}

/// <inheritdoc/>
public class ElementRemovalLogic : IElementRemovalLogic
{
    /// <inheritdoc/>
    public bool RemoveElement(GumProjectSave project, ElementSave element)
    {
        if (element is ScreenSave screen)
        {
            RemoveReference(element.Name, project.ScreenReferences);
            project.Screens.Remove(screen);
            return true;
        }
        if (element is ComponentSave component)
        {
            RemoveReference(element.Name, project.ComponentReferences);
            project.Components.Remove(component);
            return true;
        }
        return false;
    }

    private static void RemoveReference(string name, List<ElementReference> references)
    {
        int index = references.FindIndex(item => item.Name == name);
        if (index >= 0)
        {
            references.RemoveAt(index);
        }
    }
}
