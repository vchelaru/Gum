using System.Diagnostics;

namespace Gum.Cli.Tests.EndToEnd;

/// <summary>
/// Runs the built gumcli as its own process, the way a script or CI job does, with no console
/// window. On Windows it runs the apphost (gumcli.exe), whose folder is the test output folder, so
/// the software OpenGL copied there for the rendering commands is the one it loads; elsewhere it
/// runs gumcli.dll through the dotnet host.
/// </summary>
internal sealed class GumCliProcess
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

    private GumCliProcess(int exitCode, string standardOutput, string standardError)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    public int ExitCode { get; }

    public string StandardOutput { get; }

    public string StandardError { get; }

    /// <summary>Both streams, for a failure message.</summary>
    public string Transcript => $"exit {ExitCode}\nstdout:\n{StandardOutput}\nstderr:\n{StandardError}";

    public static GumCliProcess Run(string workingDirectory, params string[] args)
    {
        string folder = AppContext.BaseDirectory;
        ProcessStartInfo startInfo;
        if (OperatingSystem.IsWindows())
        {
            startInfo = new ProcessStartInfo(Path.Combine(folder, "gumcli.exe"));
        }
        else
        {
            startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet");
            startInfo.ArgumentList.Add(Path.Combine(folder, "gumcli.dll"));
        }
        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }
        startInfo.WorkingDirectory = workingDirectory;
        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("gumcli did not start.");
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(Timeout))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"gumcli {string.Join(" ", args)} did not exit within {Timeout}.");
        }
        return new GumCliProcess(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }
}
