using WinForms = System.Windows.Forms;

namespace Gum.Input;

/// <summary>
/// Translates WinForms mouse event types to Gum's framework-neutral <see cref="GumMouseEventArgs"/> at
/// the editor-host boundary (e.g. <c>WireframeControl</c>), so the wireframe editing subsystem
/// (camera panning/zoom, etc.) need not reference WinForms.
/// </summary>
public static class MouseExtensions
{
    /// <summary>Maps a WinForms <see cref="WinForms.MouseButtons"/> value to the matching <see cref="GumMouseButton"/>.</summary>
    public static GumMouseButton ToGumMouseButton(this WinForms.MouseButtons buttons) =>
        buttons switch
        {
            WinForms.MouseButtons.Left => GumMouseButton.Left,
            WinForms.MouseButtons.Right => GumMouseButton.Right,
            WinForms.MouseButtons.Middle => GumMouseButton.Middle,
            _ => GumMouseButton.None,
        };

    /// <summary>Builds a framework-neutral <see cref="GumMouseEventArgs"/> from a WinForms mouse event.</summary>
    public static GumMouseEventArgs ToGumMouseEventArgs(this WinForms.MouseEventArgs e)
    {
        return new GumMouseEventArgs
        {
            X = e.X,
            Y = e.Y,
            Button = e.Button.ToGumMouseButton(),
            Delta = e.Delta,
        };
    }
}
