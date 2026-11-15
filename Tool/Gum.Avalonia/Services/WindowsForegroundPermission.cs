using System;
using System.Runtime.InteropServices;

namespace Gum.Avalonia.Services;

/// <summary>
/// Windows only lets a process move its window to the foreground when it was recently given input
/// or was granted that right by a process that was. A second launch started by the user has the
/// right and passes it to the running Gum before asking it to come forward. This is the head's
/// sanctioned per-OS P/Invoke file for it (see BannedSymbols.CrossPlatform.txt).
/// </summary>
internal static class WindowsForegroundPermission
{
    /// <summary>Lets <paramref name="processId"/> take the foreground; a no-op off Windows.</summary>
    public static void Allow(int processId)
    {
        if (OperatingSystem.IsWindows())
        {
            AllowSetForegroundWindow(processId);
        }
    }

#pragma warning disable RS0030 // Sanctioned per-OS P/Invoke file for the Avalonia head.
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(int processId);
#pragma warning restore RS0030
}
