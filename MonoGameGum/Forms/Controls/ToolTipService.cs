using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
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

    private static readonly ConditionalWeakTable<FrameworkElement, Tooltip> _tooltipsByHost = new();

    private static FrameworkElement? _hoveredElement;
    private static double _hoverStartTimeSeconds;
    private static double _tooltipShownAtSeconds;
    private static double _lastTooltipClosedAtSeconds = double.NegativeInfinity;
    private static Tooltip? _currentOpenTooltip;

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

        _tooltipsByHost.Remove(host);
        if (tooltip != null)
        {
            tooltip.PlacementTarget = host;
            _tooltipsByHost.Add(host, tooltip);
        }

        if (_hoveredElement == host && tooltip == null && _currentOpenTooltip != null)
        {
            _currentOpenTooltip.Hide();
            _currentOpenTooltip = null;
        }
    }

    internal static Tooltip? GetTooltip(FrameworkElement host)
    {
        if (host == null)
        {
            return null;
        }
        _tooltipsByHost.TryGetValue(host, out var tooltip);
        return tooltip;
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

        FrameworkElement? hovered = GetHoveredFrameworkElementWithTooltip(cursor);

        if (hovered != _hoveredElement)
        {
            if (_currentOpenTooltip != null)
            {
                _hoveredElement?.RaiseToolTipClosing();
                _currentOpenTooltip.Hide();
                _currentOpenTooltip = null;
                _lastTooltipClosedAtSeconds = gameTimeSeconds;
            }

            _hoveredElement = hovered;
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
                hovered.RaiseToolTipClosing();
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

        if (hoverElapsed >= required)
        {
            var tooltip = GetTooltip(hovered);
            if (tooltip != null)
            {
                hovered.RaiseToolTipOpening();
                tooltip.Show(cursor.XRespectingGumZoomAndBounds(), cursor.YRespectingGumZoomAndBounds());
                _currentOpenTooltip = tooltip;
                _tooltipShownAtSeconds = gameTimeSeconds;
            }
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
        _hoveredElement = null;
        _hoverStartTimeSeconds = 0;
        _tooltipShownAtSeconds = 0;
        _lastTooltipClosedAtSeconds = double.NegativeInfinity;
    }

    private static FrameworkElement? GetHoveredFrameworkElementWithTooltip(ICursor cursor)
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
            current = current.Parent as InteractiveGue;
        }
        return null;
    }
}
