using System;
using System.Drawing;
using System.IO;
using Gum.Settings;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests.Settings;

/// <summary>
/// The <c>GeneralSettings.xml</c> the WPF head wrote to the per-user Gum folder, which the Avalonia
/// head reads on its first launch. Runs against a temp user-data folder, never the real one.
/// </summary>
public class GeneralSettingsFileWpfCarryoverTests : IDisposable
{
    private readonly string _userDataFolder;
    private readonly string? _originalOverride;

    public GeneralSettingsFileWpfCarryoverTests()
    {
        _userDataFolder = Path.Combine(Path.GetTempPath(), "GumPresentationTests", "GeneralSettings", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_userDataFolder);
        _originalOverride = FileManager.UserApplicationDataFolderOverride;
        FileManager.UserApplicationDataFolderOverride = _userDataFolder;
    }

    public void Dispose()
    {
        FileManager.UserApplicationDataFolderOverride = _originalOverride;
        Directory.Delete(_userDataFolder, recursive: true);
    }

    [Fact]
    public void LoadOrCreateNew_ReadsFileWrittenByWpf()
    {
        // XmlSerializer's shape for the WPF head's GeneralSettingsFile, including the WinForms-era
        // MainWindowState and a System.Drawing.Rectangle.
        string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GeneralSettingsFile xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <LastProject>/games/Shooter/Content/GumProject/GumProject.gumx</LastProject>
              <ShowTextOutlines>true</ShowTextOutlines>
              <AutoSave>false</AutoSave>
              <UseStandardsPalette>false</UseStandardsPalette>
              <FrameRate>60</FrameRate>
              <RecentProjects>
                <RecentProjectReference>
                  <LastTimeOpened>2026-08-30T10:15:00-07:00</LastTimeOpened>
                  <AbsoluteFileName>/games/Shooter/Content/GumProject/GumProject.gumx</AbsoluteFileName>
                  <IsFavorite>true</IsFavorite>
                </RecentProjectReference>
                <RecentProjectReference>
                  <LastTimeOpened>2026-07-01T09:00:00-07:00</LastTimeOpened>
                  <AbsoluteFileName>/games/Puzzle/Gum/Puzzle.gumx</AbsoluteFileName>
                  <IsFavorite>false</IsFavorite>
                </RecentProjectReference>
              </RecentProjects>
              <MainWindowBounds>
                <Location><X>60</X><Y>40</Y></Location>
                <Size><Width>1600</Width><Height>900</Height></Size>
                <X>60</X>
                <Y>40</Y>
                <Width>1600</Width>
                <Height>900</Height>
              </MainWindowBounds>
              <MainWindowState>Maximized</MainWindowState>
              <LeftAndEverythingSplitterDistance>250</LeftAndEverythingSplitterDistance>
              <CheckerColor1R>10</CheckerColor1R>
              <CheckerColor1G>20</CheckerColor1G>
              <CheckerColor1B>30</CheckerColor1B>
            </GeneralSettingsFile>
            """;
        File.WriteAllText(Path.Combine(_userDataFolder, "GeneralSettings.xml"), xml);

        GeneralSettingsFile settings = GeneralSettingsFile.LoadOrCreateNew();

        settings.LastProject.ShouldBe("/games/Shooter/Content/GumProject/GumProject.gumx");
        settings.ShowTextOutlines.ShouldBeTrue();
        settings.AutoSave.ShouldBeFalse();
        settings.UseStandardsPalette.ShouldBe(false);
        settings.FrameRate.ShouldBe(60);
        settings.RecentProjects.Count.ShouldBe(2);
        settings.RecentProjects[0].AbsoluteFileName.ShouldBe("/games/Shooter/Content/GumProject/GumProject.gumx");
        settings.RecentProjects[0].IsFavorite.ShouldBeTrue();
        settings.RecentProjects[1].IsFavorite.ShouldBeFalse();
        settings.MainWindowBounds.ShouldBe(new Rectangle(60, 40, 1600, 900));
        settings.MainWindowState.ShouldBe(LegacyMainWindowState.Maximized);
        settings.CheckerColor1R.ShouldBe((byte)10);
        // Absent from the file: keeps the constructor default rather than zero.
        settings.CheckerColor2R.ShouldBe((byte)170);
    }

    [Theory]
    [InlineData("<GeneralSettingsFile><RecentProjects><RecentProjectRef")] // Truncated mid-write.
    [InlineData("")]
    public void LoadOrCreateNew_UnreadableFile_ReturnsDefaultsAndKeepsTheOldFile(string contents)
    {
        string path = Path.Combine(_userDataFolder, "GeneralSettings.xml");
        File.WriteAllText(path, contents);

        GeneralSettingsFile settings = GeneralSettingsFile.LoadOrCreateNew();

        settings.RecentProjects.ShouldBeEmpty();
        settings.AutoSave.ShouldBeTrue();
        // The next save overwrites GeneralSettings.xml, so the recent-project list the user lost is
        // kept beside it.
        File.ReadAllText(path + ".unreadable").ShouldBe(contents);
    }
}
