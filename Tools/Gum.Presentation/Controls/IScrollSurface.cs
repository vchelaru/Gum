using System;

namespace Gum.Controls;

/// <summary>
/// The render surface a pair of <see cref="IScrollBar"/>s scrolls over: its size in pixels, plus
/// notification when that size changes. Framework-neutral so scrolling logic does not need a
/// WinForms <c>Control</c> or a WPF <c>FrameworkElement</c>.
/// </summary>
public interface IScrollSurface
{
    /// <summary>The surface's width, in pixels.</summary>
    int Width { get; }

    /// <summary>The surface's height, in pixels.</summary>
    int Height { get; }

    /// <summary>Raised after <see cref="Width"/> or <see cref="Height"/> changes.</summary>
    event EventHandler? SizeChanged;
}
