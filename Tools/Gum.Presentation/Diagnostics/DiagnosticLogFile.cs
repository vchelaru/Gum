using System;
using System.IO;

namespace Gum.Diagnostics;

/// <summary>
/// Shared by <c>ToolLog</c> (the tool) and <c>PreviewDebugLog</c> (Preview): decides where a
/// diagnostic log goes and opens it. In a Debug build the log is always on, at a fixed path, so
/// after any error "read the log" works without having enabled anything first; a Release build is
/// off unless a path is given. An explicit path (<c>--log</c>, <c>GUM_PREVIEW_LOG</c>,
/// <c>GUM_TOOL_LOG</c>) overrides the fixed one in either build. Opening a log moves the previous
/// run's file to <c>{name}.previous{ext}</c> (one generation kept), so the file never grows past one run.
/// Compiled into each assembly that uses it, so <see cref="IsDebugBuild"/> is that assembly's own configuration.
/// </summary>
public static class DiagnosticLogFile
{
#if DEBUG
    /// <summary>True when this assembly was built as Debug, which turns the log on by default.</summary>
    public const bool IsDebugBuild = true;
#else
    /// <summary>True when this assembly was built as Debug, which turns the log on by default.</summary>
    public const bool IsDebugBuild = false;
#endif

    /// <summary>The path to log to: <paramref name="explicitPath"/> if given, else <paramref name="defaultPath"/> in a Debug build, else null (off).</summary>
    public static string? ResolvePath(string? explicitPath, string defaultPath, bool isDebugBuild)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return explicitPath;
        }
        return isDebugBuild ? defaultPath : null;
    }

    /// <summary>The previous-run file for <paramref name="path"/>: <c>preview.log</c> becomes <c>preview.previous.log</c>.</summary>
    public static string PreviousPath(string path) =>
        Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path) + ".previous" + Path.GetExtension(path));

    /// <summary>
    /// Creates the folder, moves an existing log to <see cref="PreviousPath"/> (best effort: another
    /// running process may hold it, in which case the file is appended to), and opens the log for
    /// writing with shared read. Throws on failure; callers catch, since a log must never stop the app.
    /// </summary>
    public static FileStream Open(string path, bool rotate = true)
    {
        bool append = !rotate || !Rotate(path);
        return new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
    }

    /// <summary>
    /// Creates the folder and moves an existing log to <see cref="PreviousPath"/>. Returns false when
    /// the move failed (a process holds the file), so the caller should append instead. Never throws
    /// for a bad path other than by returning false.
    /// </summary>
    public static bool Rotate(string path)
    {
        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (File.Exists(path))
            {
                File.Move(path, PreviousPath(path), overwrite: true);
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
