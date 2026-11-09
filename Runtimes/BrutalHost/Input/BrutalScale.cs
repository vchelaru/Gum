namespace Gum.Input;

internal static class BrutalScale
{
    /// <summary>
    /// Converts a window coordinate to a framebuffer pixel. Returns it unchanged when either size is
    /// unknown (zero, for example a minimized window), so a missing size never zeroes the cursor.
    /// </summary>
    public static double WindowToFramebuffer(double value, int windowSize, int framebufferSize) =>
        windowSize > 0 && framebufferSize > 0 ? value * framebufferSize / windowSize : value;
}
