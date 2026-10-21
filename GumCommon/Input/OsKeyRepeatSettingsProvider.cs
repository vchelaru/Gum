using System;
using System.Runtime.InteropServices;

namespace Gum.Input;

/// <summary>
/// Reads the key repeat settings from the operating system on the desktop OSes where Gum can do so
/// without extra dependencies. Windows is read; any other OS, or a failed read, returns null so the
/// keyboard keeps its defaults.
/// </summary>
/// <remarks>
/// <c>RuntimeInformation</c> is used instead of <c>OperatingSystem.Is*</c> because the latter is compiled
/// out on netstandard2.1 (Unity). Mobile, browser and other sandboxed targets are not Windows, so they
/// never reach the native call.
/// </remarks>
internal sealed class OsKeyRepeatSettingsProvider : IKeyRepeatSettingsProvider
{
    private const uint SpiGetKeyboardSpeed = 0x000A;
    private const uint SpiGetKeyboardDelay = 0x0016;

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint action, uint param, ref uint value, uint winIni);

    /// <inheritdoc/>
    public KeyRepeatSettings? GetSettings()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ReadWindows();
            }
        }
        catch (Exception)
        {
            // Missing user32 or entry point (a Windows variant without it), or a blocked call.
        }

        return null;
    }

    private static KeyRepeatSettings? ReadWindows()
    {
        uint delayLevel = 0;
        uint speedLevel = 0;
        if (!SystemParametersInfo(SpiGetKeyboardDelay, 0, ref delayLevel, 0) ||
            !SystemParametersInfo(SpiGetKeyboardSpeed, 0, ref speedLevel, 0))
        {
            return null;
        }

        return FromWindowsLevels((int)Math.Min(delayLevel, int.MaxValue), (int)Math.Min(speedLevel, int.MaxValue));
    }

    /// <summary>
    /// Maps the Windows keyboard delay level (0 to 3, about 250 ms to 1 second in 250 ms steps) and speed
    /// level (0 to 31, about 2.5 to 30 repeats per second) to a delay and repeat interval. Out-of-range levels
    /// are clamped.
    /// </summary>
    internal static KeyRepeatSettings FromWindowsLevels(int delayLevel, int speedLevel)
    {
        int clampedDelay = Math.Max(0, Math.Min(3, delayLevel));
        int clampedSpeed = Math.Max(0, Math.Min(31, speedLevel));

        TimeSpan delay = TimeSpan.FromMilliseconds((clampedDelay + 1) * 250);
        double repeatsPerSecond = 2.5 + (30.0 - 2.5) * clampedSpeed / 31.0;
        TimeSpan rate = TimeSpan.FromSeconds(1.0 / repeatsPerSecond);

        return new KeyRepeatSettings(delay, rate);
    }
}
