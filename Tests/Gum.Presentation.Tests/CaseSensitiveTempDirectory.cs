using System;
using System.Diagnostics;
using System.IO;

namespace Gum.Presentation.Tests;

/// <summary>
/// A temp directory whose entries are case-sensitive, so <c>Foo</c> and <c>foo</c> can both exist:
/// natively on Linux, and on Windows through <c>fsutil file setCaseSensitiveInfo</c>.
/// </summary>
internal static class CaseSensitiveTempDirectory
{
    /// <summary>
    /// Creates the directory, or returns null where the file system can't be made case-sensitive
    /// (a default macOS volume). A test that needs one returns early on null.
    /// </summary>
    public static string? TryCreate()
    {
        string path = Path.Combine(Path.GetTempPath(), "GumCaseSensitive_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);

        if (OperatingSystem.IsWindows())
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("fsutil", $"file setCaseSensitiveInfo \"{path}\" enable")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using Process? process = Process.Start(startInfo);
            process?.WaitForExit();
        }

        string probe = Path.Combine(path, "probe");
        Directory.CreateDirectory(probe);
        bool isCaseSensitive = !Directory.Exists(Path.Combine(path, "PROBE"));
        Directory.Delete(probe);

        if (!isCaseSensitive)
        {
            Directory.Delete(path);
            return null;
        }
        return path;
    }
}
