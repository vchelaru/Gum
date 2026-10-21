using System;
using System.Runtime.InteropServices;

namespace Gum.Input;

/// <summary>
/// Reads the key repeat settings from the operating system on the desktop OSes where Gum can do so
/// without extra dependencies. Windows and macOS are read; any other OS, or a failed read, returns null so
/// the keyboard keeps its defaults.
/// </summary>
/// <remarks>
/// <c>RuntimeInformation</c> is used instead of <c>OperatingSystem.Is*</c> because the latter is compiled
/// out on netstandard2.1 (Unity). Mobile, browser and other sandboxed targets are not Windows, so they
/// never reach the native call.
/// <para>
/// Linux is deliberately not read. X11 would need a native libX11 dependency and does not cover Wayland,
/// GNOME keeps its values in a binary dconf database that only the <c>gsettings</c> process (or libdconf)
/// reads, and KDE keeps them in a different file, so no single route is reliable on every desktop and
/// every one of them is either a new dependency or a process launch during startup. Linux keeps the
/// 500 ms / 70 ms defaults.
/// </para>
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return ReadMac();
            }
        }
        catch (Exception)
        {
            // Missing user32 or entry point (a Windows variant without it), or a blocked call.
        }

        return null;
    }

    private static KeyRepeatSettings? ReadMac()
    {
        // Absent unless the user moved a slider in System Settings, in which case the defaults apply.
        return FromMacTicks(
            MacPreferences.ReadGlobalInt("InitialKeyRepeat"),
            MacPreferences.ReadGlobalInt("KeyRepeat"));
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

    /// <summary>
    /// Maps macOS's <c>InitialKeyRepeat</c> (delay) and <c>KeyRepeat</c> (interval) values, both counted in
    /// 15 ms ticks, to a delay and repeat interval. A value that is missing or not positive keeps the
    /// shared default for that half; null is returned when neither value is usable.
    /// </summary>
    internal static KeyRepeatSettings? FromMacTicks(int? initialKeyRepeatTicks, int? keyRepeatTicks)
    {
        bool hasDelay = initialKeyRepeatTicks > 0;
        bool hasRate = keyRepeatTicks > 0;
        if (!hasDelay && !hasRate)
        {
            return null;
        }

        TimeSpan delay = hasDelay ? TimeSpan.FromMilliseconds(initialKeyRepeatTicks!.Value * 15.0) : KeyRepeatTimer.DefaultDelay;
        TimeSpan rate = hasRate ? TimeSpan.FromMilliseconds(keyRepeatTicks!.Value * 15.0) : KeyRepeatTimer.DefaultRate;
        return new KeyRepeatSettings(delay, rate);
    }
}

/// <summary>
/// Reads a number from macOS's global preferences through CoreFoundation, in process, so startup never
/// launches another program. Every failure (library missing, key unset, value not a number) returns null.
/// </summary>
internal static class MacPreferences
{
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const uint Utf8Encoding = 0x08000100;
    private const int SInt32Type = 3;

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFStringCreateWithCString(
        IntPtr allocator, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, uint encoding);

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFPreferencesCopyAppValue(IntPtr key, IntPtr applicationId);

    [DllImport(CoreFoundation)]
    private static extern UIntPtr CFGetTypeID(IntPtr value);

    [DllImport(CoreFoundation)]
    private static extern UIntPtr CFNumberGetTypeID();

    [DllImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool CFNumberGetValue(IntPtr number, IntPtr type, out int value);

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr value);

    /// <summary>
    /// Returns the integer stored under <paramref name="key"/> in the user's global domain (the one
    /// <c>defaults read -g</c> shows), or null.
    /// </summary>
    internal static int? ReadGlobalInt(string key)
    {
        IntPtr keyString = IntPtr.Zero;
        IntPtr domainString = IntPtr.Zero;
        IntPtr value = IntPtr.Zero;
        try
        {
            keyString = CFStringCreateWithCString(IntPtr.Zero, key, Utf8Encoding);
            domainString = CFStringCreateWithCString(IntPtr.Zero, "kCFPreferencesAnyApplication", Utf8Encoding);
            if (keyString == IntPtr.Zero || domainString == IntPtr.Zero)
            {
                return null;
            }

            value = CFPreferencesCopyAppValue(keyString, domainString);
            if (value == IntPtr.Zero || CFGetTypeID(value) != CFNumberGetTypeID())
            {
                return null;
            }

            return CFNumberGetValue(value, (IntPtr)SInt32Type, out int result) ? result : null;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            ReleaseIfSet(value);
            ReleaseIfSet(domainString);
            ReleaseIfSet(keyString);
        }
    }

    private static void ReleaseIfSet(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            CFRelease(handle);
        }
    }
}
