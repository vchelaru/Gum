using System;
using System.Runtime.InteropServices;

namespace GumPreview;

/// <summary>Keeps an unattended run's window from taking focus (#5471).</summary>
internal static class UnattendedWindow
{
    /// <summary>
    /// Sets the SDL hints that show the window without activating it. SDL reads hints from the
    /// process environment when it initializes, which happens in the <see cref="Game1"/> constructor,
    /// so this must run before it.
    /// </summary>
    public static void KeepInBackground()
    {
        // macOS: SDL leaves the app in the background instead of activating it.
        SetNativeEnvironmentVariable("SDL_MAC_BACKGROUND_APP", "1");
        // Windows (and X11 where the window manager honors it): show the window without activating it.
        SetNativeEnvironmentVariable("SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN", "1");
    }

    // On Unix, Environment.SetEnvironmentVariable changes only .NET's own copy of the environment,
    // which SDL's getenv never sees.
    private static void SetNativeEnvironmentVariable(string name, string value)
    {
        if (OperatingSystem.IsWindows())
        {
            Environment.SetEnvironmentVariable(name, value);
        }
        else
        {
            setenv(name, value, 1);
        }
    }

    [DllImport("libc", CharSet = CharSet.Ansi)]
    private static extern int setenv(string name, string value, int overwrite);
}
