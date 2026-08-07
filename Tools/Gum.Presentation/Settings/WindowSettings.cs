namespace Gum.Settings;

/// <summary>
/// Relocated from <c>Gum/Settings/LayoutSettings.cs</c> (part of #3856) — pure data records, no WPF
/// dependency. Pure file-location move: namespace and members are unchanged, so no consumer needed
/// an import change. <see cref="LayoutSettings"/> itself has since relocated alongside these records
/// once <c>GeneralSettingsFile.MainWindowState</c> stopped being WinForms-typed (see
/// <see cref="LegacyMainWindowState"/>).
/// </summary>
public record WindowSettings(
    double Width = WindowSettings.DefaultWidth,
    double Height = WindowSettings.DefaultHeight,
    double? Top = null,
    double? Left = null,
    bool IsMaximized = false
)
{
    public const double DefaultWidth = 1280;
    public const double DefaultHeight = 720;

    /// <summary>
    /// Smallest size the main window may be sized to. Enforced by MainWindow's MinWidth/MinHeight,
    /// and treated as the floor for persisted geometry so a window shrunk to the OS minimum track
    /// size (title bar only) can't be saved and restored forever - see #4361.
    /// </summary>
    public const double MinimumWidth = 480;
    public const double MinimumHeight = 320;
}

public record MainTabDimensions(
    double LeftColumnWidth = 250,
    double CenterColumnWidth = 320,
    double BottomRightHeight = 200
);
