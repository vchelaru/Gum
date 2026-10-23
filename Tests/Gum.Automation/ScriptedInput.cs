using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using GumKeys = Gum.Forms.Input.Keys;

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
/// Gum's real <see cref="Cursor"/> fed from scripted state instead of <c>Mouse.GetState()</c>, so
/// push, click, double-click, drag and roll-over logic is Gum's own and the OS mouse is never read.
/// </summary>
/// <remarks>
/// Re-implements <see cref="ICursor.Activity"/>, which is what Forms calls each frame. Calling
/// <see cref="Cursor.Activity(double)"/> through the concrete <see cref="Cursor"/> type would poll the
/// real mouse, so hosts must only reach it through <see cref="ICursor"/>. Positions are screen pixels.
/// </remarks>
public sealed class ScriptedCursor : Cursor, ICursor
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

    /// <inheritdoc/>
    public new void Activity(double gameTime)
    {
        MouseState state = new MouseState(_x, _y, _wheel,
            PrimaryHeld ? ButtonState.Pressed : ButtonState.Released,
            MiddleHeld ? ButtonState.Pressed : ButtonState.Released,
            SecondaryHeld ? ButtonState.Pressed : ButtonState.Released,
            ButtonState.Released, ButtonState.Released);
        Activity(gameTime, state, new TouchCollection(), isMobile: false);
    }
}

/// <summary>
/// A keyboard whose held keys and typed text come from the script. Key repeat follows Gum's own
/// <see cref="PolledKeyboard"/> timing on the game time Forms passes in.
/// </summary>
public sealed class ScriptedKeyboard : PolledKeyboard
{
    private static readonly GumKeys[] AllKeys = Enum.GetValues<GumKeys>().Distinct().ToArray();
    private readonly HashSet<GumKeys> _held = new();

    /// <inheritdoc/>
    protected override IEnumerable<GumKeys> SupportedKeys => AllKeys;

    /// <inheritdoc/>
    protected override bool IsDeviceKeyDown(GumKeys key) => _held.Contains(key);

    /// <summary>Sets whether <paramref name="key"/> is held on the next frame.</summary>
    public void SetKey(GumKeys key, bool held)
    {
        if (held) { _held.Add(key); } else { _held.Remove(key); }
    }

    /// <summary>Queues text delivered to the focused control on the next frame.</summary>
    public void QueueText(string text) => AppendTypedText(text);
}
