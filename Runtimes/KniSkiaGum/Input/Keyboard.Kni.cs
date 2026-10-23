using System;
using System.Collections.Generic;
using System.Linq;
using XnaGameWindow = Microsoft.Xna.Framework.GameWindow;
using XnaKeyboardState = Microsoft.Xna.Framework.Input.KeyboardState;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Keyboard for KNI. Down-state is polled through <see cref="KeyboardStateProcessor"/> once per frame;
/// <see cref="PolledKeyboard"/> derives push/release edges and key repeat from it. Typed text comes
/// from the game window's <c>TextInput</c> event. Gum's <see cref="GumKeys"/> has the same names and
/// numeric values as KNI's <c>Keys</c> (a permanent commitment documented on the enum), so keys cast
/// across with no translation table. Mirrors KniGum's keyboard API, including its XNA <c>Keys</c> members.
/// </summary>
public class Keyboard : PolledKeyboard, IInputReceiverKeyboardMonoGame
{
    /// <summary>
    /// The number of key codes, matching KniGum's <c>Keyboard.NumberOfKeys</c>.
    /// </summary>
    public const int NumberOfKeys = 255;

    // Every named Gum key except None, which has no key. Fixed: the enum is a permanent commitment.
    private static readonly GumKeys[] _supportedKeys =
        Enum.GetValues<GumKeys>().Where(key => key != GumKeys.None).Distinct().ToArray();

    private readonly List<XnaKeys> _keysTyped = new List<XnaKeys>();

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
    /// Gets or sets the processor that reads the device each frame.
    /// </summary>
    /// <remarks>
    /// Set by default on the Keyboard and should not be modified except for unit tests or to customize
    /// how input reading is done in advanced scenarios.
    /// </remarks>
    public KeyboardStateProcessor KeyboardStateProcessor { get; set; } = new KeyboardStateProcessor();

    /// <summary>
    /// Reads the keyboard for the current <see cref="KeyboardStateProcessor"/>. Replaced in tests; KNI's
    /// <c>Keyboard.GetState</c> is a static device read.
    /// </summary>
    internal Func<XnaKeyboardState> ReadKeyboard
    {
        get => KeyboardStateProcessor.ReadKeyboardState;
        set => KeyboardStateProcessor.ReadKeyboardState = value;
    }

    /// <summary>
    /// Returns the keys typed this frame (pushed, or repeating while held).
    /// </summary>
    public IReadOnlyCollection<XnaKeys> KeysTyped
    {
        get
        {
            _keysTyped.Clear();
            foreach (GumKeys key in _supportedKeys)
            {
                if (KeyTyped(key))
                {
                    _keysTyped.Add((XnaKeys)(int)key);
                }
            }
            return _keysTyped;
        }
    }

    /// <summary>
    /// Returns whether the specified key is a letter (A through Z).
    /// </summary>
    public bool IsKeyLetter(GumKeys key) => key >= GumKeys.A && key <= GumKeys.Z;

    /// <summary>
    /// XNA <see cref="XnaKeys"/> overload. Use the <see cref="GumKeys"/> overload instead.
    /// </summary>
    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(XnaKeysObsolescence.Message)]
    public bool KeyDown(XnaKeys key) => KeyDown((GumKeys)(int)key);

    /// <summary>
    /// XNA <see cref="XnaKeys"/> overload. Use the <see cref="GumKeys"/> overload instead.
    /// </summary>
    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(XnaKeysObsolescence.Message)]
    public bool KeyPushed(XnaKeys key) => KeyPushed((GumKeys)(int)key);

    /// <summary>
    /// XNA <see cref="XnaKeys"/> overload. Use the <see cref="GumKeys"/> overload instead.
    /// </summary>
    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(XnaKeysObsolescence.Message)]
    public bool KeyReleased(XnaKeys key) => KeyReleased((GumKeys)(int)key);

    /// <summary>
    /// XNA <see cref="XnaKeys"/> overload. Use the <see cref="GumKeys"/> overload instead.
    /// </summary>
    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(XnaKeysObsolescence.Message)]
    public bool KeyTyped(XnaKeys key) => KeyTyped((GumKeys)(int)key);

    /// <summary>
    /// XNA <see cref="XnaKeys"/> overload. Use the <see cref="GumKeys"/> overload instead.
    /// </summary>
    // Obsoleted on 2026-10-20 (#5028). Earliest error: March 2027. Earliest removal: July 2027.
    [Obsolete(XnaKeysObsolescence.Message)]
    public bool IsKeyLetter(XnaKeys key) => IsKeyLetter((GumKeys)(int)key);

    /// <inheritdoc/>
    protected override IEnumerable<GumKeys> SupportedKeys => _supportedKeys;

    /// <inheritdoc/>
    protected override void BeginPoll() => KeyboardStateProcessor.Update();

    /// <inheritdoc/>
    protected override bool IsDeviceKeyDown(GumKeys key) => KeyboardStateProcessor.IsKeyDown((XnaKeys)(int)key);
}
