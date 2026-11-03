using System;
using System.IO;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Managers;

public class LastProjectLoadMarkerTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "LastProjectLoadMarkerTests", Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void InterruptedProject_ReportsAStartedLoad_UntilCleared()
    {
        string projectPath = "c:/projects/My Game.gumx";
        Directory.CreateDirectory(_folder);
        var marker = new LastProjectLoadMarker(() => _folder);
        marker.InterruptedProject.ShouldBeNull();

        marker.MarkStarted(projectPath);
        // A new instance stands in for the next launch reading what the crashed one left behind.
        new LastProjectLoadMarker(() => _folder).InterruptedProject.ShouldBe(projectPath);

        marker.Clear();
        new LastProjectLoadMarker(() => _folder).InterruptedProject.ShouldBeNull();
    }

    [Fact]
    public void MarkStartedClearAndRead_WhenAnotherProcessHoldsTheFile_DoNotThrow()
    {
        // Another running Gum has the marker open with no sharing.
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "LoadingLastProject.txt");
        using FileStream otherInstance = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        var marker = new LastProjectLoadMarker(() => _folder);

        Should.NotThrow(() => marker.MarkStarted("c:/projects/Game.gumx"));
        Should.NotThrow(() => marker.Clear());
        Should.NotThrow(() => _ = marker.InterruptedProject);
    }

    [Fact]
    public void MarkStarted_WhenTheFileIsBrieflyHeld_RetriesAndWrites()
    {
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "LoadingLastProject.txt");
        FileStream other = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        var marker = new LastProjectLoadMarker(() => _folder);
        // A dedicated thread that is running before the write starts: a thread-pool task can start later
        // than the marker's whole retry window (3 attempts, 50 ms apart) on a loaded CI machine.
        using System.Threading.ManualResetEventSlim releaserRunning = new System.Threading.ManualResetEventSlim();
        System.Threading.Thread releaser = new System.Threading.Thread(() =>
        {
            releaserRunning.Set();
            System.Threading.Thread.Sleep(15);
            other.Dispose();
        });
        releaser.Start();
        releaserRunning.Wait();

        marker.MarkStarted("c:/projects/Game.gumx");

        marker.InterruptedProject.ShouldBe("c:/projects/Game.gumx");
    }

    [Fact]
    public void MarkStarted_CreatesTheSettingsFolder_WhenMissing()
    {
        var marker = new LastProjectLoadMarker(() => _folder);

        marker.MarkStarted("c:/projects/Game.gumx");

        marker.InterruptedProject.ShouldBe("c:/projects/Game.gumx");
    }
}
