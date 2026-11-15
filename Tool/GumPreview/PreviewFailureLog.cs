using System;
using System.IO;

namespace GumPreview;

/// <summary>
/// Turns a failure inside Preview's frame loop into one stderr line instead of an unhandled
/// exception, and says each distinct failure once so a failing poll doesn't print every frame.
/// </summary>
public sealed class PreviewFailureLog
{
    private string? _lastMessage;

    /// <summary>The one-line text for a project that could not be loaded.</summary>
    public static string LoadMessage(string projectPath, Exception exception) =>
        $"could not load {projectPath}: {FirstLine(exception)}";

    /// <summary>
    /// Runs <paramref name="action"/>; an exception is written to <paramref name="errors"/> as
    /// <c>GumPreview: {context}: {reason}</c> unless it repeats the previous failure, and is not rethrown.
    /// A success makes the next failure report again. Returns whether the action succeeded.
    /// </summary>
    public bool TryRun(Action action, string context, TextWriter errors, PreviewDebugLog? debugLog = null)
    {
        try
        {
            action();
            _lastMessage = null;
            return true;
        }
        catch (Exception exception)
        {
            debugLog?.LogException(context, exception);
            string message = $"{context}: {FirstLine(exception)}";
            if (message != _lastMessage)
            {
                _lastMessage = message;
                errors.WriteLine($"GumPreview: {message}");
                errors.Flush();
            }
            return false;
        }
    }

    private static string FirstLine(Exception exception)
    {
        string text = exception.Message.Trim();
        if (exception.InnerException != null)
        {
            text = $"{text.TrimEnd('.')}: {exception.InnerException.Message.Trim()}";
        }
        int newLine = text.IndexOfAny(new[] { '\r', '\n' });
        return newLine < 0 ? text : text.Substring(0, newLine);
    }
}
