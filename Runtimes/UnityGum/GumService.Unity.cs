#nullable enable

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using ICursor = Gum.Wireframe.ICursor;

namespace Gum;

/// <summary>
/// Unity's GumService: a <see cref="GumServiceSkiaBase"/> whose cursor and keyboard are fed by the
/// host. Each frame the host pushes Unity's input into <see cref="Cursor"/> and <see cref="Keyboard"/>,
/// calls <see cref="Update"/>, then draws with <see cref="GumServiceSkiaBase.Draw"/> into the canvas
/// it owns. The Unity package's <c>GumRenderer</c> and <c>GumInput</c> components do all of this.
/// </summary>
public class GumService : GumServiceSkiaBase, IGumService
{
    private static GumService? _default;

    /// <summary>
    /// The singleton service instance.
    /// </summary>
    public static GumService Default => _default ??= new GumService();

    /// <summary>
    /// The default cursor. The host sets its state each frame with <see cref="Input.Cursor.SetMouseState"/>.
    /// </summary>
    public Cursor Cursor => (FormsUtilities.Cursor as Cursor)!;

    /// <summary>
    /// The default keyboard. The host reports held keys with <see cref="Input.Keyboard.SetKeyDown"/> and
    /// typed characters with <see cref="Input.Keyboard.AddTypedText"/>.
    /// </summary>
    /// <remarks>
    /// Null while a custom keyboard is installed with <see cref="FormsUtilities.SetKeyboard"/>.
    /// </remarks>
    public Keyboard Keyboard => (FormsUtilities.Keyboard as Keyboard)!;

    /// <inheritdoc/>
    ICursor? IGumService.CreateCursor() => new Cursor();

    /// <inheritdoc/>
    IInputReceiverKeyboard? IGumService.CreateKeyboard() => new Keyboard();

    /// <summary>
    /// Sets the clipboard TextBox copy and paste use. The Unity package passes one backed by
    /// <c>GUIUtility.systemCopyBuffer</c>.
    /// </summary>
    public void UseClipboard(IGumClipboard clipboard) => Clipboard = clipboard;

    /// <summary>
    /// Per-frame tick. Push this frame's input first, then call once before
    /// <see cref="GumServiceSkiaBase.Draw"/> with total elapsed seconds since startup. Runs the base's
    /// deferred-queue and animation tick, then pumps Forms input.
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
    public override void Update(double totalSeconds)
    {
        base.Update(totalSeconds);
        FormsUtilities.Update(totalSeconds, Root);
    }
}
