using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Raylib_cs;
using Shouldly;

namespace RaylibGum.Tests.Inputs;

/// <summary>
/// Pins the protected input-source seams on raylib: a <see cref="Cursor"/> overriding
/// <c>GetMouseState</c>/<c>GetTouchCollection</c>, and a <see cref="Keyboard"/> overriding its device reads,
/// drive real Forms controls through Gum's unmodified Activity and Forms loop.
/// </summary>
public class InputSourceSeamTests : BaseTestClass
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    private double _seconds;

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
        public MouseState Next { get; set; }

        protected override MouseState GetMouseState() => Next;

        protected override TouchCollection GetTouchCollection() => new TouchCollection();
    }

    private sealed class SourcedKeyboard : Keyboard
    {
        private readonly Queue<int> _chars = new();
        private readonly HashSet<KeyboardKey> _held = new();
        private readonly HashSet<KeyboardKey> _heldLastFrame = new();

        public void Type(string text)
        {
            foreach (char character in text)
            {
                _chars.Enqueue(character);
            }
        }

        public void SetHeld(params KeyboardKey[] keys)
        {
            _heldLastFrame.Clear();
            _heldLastFrame.UnionWith(_held);
            _held.Clear();
            _held.UnionWith(keys);
        }

        protected override int GetCharPressed() => _chars.Count > 0 ? _chars.Dequeue() : 0;
        protected override bool IsKeyDown(KeyboardKey key) => _held.Contains(key);
        protected override bool IsKeyPressed(KeyboardKey key) => _held.Contains(key) && !_heldLastFrame.Contains(key);
        protected override bool IsKeyReleased(KeyboardKey key) => !_held.Contains(key) && _heldLastFrame.Contains(key);
        protected override bool IsKeyPressedRepeat(KeyboardKey key) => false;
    }

    private void Frame()
    {
        _seconds += 1 / 60.0;
        Gum.GumService.Default.Update(_seconds);
    }

    private static MouseState MouseAt(int x, int y, bool leftDown) => new MouseState
    {
        X = x,
        Y = y,
        LeftButton = leftDown ? ButtonState.Pressed : ButtonState.Released,
    };

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
    }

    [Fact]
    public void KeyboardDeviceReads_Overridden_TypeIntoAFocusedTextBoxAndPressAKey()
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        SourcedKeyboard keyboard = new SourcedKeyboard();
        FormsUtilities.SetKeyboard(keyboard);
        FormsUtilities.SetCursor(new SourcedCursor { Next = MouseAt(-50, -50, leftDown: false) });
        textBox.IsFocused = true;

        keyboard.Type("abc");
        Frame();
        keyboard.SetHeld(KeyboardKey.Backspace);
        Frame();
        keyboard.SetHeld();
        Frame();

        textBox.Text.ShouldBe("ab");
    }
}
