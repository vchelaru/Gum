using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Shouldly;
using Xunit;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace MonoGameGum.Tests.Input;

/// <summary>
/// Pins the protected input-source seams: a <see cref="Cursor"/>, <see cref="KeyboardStateProcessor"/> or
/// <see cref="Gum.Input.Keyboard"/> subclass replaces only where input comes from, and Gum's unmodified
/// Activity and Forms loop do the rest.
/// </summary>
public class InputSourceSeamTests : BaseTestClass
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    private TimeSpan _total;

    public override void Dispose()
    {
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        base.Dispose();
    }

    private sealed class SourcedCursor : Cursor
    {
        public SourcedCursor() : base((GameWindow?)null) { }

        public MouseState Next { get; set; }

        protected override MouseState GetMouseState() => Next;

        protected override TouchCollection GetTouchCollection() => new TouchCollection();
    }

    private sealed class SourcedKeyboardState : KeyboardStateProcessor
    {
        public XnaKeys[] Held { get; set; } = Array.Empty<XnaKeys>();

        protected override KeyboardState GetKeyboardState() => new KeyboardState(Held);
    }

    private sealed class SourcedKeyboard : Gum.Input.Keyboard
    {
        public string NextText { get; set; } = "";

        protected override string? GetTypedText()
        {
            string text = NextText;
            NextText = "";
            return text;
        }
    }

    private void Frame()
    {
        TimeSpan step = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        _total += step;
        Gum.GumService.Default.Update(new GameTime(_total, step));
    }

    private static MouseState MouseAt(int x, int y, bool leftDown) =>
        new MouseState(x, y, 0, leftDown ? ButtonState.Pressed : ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    [Fact]
    public void GetMouseState_Overridden_DrivesARealButtonClickThroughTheFormsLoop()
    {
        Button button = new Button();
        button.AddToRoot();
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        SourcedCursor cursor = new SourcedCursor();
        FormsUtilities.SetCursor(cursor);
        int x = (int)(button.Visual.AbsoluteLeft + button.Visual.AbsoluteWidth / 2);
        int y = (int)(button.Visual.AbsoluteTop + button.Visual.AbsoluteHeight / 2);

        cursor.Next = MouseAt(x, y, leftDown: false);
        Frame();
        cursor.Next = MouseAt(x, y, leftDown: true);
        Frame();
        cursor.Next = MouseAt(x, y, leftDown: false);
        Frame();

        clicks.ShouldBe(1);
        cursor.X.ShouldBe(x);
    }

    [Fact]
    public void GetKeyboardStateAndGetTypedText_Overridden_TypeIntoAFocusedTextBoxAndPressAKey()
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        SourcedKeyboardState state = new SourcedKeyboardState();
        SourcedKeyboard keyboard = new SourcedKeyboard { KeyboardStateProcessor = state };
        FormsUtilities.SetKeyboard(keyboard);
        // A real cursor with no buttons held, so nothing but the keyboard acts on the TextBox.
        FormsUtilities.SetCursor(new SourcedCursor { Next = MouseAt(-50, -50, leftDown: false) });
        textBox.IsFocused = true;

        keyboard.NextText = "abc";
        Frame();
        state.Held = new[] { XnaKeys.Back };
        Frame();
        state.Held = Array.Empty<XnaKeys>();
        Frame();

        textBox.Text.ShouldBe("ab");
        keyboard.KeyPushed(XnaKeys.Back).ShouldBeFalse();
    }
}
