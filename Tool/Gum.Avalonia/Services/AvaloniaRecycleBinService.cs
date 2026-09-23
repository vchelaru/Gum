using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gum.Managers;
using ToolsUtilities;

namespace Gum.Avalonia.Services;

/// <summary>
/// Moves files to the OS trash: the shell recycle bin on Windows, Finder's trash on macOS, and
/// the freedesktop trash through <c>gio</c> on Linux. Never deletes permanently on its own; a
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

        ProcessStartInfo command = CreateTrashCommand(
            filePaths.Select(filePath => filePath.FullPath).ToList(), OperatingSystem.IsMacOS());
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

    /// <summary>
    /// Builds the single macOS (Finder via <c>osascript</c>) or Linux (<c>gio trash</c>) command that
    /// trashes every path in <paramref name="fullPaths"/>. One Finder call for the whole batch plays the
    /// trash sound once instead of once per file.
    /// </summary>
    internal static ProcessStartInfo CreateTrashCommand(IReadOnlyList<string> fullPaths, bool isMacOS)
    {
        ProcessStartInfo command;
        if (isMacOS)
        {
            string files = string.Join(", ", fullPaths.Select(path => $"POSIX file \"{EscapeAppleScriptString(path)}\""));
            command = new ProcessStartInfo("osascript");
            command.ArgumentList.Add("-e");
            command.ArgumentList.Add($"tell application \"Finder\" to delete {{{files}}}");
        }
        else
        {
            command = new ProcessStartInfo("gio");
            command.ArgumentList.Add("trash");
            foreach (string path in fullPaths)
            {
                command.ArgumentList.Add(path);
            }
        }
        return command;
    }

    private static string EscapeAppleScriptString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
