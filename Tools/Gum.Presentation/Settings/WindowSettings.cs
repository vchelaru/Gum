namespace Gum.Settings;

/// <summary>
/// Relocated from <c>Gum/Settings/LayoutSettings.cs</c> (part of #3856) — pure data records, no WPF
/// dependency. <see cref="LayoutSettings"/> itself stays in <c>Gum.csproj</c> because
/// <c>LayoutSettings.MigrateLegacyLayout</c> reads WinForms-typed legacy settings
/// (<c>GeneralSettingsFile.MainWindowBounds</c>/<c>MainWindowState</c>). Pure file-location move:
/// namespace and members are unchanged, so no consumer needed an import change.
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
