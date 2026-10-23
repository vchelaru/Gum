using Gum.Input;
using System.Text;
using Microsoft.Xna.Framework;
using GumKeys = Gum.Forms.Input.Keys;
using Keyboard = Gum.Input.Keyboard;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using KeyboardState = Microsoft.Xna.Framework.Input.KeyboardState;
#if KNISKIA
// KniSkiaGum's cursor reads Gum's engine-neutral mouse state; its keyboard takes typed text through
// PolledKeyboard.AppendTypedText.
using ButtonState = Gum.Input.ButtonState;
using MouseState = Gum.Input.MouseState;
#else
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
#endif

namespace Gum.Automation;

/// <summary>A mouse button the driver can press.</summary>
public enum MouseButton
{
    /// <summary>The left button.</summary>
    Primary,
    /// <summary>The right button.</summary>
    Secondary,
    /// <summary>The wheel button.</summary>
    Middle,
}

/// <summary>
/// Gum's real <see cref="Cursor"/> whose input source is scripted state instead of the OS mouse: it
/// overrides only <see cref="Cursor.GetMouseState"/> (and, on MonoGame, <c>GetTouchCollection</c>), so
/// Gum's unmodified Activity does push, click, double-click, drag and roll-over. Positions are screen pixels.
/// </summary>
public sealed class ScriptedCursor : Cursor
{
    private int _x;
    private int _y;
    private int _wheel;

    /// <summary>Creates a cursor with no window and every button up at (0, 0).</summary>
    public ScriptedCursor() : base((GameWindow?)null) { }

    /// <summary>Whether the primary button is held on the next frame.</summary>
    public bool PrimaryHeld { get; set; }

    /// <summary>Whether the secondary button is held on the next frame.</summary>
    public bool SecondaryHeld { get; set; }

    /// <summary>Whether the middle button is held on the next frame.</summary>
    public bool MiddleHeld { get; set; }

    /// <summary>Sets the position read on the next frame, in screen pixels.</summary>
    public void MoveTo(int x, int y)
    {
        _x = x;
        _y = y;
    }

    /// <summary>Sets the position and whether the primary button is held on the next frame.</summary>
    public void Place(int x, int y, bool leftDown)
    {
        MoveTo(x, y);
        PrimaryHeld = leftDown;
    }

    /// <summary>Sets whether <paramref name="button"/> is held on the next frame.</summary>
    public void SetButton(MouseButton button, bool held)
    {
        switch (button)
        {
            case MouseButton.Primary: PrimaryHeld = held; break;
            case MouseButton.Secondary: SecondaryHeld = held; break;
            default: MiddleHeld = held; break;
        }
    }

    /// <summary>Turns the wheel; negative scrolls down. One detent is 120 wheel units.</summary>
    public void Scroll(int detents) => _wheel += detents * 120;

#if KNISKIA
    /// <inheritdoc/>
    protected override MouseState GetMouseState() => new MouseState
    {
        X = _x,
        Y = _y,
        ScrollWheelValue = _wheel,
        LeftButton = PrimaryHeld ? ButtonState.Pressed : ButtonState.Released,
        MiddleButton = MiddleHeld ? ButtonState.Pressed : ButtonState.Released,
        RightButton = SecondaryHeld ? ButtonState.Pressed : ButtonState.Released,
    };
#else
    /// <inheritdoc/>
    protected override MouseState GetMouseState() => new MouseState(_x, _y, _wheel,
        PrimaryHeld ? ButtonState.Pressed : ButtonState.Released,
        MiddleHeld ? ButtonState.Pressed : ButtonState.Released,
        SecondaryHeld ? ButtonState.Pressed : ButtonState.Released,
        ButtonState.Released, ButtonState.Released);

    /// <inheritdoc/>
    protected override TouchCollection GetTouchCollection() => new TouchCollection();
#endif
}

/// <summary>
/// Gum's real <see cref="Keyboard"/> whose input source is scripted: held keys come from a
/// <see cref="KeyboardStateProcessor"/> overriding <c>GetKeyboardState</c>, and typed text from an
/// override of <c>GetTypedText</c> (on KNI Skia, <c>AppendTypedText</c>). Key repeat and typed-key logic stay Gum's own.
/// </summary>
public sealed class ScriptedKeyboard : Keyboard
{
    private readonly ScriptedKeyboardState _state = new();
#if !KNISKIA
    private readonly StringBuilder _pendingText = new();
#endif

    /// <summary>Creates a keyboard with no keys held and no window.</summary>
    public ScriptedKeyboard() : base(null)
    {
        KeyboardStateProcessor = _state;
    }

    /// <summary>Sets whether <paramref name="key"/> is held on the next frame.</summary>
    public void SetKey(GumKeys key, bool held)
    {
        Keys xnaKey = (Keys)(int)key;
        if (held) { _state.Held.Add(xnaKey); } else { _state.Held.Remove(xnaKey); }
    }

#if KNISKIA
    /// <summary>Queues text delivered to the focused control on the next frame.</summary>
    public void QueueText(string text) => AppendTypedText(text);
#else
    /// <summary>Queues text delivered to the focused control on the next frame.</summary>
    public void QueueText(string text) => _pendingText.Append(text);

    /// <inheritdoc/>
    protected override string GetTypedText()
    {
        string text = _pendingText.ToString();
        _pendingText.Clear();
        return text;
    }
#endif

    private sealed class ScriptedKeyboardState : KeyboardStateProcessor
    {
        public HashSet<Keys> Held { get; } = new();

        protected override KeyboardState GetKeyboardState() => new KeyboardState(Held.ToArray());
    }
}
