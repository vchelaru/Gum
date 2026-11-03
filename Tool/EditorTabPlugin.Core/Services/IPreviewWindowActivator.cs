using System;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Brings another process's top-level window to the foreground. The OS call lives in the head
/// (shared code may not P/Invoke); a head without one passes null and the preview raises its own
/// window instead.
/// </summary>
public interface IPreviewWindowActivator
{
    /// <summary>Restores <paramref name="window"/> if minimized and makes it the foreground window.</summary>
    void Activate(IntPtr window);
}
