#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#if XNALIKE
using MonoGameGum;
namespace MonoGameGum.Input;
#elif RAYLIB
using RaylibGum;
namespace Gum.Input;
#else
namespace Gum.Input;
#endif

public static class CursorExtensions 
{
    /// <summary>
    /// Returns information about why events may not be happening for the argument FrameworkElement.
    /// </summary>
    /// <param name="cursor">A reference to the cursor, such as GumService.Default.Cursor</param>
    /// <param name="frameworkElement">The FrameworkElement such as a Button.</param>
    /// <returns>A string explaining why events are not being raised, or null if events should be happening.</returns>
    public static string? GetEventFailureReason(this ICursor cursor, FrameworkElement frameworkElement)
    {
        if(frameworkElement == null)
        {
            return "The argument framework element is null, so it cannot raise events";
        }
        else if(frameworkElement.Visual == null)
        {
            return $"The {frameworkElement.GetType()} has a null Visual, so it will not be able to raise events";
        }
        else
        {
            return GetEventFailureReason(cursor, frameworkElement.Visual);
        }
    }

    /// <summary>
    /// Returns information about why events may not be happening for the argument InteractiveGue.
    /// </summary>
    /// <param name="cursor">A reference to the cursor, such as GumService.Default.Cursor</param>
    /// <param name="interactiveGue">The InteractiveGue, which is usually a Visual for a Forms control.</param>
    /// <returns>A string explaining why events are not being raised, or null if events should be happening.</returns>
    public static string? GetEventFailureReason(this ICursor cursor, InteractiveGue interactiveGue)
    {
        if(interactiveGue == null)
        {
            return "No InteractiveGue was passed, so events cannot be detected";
        }

        if(interactiveGue.Visible == false)
        {
            return $"The argument {NameOrType(interactiveGue)} is invisible so it will not raise events";
        }
        if (GetInvisibleParent(interactiveGue) is GraphicalUiElement invisibleParent)
        {
            return $"The parent {invisibleParent} is invisible, preventing {interactiveGue} from raising events";
        }
        if (interactiveGue.IsEnabled == false)
        {
            return "The argument InteractiveGue is disaled so it will not raise events";
        }
        if(GetDisabledParent(interactiveGue) is GraphicalUiElement disabledParent)
        {
            return $"The parent {disabledParent} is disabled, preventing {interactiveGue} from raising events";
        }
        if(interactiveGue.HasEvents == false)
        {
            return $"The argument InteractiveGue does not have its HasEvents set to true";
        }
        if(GetNotExposedChildrenEventsParent(interactiveGue) is InteractiveGue notExposingParent)
        {
            return $"The parent {NameOrType(notExposingParent)} does not raise its children's events, preventing {NameOrType(interactiveGue)} from raising events\n" +
                GetAncestorTree(interactiveGue, notExposingParent);
        }
        if (interactiveGue.GetAbsoluteWidth() == 0)
        {
            return "The argument InteractiveGue has an AbsoluteWidth of 0";
        }
        if(interactiveGue.GetAbsoluteHeight () == 0)
        {
            return "The argument InteractiveGue has an AbsoluteHeight of 0";
        }
        if(Get0WidthOrHeightParent(interactiveGue) is GraphicalUiElement parentWith0WidthOrHeight)
        {
            if(parentWith0WidthOrHeight.GetAbsoluteHeight() == 0)
            {
                return $"The parent {parentWith0WidthOrHeight} has an AbsoluteHeight of 0";
            }
            else if(parentWith0WidthOrHeight.GetAbsoluteWidth() == 0)
            {
                return $"The parent {parentWith0WidthOrHeight} has an AbsoluteWidth of 0";
            }
        }

        if(interactiveGue.EffectiveManagers == null)
        {
            if(!IsInLastEventRoots(interactiveGue))
            {
                return $"The {NameOrType(interactiveGue)} does not have EffectiveManagers and was not included " +
                    $"in the roots passed to GumService.Update(). Either add it to managers or include it (or a parent) " +
                    $"in the roots passed to Update.";
            }
            // If it is in the last event roots, it's a GumBatch scenario — skip the managers
            // check and continue with the remaining diagnostics.
        }


        List<GraphicalUiElement> inheritanceChain = new List<GraphicalUiElement>();

        GraphicalUiElement? current = interactiveGue;
        while(current != null)
        {
            inheritanceChain.Insert(0, current);

            current = current.Parent as GraphicalUiElement;
        }

        for(int i = 0; i < inheritanceChain.Count; i++)
        {
            var item = inheritanceChain[i];

            if(item is InteractiveGue itemAsInteractiveGue)
            {
                if(!itemAsInteractiveGue.HasCursorOver(cursor))
                {
                    return $"Item {item} does not have the cursor over it. " + GetNotOverBoundsInformation(itemAsInteractiveGue) + "\n" + GetStack(itemAsInteractiveGue);
                }
            }
            else
            {
                if(!item.HasCursorOver(cursor.XRespectingGumZoomAndBounds(), cursor.YRespectingGumZoomAndBounds()))
                {
                    return $"Item {item} does not have the cursor over it. " + GetNotOverBoundsInformation(item) + "\n" + GetStack(item);
                }
            }


            string GetNotOverBoundsInformation(GraphicalUiElement gue)
            {
                var absoluteX = gue.GetAbsoluteX();
                var absoluteY = gue.GetAbsoluteY();
                var absoluteWidth = gue.GetAbsoluteWidth();
                var absoluteHeight = gue.GetAbsoluteHeight();
                if(cursor.XRespectingGumZoomAndBounds() < absoluteX)
                {
                    return $"The cursor X={cursor.XRespectingGumZoomAndBounds()} is to the left of the element which starts at X={absoluteX}";
                }
                else if(cursor.XRespectingGumZoomAndBounds() > absoluteX + absoluteWidth)
                {
                    return $"The cursor X={cursor.XRespectingGumZoomAndBounds()} is to the right of the element which ends at X={absoluteX + absoluteWidth}";
                }
                else if(cursor.YRespectingGumZoomAndBounds() < absoluteY)
                {
                    return $"The cursor Y={cursor.YRespectingGumZoomAndBounds()} is above the element which starts at Y={absoluteY}";
                }
                else // cursor.Y > absoluteY + absoluteHeight
                {
                    return $"The cursor Y={cursor.YRespectingGumZoomAndBounds()} is below the element which ends at Y={absoluteY + absoluteHeight}";
                }
            }

        }

        if(cursor.VisualOver != null && cursor.VisualOver != interactiveGue)
        {
            if(IsDescendantOf(cursor.VisualOver, interactiveGue))
            {
                return $"The cursor is not directly over {NameOrType(interactiveGue)}, " +
                    $"but is over its child {NameOrType(cursor.VisualOver)}. " +
                    $"A child is receiving events instead of the parent.\n" +
                    GetStack(interactiveGue);
            }
            return $"The cursor is over {NameOrType(cursor.VisualOver)} instead of {NameOrType(interactiveGue)}";
        }

        var rootParent = interactiveGue.GetTopParent();
        var isInEventRoots = IsInLastEventRoots(interactiveGue);

        if(!isInEventRoots)
        {
            var gumUI = GumService.Default;
            if(rootParent != gumUI.Root && rootParent != gumUI.PopupRoot && rootParent != gumUI.ModalRoot)
            {
                return $"The object must ultimately be added to one of the root objects, but it is not. The top parent {rootParent} is an orphan object";
            }

            if(rootParent != gumUI.ModalRoot && gumUI.ModalRoot.Children.Count(item => item.Visible) > 0)
            {
                var firstVisible = gumUI.ModalRoot.Children.First(item => item.Visible);

                return $"There is a modal that is blocking clicks to {interactiveGue}: {firstVisible}";
            }
        }

        return null;

        string GetStack(GraphicalUiElement itemInStack) => GetAncestorTree(interactiveGue, itemInStack);
    }

    /// <summary>
    /// Builds an indented ancestor tree string from the root down to <paramref name="leaf"/>.
    /// If <paramref name="highlighted"/> is provided and appears in the chain, it is marked with " &lt;---- THIS".
    /// </summary>
    public static string GetAncestorTree(GraphicalUiElement leaf, GraphicalUiElement? highlighted = null)
    {
        var inheritanceChain = new List<GraphicalUiElement>();

        GraphicalUiElement? current = leaf;
        while (current != null)
        {
            inheritanceChain.Insert(0, current);
            current = current.Parent as GraphicalUiElement;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < inheritanceChain.Count; i++)
        {
            var item = inheritanceChain[i];
            sb.Append(new string(' ', i * 2));
            sb.Append("└-");
            sb.Append(NameOrType(item));
            if (item == highlighted)
            {
                sb.Append(" <---- THIS");
            }
            sb.Append('\n');
        }

        return sb.ToString();
    }

    static string NameOrType(GraphicalUiElement gue)
    {
        // If this visual backs a Forms control, the user thinks of it by the Forms type
        // (e.g. "Button", "Panel") — not the Visual class ("ButtonVisual", "InteractiveGue").
        var typeName = (gue is InteractiveGue interactiveGue && interactiveGue.FormsControlAsObject != null)
            ? interactiveGue.FormsControlAsObject.GetType().Name
            : gue.GetType().Name;

        return string.IsNullOrEmpty(gue.Name)
            ? typeName
            : $"{typeName} named {gue.Name}";
    }


    private static GraphicalUiElement Get0WidthOrHeightParent(GraphicalUiElement visual)
    {
        if(visual.Parent as GraphicalUiElement == null)
        {
            return null;
        }
        else if(visual.Parent is GraphicalUiElement parentGue && 
            (parentGue.GetAbsoluteWidth() == 0 || parentGue.GetAbsoluteHeight() == 0))
        {
            return visual.Parent as GraphicalUiElement;
        }
        else
        {
            return Get0WidthOrHeightParent(visual.Parent as GraphicalUiElement);
        }
    }

    private static GraphicalUiElement? GetInvisibleParent(GraphicalUiElement visual)
    {
        if(visual.Parent as GraphicalUiElement == null)
        {
            return null;
        }
        else if(visual.Visible == false)
        {
            return visual;
        }
        else
        {
            return GetInvisibleParent(visual.Parent as GraphicalUiElement);
        }
    }

    private static GraphicalUiElement? GetDisabledParent(GraphicalUiElement visual)
    {
        if(visual.Parent == null)
        {
            return null;
        }
        else if(visual is InteractiveGue { IsEnabled: false }  interactiveGue)
        {
            return interactiveGue;
        }
        else
        {
            return GetDisabledParent(visual.Parent as GraphicalUiElement);
        }
    }

    private static InteractiveGue? GetNotExposedChildrenEventsParent(GraphicalUiElement visual)
    {
        var current = visual.Parent as GraphicalUiElement;
        while (current != null)
        {
            if (current is InteractiveGue { ExposeChildrenEvents: false } parent)
            {
                return parent;
            }
            current = current.Parent as GraphicalUiElement;
        }
        return null;
    }

    private static bool IsDescendantOf(GraphicalUiElement possibleDescendant, GraphicalUiElement possibleAncestor)
    {
        GraphicalUiElement? current = possibleDescendant.Parent as GraphicalUiElement;
        while(current != null)
        {
            if(current == possibleAncestor)
            {
                return true;
            }
            current = current.Parent as GraphicalUiElement;
        }
        return false;
    }

    /// <summary>
    /// Searches Root, PopupRoot, and ModalRoot recursively for a <see cref="FrameworkElement"/>
    /// of type <typeparamref name="T"/> and returns the event-failure reason for it.
    /// If multiple elements match, diagnostics for each are returned in a single string.
    /// </summary>
    /// <returns>
    /// <c>null</c> if exactly one match was found and its events should be working;
    /// otherwise a string describing the match(es) and any event failures.
    /// </returns>
    public static string? GetEventFailureReason<T>(this ICursor cursor) where T : FrameworkElement
        => GetEventFailureReasonForMatches(cursor, typeof(T), name: null);

    /// <summary>
    /// Searches Root, PopupRoot, and ModalRoot recursively for any <see cref="FrameworkElement"/>
    /// whose name matches <paramref name="name"/> and returns the event-failure reason for it.
    /// If multiple elements match, diagnostics for each are returned in a single string.
    /// </summary>
    public static string? GetEventFailureReason(this ICursor cursor, string name)
        => GetEventFailureReasonForMatches(cursor, type: null, name: name);

    /// <summary>
    /// Searches Root, PopupRoot, and ModalRoot recursively for a <see cref="FrameworkElement"/>
    /// of type <typeparamref name="T"/> whose name matches <paramref name="name"/> and returns
    /// the event-failure reason for it. If multiple elements match, diagnostics for each are
    /// returned in a single string.
    /// </summary>
    public static string? GetEventFailureReason<T>(this ICursor cursor, string name) where T : FrameworkElement
        => GetEventFailureReasonForMatches(cursor, typeof(T), name);

    private static string? GetEventFailureReasonForMatches(ICursor cursor, Type? type, string? name)
    {
        var matches = FindFrameworkElements(type, name);

        var filterDescription = DescribeFilter(type, name);

        if (matches.Count == 0)
        {
            return $"No FrameworkElement matching {filterDescription} was found under Root, PopupRoot, or ModalRoot.";
        }

        if (matches.Count == 1)
        {
            return cursor.GetEventFailureReason(matches[0]);
        }

        var sb = new StringBuilder();
        sb.Append("Found ").Append(matches.Count).Append(" elements matching ")
            .Append(filterDescription).AppendLine(":");

        for (int i = 0; i < matches.Count; i++)
        {
            var element = matches[i];
            var label = element.Visual != null ? NameOrType(element.Visual) : element.GetType().Name;
            sb.Append("  [").Append(i + 1).Append("] ").AppendLine(label);

            var reason = cursor.GetEventFailureReason(element);
            var indented = reason == null
                ? "(null — events appear to be working correctly)"
                : IndentLines(reason, "      ");
            sb.Append("      → ").AppendLine(indented.TrimStart());
        }

        return sb.ToString();
    }

    private static string DescribeFilter(Type? type, string? name)
    {
        if (type != null && name != null) return $"type {type.Name} named \"{name}\"";
        if (type != null) return $"type {type.Name}";
        if (name != null) return $"name \"{name}\"";
        return "(no filter)";
    }

    private static string IndentLines(string text, string indent)
    {
        var lines = text.Split('\n');
        var sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) sb.Append(indent);
            sb.Append(lines[i]);
            if (i < lines.Length - 1) sb.Append('\n');
        }
        return sb.ToString();
    }

    private static List<FrameworkElement> FindFrameworkElements(Type? type, string? name)
    {
        var results = new List<FrameworkElement>();
        var gumUI = GumService.Default;

        Collect(gumUI.Root, results, type, name);
        if (gumUI.PopupRoot != null) Collect(gumUI.PopupRoot, results, type, name);
        if (gumUI.ModalRoot != null) Collect(gumUI.ModalRoot, results, type, name);

        return results;

        static void Collect(GraphicalUiElement gue, List<FrameworkElement> results, Type? type, string? name)
        {
            if (gue is InteractiveGue interactiveGue &&
                interactiveGue.FormsControlAsObject is FrameworkElement formsElement)
            {
                bool typeMatches = type == null || type.IsInstanceOfType(formsElement);
                bool nameMatches = name == null || string.Equals(formsElement.Name, name, StringComparison.Ordinal);

                if (typeMatches && nameMatches)
                {
                    results.Add(formsElement);
                }
            }

            if (gue.Children != null)
            {
                foreach (var child in gue.Children)
                {
                    if (child is GraphicalUiElement childGue)
                    {
                        Collect(childGue, results, type, name);
                    }
                }
            }
        }
    }

    private static bool IsInLastEventRoots(GraphicalUiElement element)
    {
        var lastRoots = FormsUtilities.LastEventRoots;
        if(lastRoots.Count == 0)
        {
            return false;
        }

        var topParent = element.GetTopParent();
        foreach(var root in lastRoots)
        {
            if(root == element || root == topParent)
            {
                return true;
            }
        }
        return false;
    }
}
