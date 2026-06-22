using Shouldly;
using StateAnimationPlugin.Managers;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

public class SettingsManagerTests : BaseTestClass
{
    private readonly string _tempDirectory;

    public SettingsManagerTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "GumSettingsManagerTests", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void LoadOrCreateSettings_leaves_defaults_when_file_missing()
    {
        FilePath settingsFile = new FilePath(Path.Combine(_tempDirectory, "GlobalAnimationSettings.json"));
        SettingsManager settingsManager = new SettingsManager(settingsFile);

        settingsManager.LoadOrCreateSettings();

        settingsManager.GlobalSettings.ShouldNotBeNull();
        settingsManager.GlobalSettings.FirstToSecondColumnRatio.ShouldBe(1m);
    }

    [Fact]
    public void LoadOrCreateSettings_round_trips_saved_settings()
    {
        FilePath settingsFile = new FilePath(Path.Combine(_tempDirectory, "GlobalAnimationSettings.json"));

        SettingsManager toSave = new SettingsManager(settingsFile);
        toSave.GlobalSettings.FirstToSecondColumnRatio = 2.5m;
        toSave.SaveSettings();

        SettingsManager toLoad = new SettingsManager(settingsFile);
        toLoad.LoadOrCreateSettings();

        toLoad.GlobalSettings.FirstToSecondColumnRatio.ShouldBe(2.5m);
    }

    [Fact]
    public void SaveSettings_creates_missing_directory()
    {
        FilePath settingsFile = new FilePath(
            Path.Combine(_tempDirectory, "nested", "GlobalAnimationSettings.json"));
        SettingsManager settingsManager = new SettingsManager(settingsFile);

        settingsManager.SaveSettings();

        File.Exists(settingsFile.FullPath).ShouldBeTrue();
    }

    public override void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }

        base.Dispose();
    }
}
