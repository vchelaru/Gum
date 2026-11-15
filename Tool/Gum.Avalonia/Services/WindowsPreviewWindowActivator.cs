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

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

#pragma warning disable RS0030 // Sanctioned per-OS P/Invoke file for the Avalonia head.
    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint attachThread, uint attachToThread, bool attach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
#pragma warning restore RS0030

    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    /// <inheritdoc/>
    public IntPtr FindWindow(int processId)
    {
        IntPtr best = IntPtr.Zero;
        long bestScore = -1;
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out uint owner);
            if (owner != (uint)processId || !IsWindowVisible(window))
            {
                return true;
            }
            long area = GetWindowRect(window, out Rect rect) ? (long)(rect.Right - rect.Left) * (rect.Bottom - rect.Top) : 0;
            // A titled window beats any untitled one; the largest wins within each group.
            long score = (GetWindowTextLength(window) > 0 ? 1L << 40 : 0) + area;
            if (score > bestScore)
            {
                bestScore = score;
                best = window;
            }
            return true;
        }, IntPtr.Zero);
        return best;
    }

    /// <inheritdoc/>
    public bool Activate(IntPtr window, out string detail)
    {
        bool wasIconic = IsIconic(window);
        bool showResult = wasIconic && ShowWindow(window, SW_RESTORE);
        bool setResult = SetForegroundWindow(window);
        bool attached = false;
        if (!setResult)
        {
            // Windows' foreground lock refused it; sharing the foreground thread's input queue lifts it
            // for the call.
            IntPtr foreground = GetForegroundWindow();
            uint foregroundThread = GetWindowThreadProcessId(foreground, out _);
            uint thisThread = GetCurrentThreadId();
            if (foregroundThread != 0 && foregroundThread != thisThread)
            {
                attached = AttachThreadInput(thisThread, foregroundThread, true);
                try
                {
                    setResult = SetForegroundWindow(window);
                }
                finally
                {
                    if (attached)
                    {
                        AttachThreadInput(thisThread, foregroundThread, false);
                    }
                }
            }
        }
        detail = $"iconic={wasIconic}, ShowWindow={showResult}, SetForegroundWindow={setResult} (AttachThreadInput retry={attached})";
        return setResult;
    }

    /// <inheritdoc/>
    IntPtr IPreviewWindowActivator.GetForegroundWindow() => GetForegroundWindow();
}
