namespace Gum.Settings;

public class LayoutSettings
{
    public WindowSettings MainWindow { get; set; } = new();
    public MainTabDimensions MainTabDimensions{ get; set; } = new();

    public static void MigrateLegacyLayout(GeneralSettingsFile legacySettings, LayoutSettings settings)
    {
        if (legacySettings.MainWindowBounds != default)
        {
            settings.MainWindow = new WindowSettings()
            {
                Width = legacySettings.MainWindowBounds.Width,
                Height = legacySettings.MainWindowBounds.Height,
                Left = legacySettings.MainWindowBounds.Left,
                Top = legacySettings.MainWindowBounds.Top,
                IsMaximized = legacySettings.MainWindowState == System.Windows.Forms.FormWindowState.Maximized
            };
        }
    }
}

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