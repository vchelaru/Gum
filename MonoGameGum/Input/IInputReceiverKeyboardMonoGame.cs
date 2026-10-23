using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Gum.Input;

// Shared by link with the KNI Skia host (Runtimes/KniSkiaGum), which mirrors KniGum's keyboard API.

/// <summary>
/// An <see cref="IInputReceiverKeyboard"/> that also reports XNA <see cref="Keys"/>.
/// </summary>
public interface IInputReceiverKeyboardMonoGame : IInputReceiverKeyboard
{
    new IReadOnlyCollection<Microsoft.Xna.Framework.Input.Keys> KeysTyped { get; }
    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(XnaKeysObsolescence.Message)]
    bool KeyDown(Keys key);
    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(XnaKeysObsolescence.Message)]
    bool KeyPushed(Keys key);
    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(XnaKeysObsolescence.Message)]
    bool KeyReleased(Keys key);
    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(XnaKeysObsolescence.Message)]
    bool KeyTyped(Keys key);
}
