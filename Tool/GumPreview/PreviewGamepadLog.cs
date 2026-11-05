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

    private long _nextHeartbeatMs = long.MinValue;

    /// <summary>
    /// True on the first call and then once per <paramref name="intervalMs"/>, so a quiet log can be told
    /// apart from a logger that stopped running.
    /// </summary>
    public bool HeartbeatDue(long nowMs, long intervalMs = 5000)
    {
        if (nowMs < _nextHeartbeatMs)
        {
            return false;
        }
        _nextHeartbeatMs = nowMs + intervalMs;
        return true;
    }

    /// <summary>The periodic raw MonoGame line body: connection, packet, pressed buttons and window activity.</summary>
    public static string FormatHeartbeat(bool windowActive, bool connected, int packetNumber, IReadOnlyList<string> pressed) =>
        $"heartbeat active={windowActive} connected={connected} packet={packetNumber} pressed=[{string.Join(",", pressed)}]";

    /// <summary>Capabilities of one SDL joystick slot (MonoGame's Joystick layer, below the GameController mapping).</summary>
    public static string FormatJoystickCaps(bool connected, string? name, string? id, int buttonCount, int axisCount, int hatCount) =>
        $"connected={connected} name='{name}' id='{id}' buttons={buttonCount} axes={axisCount} hats={hatCount}";

    /// <summary>
    /// One SDL joystick state: pressed button indices, axes away from rest (|value| above
    /// <paramref name="axisDeadZone"/>, raw -32768..32767) and hats that are not centered.
    /// </summary>
    public static string FormatJoystickState(bool connected, IReadOnlyList<bool> buttons, IReadOnlyList<int> axes,
        IReadOnlyList<string> hats, int axisDeadZone = 4096)
    {
        List<string> pressed = new();
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i]) pressed.Add(i.ToString(CultureInfo.InvariantCulture));
        }
        List<string> moved = new();
        for (int i = 0; i < axes.Count; i++)
        {
            if (Math.Abs(axes[i]) > axisDeadZone) moved.Add($"{i}:{axes[i]}");
        }
        List<string> activeHats = new();
        for (int i = 0; i < hats.Count; i++)
        {
            if (!string.IsNullOrEmpty(hats[i])) activeHats.Add($"{i}:{hats[i]}");
        }
        return $"connected={connected} buttons=[{string.Join(",", pressed)}] axes=[{string.Join(",", moved)}] hats=[{string.Join(",", activeHats)}]";
    }

    private static readonly (int Mask, string Name)[] XInputButtonNames =
    {
        (0x0001, "DPadUp"), (0x0002, "DPadDown"), (0x0004, "DPadLeft"), (0x0008, "DPadRight"),
        (0x0010, "Start"), (0x0020, "Back"), (0x0040, "LeftStick"), (0x0080, "RightStick"),
        (0x0100, "LeftShoulder"), (0x0200, "RightShoulder"), (0x1000, "A"), (0x2000, "B"), (0x4000, "X"), (0x8000, "Y"),
    };

    /// <summary>
    /// One XInput (Windows OS layer, independent of SDL) state. Sticks use the raw -32768..32767 range and are
    /// listed only above <paramref name="stickDeadZone"/>; triggers are raw 0..255 and listed when nonzero.
    /// </summary>
    public static string FormatXInput(bool connected, uint packetNumber, ushort buttonMask,
        byte leftTrigger, byte rightTrigger, short leftX, short leftY, short rightX, short rightY, int stickDeadZone = 7849)
    {
        if (!connected)
        {
            return "connected=False";
        }
        List<string> pressed = new();
        foreach ((int mask, string name) in XInputButtonNames)
        {
            if ((buttonMask & mask) != 0) pressed.Add(name);
        }
        List<string> parts = new() { "connected=True", $"packet={packetNumber}", $"pressed=[{string.Join(",", pressed)}]" };
        if (Math.Abs((int)leftX) > stickDeadZone || Math.Abs((int)leftY) > stickDeadZone) parts.Add($"leftStick={leftX},{leftY}");
        if (Math.Abs((int)rightX) > stickDeadZone || Math.Abs((int)rightY) > stickDeadZone) parts.Add($"rightStick={rightX},{rightY}");
        if (leftTrigger > 0) parts.Add($"leftTrigger={leftTrigger}");
        if (rightTrigger > 0) parts.Add($"rightTrigger={rightTrigger}");
        return string.Join(" ", parts);
    }

    /// <summary>Plain-English checklist logged once at startup for when XInput also reports nothing.</summary>
    public const string InputLostHint =
        "hint if xinput also shows no presses: another app may be holding the pad (Steam with controller support, "
        + "Xbox Game Bar, a game launcher or remapper such as DS4Windows or reWASD); close them and retry; "
        + "a controller on Bluetooth may be in a different mode than over USB (try a USB cable or re-pair it).";

    /// <summary>Name of the environment variable that picks which probe layers <c>--log</c> runs.</summary>
    public const string ProbesEnvironmentVariable = "GUM_PREVIEW_GAMEPAD_PROBES";

    /// <summary>Which diagnostic layers run next to the always-on raw MonoGame and Gum lines.</summary>
    public sealed record GamepadProbes(string Active, bool Joystick, bool XInput, bool Fix, string? Warning);

    /// <summary>
    /// Parses <see cref="ProbesEnvironmentVariable"/>: <c>none</c> (raw and Gum lines only), <c>joystick</c>,
    /// <c>xinput</c>, <c>all</c> (default when unset) or <c>fix</c> (all, plus the experimental SDL background-events
    /// hint). An unknown value falls back to <c>all</c> with a warning.
    /// </summary>
    public static GamepadProbes ParseProbes(string? value)
    {
        string v = value?.Trim().ToLowerInvariant() ?? "";
        return v switch
        {
            "" or "all" => new GamepadProbes("all", true, true, false, null),
            "none" => new GamepadProbes("none", false, false, false, null),
            "joystick" => new GamepadProbes("joystick", true, false, false, null),
            "xinput" => new GamepadProbes("xinput", false, true, false, null),
            "fix" => new GamepadProbes("fix", true, true, true, null),
            _ => new GamepadProbes("all", true, true, false, $"unknown value '{value}' (use none|joystick|xinput|all|fix), using all"),
        };
    }

    private static string Round(float value) => Math.Round(value, 2).ToString("0.00", CultureInfo.InvariantCulture);
}
