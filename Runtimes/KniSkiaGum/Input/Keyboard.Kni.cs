using System;
using System.Collections.Generic;
using System.Linq;
using XnaGameWindow = Microsoft.Xna.Framework.GameWindow;
using XnaKeyboard = Microsoft.Xna.Framework.Input.Keyboard;
using XnaKeyboardState = Microsoft.Xna.Framework.Input.KeyboardState;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Keyboard for KNI. Down-state is polled from <c>Keyboard.GetState</c> once per frame;
/// <see cref="PolledKeyboard"/> derives push/release edges and key repeat from it. Typed text comes
/// from the game window's <c>TextInput</c> event. Gum's <see cref="GumKeys"/> has the same names and
/// numeric values as KNI's <c>Keys</c> (a permanent commitment documented on the enum), so keys cast
/// across with no translation table.
/// </summary>
public class Keyboard : PolledKeyboard
{
    // Every named Gum key except None, which has no key. Fixed: the enum is a permanent commitment.
    private static readonly GumKeys[] _supportedKeys =
        Enum.GetValues<GumKeys>().Where(key => key != GumKeys.None).Distinct().ToArray();

    private XnaKeyboardState _state;

    /// <summary>
    /// Constructs a keyboard. When <paramref name="window"/> is supplied, typed text is captured from
    /// its <c>TextInput</c> event.
    /// </summary>
    public Keyboard(XnaGameWindow? window = null)
    {
        if (window != null)
        {
            window.TextInput += HandleTextInput;
            OnDispose(() => window.TextInput -= HandleTextInput);
        }
    }

    private void HandleTextInput(object? sender, Microsoft.Xna.Framework.Input.TextInputEventArgs e) =>
        AppendTypedText(e.Character);

    /// <summary>
    /// Reads the keyboard. Replaced in tests; KNI's <c>Keyboard.GetState</c> is a static device read.
    /// </summary>
    internal Func<XnaKeyboardState> ReadKeyboard { get; set; } = XnaKeyboard.GetState;

    /// <inheritdoc/>
    protected override IEnumerable<GumKeys> SupportedKeys => _supportedKeys;

    /// <inheritdoc/>
    protected override void BeginPoll() => _state = ReadKeyboard();

    /// <inheritdoc/>
    protected override bool IsDeviceKeyDown(GumKeys key) => _state.IsKeyDown((XnaKeys)(int)key);
}
