using System.Diagnostics;
using System.Runtime.InteropServices;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The full-startup layer: the real head executable, run unattended on a copy of a fixture project,
/// goes through the whole startup sequence (settings, plugins, canvas, project load, selection) and
/// exits cleanly. Composition tests prove the container resolves; only a real run proves the order.
/// </summary>
/// <remarks>
/// Needs a display and a GL driver for the canvas, so it is skipped on headless machines and on CI
/// runners (which have no GPU; the CI job would need Mesa's software GL, as the raylib job has).
/// </remarks>
public class HeadProcessTests
{
    private static bool CanRunTheHead =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) &&
        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
         RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
         !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) ||
         !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")));

    [SkippableFact]
    public async Task UnattendedRun_LoadsAProject_AndExitsCleanly()
    {
        Skip.IfNot(CanRunTheHead, "needs a display and a GL driver, and is not run on CI");

        string repositoryRoot = FindRepositoryRoot();
        string configuration = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).Parent!.Name;
        string head = Path.Combine(repositoryRoot, "Tool", "Gum.Avalonia", "bin", configuration, "net10.0", "Gum.Avalonia.dll");
        File.Exists(head).ShouldBeTrue(head);

        string workingDirectory = Path.Combine(Path.GetTempPath(), "GumHeadRun_" + Guid.NewGuid().ToString("N"));
        CopyDirectory(Path.Combine(repositoryRoot, "Tests", "CodeGen_Skia_ByReference", "Content", "GumProject"), workingDirectory);
        string project = Directory.GetFiles(workingDirectory, "*.gumx").Single();
        string screenshot = Path.Combine(workingDirectory, "run.png");

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (string argument in new[] { head, project, "--exit-after", "10", "--screenshot", screenshot })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using Process process = Process.Start(startInfo)!;
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> errors = process.StandardError.ReadToEndAsync();
            using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("The head did not exit within two minutes; a modal dialog may be blocking it.");
            }

            string errorText = await errors;
            await output;
            process.ExitCode.ShouldBe(0, errorText);
            errorText.ShouldNotContain("Startup failed");
            File.Exists(screenshot).ShouldBeTrue();
        }
        finally
        {
            try { Directory.Delete(workingDirectory, recursive: true); } catch { }
        }
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return directory.FullName;
            }
        }
        throw new InvalidOperationException("The test is not running inside the Gum repository.");
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }
}
