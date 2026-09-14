using System;
using System.Diagnostics;
using System.IO;
using Gum.Managers;
using ToolsUtilities;

namespace Gum.Avalonia.Services;

/// <summary>
/// Moves a file to the OS trash: the shell recycle bin on Windows, Finder's trash on macOS, and
/// the freedesktop trash through <c>gio</c> on Linux. Never deletes permanently on its own; a
/// platform without a trash command raises so the caller can decide.
/// </summary>
public class AvaloniaRecycleBinService : IRecycleBinService
{
    /// <inheritdoc/>
    public void MoveToRecycleBin(FilePath filePath)
    {
        string fullPath = filePath.FullPath;
        if (OperatingSystem.IsWindows())
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(fullPath,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            return;
        }

        (string fileName, string arguments) = OperatingSystem.IsMacOS()
            ? ("osascript", $"-e \"tell application \\\"Finder\\\" to delete POSIX file \\\"{fullPath}\\\"\"")
            : ("gio", $"trash \"{fullPath}\"");

        using Process? process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        });
        if (process == null)
        {
            throw new IOException($"Could not start {fileName} to move {fullPath} to the trash.");
        }
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new IOException($"{fileName} could not move {fullPath} to the trash: {process.StandardError.ReadToEnd()}");
        }
    }
}
