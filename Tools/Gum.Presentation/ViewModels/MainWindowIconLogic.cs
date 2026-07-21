using Gum.Dialogs;

namespace Gum.ViewModels;

/// <summary>
/// Picks the main window's title-bar/taskbar icon for a given theme. Relocated from
/// <c>MainWindowViewModel.Receive(ThemeChangedMessage)</c> (part of #3856) — pure mapping, no WPF
/// dependency.
/// </summary>
public static class MainWindowIconLogic
{
    /// <summary>
    /// Returns the pack URI of the icon that should be shown for <paramref name="mode"/>.
    /// </summary>
    public static string GetIconSource(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => "pack://application:,,,/GumLogo64Light.png",
        _ => "pack://application:,,,/GumLogo64.png"
    };
}
