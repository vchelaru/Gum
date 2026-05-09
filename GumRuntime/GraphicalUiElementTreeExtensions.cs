using System.Collections.Generic;

namespace Gum.Wireframe;

/// <summary>
/// Extension methods for traversing a <see cref="GraphicalUiElement"/> tree.
/// Compose with LINQ (<c>.OfType&lt;T&gt;()</c>, <c>.Where(...)</c>, <c>.FirstOrDefault(...)</c>)
/// for any case the <see cref="Find{T}(GraphicalUiElement)"/> / <see cref="FindByName(GraphicalUiElement, string)"/>
/// sugar doesn't cover.
/// </summary>
public static class GraphicalUiElementTreeExtensions
{
    /// <summary>
    /// Enumerates every descendant of <paramref name="element"/>, shallowest-first
    /// (breadth-first). Common lookups (<see cref="Find{T}(GraphicalUiElement)"/>,
    /// <see cref="FindByName(GraphicalUiElement, string)"/>) short-circuit on this
    /// ordering before descending into deep subtrees, so the closest match wins.
    /// </summary>
    public static IEnumerable<GraphicalUiElement> Descendants(this GraphicalUiElement element)
    {
        Queue<GraphicalUiElement> queue = new Queue<GraphicalUiElement>();
        foreach (GraphicalUiElement child in element.Children)
        {
            queue.Enqueue(child);
        }
        while (queue.Count > 0)
        {
            GraphicalUiElement current = queue.Dequeue();
            yield return current;
            foreach (GraphicalUiElement child in current.Children)
            {
                queue.Enqueue(child);
            }
        }
    }

    /// <summary>
    /// Enumerates <paramref name="element"/> followed by every descendant
    /// (shallowest-first). Equivalent to <c>[element].Concat(element.Descendants())</c>.
    /// </summary>
    public static IEnumerable<GraphicalUiElement> DescendantsAndSelf(this GraphicalUiElement element)
    {
        yield return element;
        foreach (GraphicalUiElement descendant in element.Descendants())
        {
            yield return descendant;
        }
    }

    /// <summary>
    /// Enumerates every ancestor of <paramref name="element"/>, nearest-first,
    /// by walking <see cref="GraphicalUiElement.Parent"/>.
    /// </summary>
    public static IEnumerable<GraphicalUiElement> Ancestors(this GraphicalUiElement element)
    {
        GraphicalUiElement? current = element.Parent;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    /// <summary>
    /// Enumerates <paramref name="element"/> followed by every ancestor (nearest-first).
    /// </summary>
    public static IEnumerable<GraphicalUiElement> AncestorsAndSelf(this GraphicalUiElement element)
    {
        yield return element;
        foreach (GraphicalUiElement ancestor in element.Ancestors())
        {
            yield return ancestor;
        }
    }

    /// <summary>
    /// Returns the first descendant assignable to <typeparamref name="T"/>, or null if none.
    /// Search is shallowest-first; subclasses match (<c>is T</c> semantics).
    /// </summary>
    public static T? Find<T>(this GraphicalUiElement element) where T : GraphicalUiElement
    {
        foreach (GraphicalUiElement descendant in element.Descendants())
        {
            if (descendant is T match)
            {
                return match;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the first descendant whose <see cref="GraphicalUiElement.Name"/>
    /// equals <paramref name="name"/>, or null if none. Search is shallowest-first.
    /// </summary>
    public static GraphicalUiElement? FindByName(this GraphicalUiElement element, string name)
    {
        foreach (GraphicalUiElement descendant in element.Descendants())
        {
            if (descendant.Name == name)
            {
                return descendant;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the first descendant assignable to <typeparamref name="T"/>
    /// whose <see cref="GraphicalUiElement.Name"/> equals <paramref name="name"/>,
    /// or null if none. Search is shallowest-first.
    /// </summary>
    public static T? Find<T>(this GraphicalUiElement element, string name) where T : GraphicalUiElement
    {
        foreach (GraphicalUiElement descendant in element.Descendants())
        {
            if (descendant is T match && match.Name == name)
            {
                return match;
            }
        }
        return null;
    }
}
