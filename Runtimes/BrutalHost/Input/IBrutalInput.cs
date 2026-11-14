using Gum.Forms.Input;
using Gum.Wireframe;
using System;

namespace Gum.Input;

/// <summary>A mouse button Gum reads from a host.</summary>
public enum MouseButton
{
    Left,
    Middle,
    Right,
}

/// <summary>
/// Everything Gum's cursor, keyboard, clipboard and gamepad read from the host's window, in Gum's own
/// types. The default implementation reads a BRUTAL GLFW window. A host that owns the window and its
/// events itself (SDL3, a custom loop) implements this and passes it to <c>GumService.Initialize</c>
/// instead of a <c>GlfwWindow</c>; Gum then never touches GLFW.
/// </summary>
/// <remarks>
/// Gum polls the state members once per <c>Update</c> and derives pushes, releases and key repeat
/// itself, so report what is held now. The two events are raised by the host as they happen.
/// </remarks>
public interface IBrutalInput
{
    /// <summary>Cursor position in window coordinates (origin top left), the space of <see cref="WindowWidth"/>.</summary>
    double CursorX { get; }

    /// <inheritdoc cref="CursorX"/>
    double CursorY { get; }

    /// <summary>Window size in screen coordinates, the space <see cref="CursorX"/> is in.</summary>
    int WindowWidth { get; }

    /// <inheritdoc cref="WindowWidth"/>
    int WindowHeight { get; }

    /// <summary>
    /// Framebuffer size in pixels, which is Gum's canvas size. Larger than the window on a high-DPI display;
    /// the same as the window size otherwise. A size of zero (a minimized window) skips drawing.
    /// </summary>
    int FramebufferWidth { get; }

    /// <inheritdoc cref="FramebufferWidth"/>
    int FramebufferHeight { get; }

    /// <summary>Whether <paramref name="button"/> is held right now.</summary>
    bool IsMouseButtonDown(MouseButton button);

    /// <summary>
    /// Whether <paramref name="key"/> is held right now. Return false for a key the host has no equivalent for.
    /// </summary>
    bool IsKeyDown(Keys key);

    /// <summary>Vertical scroll in notches (one per wheel click, positive away from the user), raised once per wheel event.</summary>
    event Action<double>? Scrolled;

    /// <summary>A typed Unicode code point, raised once per character the user types.</summary>
    event Action<uint>? CharTyped;

    /// <summary>The operating system clipboard text, used by copy, cut and paste. Null when it holds no text.</summary>
    string? ClipboardText { get; set; }

    /// <summary>Shows <paramref name="shape"/> as the mouse cursor, such as a resize arrow over a splitter.</summary>
    void SetCursorShape(Cursors shape);

    /// <summary>
    /// Reads the gamepad at <paramref name="index"/> into <paramref name="snapshot"/>. Returns false when
    /// no gamepad is present at that index; a host without gamepad support returns false.
    /// </summary>
    bool TryReadGamepad(int index, out GamePadSnapshot snapshot);
}
