using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gum;

namespace GumPreview;

/// <summary>
/// Opt-in diagnostic log for Preview (<c>--log &lt;path&gt;</c>): timestamped lines in a file, never the
/// console or the window, so a person or an agent can read what a live edit or an input did.
/// Preview holds a null <see cref="PreviewDebugLog"/> when the option is absent, so an off log costs a
/// null check and creates no file.
/// </summary>
public sealed class PreviewDebugLog : IDisposable
{
    private readonly StreamWriter _writer;

    private PreviewDebugLog(StreamWriter writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Opens (appends to) the log file at <paramref name="path"/>, creating its folder. Returns null
    /// when <paramref name="path"/> is null or the file cannot be opened, so logging never stops Preview.
    /// </summary>
    public static PreviewDebugLog? Open(string? path)
    {
        if (path == null)
        {
            return null;
        }

        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            // Shared read so the file can be read while Preview runs; AutoFlush so a crash loses nothing.
            FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            PreviewDebugLog log = new PreviewDebugLog(new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true });
            log.Write("log", "started");
            return log;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"GumPreview: could not open the log {path}: {exception.Message}");
            return null;
        }
    }

    /// <summary>Appends <c>{local time} {category} {message}</c>.</summary>
    public void Write(string category, string message)
    {
        try
        {
            lock (_writer)
            {
                _writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {category} {message}");
            }
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            // A full disk or a closed log must not take Preview down.
        }
    }

    /// <summary>A finished hot reload and each issue it reported.</summary>
    public void LogReload(HotReloadResult result)
    {
        Write("reload", $"finished, {result.Issues.Count} issue(s), matches restart: {result.MatchesRestart}");
        foreach (HotReloadIssue issue in result.Issues)
        {
            Write("reload", $"{issue.Kind} {issue.ElementName}/{issue.InstanceName}: {issue.Message}");
        }
    }

    /// <summary>The control that now has keyboard focus, or null when none does.</summary>
    public void LogFocus(string? controlName) => Write("focus", controlName ?? "(none)");

    /// <summary>Keys typed this frame and the modifiers held, as forwarded to the focused control.</summary>
    public void LogKeys(IEnumerable<string> keys, string modifiers, string? focusedControl) =>
        Write("key", $"{string.Join(",", keys)} modifiers={modifiers} focused={focusedControl ?? "(none)"}");

    /// <summary>An exception, with where it happened.</summary>
    public void LogException(string context, Exception exception) =>
        Write("error", $"{context}: {exception}");

    public void Dispose() => _writer.Dispose();
}
