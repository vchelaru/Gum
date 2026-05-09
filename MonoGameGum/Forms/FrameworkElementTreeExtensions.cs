using Gum.Wireframe;
using System.Collections.Generic;

#if FRB
using FlatRedBall.Forms.Controls;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace MonoGameGum.Forms;
#endif

#if !FRB
using Gum.Forms.Controls;
namespace Gum.Forms;
#endif

/// <summary>
/// Extension methods for traversing the Forms control tree (the sparse projection of
/// the visual tree that contains <see cref="FrameworkElement"/> back-references) and
/// for reaching into a control's underlying visuals via the <c>FindVisual*</c> sugar.
/// </summary>
// The Find* methods below are intentionally hand-rolled rather than expressed as
// Descendants().OfType<T>().FirstOrDefault(predicate). Algorithmically the two are
// equivalent (BFS, lazy, short-circuit on first match), but the LINQ form allocates
// a closure and a delegate per call when the predicate captures `name`. These
// methods are general-purpose and may end up on hot paths (e.g. inside per-frame
// update or input handling), so the hand-rolled form avoids that overhead. Do not
// "simplify" them back to LINQ without a perf review of the call sites.
public static class FrameworkElementTreeExtensions
{
    #region FE-returning traversal (Forms-first)

    /// <summary>
    /// Enumerates every descendant <see cref="FrameworkElement"/> under <paramref name="element"/>,
    /// shallowest-first. Skips visual-only nodes that have no Forms control attached.
    /// </summary>
    public static IEnumerable<FrameworkElement> Descendants(this FrameworkElement element)
        => ProjectToFrameworkElements(element.Visual.Descendants());

    /// <summary>
    /// Enumerates <paramref name="element"/> followed by every descendant
    /// <see cref="FrameworkElement"/> (shallowest-first).
    /// </summary>
    public static IEnumerable<FrameworkElement> DescendantsAndSelf(this FrameworkElement element)
    {
        yield return element;
        foreach (FrameworkElement descendant in element.Descendants())
        {
            yield return descendant;
        }
    }

    /// <summary>
    /// Enumerates every ancestor <see cref="FrameworkElement"/> by walking the visual
    /// parent chain and projecting each visual to its <c>FormsControlAsObject</c>
    /// where one exists. Nearest-first.
    /// </summary>
    public static IEnumerable<FrameworkElement> Ancestors(this FrameworkElement element)
        => ProjectToFrameworkElements(element.Visual.Ancestors());

    /// <summary>
    /// Enumerates <paramref name="element"/> followed by every ancestor
    /// <see cref="FrameworkElement"/> (nearest-first).
    /// </summary>
    public static IEnumerable<FrameworkElement> AncestorsAndSelf(this FrameworkElement element)
    {
        yield return element;
        foreach (FrameworkElement ancestor in element.Ancestors())
        {
            yield return ancestor;
        }
    }

    /// <summary>
    /// Returns the first descendant <see cref="FrameworkElement"/> assignable to
    /// <typeparamref name="T"/>, or null if none. Search is shallowest-first; subclasses match.
    /// </summary>
    public static T? Find<T>(this FrameworkElement element) where T : FrameworkElement
    {
        foreach (FrameworkElement descendant in element.Descendants())
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
    /// <c>Visual.Name</c> equals <paramref name="name"/>, or null if none.
    /// </summary>
    public static FrameworkElement? FindByName(this FrameworkElement element, string name)
    {
        foreach (FrameworkElement descendant in element.Descendants())
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
    /// <paramref name="name"/>, or null if none.
    /// </summary>
    public static T? Find<T>(this FrameworkElement element, string name) where T : FrameworkElement
    {
        foreach (FrameworkElement descendant in element.Descendants())
        {
            if (descendant is T match && match.Visual?.Name == name)
            {
                return match;
            }
        }
        return null;
    }

    private static IEnumerable<FrameworkElement> ProjectToFrameworkElements(IEnumerable<GraphicalUiElement> visuals)
    {
        foreach (GraphicalUiElement visual in visuals)
        {
            if (visual is InteractiveGue interactive && interactive.FormsControlAsObject is FrameworkElement fe)
            {
                yield return fe;
            }
        }
    }

    #endregion

    #region Visual-returning sugar (drill into the visual tree)

    /// <summary>
    /// Returns the first visual descendant of <paramref name="element"/>.Visual that
    /// is assignable to <typeparamref name="T"/>, or null if none. Sugar over
    /// <c>element.Visual.Find&lt;T&gt;()</c>.
    /// </summary>
    public static T? FindVisual<T>(this FrameworkElement element) where T : GraphicalUiElement
        => element.Visual.Find<T>();

    /// <summary>
    /// Returns the first visual descendant whose <c>Name</c> equals
    /// <paramref name="name"/>, or null if none. Sugar over
    /// <c>element.Visual.FindByName(name)</c>.
    /// </summary>
    public static GraphicalUiElement? FindVisualByName(this FrameworkElement element, string name)
        => element.Visual.FindByName(name);

    /// <summary>
    /// Returns the first visual descendant assignable to <typeparamref name="T"/>
    /// whose <c>Name</c> equals <paramref name="name"/>, or null if none.
    /// Sugar over <c>element.Visual.Find&lt;T&gt;(name)</c>.
    /// </summary>
    public static T? FindVisual<T>(this FrameworkElement element, string name) where T : GraphicalUiElement
        => element.Visual.Find<T>(name);

    #endregion
}
