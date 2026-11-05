using System;
using System.Collections.Generic;
using System.Globalization;

namespace GumPreview;

/// <summary>
/// Pure formatting and change detection for the <c>gamepad</c> log lines, so the Preview host only reads
/// the devices and the logic that decides what to write is testable without one. Only used with <c>--log</c>.
/// </summary>
public sealed class PreviewGamepadLog
{
    private readonly Dictionary<string, string> _last = new();

    /// <summary>
    /// Returns <paramref name="text"/> the first time <paramref name="key"/> is seen and whenever it differs
    /// from the previous call with that key; null while it is unchanged (so a held button does not spam).
    /// </summary>
    public string? Changed(string key, string text)
    {
        if (_last.TryGetValue(key, out string? previous) && previous == text)
        {
            return null;
        }
        _last[key] = text;
        return text;
    }

    /// <summary>A stick as "0.52,-0.31", or null while it is inside <paramref name="deadZone"/> on both axes.</summary>
    public static string? FormatStick(float x, float y, float deadZone = 0.15f)
    {
        if (Math.Abs(x) <= deadZone && Math.Abs(y) <= deadZone)
        {
            return null;
        }
        return $"{Round(x)},{Round(y)}";
    }

    /// <summary>A trigger as "0.80", or null while it is at rest (0).</summary>
    public static string? FormatTrigger(float value) => Math.Abs(value) < 0.005f ? null : Round(value);

    /// <summary>
    /// One raw-state line body: connection, packet number, every pressed button, then the sticks and triggers
    /// that are away from rest. Same inputs always give the same text, so it works as a change key.
    /// </summary>
    public static string FormatRaw(bool connected, int packetNumber, IReadOnlyList<string> pressed,
        float leftX, float leftY, float rightX, float rightY, float leftTrigger, float rightTrigger)
    {
        List<string> parts = new() { $"connected={connected}", $"pressed=[{string.Join(",", pressed)}]" };
        string? left = FormatStick(leftX, leftY);
        string? right = FormatStick(rightX, rightY);
        string? lt = FormatTrigger(leftTrigger);
        string? rt = FormatTrigger(rightTrigger);
        if (left != null) parts.Add($"leftStick={left}");
        if (right != null) parts.Add($"rightStick={right}");
        if (lt != null) parts.Add($"leftTrigger={lt}");
        if (rt != null) parts.Add($"rightTrigger={rt}");
        parts.Add($"packet={packetNumber}");
        return string.Join(" ", parts);
    }

    /// <summary>One Gum-side button line body: the four per-frame answers Forms navigation reads.</summary>
    public static string FormatGumButton(bool down, bool pushed, bool released, bool repeat) =>
        $"down={down} pushed={pushed} released={released} repeat={repeat}";

    private static string Round(float value) => Math.Round(value, 2).ToString("0.00", CultureInfo.InvariantCulture);
}
