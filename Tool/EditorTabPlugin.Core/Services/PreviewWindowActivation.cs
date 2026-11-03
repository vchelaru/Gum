using System;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Picks the preview's window (the process's main window, else a search by process id) and asks
/// the activator to foreground it. Never throws: window activation must not fail a launch.
/// </summary>
public static class PreviewWindowActivation
{
    /// <param name="mainWindowHandle">The process's reported main window, or zero when it has none.</param>
    /// <param name="log">Receives one line per attempt, for the diagnostic log.</param>
    /// <returns>Whether a window was found and activation was attempted.</returns>
    public static bool Activate(IPreviewWindowActivator activator, int processId, IntPtr mainWindowHandle, Action<string> log)
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
            string result = activator.Activate(window);
            log($"Preview activate: pid {processId}, window 0x{window.ToInt64():X} via {how}, {result}");
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            log($"Preview activate: pid {processId} failed: {exception.Message}");
            return false;
        }
    }
}
