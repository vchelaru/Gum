#if SILK
#nullable enable

using System;

namespace Gum;

/// <summary>
/// Defers a window resize to the next frame. The resize event can fire from inside the OS's nested
/// live-resize loop, where the GL context is not safe to touch, so the event only calls
/// <see cref="Request"/> and the next <c>Update</c> calls <see cref="TryConsume"/>, which reads the
/// framebuffer size at that point. A minimized window (size zero) or an unchanged size is not a resize.
/// </summary>
internal sealed class FramebufferResizeTracker
{
    private readonly Func<(int Width, int Height)> _readFramebufferSize;
    private bool _requested;
    private (int Width, int Height) _applied;

    public FramebufferResizeTracker(Func<(int Width, int Height)> readFramebufferSize, int appliedWidth, int appliedHeight)
    {
        _readFramebufferSize = readFramebufferSize;
        _applied = (appliedWidth, appliedHeight);
    }

    /// <summary>Records that the window was resized. Safe to call from the resize event.</summary>
    public void Request() => _requested = true;

    /// <summary>
    /// Returns true with the new framebuffer size when a requested resize changed it to a non-empty size.
    /// </summary>
    public bool TryConsume(out int width, out int height)
    {
        width = _applied.Width;
        height = _applied.Height;
        if (!_requested)
        {
            return false;
        }
        _requested = false;

        (int Width, int Height) size = _readFramebufferSize();
        if (size.Width <= 0 || size.Height <= 0 || size == _applied)
        {
            return false;
        }

        _applied = size;
        width = size.Width;
        height = size.Height;
        return true;
    }
}
#endif
