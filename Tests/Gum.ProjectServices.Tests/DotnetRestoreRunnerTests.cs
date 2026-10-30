using System.Diagnostics;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class DotnetRestoreRunnerTests
{
    [Fact]
    public async Task RestoreAsync_ProcessThatDoesNotExist_ReportsProcessNotFound()
    {
        DotnetRestoreRunner runner = new DotnetRestoreRunner(_ => new ProcessStartInfo("gum-no-such-executable-7208"));

        RestoreResult result = await runner.RestoreAsync("x.csproj", null, CancellationToken.None);

        result.Status.ShouldBe(RestoreStatus.ProcessNotFound);
        result.ErrorMessage.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task RestoreAsync_AlreadyCanceled_DoesNotStartTheProcess()
    {
        DotnetRestoreRunner runner = new DotnetRestoreRunner(_ => throw new InvalidOperationException("must not start"));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        RestoreResult result = await runner.RestoreAsync("x.csproj", null, cancellation.Token);

        result.Status.ShouldBe(RestoreStatus.Canceled);
    }

    [Fact]
    public async Task RestoreAsync_NonZeroExit_ReportsFailureWithExitCodeAndOutputTail()
    {
        DotnetRestoreRunner runner = new DotnetRestoreRunner(path =>
        {
            ProcessStartInfo info = new ProcessStartInfo("dotnet");
            info.ArgumentList.Add("restore");
            info.ArgumentList.Add(path);
            return info;
        });
        List<string> lines = new List<string>();

        // No such project file: dotnet fails at once without touching the network.
        RestoreResult result = await runner.RestoreAsync(
            Path.Combine(Path.GetTempPath(), "gum-7208-missing", "Missing.csproj"), lines.Add, CancellationToken.None);

        result.Status.ShouldBe(RestoreStatus.Failed);
        result.ExitCode.ShouldNotBe(0);
        result.OutputTail.ShouldNotBeEmpty();
        lines.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task RestoreAsync_CanceledWhileRunning_KillsTheProcessAndReturnsCanceled()
    {
        DotnetRestoreRunner runner = new DotnetRestoreRunner(_ =>
        {
            ProcessStartInfo info = new ProcessStartInfo("pwsh");
            info.ArgumentList.Add("-NoProfile");
            info.ArgumentList.Add("-Command");
            info.ArgumentList.Add("Start-Sleep -Seconds 60");
            return info;
        });
        using CancellationTokenSource cancellation = new CancellationTokenSource();

        Stopwatch stopwatch = Stopwatch.StartNew();
        Task<RestoreResult> restore = runner.RestoreAsync("x.csproj", null, cancellation.Token);
        await Task.Delay(1500);
        cancellation.Cancel();
        RestoreResult result = await restore;

        result.Status.ShouldBe(RestoreStatus.Canceled);
        stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(30);
    }
}
