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

    // pwsh is installed on the GitHub ubuntu, macOS and Windows runners; skipped where it is not.
    [SkippableFact]
    public async Task RestoreAsync_CanceledWhileRunning_KillsTheProcessAndReturnsCanceled()
    {
        Skip.IfNot(IsPwshOnPath(), "pwsh is not installed");
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

    [SkippableFact]
    public async Task RestoreAsync_CanceledWhileRunning_KillsTheChildProcessTooNotJustTheParent()
    {
        Skip.IfNot(IsPwshOnPath(), "pwsh is not installed");
        string folder = Path.Combine(Path.GetTempPath(), "GumRestoreTree_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string pidFile = Path.Combine(folder, "child.pid");
            string childScript = Path.Combine(folder, "child.ps1");
            string parentScript = Path.Combine(folder, "parent.ps1");
            File.WriteAllText(childScript, $"$PID | Set-Content '{pidFile}'; Start-Sleep -Seconds 60");
            File.WriteAllText(parentScript, $"& pwsh -NoProfile -File '{childScript}'");
            DotnetRestoreRunner runner = new DotnetRestoreRunner(_ =>
            {
                ProcessStartInfo info = new ProcessStartInfo("pwsh");
                info.ArgumentList.Add("-NoProfile");
                info.ArgumentList.Add("-File");
                info.ArgumentList.Add(parentScript);
                return info;
            });
            using CancellationTokenSource cancellation = new CancellationTokenSource();

            Task<RestoreResult> restore = runner.RestoreAsync("x.csproj", null, cancellation.Token);
            for (int i = 0; i < 100 && !File.Exists(pidFile); i++)
            {
                await Task.Delay(200);
            }
            File.Exists(pidFile).ShouldBeTrue("the grandchild process never started");
            string pidText = "";
            for (int i = 0; i < 20 && pidText.Trim().Length == 0; i++)
            {
                await Task.Delay(100);
                pidText = File.ReadAllText(pidFile);
            }
            int childPid = int.Parse(pidText.Trim());

            cancellation.Cancel();
            RestoreResult result = await restore;

            result.Status.ShouldBe(RestoreStatus.Canceled);
            bool gone = false;
            for (int i = 0; i < 50 && !gone; i++)
            {
                try
                {
                    using Process child = Process.GetProcessById(childPid);
                    gone = child.HasExited;
                }
                catch (ArgumentException)
                {
                    gone = true;
                }

                if (!gone)
                {
                    await Task.Delay(100);
                }
            }
            gone.ShouldBeTrue("the child process survived the cancel");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static bool IsPwshOnPath()
    {
        string executable = OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh";
        return (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(directory => File.Exists(Path.Combine(directory, executable)));
    }
}
