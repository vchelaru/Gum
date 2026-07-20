namespace Gum.Input;

/// <summary>
/// Read-only live cursor state (screen position, frame deltas, primary-button state) that the
/// wireframe editor's input handlers poll every frame while processing a drag. Gum-owned and
/// platform-neutral so this family can live in headless Gum.Presentation — the concrete cursor
/// singleton (<c>InputLibrary.Cursor</c>) can't be referenced directly: <c>InputLibrary.csproj</c>
/// targets <c>net8.0-windows7.0</c> with <c>UseWindowsForms</c>/<c>UseWPF</c> set, which fails a
/// <c>Gum.Presentation</c> project reference at restore (NU1201) the same way <c>InputLibrary
/// .CursorKind</c> does, regardless of this interface's own dependency surface.
/// </summary>
/// <remarks>
/// This is a different seam from <see cref="GumMouseEventArgs"/>/<see cref="GumKeyEventArgs"/>:
/// those neutralize discrete WinForms mouse/key *events*, this neutralizes continuous cursor
/// *state* the input handlers poll frame-to-frame (drag deltas, button-held state), which is a
/// different access pattern (`InputLibrary.Cursor.Self.XChange`, not an event handler parameter).
/// </remarks>
public interface IGumCursorState
{
    float X { get; }
    float Y { get; }
    float XChange { get; }
    float YChange { get; }
    bool PrimaryDown { get; }
    bool PrimaryPush { get; }
    bool PrimaryClick { get; }

    /// <summary>Whether the cursor is currently positioned over the editor window.</summary>
    bool IsInWindow { get; }

    /// <summary>Whether the current <see cref="PrimaryClick"/> is the second click of a double-click.</summary>
    bool PrimaryDoubleClick { get; }

    /// <summary>Whether the secondary (right) mouse button was pushed this frame.</summary>
    bool SecondaryPush { get; }

    /// <summary>Same as <see cref="PrimaryDown"/> but does not require <see cref="IsInWindow"/> to be true.</summary>
    bool PrimaryDownIgnoringIsInWindow { get; }

    /// <summary>
    /// Requests that the given cursor icon be shown. The mapping to a real framework cursor type
    /// (e.g. WinForms <c>Cursor</c>) is the implementation's job, so headless callers never need to
    /// reference a framework cursor type directly.
    /// </summary>
    void SetCursor(GumCursorKind kind);
}
