#nullable enable

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;

namespace Gum;

// The typed half of an interactive Skia host's GumService, compiled into each host assembly
// (SilkNetGum, StrideGum, UnityGum) through SkiaInputHost.props. It lives in source, not in
// GumServiceSkiaInteractiveBase, because Cursor and Keyboard are the host's own Gum.Input types.
// The host's GumService declares the base class and the capability hooks (CreateCursor,
// CreateKeyboard, ApplyGamePadState).
public partial class GumService
{
    private static GumService? _default;

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
