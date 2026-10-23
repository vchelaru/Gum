using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Gum.Input;

// Shared by link with the KNI Skia host (Runtimes/KniSkiaGum), which mirrors KniGum's keyboard API.

/// <summary>
/// An <see cref="IInputReceiverKeyboard"/> that also reports XNA <see cref="Keys"/>.
/// </summary>
public interface IInputReceiverKeyboardMonoGame : IInputReceiverKeyboard
{
    new IReadOnlyCollection<Microsoft.Xna.Framework.Input.Keys> KeysTyped { get; }
    bool KeyDown(Keys key);
    bool KeyPushed(Keys key);
    bool KeyReleased(Keys key);
    bool KeyTyped(Keys key);
}
