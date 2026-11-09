using Brutal.GlfwApi;
using System;

namespace Gum.Input;

/// <summary>
/// The slice of a BRUTAL GLFW window that Gum's cursor, keyboard, clipboard and gamepad driver read.
/// <see cref="GlfwWindow"/> is a sealed class that needs a live GLFW, so the host reads it through this
/// seam and tests supply a fake. Production code uses <see cref="BrutalGlfwInput"/>.
/// </summary>
internal interface IBrutalInput
{
    /// <summary>Cursor position in window coordinates (GLFW screen coordinates, origin top left).</summary>
    double CursorX { get; }

    /// <inheritdoc cref="CursorX"/>
    double CursorY { get; }

    /// <summary>Window size in screen coordinates, the space <see cref="CursorX"/> is in.</summary>
    int WindowWidth { get; }

    /// <inheritdoc cref="WindowWidth"/>
    int WindowHeight { get; }

    /// <summary>Framebuffer size in pixels, which is Gum's canvas size. Larger than the window on a high-DPI display.</summary>
    int FramebufferWidth { get; }

    /// <inheritdoc cref="FramebufferWidth"/>
    int FramebufferHeight { get; }

    bool IsMouseButtonDown(GlfwMouseButton button);

    bool IsKeyDown(GlfwKey key);

    /// <summary>Vertical scroll in notches, raised once per wheel event.</summary>
    event Action<double>? Scrolled;

    /// <summary>A typed Unicode code point.</summary>
    event Action<uint>? CharTyped;

    string? ClipboardText { get; set; }

    void SetCursorShape(GlfwStandardCursor shape);

    /// <summary>
    /// Reads the gamepad at <paramref name="index"/> into the GLFW gamepad layout
    /// (<see cref="GlfwGamepadButton"/> indexes into <paramref name="buttons"/>, 15 entries, 1 when held;
    /// <see cref="GlfwGamepadAxis"/> indexes into <paramref name="axes"/>, 6 entries). Returns false when
    /// no gamepad is present.
    /// </summary>
    bool TryReadGamepad(int index, byte[] buttons, float[] axes);
}
