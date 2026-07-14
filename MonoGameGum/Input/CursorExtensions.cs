#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif

#if FRB
// FRB1 links this file and compiles it with the FRB constant defined. Its cursor is the
// concrete FlatRedBall.Gui.Cursor (which does not implement Gum's ICursor), and its Forms
// controls live in FlatRedBall.Forms.Controls.
using FlatRedBall.Forms.Controls;
using CursorType = FlatRedBall.Gui.Cursor;
// FRB does not compile Gum's InteractiveGue; its event members (HasEvents, ExposeChildrenEvents,
// FormsControlAsObject, IsEnabled) live on GraphicalUiElement instead. This alias — the same one
// the shared Forms control files use — lets the diagnostic body compile unchanged. In the
// MonoGameGum build InteractiveGue stays the real subclass (resolved via using Gum.Wireframe).
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
#else
using Gum.Forms;
using Gum.Forms.Controls;
using CursorType = Gum.Wireframe.ICursor;
#endif
using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#if SOKOL
// Sokol keeps its own GumService (no Gum-namespace move there).
using GumServiceType = SokolGum.GumService;
#elif !FRB
// GumService lives in the Gum namespace (issue #3119). Any backend other than the
// un-migrated Sokol uses it — including future backends by default. The alias also
// dodges the [Obsolete] legacy shim that unqualified lookup would otherwise find
// through this file's enclosing MonoGameGum namespace in the XNALIKE build.
using GumServiceType = Gum.GumService;
// FRB has no GumService; it roots its UI in the GuiManager (see the #if FRB branches below).
#endif

#if FRB
namespace FlatRedBall.Gui;
#elif XNALIKE
namespace MonoGameGum.Input;
#elif RAYLIB
namespace Gum.Input;
#else
namespace Gum.Input;
#endif

// The diagnostic logic below is shared verbatim between MonoGameGum and FRB1 (which links this
// file). The cursor type differs by backend — ICursor vs FlatRedBall.Gui.Cursor — via the
// CursorType alias; only root enumeration and event-root tracking are #if FRB-gated. The FRB
// class name differs to avoid colliding with FlatRedBall.Gui.CursorExtensions.
#if FRB
public static class CursorEventFailureExtensions
#else
public static class CursorExtensions
#endif
{
    /// <summary>
    /// Returns information about why events may not be happening for the argument FrameworkElement.
    /// </summary>
    /// <param name="cursor">A reference to the cursor, such as GumService.Default.Cursor</param>
    /// <param name="frameworkElement">The FrameworkElement such as a Button.</param>
    /// <returns>A string explaining why events are not being raised, or null if events should be happening.</returns>
    public static string? GetEventFailureReason(this CursorType cursor, FrameworkElement frameworkElement)
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
    public static string? GetEventFailureReason(this CursorType cursor, InteractiveGue interactiveGue)
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
#if FRB
                return $"The {NameOrType(interactiveGue)} does not have EffectiveManagers, meaning it has not been " +
                    $"added to the GuiManager. Call Show (or add it to a shown parent) so it is registered for Cursor interaction.";
#else
                return $"The {NameOrType(interactiveGue)} does not have EffectiveManagers and was not included " +
                    $"in the roots passed to GumService.Update(). Either add it to managers or include it (or a parent) " +
                    $"in the roots passed to Update.";
#endif
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
#if FRB
                // FlatRedBall.Gui.Cursor is not an ICursor, so the ICursor-typed HasCursorOver
                // overload is unavailable; use the geometric (x, y) overload instead.
                if(!itemAsInteractiveGue.HasCursorOver(cursor.XRespectingGumZoomAndBounds(), cursor.YRespectingGumZoomAndBounds()))
#else
                if(!itemAsInteractiveGue.HasCursorOver(cursor))
#endif
                {
                    return $"Item {item} does not have the cursor over it. " + GetNotOverBoundsInformation(itemAsInteractiveGue) + "\n" + GetCursorContext() + GetStack(itemAsInteractiveGue);
                }
            }
            else
            {
                if(!item.HasCursorOver(cursor.XRespectingGumZoomAndBounds(), cursor.YRespectingGumZoomAndBounds()))
                {
                    return $"Item {item} does not have the cursor over it. " + GetNotOverBoundsInformation(item) + "\n" + GetCursorContext() + GetStack(item);
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

        // Normalize the cursor's "over" visual to a GraphicalUiElement. On MonoGameGum this is an
        // InteractiveGue (upcast); under FRB the cursor exposes it as an IWindow, which is a
        // GraphicalUiElement at runtime (GraphicalUiElement implements IWindow in the FRB build).
        var visualOver = cursor.VisualOver as GraphicalUiElement;
        if(visualOver != null && visualOver != interactiveGue)
        {
            if(IsDescendantOf(visualOver, interactiveGue))
            {
                return $"The cursor is not directly over {NameOrType(interactiveGue)}, " +
                    $"but is over its child {NameOrType(visualOver)}. " +
                    $"A child is receiving events instead of the parent.\n" +
                    GetStack(interactiveGue);
            }
            return $"The cursor is over {NameOrType(visualOver)} instead of {NameOrType(interactiveGue)}. " +
                DescribeRelationship(visualOver, interactiveGue);
        }

        var rootParent = interactiveGue.GetTopParent();
        var isInEventRoots = IsInLastEventRoots(interactiveGue);

        if(!isInEventRoots)
        {
#if FRB
            // FRB roots its UI in the GuiManager rather than in GumService's Root/PopupRoot/ModalRoot.
            var rootAsWindow = rootParent as IWindow;
            if(rootAsWindow == null ||
                (!GuiManager.Windows.Contains(rootAsWindow) && !GuiManager.DominantWindows.Contains(rootAsWindow)))
            {
                return $"The object must ultimately be added to the GuiManager, but it is not. The top parent {rootParent} is an orphan object";
            }

            // A DominantWindow behaves like a modal: while one exists, the cursor cannot interact
            // with anything that is not the dominant window (or under it).
            var dominant = GuiManager.DominantWindows.FirstOrDefault();
            if(dominant != null && dominant != rootAsWindow)
            {
                return $"There is a dominant (modal) window that is blocking clicks to {interactiveGue}: {dominant}";
            }
#else
            var gumUI = GumServiceType.Default;
            if(rootParent != gumUI.Root && rootParent != gumUI.PopupRoot && rootParent != gumUI.ModalRoot)
            {
                return $"The object must ultimately be added to one of the root objects, but it is not. The top parent {rootParent} is an orphan object";
            }

            if(rootParent != gumUI.ModalRoot && gumUI.ModalRoot.Children.Count(item => item.Visible) > 0)
            {
                var firstVisible = gumUI.ModalRoot.Children.First(item => item.Visible);

                return $"There is a modal that is blocking clicks to {interactiveGue}: {firstVisible}";
            }
#endif
        }

        return null;

        string GetStack(GraphicalUiElement itemInStack) => GetAncestorTree(interactiveGue, itemInStack);

        // Reports the coordinate space the hit-test ran in. When a control looks like it is under the
        // cursor but still fails the over-test, the culprit is almost always a mismatch between where
        // the UI is drawn and the coordinates Gum hit-tests in — a non-1 camera zoom, a canvas that
        // does not match the window, or the UI being drawn through a transformed/perspective camera or
        // render target. Surfacing zoom + canvas here makes those cases self-evident.
        string GetCursorContext()
        {
            var helperX = cursor.XRespectingGumZoomAndBounds();
            var helperY = cursor.YRespectingGumZoomAndBounds();

            var context = $"Cursor Gum coordinates: ({helperX:0.#},{helperY:0.#}).";

            var camera = global::RenderingLibrary.SystemManagers.Default?.Renderer?.Camera;
            if (camera != null)
            {
                context += $" Gum camera zoom: {camera.Zoom:0.###}.";
            }

            context += $" Canvas: {GraphicalUiElement.CanvasWidth:0.#}x{GraphicalUiElement.CanvasHeight:0.#}.\n";

            // The failure above compared the zoom-adjusted cursor against the element's bounds. If the RAW
            // (screen-scale) cursor would have been over the element but the zoom-adjusted one is not, the UI
            // is drawn at screen scale while hit-testing runs through the camera zoom — the precise signature
            // of the zoom/camera decoupling — so we say so instead of leaving the reader to reason it out.
#if FRB
            float rawX = cursor.ScreenX;
            float rawY = cursor.ScreenY;
#else
            float rawX = cursor.X;
            float rawY = cursor.Y;
#endif
            var zoom = camera?.Zoom ?? 1f;
            bool rawScreenIsOver = interactiveGue.IsPointInside(rawX, rawY);
            bool adjustedIsOver = interactiveGue.IsPointInside(helperX, helperY);
            var decoupling = DescribeCoordinateDecoupling(rawScreenIsOver, adjustedIsOver, rawX, rawY, helperX, helperY, zoom);

            context += decoupling ?? "If the control visually appears under the cursor but these coordinates place it " +
                "elsewhere, the UI is likely drawn through a transformed camera (zoom / perspective / render target) that " +
                "Gum's hit-testing does not see. Put the UI on a screen-space (2D) layer, or set Cursor.TransformMatrix.";
            context += "\n";
            return context;
        }
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
            sb.Append(' ');
            sb.Append(GetAbsoluteBoundsInformation(item));
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
            ? $"[{typeName}]"
            : $"[{typeName} named {gue.Name}]";
    }

    /// <summary>
    /// Formats an element's absolute (world-space) bounds for the ancestor tree, and appends the
    /// layer of its top parent when one is present. These bounds are world-space; if the element
    /// is on a layer with an offset camera, its rendered (and clicked) position can differ from
    /// these values — which is the usual reason a control that looks correctly placed still fails
    /// the cursor-over test.
    /// </summary>
    static string GetAbsoluteBoundsInformation(GraphicalUiElement gue)
    {
        var x = gue.GetAbsoluteX();
        var y = gue.GetAbsoluteY();
        var width = gue.GetAbsoluteWidth();
        var height = gue.GetAbsoluteHeight();

        var info = $"abs=({x:0.#},{y:0.#} {width:0.#}x{height:0.#})";

        var layer = (gue.GetTopParent() as GraphicalUiElement)?.Layer;
        if (layer != null)
        {
            info += $" layer={layer}";
        }

        return info;
    }

    /// <summary>
    /// Detects when the UI is drawn at raw screen scale but hit-tested through a camera zoom/transform.
    /// If the <em>raw</em> screen cursor lands inside the element but the <em>zoom-adjusted</em> cursor
    /// (what hit-testing actually uses) does not, the two paths disagree — the UI is drawn in one space
    /// and hit-tested in another — and the returned string names it. Returns <c>null</c> otherwise.
    /// </summary>
    public static string? DescribeCoordinateDecoupling(
        bool rawScreenIsOver, bool adjustedIsOver,
        float rawX, float rawY, float adjustedX, float adjustedY, float cameraZoom)
    {
        if (rawScreenIsOver && !adjustedIsOver)
        {
            return "WARNING: hit-testing is decoupled from where the UI is drawn. The raw cursor " +
                $"({rawX:0.#},{rawY:0.#}) is over the element, but the zoom-adjusted cursor ({adjustedX:0.#},{adjustedY:0.#}) " +
                $"is not — the UI is drawn at screen scale while hit-testing runs through a camera zoom of {cameraZoom:0.###}. " +
                "Put the UI on a screen-space (2D) layer, or set Cursor.TransformMatrix.";
        }
        return null;
    }

    private static string DescribeRelationship(GraphicalUiElement? a, GraphicalUiElement b)
    {
        if (a == null) return string.Empty;

        if (IsDescendantOf(b, a))
        {
            return $"{NameOrType(a)} is an ancestor of {NameOrType(b)}.\n" + GetAncestorTree(b, a);
        }

        var lca = GetLowestCommonAncestor(a, b);
        if (lca != null)
        {
            return $"They share the common ancestor {NameOrType(lca)}.\n" +
                $"Path to {NameOrType(a)}:\n{GetAncestorTree(a, lca)}" +
                $"Path to {NameOrType(b)}:\n{GetAncestorTree(b, lca)}";
        }

        return $"They do not share a common ancestor — they are likely under separate roots passed to GumService.Update.\n" +
            $"Path to {NameOrType(a)}:\n{GetAncestorTree(a)}" +
            $"Path to {NameOrType(b)}:\n{GetAncestorTree(b)}";
    }

    private static GraphicalUiElement? GetLowestCommonAncestor(GraphicalUiElement a, GraphicalUiElement b)
    {
        var ancestors = new HashSet<GraphicalUiElement>();
        GraphicalUiElement? current = a;
        while (current != null)
        {
            ancestors.Add(current);
            current = current.Parent as GraphicalUiElement;
        }

        current = b;
        while (current != null)
        {
            if (ancestors.Contains(current)) return current;
            current = current.Parent as GraphicalUiElement;
        }
        return null;
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
        // Walk every ancestor, including the topmost (whose Parent is null). The topmost element is
        // often a window registered directly with the manager, and managers skip invisible windows —
        // so its own Visible must be tested. Checking Parent-is-null before Visible would return at
        // the root without ever testing it, letting an invisible top-level element slip through.
        var current = visual.Parent as GraphicalUiElement;
        while (current != null)
        {
            if (current.Visible == false)
            {
                return current;
            }
            current = current.Parent as GraphicalUiElement;
        }
        return null;
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
    public static string? GetEventFailureReason<T>(this CursorType cursor) where T : FrameworkElement
        => GetEventFailureReasonForMatches(cursor, typeof(T), name: null);

    /// <summary>
    /// Searches Root, PopupRoot, and ModalRoot recursively for any <see cref="FrameworkElement"/>
    /// whose name matches <paramref name="name"/> and returns the event-failure reason for it.
    /// If multiple elements match, diagnostics for each are returned in a single string.
    /// </summary>
    public static string? GetEventFailureReason(this CursorType cursor, string name)
        => GetEventFailureReasonForMatches(cursor, type: null, name: name);

    /// <summary>
    /// Searches Root, PopupRoot, and ModalRoot recursively for a <see cref="FrameworkElement"/>
    /// of type <typeparamref name="T"/> whose name matches <paramref name="name"/> and returns
    /// the event-failure reason for it. If multiple elements match, diagnostics for each are
    /// returned in a single string.
    /// </summary>
    public static string? GetEventFailureReason<T>(this CursorType cursor, string name) where T : FrameworkElement
        => GetEventFailureReasonForMatches(cursor, typeof(T), name);

    private static string? GetEventFailureReasonForMatches(CursorType cursor, Type? type, string? name)
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

#if FRB
        // FRB registers shown UI with the GuiManager rather than GumService roots.
        foreach (var window in GuiManager.Windows)
        {
            if (window is GraphicalUiElement gue)
            {
                Collect(gue, results, type, name);
            }
        }
        foreach (var window in GuiManager.DominantWindows)
        {
            if (window is GraphicalUiElement gue)
            {
                Collect(gue, results, type, name);
            }
        }
#else
        var gumUI = GumServiceType.Default;

        Collect(gumUI.Root, results, type, name);
        if (gumUI.PopupRoot != null) Collect(gumUI.PopupRoot, results, type, name);
        if (gumUI.ModalRoot != null) Collect(gumUI.ModalRoot, results, type, name);
#endif

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
#if FRB
        // FRB does not track per-frame event roots the way MonoGameGum's FormsUtilities does;
        // shown elements are registered with the GuiManager, which the orphan check above uses.
        return false;
#else
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
#endif
    }
}
