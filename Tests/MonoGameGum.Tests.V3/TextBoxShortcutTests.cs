using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.V3;

/// <summary>
/// macOS users press Command, not Ctrl, for the clipboard and select-all shortcuts. A keyboard that reports
/// <see cref="IInputReceiverKeyboard.IsCommandDown"/> must drive them just as Ctrl does.
/// </summary>
public class TextBoxShortcutTests : BaseTestClass
{
    [Fact]
    public void DoKeyboardAction_CommandA_SelectsAll()
    {
        TextBox textBox = Focused("hello");

        textBox.DoKeyboardAction(new ShortcutKeyboard(Keys.A) { IsCommandDown = true });

        textBox.SelectionStart.ShouldBe(0);
        textBox.SelectionLength.ShouldBe(5);
    }

    [Fact]
    public void DoKeyboardAction_CtrlA_StillSelectsAll()
    {
        TextBox textBox = Focused("hello");

        textBox.DoKeyboardAction(new ShortcutKeyboard(Keys.A) { IsCtrlDown = true });

        textBox.SelectionLength.ShouldBe(5);
    }

    [Fact]
    public void DoKeyboardAction_AWithoutModifier_DoesNotSelect()
    {
        TextBox textBox = Focused("hello");

        textBox.DoKeyboardAction(new ShortcutKeyboard(Keys.A));

        textBox.SelectionLength.ShouldBe(0);
    }

    private static TextBox Focused(string text)
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        textBox.Text = text;
        InteractiveGue.CurrentInputReceiver = textBox;
        textBox.SelectionLength = 0;
        return textBox;
    }

    private sealed class ShortcutKeyboard(Keys typed) : IInputReceiverKeyboard
    {
        public bool IsShiftDown => false;
        public bool IsCtrlDown { get; init; }
        public bool IsAltDown => false;
        public bool IsCommandDown { get; init; }
        public IEnumerable<Keys> KeysTyped => [typed];

        public string GetStringTyped() => string.Empty;
        public void Activity(double gameTime) { }
        public bool KeyDown(Keys key) => false;
        public bool KeyPushed(Keys key) => false;
        public bool KeyReleased(Keys key) => false;
        public bool KeyTyped(Keys key) => key == typed;
    }
}
