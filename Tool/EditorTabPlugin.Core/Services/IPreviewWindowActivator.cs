using System;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Brings another process's top-level window to the foreground. The OS calls live in the head
/// (shared code may not P/Invoke); a head without them passes null and the preview raises its own
/// window instead.
/// </summary>
public interface IPreviewWindowActivator
{
    /// <summary>
    /// The first visible top-level window owned by <paramref name="processId"/> (titled, else the
    /// largest), or <see cref="IntPtr.Zero"/> when it has none yet. Used when the process reports
    /// no main window, as an SDL window may.
    /// </summary>
    IntPtr FindWindow(int processId);

    /// <summary>
    /// Restores <paramref name="window"/> if minimized and asks the OS to make it the foreground
    /// window. Returns whether the OS accepted the request; <paramref name="detail"/> describes
    /// what it did, for the diagnostic log.
    /// </summary>
    bool Activate(IntPtr window, out string detail);

    /// <summary>The current foreground window, or <see cref="IntPtr.Zero"/> while it is changing.</summary>
    IntPtr GetForegroundWindow();
}
