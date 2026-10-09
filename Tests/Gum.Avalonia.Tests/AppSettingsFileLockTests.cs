using Gum.Dialogs;
using Gum.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Another process (antivirus, a sync client, a second Gum) or Gum's own settings save can hold
/// <c>appsettings.json</c> open while the config file watcher reloads it. That must never surface
/// as an error dialog.
/// </summary>
public class AppSettingsFileLockTests : IDisposable
{
    private readonly string _folder;
    private readonly string _path;
    private readonly string? _originalOverride;

    public AppSettingsFileLockTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", "AppSettingsLock", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _path = Path.Combine(_folder, "appsettings.json");
        _originalOverride = FileManager.UserApplicationDataFolderOverride;
        FileManager.UserApplicationDataFolderOverride = _folder;
    }

    public void Dispose()
    {
        FileManager.UserApplicationDataFolderOverride = _originalOverride;
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // The config file watcher can still hold the folder for a moment; it is a temp folder.
        }
    }

    // A watcher's reload runs on a thread-pool thread, so a lock it hits faults a task nobody
    // observes. Gum saves through WritableOptions, which reloads for itself.
    [Fact]
    public void AppSettingsFile_IsNotWatchedForChanges()
    {
        File.WriteAllText(_path, "{}");

        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(_folder)
            .AddAppSettingsJsonFile("appsettings.json")
            .Build();

        config.Providers.OfType<JsonConfigurationProvider>().Single().Source.ReloadOnChange.ShouldBeFalse();
    }

    [Fact]
    public void UpdateWhileFileIsBrieflyLocked_WaitsAndSaves()
    {
        File.WriteAllText(_path, "{}");
        using IHost host = Program.CreateHostBuilder().Build();
        IWritableOptions<ThemeSettings> theme = host.Services.GetRequiredService<IWritableOptions<ThemeSettings>>();
        FileStream locker = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Task unlock = Task.Run(async () =>
        {
            await Task.Delay(300);
            locker.Dispose();
        });

        Should.NotThrow(() => theme.Update(t => t.Mode = ThemeMode.Dark));

        unlock.Wait();
        theme.CurrentValue.Mode.ShouldBe(ThemeMode.Dark);
        File.ReadAllText(_path).ShouldContain("mode");
    }

    [Fact]
    public void UpdateWhileFileStaysLocked_DoesNotThrow()
    {
        File.WriteAllText(_path, "{}");
        using IHost host = Program.CreateHostBuilder().Build();
        IWritableOptions<ThemeSettings> theme = host.Services.GetRequiredService<IWritableOptions<ThemeSettings>>();
        using FileStream locker = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        Should.NotThrow(() => theme.Update(t => t.Mode = ThemeMode.Dark));
    }
}
