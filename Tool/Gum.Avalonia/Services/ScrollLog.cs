using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Input;

namespace Gum.Avalonia.Services;

/// <summary>
/// Appends each canvas wheel event's delta, modifiers and classified source to
/// <c>gum-scroll.log</c> in the temp folder when the <see cref="EnvironmentVariable"/> is set,
/// for tuning touchpad detection on machines the developers don't have.
/// </summary>
public static class ScrollLog
{
    /// <summary>Set to <c>1</c> to turn the log on.</summary>
    public const string EnvironmentVariable = "GUM_LOG_SCROLL";

    private static readonly string? _path =
        Environment.GetEnvironmentVariable(EnvironmentVariable) == "1"
            ? Path.Combine(Path.GetTempPath(), "gum-scroll.log")
            : null;

    /// <summary>Writes one event's line, or does nothing when the log is off.</summary>
    public static void Write(Vector delta, KeyModifiers modifiers, WheelSource source)
    {
        if (_path == null)
        {
            return;
        }
        string line = string.Create(CultureInfo.InvariantCulture,
            $"{Environment.TickCount64}\t{delta.X:R}\t{delta.Y:R}\t{modifiers}\t{source}{Environment.NewLine}");
        try
        {
            File.AppendAllText(_path, line);
        }
        catch (IOException)
        {
        }
    }
}
