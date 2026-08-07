using Gum.Settings;

namespace Gum.ViewModels;

/// <summary>
/// Relocated from <c>MainWindowViewModel.LoadWindowSettings</c>'s first-launch guard (part of
/// #3856) — pure decision, no WPF dependency.
/// </summary>
public static class WindowSettingsLogic
{
    /// <summary>
    /// True when <paramref name="settings"/> looks like it was never actually saved (first launch,
    /// or a corrupt save) or holds a size too small to be usable, in which case the window should be
    /// left at its WPF-chosen default position/size instead of being placed from
    /// <paramref name="settings"/>.
    /// </summary>
    public static bool IsFirstLaunch(WindowSettings settings) =>
        settings is { Left: null, Top: null } ||
        settings.Width < WindowSettings.MinimumWidth ||
        settings.Height < WindowSettings.MinimumHeight;

    /// <summary>
    /// Returns <paramref name="settings"/> with any unusably small size replaced by the default
    /// size, so a degenerate geometry is never written to disk.
    /// </summary>
    public static WindowSettings WithUsableSize(WindowSettings settings) => settings with
    {
        Width = settings.Width < WindowSettings.MinimumWidth ? WindowSettings.DefaultWidth : settings.Width,
        Height = settings.Height < WindowSettings.MinimumHeight ? WindowSettings.DefaultHeight : settings.Height
    };
}
