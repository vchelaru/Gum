using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#if FRB
using FlatRedBall.Forms.Controls;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace MonoGameGum.Forms;

#endif


#if !FRB
using Gum.Forms.Controls;
namespace Gum.Forms;

#endif



public static class GraphicalUiElementFormsExtensions
{
    #region FindFormsControl — visual-rooted lookup of FrameworkElement descendants

    /// <summary>
    /// Returns the first descendant of <paramref name="element"/> whose
    /// <see cref="InteractiveGue.FormsControlAsObject"/> is assignable to
    /// <typeparamref name="T"/>, or null if none. Search is shallowest-first; subclasses match.
    /// Mirrors <c>FrameworkElement.FindVisual&lt;T&gt;</c> in the opposite direction:
    /// start from a visual (typically a screen or component root) and walk down to a
    /// Forms control.
    /// </summary>
    public static T? FindFormsControl<T>(this GraphicalUiElement element) where T : FrameworkElement
    {
        foreach (FrameworkElement descendant in FrameworkElementTreeExtensions.ProjectToFrameworkElements(element.Descendants()))
        {
            if (descendant is T match)
            {
                return match;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the first descendant <see cref="FrameworkElement"/> whose underlying
    /// <c>Visual.Name</c> equals <paramref name="name"/>, or null if none. Visual-only
    /// descendants with no Forms control are skipped — only nodes that project to a
    /// <see cref="FrameworkElement"/> are considered.
    /// </summary>
    public static FrameworkElement? FindFormsControlByName(this GraphicalUiElement element, string name)
    {
        foreach (FrameworkElement descendant in FrameworkElementTreeExtensions.ProjectToFrameworkElements(element.Descendants()))
        {
            if (descendant.Visual?.Name == name)
            {
                return descendant;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the first descendant <see cref="FrameworkElement"/> assignable to
    /// <typeparamref name="T"/> whose underlying <c>Visual.Name</c> equals
    /// <paramref name="name"/>, or null if none. Search is shallowest-first.
    /// </summary>
    public static T? FindFormsControl<T>(this GraphicalUiElement element, string name) where T : FrameworkElement
    {
        foreach (FrameworkElement descendant in FrameworkElementTreeExtensions.ProjectToFrameworkElements(element.Descendants()))
        {
            if (descendant is T match && match.Visual?.Name == name)
            {
                return match;
            }
        }
        return null;
    }

    #endregion

    /// <summary>
    /// Recursively returns the first matching element with the given name, or throws an exception if not found.
    /// </summary>
    /// <typeparam name="FrameworkElementType">The element type</typeparam>
    /// <param name="graphicalUiElement">The parent visual owning all children</param>
    /// <param name="name">The name to match</param>
    /// <returns>Teh found FrameworkElement</returns>
    /// <exception cref="ArgumentException">Throw if a visual is not found by the name, or if a child is found but its type doesn't match.</exception>
    [Obsolete("Use FindFormsControl<T>(name) instead. This overload's diagnostics are FULL_DIAGNOSTICS-gated and silently returns null on failure in release builds.")]
    public static FrameworkElementType GetFrameworkElementByName<FrameworkElementType>(this GraphicalUiElement graphicalUiElement, string name) where FrameworkElementType : FrameworkElement
    {
        var frameworkVisual = graphicalUiElement.GetGraphicalUiElementByName(name);

#if FULL_DIAGNOSTICS
        if (frameworkVisual == null)
        {
            throw new ArgumentException("Could not find a GraphicalUiElement with the name " + name);
        }
#endif

        var frameworkVisualAsInteractiveGue = frameworkVisual as InteractiveGue;

#if FULL_DIAGNOSTICS

        if (frameworkVisualAsInteractiveGue == null)
        {
            throw new ArgumentException("The GraphicalUiElement with the name " + name + " is not an InteractiveGue");
        }

#endif
        var formsControlAsObject = frameworkVisualAsInteractiveGue?.FormsControlAsObject;

#if FULL_DIAGNOSTICS

        if (formsControlAsObject == null)
        {
            throw new ArgumentException("The GraphicalUiElement with the name " + name + " does not have a FormsControlAsObject. In other words, this is just a visual, not a Forms control.");
        }
#endif
        var frameworkElement = formsControlAsObject as FrameworkElementType;
        if (frameworkElement == null)
        {
#if FULL_DIAGNOSTICS
            var message = "The GraphicalUiElement with the name " + name +
                " is expected to be of type " + typeof(FrameworkElementType) + " but is instead " + formsControlAsObject?.GetType();

            throw new ArgumentException(message);
#endif
        }
        return frameworkElement;
    }

    [Obsolete("Use FindFormsControl<T>(name) instead.")]
    public static FrameworkElementType TryGetFrameworkElementByName<FrameworkElementType>(this GraphicalUiElement graphicalUiElement, string name) where FrameworkElementType : FrameworkElement
    {
        var frameworkVisual = graphicalUiElement.GetGraphicalUiElementByName(name);

        if (frameworkVisual == null)
        {
            return default(FrameworkElementType);
        }

        var frameworkVisualAsInteractiveGue = frameworkVisual as InteractiveGue;

        if (frameworkVisualAsInteractiveGue == null)
        {
            return default(FrameworkElementType);
        }

        var formsControlAsObject = frameworkVisualAsInteractiveGue?.FormsControlAsObject;

        if (formsControlAsObject == null)
        {
            return default(FrameworkElementType);
        }

        var frameworkElement = formsControlAsObject as FrameworkElementType;
        if (frameworkElement == null)
        {
            return default(FrameworkElementType);

        }
        return frameworkElement;
    }

}
