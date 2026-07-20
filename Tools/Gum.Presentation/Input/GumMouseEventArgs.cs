namespace Gum.Input;

/// <summary>
/// Gum's framework-neutral mouse event payload, used as the shared currency between a host control
/// (e.g. <c>WireframeControl</c>) and framework-agnostic mouse handling code (e.g. camera panning).
/// </summary>
/// <remarks>
/// Mirrors the read/write shape of the framework mouse-event args it replaces: callers at the
/// WinForms/WPF boundary populate position/button/wheel-delta on the way in, and read
/// <see cref="Handled"/> back out to decide whether to suppress the framework's own default handling
/// (e.g. WinForms' <c>HandledMouseEventArgs.Handled</c> on mouse wheel). Because it is consumed
/// in/out, it is a reference type.
/// </remarks>
public class GumMouseEventArgs
{
    /// <summary>Cursor X position, in the host control's client coordinates.</summary>
    public int X { get; set; }

    /// <summary>Cursor Y position, in the host control's client coordinates.</summary>
    public int Y { get; set; }

    /// <summary>The button associated with this event, if any (e.g. the button held during a move/drag).</summary>
    public GumMouseButton Button { get; set; }

    /// <summary>Mouse wheel delta. Positive is away from the user (zoom in); negative is toward the user (zoom out).</summary>
    public int Delta { get; set; }

    /// <summary>
    /// Set by the handler when it consumes the event; read back by the boundary caller to suppress the
    /// framework's own default handling (e.g. container scroll on mouse wheel).
    /// </summary>
    public bool Handled { get; set; }
}
