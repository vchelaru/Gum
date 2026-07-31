using System;

namespace Gum.Controls;

/// <summary>
/// A scroll bar, expressed without any UI framework type so the same scrolling logic can drive a
/// WinForms or a WPF scroll bar. Range semantics match WinForms: <see cref="LargeChange"/> is the
/// visible portion of the scrolled area, and the largest reachable <see cref="Value"/> is
/// <see cref="Maximum"/> - <see cref="LargeChange"/> + 1.
/// </summary>
public interface IScrollBar
{
    /// <summary>The smallest value the bar can scroll to.</summary>
    int Minimum { get; set; }

    /// <summary>
    /// The end of the scrolled area. Not reachable by <see cref="Value"/> - see the range
    /// semantics on this interface.
    /// </summary>
    int Maximum { get; set; }

    /// <summary>How much of the scrolled area is visible, in the same units as the range.</summary>
    int LargeChange { get; set; }

    /// <summary>The current scroll position.</summary>
    int Value { get; set; }

    /// <summary>Raised whenever <see cref="Value"/> changes, whether by the user or by code.</summary>
    event EventHandler? ValueChanged;
}
