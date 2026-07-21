using System.Drawing;
using Gum.Settings;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization tests for <see cref="LayoutSettings.MigrateLegacyLayout"/>, pinning the
/// one-time migration of the legacy <see cref="GeneralSettingsFile"/> window bounds/state into the
/// newer <see cref="WindowSettings"/> shape.
/// </summary>
public class LayoutSettingsTests
{
    [Fact]
    public void MigrateLegacyLayout_DoesNotChangeSettings_WhenLegacyBoundsAreDefault()
    {
        GeneralSettingsFile legacySettings = new GeneralSettingsFile();
        LayoutSettings settings = new LayoutSettings();
        WindowSettings originalWindow = settings.MainWindow;

        LayoutSettings.MigrateLegacyLayout(legacySettings, settings);

        settings.MainWindow.ShouldBe(originalWindow);
    }

    [Fact]
    public void MigrateLegacyLayout_SetsIsMaximizedTrue_WhenLegacyStateIsMaximized()
    {
        GeneralSettingsFile legacySettings = new GeneralSettingsFile
        {
            MainWindowBounds = new Rectangle(10, 20, 1280, 720),
            MainWindowState = LegacyMainWindowState.Maximized
        };
        LayoutSettings settings = new LayoutSettings();

        LayoutSettings.MigrateLegacyLayout(legacySettings, settings);

        settings.MainWindow.IsMaximized.ShouldBeTrue();
        settings.MainWindow.Width.ShouldBe(1280);
        settings.MainWindow.Height.ShouldBe(720);
        settings.MainWindow.Left.ShouldBe(10);
        settings.MainWindow.Top.ShouldBe(20);
    }

    [Fact]
    public void MigrateLegacyLayout_SetsIsMaximizedFalse_WhenLegacyStateIsNormal()
    {
        GeneralSettingsFile legacySettings = new GeneralSettingsFile
        {
            MainWindowBounds = new Rectangle(0, 0, 1024, 768),
            MainWindowState = LegacyMainWindowState.Normal
        };
        LayoutSettings settings = new LayoutSettings();

        LayoutSettings.MigrateLegacyLayout(legacySettings, settings);

        settings.MainWindow.IsMaximized.ShouldBeFalse();
    }
}
