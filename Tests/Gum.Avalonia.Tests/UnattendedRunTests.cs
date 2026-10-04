using Gum.Avalonia.Diagnostics;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The unattended (<c>--exit-after</c>) run's order (#5170): the screenshot waits for the project
/// load and a canvas frame, and a run that isn't ready by the deadline fails instead of capturing.
/// </summary>
public class UnattendedRunTests
{
    private sealed class Probe
    {
        public TaskCompletionSource<UnattendedStartupOutcome> Startup { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Deadline { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<TaskCompletionSource> FrameRequests { get; } = new();
        public List<string> Events { get; } = new();
        public StringWriter Output { get; } = new();
        public StringWriter Error { get; } = new();

        public Task<int?> Run(bool zoomToFit = false, bool screenshot = true, Func<string?>? describeCanvas = null) =>
            UnattendedRun.RunAsync(
                Startup.Task,
                Deadline.Task,
                exitAfterSeconds: 8,
                nextCanvasFrame: () =>
                {
                    Events.Add("frame requested");
                    TaskCompletionSource frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    FrameRequests.Add(frame);
                    return frame.Task;
                },
                zoomToFit: zoomToFit ? () => { Events.Add("zoom"); return "zoomed"; } : null,
                capture: screenshot ? () => Events.Add("capture") : null,
                Output,
                Error,
                describeCanvas);

        public void PresentFrame() => FrameRequests[^1].SetResult();
    }

    [Fact]
    public async Task Capture_WaitsForStartup_ThenForACanvasFrame()
    {
        Probe probe = new Probe();

        Task<int?> run = probe.Run();
        await Task.Yield();
        probe.Events.ShouldBeEmpty("nothing happens before the project has loaded");

        probe.Startup.SetResult(UnattendedStartupOutcome.Ready);
        await WaitUntil(() => probe.FrameRequests.Count == 1);
        probe.Events.ShouldNotContain("capture");

        probe.PresentFrame();

        (await run.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
        probe.Events.ShouldBe(new[] { "frame requested", "capture" });
    }

    [Fact]
    public async Task ZoomToFit_RunsBetweenTwoFrames_AndReportsTheFit()
    {
        Probe probe = new Probe();

        Task<int?> run = probe.Run(zoomToFit: true);
        probe.Startup.SetResult(UnattendedStartupOutcome.Ready);
        await WaitUntil(() => probe.FrameRequests.Count == 1);
        probe.PresentFrame();
        await WaitUntil(() => probe.FrameRequests.Count == 2);
        probe.Events.ShouldNotContain("capture", "the fitted camera has not been drawn yet");
        probe.PresentFrame();

        (await run.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
        // The first frame makes the camera's view size current before the fit reads it.
        probe.Events.ShouldBe(new[] { "frame requested", "zoom", "frame requested", "capture" });
        probe.Output.ToString().ShouldContain("zoomed");
    }

    [Fact]
    public async Task DeadlineBeforeTheProjectLoads_FailsWithoutCapturing()
    {
        Probe probe = new Probe();

        Task<int?> run = probe.Run();
        probe.Deadline.SetResult();

        (await run.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(1);
        probe.Error.ToString().ShouldContain("--exit-after");
        probe.Startup.SetResult(UnattendedStartupOutcome.Ready);
        await Task.Delay(50);
        probe.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeadlineBeforeTheCanvasDraws_FailsWithoutCapturing()
    {
        Probe probe = new Probe();

        Task<int?> run = probe.Run();
        probe.Startup.SetResult(UnattendedStartupOutcome.Ready);
        await WaitUntil(() => probe.FrameRequests.Count == 1);
        probe.Deadline.SetResult();

        (await run.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(1);
        probe.Error.ToString().ShouldContain("frame");
        probe.Events.ShouldNotContain("capture");
    }

    [Fact]
    public async Task DeadlineBeforeTheCanvasDraws_ReportsTheCanvasState()
    {
        Probe probe = new Probe();

        Task<int?> run = probe.Run(describeCanvas: () => "ticks=7");
        probe.Startup.SetResult(UnattendedStartupOutcome.Ready);
        await WaitUntil(() => probe.FrameRequests.Count == 1);
        probe.Deadline.SetResult();

        (await run.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(1);
        probe.Error.ToString().ShouldContain("Canvas state: ticks=7");
    }

    [Fact]
    public async Task FailedStartup_FailsWithoutWaitingForAFrame()
    {
        Probe probe = new Probe();

        Task<int?> run = probe.Run();
        probe.Startup.SetResult(UnattendedStartupOutcome.Failed);

        (await run.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(1);
        probe.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task StartupThatAlreadyAskedToExit_IsLeftAlone()
    {
        Probe probe = new Probe();

        Task<int?> run = probe.Run();
        probe.Startup.SetResult(UnattendedStartupOutcome.ExitRequested);

        (await run.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBeNull();
        probe.Events.ShouldBeEmpty();
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            await Task.Delay(5, timeout.Token);
        }
    }
}
