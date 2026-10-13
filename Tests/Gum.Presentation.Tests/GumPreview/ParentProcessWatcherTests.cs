using System;
using System.Diagnostics;
using System.Threading;
using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

public class ParentProcessWatcherTests
{
    private static readonly TimeSpan Generous = TimeSpan.FromSeconds(30);

    [Fact]
    public void Start_WhenTheParentExits_RaisesParentExited()
    {
        using Process parent = StartLongRunningProcess();
        using ManualResetEventSlim exited = new ManualResetEventSlim();
        ParentProcessWatcher.Start(parent.Id, exited.Set);

        exited.Wait(TimeSpan.FromMilliseconds(300)).ShouldBeFalse("the parent is still running");
        parent.Kill();

        exited.Wait(Generous).ShouldBeTrue();
    }

    [Fact]
    public void Start_WhenTheParentIsAlreadyGone_RaisesParentExited()
    {
        using Process parent = StartLongRunningProcess();
        int parentId = parent.Id;
        parent.Kill();
        parent.WaitForExit();
        using ManualResetEventSlim exited = new ManualResetEventSlim();

        ParentProcessWatcher.Start(parentId, exited.Set);

        exited.Wait(Generous).ShouldBeTrue();
    }

    [Fact]
    public void Start_WhileTheParentRuns_DoesNotRaiseParentExited()
    {
        using ManualResetEventSlim exited = new ManualResetEventSlim();

        ParentProcessWatcher.Start(Environment.ProcessId, exited.Set);

        exited.Wait(TimeSpan.FromMilliseconds(500)).ShouldBeFalse();
    }

    // A process that stays up for a minute unless the test kills it.
    private static Process StartLongRunningProcess()
    {
        ProcessStartInfo startInfo = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("ping", "-n 60 127.0.0.1")
            : new ProcessStartInfo("sleep", "60");
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        return Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start a stand-in parent process.");
    }
}
