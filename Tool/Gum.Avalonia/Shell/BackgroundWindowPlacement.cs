using Avalonia;
using Avalonia.Controls;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Parks a window for an unattended run: it opens without activating, not minimized (a minimized
/// window stops rendering), and off-screen.
/// </summary>
internal static class BackgroundWindowPlacement
{
    /// <summary>Past the edge of any real monitor, but clear of Windows' -32000 minimized-window sentinel.</summary>
    public static readonly PixelPoint Position = new PixelPoint(-20000, -20000);

    /// <param name="placeBeforeShowing">
    /// False on macOS, where a window first shown at a position it has to pull back onto a screen
    /// collapses to its minimum size (#5467).
    /// </param>
    public static void Apply(Window window, bool placeBeforeShowing)
    {
        window.ShowActivated = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowState = WindowState.Normal;
        if (placeBeforeShowing)
        {
            window.Position = Position;
        }
        // Showing the window puts it back at 0,0 on Windows, so place it again once it is open. On
        // macOS this is the only placement, and the system keeps an edge of the window on screen.
        window.Opened += (_, _) => window.Position = Position;
    }
}
