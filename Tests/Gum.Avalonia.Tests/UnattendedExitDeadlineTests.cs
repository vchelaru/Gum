using Gum.Avalonia.Diagnostics;
using Shouldly;

namespace Gum.Avalonia.Tests;

public class UnattendedExitDeadlineTests
{
    [Fact]
    public void For_AddsTheGracePeriodToTheExitAfterSeconds()
    {
        UnattendedExitDeadline.For(10).ShouldBe(TimeSpan.FromSeconds(10) + UnattendedExitDeadline.GracePeriod);
    }

    [Fact]
    public async Task Start_TerminatesOffTheBlockedThread_OnceTheDeadlinePasses()
    {
        TaskCompletionSource<string> terminated = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        using UnattendedExitDeadline deadline = UnattendedExitDeadline.Start(TimeSpan.FromMilliseconds(50), terminated.SetResult);
        // The exit timer runs on the UI thread; the deadline must fire even while that thread is stuck.
        Thread.Sleep(500);

        string message = await terminated.Task.WaitAsync(TimeSpan.FromSeconds(10));
        message.ShouldContain("did not exit");
    }

    [Fact]
    public async Task Dispose_BeforeTheDeadline_KeepsTheProcessAlive()
    {
        bool terminated = false;

        UnattendedExitDeadline deadline = UnattendedExitDeadline.Start(TimeSpan.FromMilliseconds(200), _ => terminated = true);
        deadline.Dispose();
        await Task.Delay(600);

        terminated.ShouldBeFalse();
    }
}
