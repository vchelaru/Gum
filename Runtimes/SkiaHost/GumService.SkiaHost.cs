#nullable enable

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;

namespace Gum;

// The typed half of an interactive Skia host's GumService, compiled into each host assembly
// (SilkNetGum, StrideGum, UnityGum) through SkiaInputHost.props. It lives in source, not in
// GumServiceSkiaInteractiveBase, because Cursor and Keyboard are the host's own Gum.Input types.
// The host's GumService declares the base class and ApplyGamePadState, and supplies the devices
// through CreateHostCursor and CreateHostKeyboard. CreateCursor and CreateKeyboard are here so every
// host's cursor and keyboard is owned by the service and disposed with it.
public partial class GumService
{
    private static GumService? _default;

    /// <summary>
    /// Creates this host's cursor, attached to its devices.
    /// </summary>
    private partial Cursor CreateHostCursor();

    /// <summary>
    /// Creates this host's keyboard, attached to its devices, or returns null when the host has no
    /// keyboard device (headless).
    /// </summary>
    private partial Keyboard? CreateHostKeyboard();

    /// <inheritdoc/>
    Gum.Wireframe.ICursor? IGumService.CreateCursor() => OwnInput(CreateHostCursor());

    /// <inheritdoc/>
    IInputReceiverKeyboard? IGumService.CreateKeyboard() =>
        // With no device, an inert device-less Keyboard rather than null: FormsUtilities.Update
        // ticks keyboard.Activity() unconditionally, so a null here would NRE on the first Update.
        OwnInput(CreateHostKeyboard() ?? new Keyboard());

    /// <summary>
    /// The singleton service instance.
    /// </summary>
    public static GumService Default => _default ??= new GumService();

    /// <summary>
    /// Gets the default cursor.
    /// </summary>
    /// <remarks>
    /// Null while a custom cursor is installed with <see cref="FormsUtilities.SetCursor"/>.
    /// <see cref="FormsUtilities.Cursor"/> returns whichever cursor is active.
    /// </remarks>
    public Cursor Cursor => (FormsUtilities.Cursor as Cursor)!;

    /// <summary>
    /// Gets the default keyboard.
    /// </summary>
    /// <remarks>
    /// Null while a custom keyboard is installed with <see cref="FormsUtilities.SetKeyboard"/>.
    /// <see cref="FormsUtilities.Keyboard"/> returns whichever keyboard is active.
    /// </remarks>
    public Keyboard Keyboard => (FormsUtilities.Keyboard as Keyboard)!;
}
