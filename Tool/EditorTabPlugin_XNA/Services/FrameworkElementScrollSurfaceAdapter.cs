using System;
using System.Windows;
using Gum.Controls;

namespace Gum.Plugins.ScrollBarPlugin;

/// <summary>
/// Adapts a WPF <see cref="FrameworkElement"/> to <see cref="IScrollSurface"/> so
/// <see cref="Gum.Plugins.InternalPlugins.EditorTab.Services.ScrollBarControlLogic"/> can size its
/// bars from the wireframe canvas without referencing WPF. The WPF counterpart to
/// <see cref="ControlScrollSurfaceAdapter"/>.
/// </summary>
public class FrameworkElementScrollSurfaceAdapter : IScrollSurface
{
    private readonly FrameworkElement _element;

    public FrameworkElementScrollSurfaceAdapter(FrameworkElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        _element = element;
        _element.SizeChanged += (_, _) => SizeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public int Width => (int)_element.ActualWidth;

    /// <inheritdoc/>
    public int Height => (int)_element.ActualHeight;

    /// <inheritdoc/>
    public event EventHandler? SizeChanged;
}
