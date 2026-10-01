using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Gum.Avalonia.Services.MacOS;
using Gum.Managers;
using ToolsUtilities;

namespace Gum.Avalonia.Services;

/// <summary>
/// Moves files to the OS trash: the shell recycle bin on Windows, <c>NSFileManager</c> on macOS,
/// and the freedesktop trash through <c>gio</c> on Linux. Never deletes permanently on its own; a
/// platform without a trash command raises so the caller can decide.
/// </summary>
public class AvaloniaRecycleBinService : IRecycleBinService
{
    /// <inheritdoc/>
    public void MoveToRecycleBin(FilePath filePath) =>
        MoveToRecycleBin(new[] { filePath });

    /// <inheritdoc/>
    public void MoveToRecycleBin(IReadOnlyList<FilePath> filePaths)
    {
        if (filePaths.Count == 0)
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            foreach (FilePath filePath in filePaths)
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(filePath.FullPath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            }
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            MoveToMacTrash(filePaths);
            return;
        }

        ProcessStartInfo command = CreateTrashCommand(filePaths.Select(filePath => filePath.FullPath).ToList());
        command.UseShellExecute = false;
        command.CreateNoWindow = true;
        command.RedirectStandardError = true;

        using Process? process = Process.Start(command);
        if (process == null)
        {
            throw new IOException($"Could not start {command.FileName} to move {filePaths.Count} file(s) to the trash.");
        }
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new IOException($"{command.FileName} could not move {filePaths.Count} file(s) to the trash: {error}");
        }
    }

    // Trashes every file even when one fails, then reports all failures together, as gio does.
    [SupportedOSPlatform("macos")]
    private static void MoveToMacTrash(IReadOnlyList<FilePath> filePaths)
    {
        List<string> failures = new List<string>();
        foreach (FilePath filePath in filePaths)
        {
            try
            {
                MacFileTrash.MoveToTrash(filePath.FullPath);
            }
            catch (IOException exception)
            {
                failures.Add(exception.Message);
            }
        }
        if (failures.Count > 0)
        {
            throw new IOException(string.Join(Environment.NewLine, failures));
        }
    }

    /// <summary>
    /// Builds the single Linux <c>gio trash</c> command that trashes every path in
    /// <paramref name="fullPaths"/>.
    /// </summary>
    internal static ProcessStartInfo CreateTrashCommand(IReadOnlyList<string> fullPaths)
    {
        ProcessStartInfo command = new ProcessStartInfo("gio");
        command.ArgumentList.Add("trash");
        foreach (string path in fullPaths)
        {
            command.ArgumentList.Add(path);
        }
        return command;
    }
}
