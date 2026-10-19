#nullable enable

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using ICursor = Gum.Wireframe.ICursor;

namespace Gum;

/// <summary>
/// Unity's GumService: a <see cref="GumServiceSkiaBase"/> whose cursor, keyboard and gamepads are fed by
/// the host. Each frame the host pushes Unity's input into <see cref="Cursor"/>, <see cref="Keyboard"/>
/// and <see cref="GumServiceSkiaBase.Gamepads"/>,
/// calls <see cref="GumServiceSkiaInteractiveBase.Update"/>, then draws with <see cref="GumServiceSkiaBase.Draw"/> into the canvas
/// it owns. The Unity package's <c>GumRenderer</c> and <c>GumInput</c> components do all of this.
/// </summary>
public partial class GumService : GumServiceSkiaInteractiveBase, IGumService
{
    /// <inheritdoc/>
    ICursor? IGumService.CreateCursor() => new Cursor();

    /// <inheritdoc/>
    IInputReceiverKeyboard? IGumService.CreateKeyboard() => new Keyboard();

    /// <summary>
    /// Commits the state the host pushed into <paramref name="gamepad"/> with its driver-facing setters
    /// (<see cref="GamePad.SetConnected"/>, <see cref="GamePad.SetButtonState"/>, the stick setters).
    /// Pushed state persists until the host overwrites it, so the host pushes every slot each frame.
    /// </summary>
    void IGumService.ApplyGamePadState(GamePad gamepad, int index, double time) => gamepad.Activity(time);

    /// <summary>
    /// Sets the clipboard TextBox copy and paste use. The Unity package passes one backed by
    /// <c>GUIUtility.systemCopyBuffer</c>.
    /// </summary>
    public void UseClipboard(IGumClipboard clipboard) => Clipboard = clipboard;
}
