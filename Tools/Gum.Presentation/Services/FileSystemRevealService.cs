using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Gum.Services;

/// <inheritdoc cref="IFileSystemRevealService"/>
public class FileSystemRevealService : IFileSystemRevealService
{
    /// <inheritdoc/>
    public void RevealFile(string filePath)
    {
        (string fileName, string arguments) = BuildRevealCommand(filePath, CurrentPlatform());
        Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = false });
    }

    /// <inheritdoc/>
    public void OpenFolder(string folderPath) => ShellOpen(folderPath);

    /// <inheritdoc/>
    public void OpenFile(string filePath) => ShellOpen(filePath);

    /// <inheritdoc/>
    public void OpenUrl(string url) => ShellOpen(url);

    /// <summary>
    /// The file-manager command that selects <paramref name="filePath"/> on <paramref name="platform"/>.
    /// Linux file managers have no portable "select" verb, so it opens the containing folder.
    /// Pure, so it is unit-testable on any OS.
    /// </summary>
    public static (string fileName, string arguments) BuildRevealCommand(string filePath, OSPlatform platform)
    {
        string fullPath = Path.GetFullPath(filePath);
        if (platform == OSPlatform.Windows)
        {
            return ("explorer.exe", $"/select,\"{fullPath.Replace('/', '\\')}\"");
        }

        if (platform == OSPlatform.OSX)
        {
            return ("open", $"-R \"{fullPath}\"");
        }

        string folder = Path.GetDirectoryName(fullPath) ?? fullPath;
        return ("xdg-open", $"\"{folder}\"");
    }

    private static void ShellOpen(string target)
    {
        // With UseShellExecute, .NET maps this to ShellExecute on Windows and to open / xdg-open
        // on macOS / Linux, for files, folders, and URLs alike.
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    private static OSPlatform CurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return OSPlatform.Windows;
        }

        if (OperatingSystem.IsMacOS())
        {
            return OSPlatform.OSX;
        }

        return OSPlatform.Linux;
    }
}
