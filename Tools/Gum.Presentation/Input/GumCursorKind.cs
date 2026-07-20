namespace Gum.Input;

/// <summary>
/// The set of cursor icons the wireframe editor's input handlers can request while the cursor is
/// over a resize/rotation/move/polygon-point handle. Gum-owned and platform-neutral so
/// <see cref="Editors.Handlers.IInputHandler"/> and its implementers can live in the headless
/// <c>Gum.Presentation</c> assembly without a <see cref="System.Windows.Forms.Cursor"/> return type.
/// </summary>
/// <remarks>
/// Deliberately a separate type from <c>InputLibrary.CursorKind</c> (same value set, different
/// purpose: that one backs <c>IInputHostControl.Cursor</c>) rather than a shared enum:
/// <c>InputLibrary.csproj</c> targets <c>net8.0-windows7.0</c> with
/// <c>UseWindowsForms</c>/<c>UseWPF</c> set, so a project reference from <c>Gum.Presentation</c>
/// (plain <c>net8.0</c>) fails at restore (NU1201) regardless of whether the enum itself uses any
/// WinForms/WPF type — the whole compilation unit's target framework is the blocker, not the
/// enum's own dependency surface.
/// </remarks>
public enum GumCursorKind
{
    Arrow,
    Cross,
    Hand,
    SizeAll,
    SizeNS,
    SizeWE,
    SizeNESW,
    SizeNWSE
}
