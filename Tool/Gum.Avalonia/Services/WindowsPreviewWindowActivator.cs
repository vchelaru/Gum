using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Gum.Plugins.InternalPlugins.EditorTab.Services;

namespace Gum.Avalonia.Services;

/// <summary>
/// Foregrounds the Preview window from the tool, which holds the foreground right after the user
/// clicks Preview. Windows refuses the same call from the background Preview process (#7542).
/// This is the head's sanctioned per-OS P/Invoke file for it (see BannedSymbols.CrossPlatform.txt);
/// create it only when <see cref="OperatingSystem.IsWindows"/>.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsPreviewWindowActivator : IPreviewWindowActivator
{
    private const int SW_RESTORE = 9;

#pragma warning disable RS0030 // Sanctioned per-OS P/Invoke file for the Avalonia head.
    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);
#pragma warning restore RS0030

    /// <inheritdoc/>
    public void Activate(IntPtr window)
    {
        if (IsIconic(window))
        {
            ShowWindow(window, SW_RESTORE);
        }
        SetForegroundWindow(window);
    }
}
