using System;
using System.Collections.Generic;
using Gum.Managers;

namespace Gum.ViewModels;

public class ContextMenuItemViewModel
{
    public string Text { get; set; } = string.Empty;
    public Action? Action { get; set; }
    public List<ContextMenuItemViewModel> Children { get; set; } = new();
    public bool IsSeparator { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The bound shortcut shown beside the item, or null for none. Each head renders it in its
    /// platform's spelling (Cmd on macOS), so builders never pre-format it as text.
    /// </summary>
    public KeyCombination? Shortcut { get; set; }

    /// <summary>
    /// Framework-neutral key identifying which icon this item should show, or null for none.
    /// <see cref="Gum.Extensions.ContextMenuItemViewModelExtensions"/> maps known keys to the actual
    /// WPF icon element - this assembly must not reference WPF (ADR-0005).
    /// </summary>
    public string? IconKey { get; set; }

    /// <summary>
    /// Name of a palette color (see <see cref="Gum.Plugins.InternalPlugins.StatePlugin.CategoryColors"/>)
    /// drawn as a dot in the icon column, or null for none. Each head maps the name to its theme's color.
    /// </summary>
    public string? IconColorName { get; set; }

    /// <summary>Name of a palette color drawn as a dot just before the item's text, or null for none.</summary>
    public string? ColorName { get; set; }

    /// <summary>Whether a check mark shows in the icon column, marking the current choice of a group of items.</summary>
    public bool IsChecked { get; set; }
}
