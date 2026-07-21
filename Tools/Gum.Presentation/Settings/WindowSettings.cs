namespace Gum.Settings;

/// <summary>
/// Relocated from <c>Gum/Settings/LayoutSettings.cs</c> (part of #3856) — pure data records, no WPF
/// dependency. Pure file-location move: namespace and members are unchanged, so no consumer needed
/// an import change. <see cref="LayoutSettings"/> itself has since relocated alongside these records
/// once <c>GeneralSettingsFile.MainWindowState</c> stopped being WinForms-typed (see
/// <see cref="LegacyMainWindowState"/>).
/// </summary>
public record WindowSettings(
    double Width = 1280,
    double Height = 720,
    double? Top = null,
    double? Left = null,
    bool IsMaximized = false
);

public record MainTabDimensions(
    double LeftColumnWidth = 250,
    double CenterColumnWidth = 320,
    double BottomRightHeight = 200
);
