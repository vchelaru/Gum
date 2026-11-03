using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gum;
using Gum.Diagnostics;

namespace GumPreview;

/// <summary>
/// Diagnostic log for Preview: timestamped lines in a file, never the console or the window, so a
/// person or an agent can read what a live edit or an input did.
/// <para>
/// WHERE: the published Preview the tool launches is a Release build, so the tool decides: a Debug
/// tool passes <c>--log</c> with <see cref="DefaultPath"/> on every launch (the tool rotates the file once
/// per session; Preview appends to a path it is given). A Preview built as Debug and run by hand also
/// defaults to it, rotating. Either way the file is always on in Debug, at <see cref="DefaultPath"/>,
/// <c>{temp folder}/GumPreview/preview.log</c> (Windows: <c>%TEMP%\GumPreview\preview.log</c>); a Release
/// build is off. <c>--log &lt;path&gt;</c> (or <c>GUM_PREVIEW_LOG</c> via the tool) writes there instead, in
/// either build; <c>--log-default</c> is accepted and means <see cref="DefaultPath"/>. Each run moves the
/// previous file to <c>preview.previous.log</c> and starts a fresh <c>preview.log</c>, flushed per line,
/// so the file is current while Preview runs or after a crash. Unhandled exceptions are logged with
/// their stack before the process ends.
/// </para>
/// <para>
/// HOW TO READ IT: lines are <c>{HH:mm:ss.fff} {category} {message}</c>, oldest first. Categories:
/// <c>log</c> (run start); <c>tool-selection</c> (the tool changed what Preview shows: element, state);
/// <c>tool-var</c> (a variable the tool saved that hot reload applied: <c>Element/Instance Name=value</c>);
/// <c>reload</c> (a finished hot reload and its issues); <c>focus</c>, <c>key</c>, <c>mouse</c> (input
/// Preview received); <c>selection</c> (a text box's selection start/length/caret);
/// <c>selection-instances</c> (how many highlight instances a text box has and how many are visible,
/// written when text, wrap or those counts change); <c>geometry</c> (after each of those and after each
/// reload: one line per selection instance with x/y/w/h/units/parent, per wrapped line with start, length
/// and Y, the caret, the clip size and text scroll, and the cached selection Y offset next to the
/// template's); <c>selection-update</c> (the selection visuals were recomputed, and by which member); <c>error</c> (exception with stack). To follow a
/// bug, read the <c>tool-var</c>/<c>tool-selection</c> lines for what the tool sent, then the
/// <c>key</c>/<c>selection</c>/<c>selection-instances</c> lines for what Preview did with it.
/// </para>
/// Preview holds a null <see cref="PreviewDebugLog"/> when logging is off, so an off log costs a null check.
/// </summary>
public sealed class PreviewDebugLog : IDisposable
{
    private readonly StreamWriter _writer;

    /// <summary>The path Preview logs to: <paramref name="explicitPath"/> if given, else <see cref="DefaultPath"/> in a Debug build, else null (off).</summary>
    public static string? ResolvePath(string? explicitPath, bool isDebugBuild = DiagnosticLogFile.IsDebugBuild) =>
        DiagnosticLogFile.ResolvePath(explicitPath, DefaultPath, isDebugBuild);

    /// <summary>The file Debug builds and <c>--log-default</c> write to: <c>GumPreview/preview.log</c> in the user's temp folder.</summary>
    public static string DefaultPath => Path.Combine(Path.GetTempPath(), "GumPreview", "preview.log");

    private PreviewDebugLog(StreamWriter writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Opens (appends to) the log file at <paramref name="path"/>, creating its folder. Returns null
    /// when <paramref name="path"/> is null or the file cannot be opened, so logging never stops Preview.
    /// </summary>
    public static PreviewDebugLog? Open(string? path, bool rotate = true)
    {
        if (path == null)
        {
            return null;
        }

        try
        {
            // Shared read so the file can be read while Preview runs; AutoFlush flushes after every line.
            FileStream stream = DiagnosticLogFile.Open(path, rotate);
            PreviewDebugLog log = new PreviewDebugLog(new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true });
            log.Write("log", "started");
            return log;
        }
        catch (Exception)
        {
            // No console output: a log that cannot open must not change anything else.
            return null;
        }
    }

    /// <summary>The first-line description of this process: build config, version, exe, args, log path. Makes a missing or surprising log explainable.</summary>
    public static string DescribeProcess(string? logPath, string[] args) =>
        $"config={(DiagnosticLogFile.IsDebugBuild ? "Debug" : "Release")} version={typeof(PreviewDebugLog).Assembly.GetName().Version} "
        + $"exe={Environment.ProcessPath} pid={Environment.ProcessId} log={logPath} args={string.Join(" ", args)}";

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
        catch (Exception)
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

    /// <summary>A mouse press, drag step or release at a window position.</summary>
    public void LogMouse(string kind, int x, int y) => Write("mouse", $"{kind} {x},{y}");

    /// <summary>A text box's selection: where it starts, how long it is, and where the caret is.</summary>
    public void LogSelection(string control, int start, int length, int caret) =>
        Write("selection", $"{control} start={start} length={length} caret={caret}");

    /// <summary>How many selection highlight instances a text box has and how many are visible.</summary>
    public void LogSelectionInstances(string control, bool wrap, int textLength, int count, int visible) =>
        Write("selection-instances", $"{control} wrap={wrap} textLength={textLength} count={count} visible={visible}");

    /// <summary>An exception, with where it happened.</summary>
    public void LogException(string context, Exception exception) =>
        Write("error", $"{context}: {exception}");

    /// <summary>One <c>geometry</c> line per entry (selection instances, wrapped lines, caret, clip and scroll, cached Y offset) for a text box.</summary>
    public void LogGeometry(string control, IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            Write("geometry", $"{control} {line}");
        }
    }

    /// <summary>The selection visuals of a text box were recomputed; <paramref name="caller"/> is the member that asked.</summary>
    public void LogSelectionUpdate(string control, string caller) => Write("selection-update", $"{control} by {caller}");

    /// <summary>An unhandled exception object from <c>AppDomain.UnhandledException</c>, logged with its stack.</summary>
    public void LogUnhandled(object? exceptionObject) =>
        Write("error", $"unhandled: {exceptionObject}");

    /// <summary>A selection the tool sent: what Preview now shows.</summary>
    public void LogToolSelection(string element, string? category, string? state) =>
        Write("tool-selection", $"{element} category={category ?? "(none)"} state={state ?? "(default)"}");

    /// <summary>A variable value the tool saved that hot reload applied to a live visual.</summary>
    public void LogToolVariable(string element, string? instance, string variable, object? value) =>
        Write("tool-var", $"{element}/{instance ?? "(root)"} {variable}={value ?? "(null)"}");

    public void Dispose()
    {
        try
        {
            _writer.Dispose();
        }
        catch (Exception)
        {
            // Nothing to flush that matters more than the app closing.
        }
    }
}
