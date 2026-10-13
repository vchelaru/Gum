using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace GumPreview;

/// <summary>
/// Reports when the tool that launched the preview is gone, however it ended (closed, killed, or
/// crashed), so the preview doesn't stay open with nothing driving it.
/// </summary>
public static class ParentProcessWatcher
{
    /// <summary>
    /// Watches process <paramref name="parentProcessId"/> on a background thread and calls
    /// <paramref name="onParentExited"/> from it once that process has exited, or straight away if it
    /// already has.
    /// </summary>
    public static void Start(int parentProcessId, Action onParentExited)
    {
        Thread thread = new Thread(() =>
        {
            WaitForExit(parentProcessId);
            onParentExited();
        })
        {
            IsBackground = true,
            Name = "GumPreview parent watcher",
        };
        thread.Start();
    }

    private static void WaitForExit(int parentProcessId)
    {
        try
        {
            // Holding the handle keeps the id from being reused while we wait.
            using Process parent = Process.GetProcessById(parentProcessId);
            parent.WaitForExit();
        }
        catch (ArgumentException)
        {
            // No process has that id any more.
        }
        catch (InvalidOperationException)
        {
            // It exited while we were opening it.
        }
        catch (Win32Exception)
        {
            // It cannot be watched (access denied); treat it as gone rather than leaving the preview orphaned.
        }
    }
}
