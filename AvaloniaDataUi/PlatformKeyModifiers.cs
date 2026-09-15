using Avalonia;
using Avalonia.Input;

namespace AvaloniaDataUi;

/// <summary>
/// The platform's primary shortcut modifier, from Avalonia's hotkey configuration: Cmd on macOS,
/// Ctrl on Windows and Linux. Every "is Ctrl held" check in the tool's Avalonia code goes through
/// here so Cmd+C, Cmd+click and the rest work on a Mac.
/// </summary>
public static class PlatformKeyModifiers
{
    /// <summary>The command modifier for the running platform; Ctrl when no platform is up (tests).</summary>
    public static KeyModifiers Command =>
        Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;

    /// <summary>Whether <paramref name="modifiers"/> includes the platform's command modifier.</summary>
    public static bool HasCommand(this KeyModifiers modifiers) => modifiers.HasFlag(Command);
}
