using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;

namespace Gum.Avalonia.Services;

/// <summary>
/// Keeps Close enabled in a window's system menu. Avalonia 11.3 greys it for a window that extends
/// its client area into the title bar, and taskbar tools that close windows (7+ Taskbar Tweaker's
/// middle-click Close) check it before posting <c>SC_CLOSE</c>. Avalonia greys it inside
/// <c>ExtendClientArea</c>, which ends with a <c>SWP_FRAMECHANGED</c> <c>SetWindowPos</c>, so
/// re-enabling it on <c>WM_WINDOWPOSCHANGED</c> follows every greying. Remove once Gum is on an
/// Avalonia release that includes AvaloniaUI/Avalonia#21630, which stops greying it (#7499).
/// This is the head's sanctioned per-OS P/Invoke file for the system menu (see
/// BannedSymbols.CrossPlatform.txt); callers must check <see cref="OperatingSystem.IsWindows"/>.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowsSystemMenuClose
{
    private const uint WM_WINDOWPOSCHANGED = 0x0047;
    private const uint SC_CLOSE = 0xF060;
    private const uint MF_BYCOMMAND = 0x0;
    private const uint MF_ENABLED = 0x0;

#pragma warning disable RS0030 // Sanctioned per-OS P/Invoke file for the Avalonia head.
    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr window, bool revert);

    [DllImport("user32.dll")]
    private static extern bool EnableMenuItem(IntPtr menu, uint id, uint flags);
#pragma warning restore RS0030

    /// <summary>
    /// Keeps Close enabled in <paramref name="window"/>'s system menu. Call before setting the
    /// window's extend-client-area hints, which grey it right away.
    /// </summary>
    public static void KeepEnabled(Window window) =>
        Win32Properties.AddWndProcHookCallback(window, HandleWindowMessage);

    private static IntPtr HandleWindowMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_WINDOWPOSCHANGED)
        {
            EnableMenuItem(GetSystemMenu(hWnd, false), SC_CLOSE, MF_BYCOMMAND | MF_ENABLED);
        }
        return IntPtr.Zero;
    }
}
