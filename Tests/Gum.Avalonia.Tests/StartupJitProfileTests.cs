using Gum.Avalonia.Diagnostics;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The startup JIT profile lives in the per-user folder and never stops startup.</summary>
public class StartupJitProfileTests : IDisposable
{
    private readonly string _appData = Path.Combine(Path.GetTempPath(), "StartupJitProfileTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_appData))
        {
            Directory.Delete(_appData, recursive: true);
        }
    }

    [Fact]
    public void Start_AppDataFolderIsAFile_DoesNotStartAndDoesNotThrow()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_appData)!);
        File.WriteAllText(_appData, "not a folder");
        bool started = false;

        StartupJitProfile.Start(_appData, (_, _) => started = true);

        started.ShouldBeFalse();
        File.Delete(_appData);
    }

    [Fact]
    public void Start_StartsTheProfileInAFolderUnderTheAppDataFolder()
    {
        string? startedFolder = null;
        string? startedFile = null;

        StartupJitProfile.Start(_appData, (folder, file) => { startedFolder = folder; startedFile = file; });

        startedFolder.ShouldBe(Path.Combine(_appData, "JitProfile"));
        Directory.Exists(startedFolder).ShouldBeTrue();
        startedFile.ShouldBe("Startup.jitprofile");
    }
}
