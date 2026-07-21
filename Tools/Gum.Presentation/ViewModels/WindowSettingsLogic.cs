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
    /// or a corrupt 0-sized save) and the window should be left at its WPF-chosen default
    /// position/size instead of being placed from <paramref name="settings"/>.
    /// </summary>
    public static bool IsFirstLaunch(WindowSettings settings) =>
        settings is { Left: null, Top: null } or { Width: 0 } or { Height: 0 };
}
