using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using ICursor = global::FlatRedBall.Gui.Cursor;
using FlatRedBall.Gui;
namespace FlatRedBall.Forms.Controls;
#else
namespace Gum.Forms.Controls;
#endif

/// <summary>
/// Static service that drives hover-based tooltip display. Mirrors WPF's
/// <c>System.Windows.Controls.ToolTipService</c> in naming and default delay semantics.
/// </summary>
/// <remarks>
/// This service is called once per frame by <c>FormsUtilities.Update</c>. End users do not
/// call <see cref="Update"/> directly. Set <see cref="InitialShowDelay"/>, <see cref="ShowDuration"/>,
/// and <see cref="BetweenShowDelay"/> globally to tune hover timing.
/// <para>
/// A tooltip host is either a Forms control (assign <see cref="FrameworkElement.ToolTip"/>) or a plain
/// visual such as a <c>TextRuntime</c>, <c>ContainerRuntime</c> or <c>NineSliceRuntime</c> (call
/// <see cref="SetToolTip(InteractiveGue, object?)"/>). Both kinds go through the same hover, delay and
/// placement logic. When hosts nest, the innermost host under the cursor wins.
/// </para>
/// </remarks>
public static class ToolTipService
{
    /// <summary>
    /// Time between the cursor entering an element and the tooltip appearing. Default 500ms, matching WPF.
    /// </summary>
    public static TimeSpan InitialShowDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Length of time a tooltip stays open while the cursor remains on the host. Default 5s, matching WPF.
    /// </summary>
    public static TimeSpan ShowDuration { get; set; } = TimeSpan.FromMilliseconds(5000);

    /// <summary>
    /// Time window after a tooltip closes during which the next hover shows a new tooltip without waiting
    /// for <see cref="InitialShowDelay"/>. Default 100ms, matching WPF.
    /// </summary>
    public static TimeSpan BetweenShowDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// What a host has registered: the tooltip to show and, optionally, a callback that supplies its
    /// content each time it opens.
    /// </summary>
    private sealed class Registration
    {
        public Registration(Tooltip tooltip, Func<object?>? contentProvider)
        {
            Tooltip = tooltip;
            ContentProvider = contentProvider;
        }

        public Tooltip Tooltip { get; }
        public Func<object?>? ContentProvider { get; }
    }

    // Keyed by the host, which is a FrameworkElement (Forms controls, so a control keeps its tooltip
    // when its Visual is swapped) or an InteractiveGue (plain visuals). Weak so a host that is
    // collected takes its entry with it.
    private static readonly ConditionalWeakTable<object, Registration> _registrations = new();

    // The FrameworkElement or InteractiveGue currently hovered, as returned by FindHoveredHost.
    private static object? _hoveredHost;
    private static double _hoverStartTimeSeconds;
    private static double _tooltipShownAtSeconds;
    private static double _lastTooltipClosedAtSeconds = double.NegativeInfinity;
    private static Tooltip? _currentOpenTooltip;

    // Set when a content callback returned null or threw, so it is not asked again every frame for as
    // long as the cursor stays on the same host. Cleared when the hovered host changes.
    private static bool _openDeclinedForThisHover;

    /// <summary>
    /// Sets the tooltip shown while the cursor is over a plain visual (a <c>TextRuntime</c>,
    /// <c>ContainerRuntime</c>, <c>NineSliceRuntime</c>, ...). Forms controls use
    /// <see cref="FrameworkElement.ToolTip"/> instead; both go through the same delays and placement.
    /// </summary>
    /// <param name="visual">The visual to attach the tooltip to. It must receive cursor events to be
    /// hovered, see the remarks.</param>
    /// <param name="content">
    /// A <see cref="string"/> (shown in a default <see cref="Tooltip"/>), a <see cref="Tooltip"/>
    /// instance (used as is), or a <see cref="Func{TResult}"/> that returns the string to show and is
    /// called each time the tooltip opens (see <see cref="SetToolTip(InteractiveGue, Func{object?}?)"/>).
    /// Null removes the tooltip.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="visual"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown for any other kind of content, matching
    /// <see cref="FrameworkElement.ToolTip"/>. The previous tooltip, if any, is kept.</exception>
    /// <remarks>
    /// The tooltip opens only when the cursor reports the visual, or one of its descendants, as the
    /// visual it is over. The cursor only considers a visual with
    /// <see cref="InteractiveGue.HasEvents"/> set to true: a <c>ContainerRuntime</c> has it on by
    /// default, a <c>TextRuntime</c> and a <c>NineSliceRuntime</c> do not, so set
    /// <c>HasEvents = true</c> on them. Registering on a visual without events does not throw; its
    /// tooltip simply never opens unless the cursor is over a descendant that has events.
    /// <para>
    /// <c>SpriteRuntime</c>, <c>CircleRuntime</c> and <c>RectangleRuntime</c> do not take cursor events,
    /// so they cannot host a tooltip. To give an icon a tooltip, put it in a <c>ContainerRuntime</c>
    /// and register the container.
    /// </para>
    /// <para>
    /// When hosts nest, the innermost one under the cursor wins. If a Forms control and its own Visual
    /// are both registered, the control's tooltip is used. The registry holds the visual weakly, so
    /// there is nothing to clean up when the visual goes away.
    /// </para>
    /// </remarks>
    public static void SetToolTip(InteractiveGue visual, object? content)
    {
        if (visual == null)
        {
            throw new ArgumentNullException(nameof(visual));
        }

        if (content is Func<object?> provider)
        {
            Register(visual, new Registration(new Tooltip(), provider));
            return;
        }

        Tooltip? tooltip = ToTooltip(content, memberName: nameof(SetToolTip) + " content");
        Register(visual, tooltip == null ? null : new Registration(tooltip, contentProvider: null));
    }

    /// <summary>
    /// Sets a tooltip on a plain visual whose text is computed by <paramref name="contentProvider"/>
    /// each time the tooltip opens, so it reflects the state at hover time.
    /// </summary>
    /// <param name="visual">The visual to attach the tooltip to. See
    /// <see cref="SetToolTip(InteractiveGue, object?)"/> for how a visual becomes hoverable.</param>
    /// <param name="contentProvider">Returns the string to show. Null removes the tooltip.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="visual"/> is null.</exception>
    /// <remarks>
    /// The callback runs when the tooltip is about to open (after the initial delay) and again on every
    /// later open, never while it is open or during the delay. Returning null opens nothing for that
    /// hover. A non-string result throws <see cref="NotSupportedException"/>, as
    /// <see cref="Tooltip.Content"/> only supports strings today. If the callback throws, the exception
    /// propagates out of <c>GumService.Update</c> (a bug in the callback should be visible), nothing is
    /// shown, and the callback is not asked again until the cursor leaves the host and returns, so one
    /// failure does not throw every frame.
    /// </remarks>
    public static void SetToolTip(InteractiveGue visual, Func<object?>? contentProvider)
    {
        SetToolTip(visual, (object?)contentProvider);
    }

    /// <summary>
    /// Associates a <see cref="Tooltip"/> instance with a host <see cref="FrameworkElement"/>.
    /// Called internally when <see cref="FrameworkElement.ToolTip"/> is assigned.
    /// </summary>
    internal static void RegisterTooltip(FrameworkElement host, Tooltip? tooltip)
    {
        if (host == null)
        {
            return;
        }

        if (tooltip != null)
        {
            tooltip.PlacementTarget = host;
        }
        Register(host, tooltip == null ? null : new Registration(tooltip, contentProvider: null));
    }

    /// <summary>
    /// Turns the content assigned to a tooltip property into the <see cref="Tooltip"/> to register:
    /// null stays null, a string is wrapped in a new <see cref="Tooltip"/>, a <see cref="Tooltip"/> is
    /// used as is, anything else throws.
    /// </summary>
    internal static Tooltip? ToTooltip(object? content, string memberName)
    {
        if (content == null)
        {
            return null;
        }

        if (content is Tooltip existing)
        {
            return existing;
        }

        if (content is not string)
        {
            throw new NotSupportedException(
                $"{memberName} currently supports only string (or Tooltip) values. " +
                $"Got {content.GetType().FullName}. Non-string content is reserved for a future release.");
        }

        Tooltip tooltip = new Tooltip();
        tooltip.Content = content;
        return tooltip;
    }

    private static void Register(object host, Registration? registration)
    {
        _registrations.Remove(host);
        if (registration != null)
        {
            _registrations.Add(host, registration);
        }

        if (_hoveredHost == host && registration == null && _currentOpenTooltip != null)
        {
            _currentOpenTooltip.Hide();
            _currentOpenTooltip = null;
        }
    }

    internal static Tooltip? GetTooltip(object? host)
    {
        if (host == null)
        {
            return null;
        }
        _registrations.TryGetValue(host, out Registration? registration);
        return registration?.Tooltip;
    }

    /// <summary>
    /// Per-frame tick that evaluates hover state and shows/hides the associated tooltip.
    /// Called by <c>FormsUtilities.Update</c>; not intended for direct use by application code.
    /// </summary>
    /// <param name="cursor">The active cursor.</param>
    /// <param name="gameTimeSeconds">Elapsed game time in seconds (use <c>InteractiveGue.CurrentGameTime</c>).</param>
    public static void Update(ICursor? cursor, double gameTimeSeconds)
    {
        if (cursor == null)
        {
            return;
        }

        object? hovered = FindHoveredHost(cursor);

        if (hovered != _hoveredHost)
        {
            if (_currentOpenTooltip != null)
            {
                (_hoveredHost as FrameworkElement)?.RaiseToolTipClosing();
                _currentOpenTooltip.Hide();
                _currentOpenTooltip = null;
                _lastTooltipClosedAtSeconds = gameTimeSeconds;
            }

            _hoveredHost = hovered;
            _hoverStartTimeSeconds = gameTimeSeconds;
            _openDeclinedForThisHover = false;
        }
        else if (hovered != null && _currentOpenTooltip == null &&
                 (cursor.XChange != 0 || cursor.YChange != 0))
        {
            // Cursor moved within the same element — restart the stationary-hover timer.
            _hoverStartTimeSeconds = gameTimeSeconds;
        }

        if (hovered == null)
        {
            return;
        }

        if (_currentOpenTooltip != null)
        {
            var elapsedShown = gameTimeSeconds - _tooltipShownAtSeconds;
            if (elapsedShown >= ShowDuration.TotalSeconds)
            {
                (hovered as FrameworkElement)?.RaiseToolTipClosing();
                _currentOpenTooltip.Hide();
                _currentOpenTooltip = null;
                _lastTooltipClosedAtSeconds = gameTimeSeconds;
            }
            return;
        }

        var hoverElapsed = gameTimeSeconds - _hoverStartTimeSeconds;
        var sinceClosed = gameTimeSeconds - _lastTooltipClosedAtSeconds;
        var required = sinceClosed <= BetweenShowDelay.TotalSeconds
            ? 0.0
            : InitialShowDelay.TotalSeconds;

        if (hoverElapsed >= required && !_openDeclinedForThisHover &&
            _registrations.TryGetValue(hovered, out Registration? registration))
        {
            if (registration.ContentProvider != null)
            {
                // Marked before the call so a callback that throws is not called again every frame.
                _openDeclinedForThisHover = true;
                object? content = registration.ContentProvider();
                if (content == null)
                {
                    return;
                }
                registration.Tooltip.Content = content;
                _openDeclinedForThisHover = false;
            }

            Tooltip tooltip = registration.Tooltip;
            (hovered as FrameworkElement)?.RaiseToolTipOpening();
            tooltip.Show(cursor.XRespectingGumZoomAndBounds(), cursor.YRespectingGumZoomAndBounds());
            _currentOpenTooltip = tooltip;
            _tooltipShownAtSeconds = gameTimeSeconds;
        }
    }

    /// <summary>
    /// Resets all internal timing state. Intended for tests so hover history does not leak between test methods.
    /// </summary>
    public static void ResetForTesting()
    {
        if (_currentOpenTooltip != null)
        {
            _currentOpenTooltip.Hide();
            _currentOpenTooltip = null;
        }
        _hoveredHost = null;
        _hoverStartTimeSeconds = 0;
        _tooltipShownAtSeconds = 0;
        _lastTooltipClosedAtSeconds = double.NegativeInfinity;
        _openDeclinedForThisHover = false;
    }

    /// <summary>
    /// Walks up from the visual under the cursor and returns the first host with a registered tooltip:
    /// the Forms control at each level (checked first, so it wins a tie with its own Visual), then the
    /// visual itself. The innermost host therefore wins.
    /// </summary>
    private static object? FindHoveredHost(ICursor cursor)
    {
        var visual = cursor.WindowPushed ?? cursor.VisualOver;
        if (visual == null)
        {
            return null;
        }

        InteractiveGue? current = visual as InteractiveGue;
        while (current != null)
        {
            if (current.FormsControlAsObject is FrameworkElement fe && GetTooltip(fe) != null)
            {
                return fe;
            }
            if (GetTooltip(current) != null)
            {
                return current;
            }
            current = current.Parent as InteractiveGue;
        }
        return null;
    }
}
