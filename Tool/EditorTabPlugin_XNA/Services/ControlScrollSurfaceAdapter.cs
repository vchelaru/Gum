using System;
using System.Windows.Forms;
using Gum.Controls;

namespace Gum.Plugins.ScrollBarPlugin;

/// <summary>
/// Adapts a WinForms <see cref="Control"/> to <see cref="IScrollSurface"/> so
/// <see cref="Gum.Plugins.InternalPlugins.EditorTab.Services.ScrollBarControlLogic"/> can size its
/// bars from the wireframe control without referencing WinForms.
/// </summary>
public class ControlScrollSurfaceAdapter : IScrollSurface
{
    private readonly Control _control;

    public ControlScrollSurfaceAdapter(Control control)
    {
        _control = control;
        _control.Resize += (_, _) => SizeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public int Width => _control.Width;

    /// <inheritdoc/>
    public int Height => _control.Height;

    /// <inheritdoc/>
    public event EventHandler? SizeChanged;
}
