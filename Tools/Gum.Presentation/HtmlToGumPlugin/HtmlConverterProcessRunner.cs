using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HtmlToGumPlugin;

/// <summary>
/// Finds Node.js and runs the HTML converter's child processes (npm install, the converter
/// itself) for Content > Import > HTML.
/// </summary>
public interface IHtmlConverterProcessRunner
{
    /// <summary>Whether <c>node</c> runs; <paramref name="hint"/> says what was found or why not.</summary>
    bool TryFindNode(out string nodePath, out string hint);

    /// <summary>
    /// Runs <paramref name="fileName"/> to completion, reporting each short line of its output to
    /// <paramref name="progress"/>. Returns its exit code and everything it wrote.
    /// </summary>
    Task<(int exitCode, string stdout, string stderr)> RunAsync(
        string fileName, string arguments, string workingDirectory, IProgress<string> progress);
}

/// <inheritdoc cref="IHtmlConverterProcessRunner"/>
public class HtmlConverterProcessRunner : IHtmlConverterProcessRunner
{
    private string? _nodeDirectory;

    /// <inheritdoc/>
    public bool TryFindNode(out string nodePath, out string hint)
    {
        // Plain "node" first (PATH). A Finder-launched app on macOS has a minimal PATH, so fall
        // back to probing known install locations and the login shell.
        if (TryRunNode("node", out hint))
        {
            nodePath = "node";
            _nodeDirectory = null;
            return true;
        }

        string firstHint = hint;
        string? resolved = new NodeResolver(new NodeSearchEnvironment()).Resolve();
        if (resolved != null && TryRunNode(resolved, out hint))
        {
            nodePath = resolved;
            _nodeDirectory = Path.GetDirectoryName(resolved);
            return true;
        }

        nodePath = "node";
        hint = firstHint;
        return false;
    }

    private static bool TryRunNode(string fileName, out string hint)
    {
        hint = "";
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = "-v",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using Process? proc = Process.Start(psi);
            if (proc is null)
            {
                hint = "Process.Start returned null.";
                return false;
            }
            string output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(5000);
            if (proc.ExitCode != 0)
            {
                hint = $"node -v exited {proc.ExitCode}.";
                return false;
            }
            hint = $"Found {output}";
            return true;
        }
        catch (Exception ex)
        {
            hint = ex.Message;
            return false;
        }
    }

    /// <inheritdoc/>
    public Task<(int exitCode, string stdout, string stderr)> RunAsync(
        string fileName, string arguments, string workingDirectory, IProgress<string> progress)
    {
        return Task.Run(() =>
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            if (_nodeDirectory != null)
            {
                // npm and the converter's postinstall find node through PATH.
                psi.Environment["PATH"] = _nodeDirectory + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
            }

            using Process proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start node.");

            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();
            proc.OutputDataReceived += (_, ev) =>
            {
                if (ev.Data is null)
                {
                    return;
                }
                stdout.AppendLine(ev.Data);
                string line = ev.Data.Trim();
                if (line.Length > 0 && line.Length < 120)
                {
                    progress.Report(line);
                }
            };
            proc.ErrorDataReceived += (_, ev) =>
            {
                if (ev.Data is null)
                {
                    return;
                }
                stderr.AppendLine(ev.Data);
            };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            return (proc.ExitCode, stdout.ToString(), stderr.ToString());
        });
    }
}
