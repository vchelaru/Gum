using System;
using System.Collections.Generic;

namespace Gum.ViewModels;

public class ContextMenuItemViewModel
{
    public string Text { get; set; } = string.Empty;
    public Action? Action { get; set; }
    public List<ContextMenuItemViewModel> Children { get; set; } = new();
    public bool IsSeparator { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Shortcut { get; set; }

    /// <summary>
    /// Framework-neutral key identifying which icon this item should show, or null for none.
    /// <see cref="Gum.Extensions.ContextMenuItemViewModelExtensions"/> maps known keys to the actual
    /// WPF icon element - this assembly must not reference WPF (ADR-0005).
    /// </summary>
    public string? IconKey { get; set; }
}
