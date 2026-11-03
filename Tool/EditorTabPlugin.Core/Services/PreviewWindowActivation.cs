using System;
using System.Threading;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Picks the preview's window (the process's main window, else a search by process id) and asks
/// the activator to foreground it. Never throws: window activation must not fail a launch.
/// </summary>
public static class PreviewWindowActivation
{
    // The foreground is briefly zero while Windows switches it, so it is re-read after these waits.
    private static readonly int[] ReReadDelaysMilliseconds = { 0, 100, 300 };

    /// <param name="mainWindowHandle">The process's reported main window, or zero when it has none.</param>
    /// <param name="log">Receives one line per attempt, for the diagnostic log.</param>
    /// <param name="sleep">Waits the given milliseconds; a test passes a no-op.</param>
    /// <returns>Whether a window was found and activation was attempted.</returns>
    public static bool Activate(
        IPreviewWindowActivator activator, int processId, IntPtr mainWindowHandle, Action<string> log, Action<int>? sleep = null)
    {
        try
        {
            IntPtr window = mainWindowHandle;
            string how = "MainWindowHandle";
            if (window == IntPtr.Zero)
            {
                window = activator.FindWindow(processId);
                how = "EnumWindows fallback";
            }
            if (window == IntPtr.Zero)
            {
                log($"Preview activate: pid {processId}, no window found.");
                return false;
            }

            bool accepted = activator.Activate(window, out string detail);
            string verdict = Verify(activator, window, accepted, sleep ?? (milliseconds => Thread.Sleep(milliseconds)));
            log($"Preview activate: pid {processId}, window 0x{window.ToInt64():X} via {how}, {detail}, {verdict}");
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            log($"Preview activate: pid {processId} failed: {exception.Message}");
            return false;
        }
    }

    // "Refused" only when the OS said no, or the foreground settles on another window.
    private static string Verify(IPreviewWindowActivator activator, IntPtr window, bool accepted, Action<int> sleep)
    {
        if (!accepted)
        {
            return "refused (SetForegroundWindow returned false)";
        }

        IntPtr foreground = IntPtr.Zero;
        int reads = 0;
        foreach (int delay in ReReadDelaysMilliseconds)
        {
            if (delay > 0)
            {
                sleep(delay);
            }
            foreground = activator.GetForegroundWindow();
            reads++;
            if (foreground == window)
            {
                return $"foreground is the preview ({reads} read(s))";
            }
        }

        return foreground == IntPtr.Zero
            ? $"foreground unknown, still 0x0 after {reads} reads"
            : $"NOT the preview: foreground is 0x{foreground.ToInt64():X} after {reads} reads";
    }
}
