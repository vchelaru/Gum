using Gum.Wireframe;
using System;

namespace SkiaGum;

/// <summary>
/// Extension methods on <see cref="GraphicalUiElement"/> for adding elements to and removing
/// them from the GumService root container.
/// </summary>
public static class GraphicalUiElementExtensionMethods
{
    /// <summary>
    /// Adds this element as a child of the GumService root container, making it a top-level
    /// element that will be rendered and receive layout updates. This is the recommended way
    /// to display a root-level element - prefer this over the obsolete <c>AddToManagers</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if GumService has not been initialized.
    /// </exception>
    public static void AddToRoot(this GraphicalUiElement element)
    {
        if (GumService.Default.IsInitialized == false)
        {
            throw new InvalidOperationException("Cannot call AddToRoot because GumService.Default " +
                "is not initialized - did you remember to call GumService.Default.Initialize(...) first?");
        }
        GumService.Default.Root.Children.Add(element);
    }

    /// <summary>
    /// Removes this element from its parent, effectively removing it from the visual tree.
    /// This reverses a previous <see cref="AddToRoot(GraphicalUiElement)"/> call.
    /// </summary>
    public static void RemoveFromRoot(this GraphicalUiElement element)
    {
        element.Parent = null;
    }
}
