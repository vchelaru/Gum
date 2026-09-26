using System.Drawing;
using Gum.Dialogs;
using Gum.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.Migration;

/// <summary>
/// The per-user <c>appsettings.json</c> the WPF head wrote carries over into this head, which reads
/// the same folder. Each test builds the head's real host (<see cref="Program.CreateHostBuilder"/>)
/// over a temp user-data folder, never the real one.
/// </summary>
public class WpfSettingsCarryoverTests : IDisposable
{
    private readonly string _userDataFolder;
    private readonly string? _originalOverride;

    public WpfSettingsCarryoverTests()
    {
        _userDataFolder = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", "WpfSettings", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_userDataFolder);
        _originalOverride = FileManager.UserApplicationDataFolderOverride;
        FileManager.UserApplicationDataFolderOverride = _userDataFolder;
    }

    public void Dispose()
    {
        FileManager.UserApplicationDataFolderOverride = _originalOverride;
        try
        {
            Directory.Delete(_userDataFolder, recursive: true);
        }
        catch (IOException)
        {
            // The config file watcher can still hold the folder for a moment; it is a temp folder.
        }
    }

    [Fact]
    public void AppSettingsWrittenByWpf_LoadsThemeAndLayout()
    {
        // The shape WritableOptions (shared by both heads) writes: camelCase names, enums as
        // numbers, colors as #RRGGBB or #AARRGGBB, unset colors as null.
        string json = """
            {
              "ThemeSettings": {
                "mode": 2,
                "accent": "#1E90FF",
                "checkerA": "#80102030",
                "checkerB": null,
                "outlineColor": null,
                "guideLine": "#FF0000",
                "guideText": null
              },
              "LayoutSettings": {
                "mainWindow": {
                  "width": 1600,
                  "height": 900,
                  "top": 40,
                  "left": 60,
                  "isMaximized": true
                },
                "mainTabDimensions": {
                  "leftColumnWidth": 310,
                  "centerColumnWidth": 420,
                  "bottomRightHeight": 180
                }
              }
            }
            """;
        File.WriteAllText(Path.Combine(_userDataFolder, "appsettings.json"), json);

        using IHost host = Program.CreateHostBuilder().Build();

        ThemeSettings theme = host.Services.GetRequiredService<IWritableOptions<ThemeSettings>>().CurrentValue;
        theme.Mode.ShouldBe(ThemeMode.Dark);
        // The binder turns a hex matching a named color into that named Color, so compare ARGB.
        theme.Accent!.Value.ToArgb().ShouldBe(Color.FromArgb(255, 0x1E, 0x90, 0xFF).ToArgb());
        theme.CheckerA!.Value.ToArgb().ShouldBe(Color.FromArgb(0x80, 0x10, 0x20, 0x30).ToArgb());
        theme.CheckerB.ShouldBeNull();
        theme.GuideLine!.Value.ToArgb().ShouldBe(Color.FromArgb(255, 255, 0, 0).ToArgb());

        LayoutSettings layout = host.Services.GetRequiredService<IWritableOptions<LayoutSettings>>().CurrentValue;
        layout.MainWindow.ShouldBe(new WindowSettings(1600, 900, Top: 40, Left: 60, IsMaximized: true));
        layout.MainTabDimensions.ShouldBe(new MainTabDimensions(310, 420, 180));
    }

    [Theory]
    [InlineData("{ \"ThemeSettings\": { \"mode\": 2, ")] // Truncated mid-write.
    [InlineData("not json at all")]
    [InlineData("")] // Zero bytes.
    [InlineData("[]")] // Valid JSON, but configuration needs an object.
    public void CorruptAppSettings_StartsWithDefaultsAndKeepsTheOldFile(string contents)
    {
        string settingsPath = Path.Combine(_userDataFolder, "appsettings.json");
        File.WriteAllText(settingsPath, contents);

        using IHost host = Program.CreateHostBuilder().Build();

        ThemeSettings theme = host.Services.GetRequiredService<IWritableOptions<ThemeSettings>>().CurrentValue;
        theme.Mode.ShouldBeNull();
        File.ReadAllText(settingsPath + ".unreadable").ShouldBe(contents);
    }

    [Fact]
    public void UnbindableThemeValue_DropsOnlyThatSectionAndKeepsIt()
    {
        string settingsPath = Path.Combine(_userDataFolder, "appsettings.json");
        File.WriteAllText(settingsPath, """
            {
              "ThemeSettings": { "mode": "Purple", "accent": "#1E90FF" },
              "LayoutSettings": { "mainWindow": { "width": 1600, "height": 900 } }
            }
            """);

        using IHost host = Program.CreateHostBuilder().Build();

        IWritableOptions<ThemeSettings> theme = host.Services.GetRequiredService<IWritableOptions<ThemeSettings>>();
        theme.CurrentValue.Mode.ShouldBeNull();
        theme.CurrentValue.Accent.ShouldBeNull();
        host.Services.GetRequiredService<IWritableOptions<LayoutSettings>>().CurrentValue.MainWindow
            .ShouldBe(new WindowSettings(1600, 900));
        File.ReadAllText(settingsPath + ".ThemeSettings.unreadable").ShouldContain("Purple");

        // The file no longer holds the bad value, so saving a theme change works.
        theme.Update(t => t.Mode = ThemeMode.Dark);
        theme.CurrentValue.Mode.ShouldBe(ThemeMode.Dark);
    }

    [Fact]
    public void UnbindableLayoutValue_StartsWithDefaultLayout()
    {
        string settingsPath = Path.Combine(_userDataFolder, "appsettings.json");
        File.WriteAllText(settingsPath, """
            {
              "ThemeSettings": { "mode": 2 },
              "LayoutSettings": { "mainWindow": { "width": "wide" } }
            }
            """);

        using IHost host = Program.CreateHostBuilder().Build();

        host.Services.GetRequiredService<IWritableOptions<LayoutSettings>>().CurrentValue.MainWindow
            .ShouldBe(new WindowSettings());
        host.Services.GetRequiredService<IWritableOptions<ThemeSettings>>().CurrentValue.Mode
            .ShouldBe(ThemeMode.Dark);
        File.ReadAllText(settingsPath + ".LayoutSettings.unreadable").ShouldContain("wide");
    }
}
