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
    public void MarkStarted_CreatesTheSettingsFolder_WhenMissing()
    {
        var marker = new LastProjectLoadMarker(() => _folder);

        marker.MarkStarted("c:/projects/Game.gumx");

        marker.InterruptedProject.ShouldBe("c:/projects/Game.gumx");
    }
}
