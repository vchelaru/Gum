using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HtmlToGumPlugin;

/// <summary>What <see cref="NodeResolver"/> needs from the machine; a test replaces it.</summary>
public interface INodeSearchEnvironment
{
    bool IsWindows { get; }
    string HomeDirectory { get; }
    string? PathVariable { get; }
    bool FileExists(string path);
    IEnumerable<string> GetSubdirectoryNames(string directory);

    /// <summary>
    /// Runs <c>command</c> in the user's login shell and returns its trimmed stdout, or null on
    /// failure or timeout. A login shell reads the profile files that put nvm/fnm/asdf node on PATH.
    /// </summary>
    string? RunLoginShell(string command);
}

/// <summary>
/// Finds the full path of <c>node</c> for an app launched from Finder, where launchd gives it a
/// minimal PATH (<c>/usr/bin:/bin:/usr/sbin:/sbin</c>) that excludes Homebrew, nvm and friends.
/// </summary>
public class NodeResolver
{
    private readonly INodeSearchEnvironment _environment;

    public NodeResolver(INodeSearchEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// The full path to node, or null when none is found. Looks on PATH, then at common install
    /// locations, then asks the login shell (non-Windows only).
    /// </summary>
    public string? Resolve()
    {
        string nodeName = _environment.IsWindows ? "node.exe" : "node";

        foreach (string directory in GetCandidateDirectories())
        {
            string candidate = Path.Combine(directory, nodeName);
            if (_environment.FileExists(candidate))
            {
                return candidate;
            }
        }

        if (_environment.IsWindows)
        {
            return null;
        }

        string? shellOutput = _environment.RunLoginShell("command -v node");
        string? lastLine = shellOutput?
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();
        return lastLine != null && _environment.FileExists(lastLine) ? lastLine : null;
    }

    private IEnumerable<string> GetCandidateDirectories()
    {
        if (!string.IsNullOrEmpty(_environment.PathVariable))
        {
            foreach (string entry in _environment.PathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                yield return entry;
            }
        }

        if (_environment.IsWindows)
        {
            yield break;
        }

        yield return "/opt/homebrew/bin";
        yield return "/usr/local/bin";
        yield return "/usr/bin";
        yield return Path.Combine(_environment.HomeDirectory, ".volta", "bin");

        string nvmVersions = Path.Combine(_environment.HomeDirectory, ".nvm", "versions", "node");
        foreach (string version in _environment.GetSubdirectoryNames(nvmVersions)
                     .OrderByDescending(ParseVersion))
        {
            yield return Path.Combine(nvmVersions, version, "bin");
        }
    }

    private static Version ParseVersion(string directoryName) =>
        Version.TryParse(directoryName.TrimStart('v'), out Version? version) ? version : new Version(0, 0);
}

/// <summary>The real machine: PATH, the file system, and a login shell.</summary>
public class NodeSearchEnvironment : INodeSearchEnvironment
{
    private const int LoginShellTimeoutMilliseconds = 5000;

    public bool IsWindows => OperatingSystem.IsWindows();

    public string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public string? PathVariable => Environment.GetEnvironmentVariable("PATH");

    public bool FileExists(string path) => File.Exists(path);

    public IEnumerable<string> GetSubdirectoryNames(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateDirectories(directory).Select(d => Path.GetFileName(d))
            : Enumerable.Empty<string>();

    public string? RunLoginShell(string command)
    {
        try
        {
            string shell = Environment.GetEnvironmentVariable("SHELL") is { Length: > 0 } configured
                ? configured
                : "/bin/zsh";
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = shell,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-l");
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(command);
            using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }
            process.StandardInput.Close();
            // Read stdout first on a task so a profile that never exits cannot block past the timeout.
            System.Threading.Tasks.Task<string> output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(LoginShellTimeoutMilliseconds))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
                return null;
            }
            return process.ExitCode == 0 ? output.GetAwaiter().GetResult().Trim() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
