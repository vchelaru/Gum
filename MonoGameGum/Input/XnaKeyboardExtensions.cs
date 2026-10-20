using Gum.Wireframe;
using System;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace Gum.Forms.Controls;

/// <summary>
/// XNA-flavored overloads on <see cref="IInputReceiverKeyboard"/> so MonoGame
/// callers can continue passing <c>Microsoft.Xna.Framework.Input.Keys</c> values
/// directly. The base interface methods take <see cref="Gum.Forms.Input.Keys"/>
/// (the shared, platform-neutral enum); these extensions do the int round-trip
/// — the enums align numerically — and forward to the underlying call. Obsolete: pass
/// <see cref="Gum.Forms.Input.Keys"/> instead.
/// </summary>
public static class XnaKeyboardExtensions
{
    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(Gum.Input.XnaKeysObsolescence.Message)]
    public static bool KeyDown(this IInputReceiverKeyboard keyboard, XnaKeys key)
        => keyboard.KeyDown((Gum.Forms.Input.Keys)(int)key);

    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(Gum.Input.XnaKeysObsolescence.Message)]
    public static bool KeyPushed(this IInputReceiverKeyboard keyboard, XnaKeys key)
        => keyboard.KeyPushed((Gum.Forms.Input.Keys)(int)key);

    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(Gum.Input.XnaKeysObsolescence.Message)]
    public static bool KeyReleased(this IInputReceiverKeyboard keyboard, XnaKeys key)
        => keyboard.KeyReleased((Gum.Forms.Input.Keys)(int)key);

    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(Gum.Input.XnaKeysObsolescence.Message)]
    public static bool KeyTyped(this IInputReceiverKeyboard keyboard, XnaKeys key)
        => keyboard.KeyTyped((Gum.Forms.Input.Keys)(int)key);
}
