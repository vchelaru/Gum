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
    [Theory]
    [InlineData(Keys.C, "Copy")]
    [InlineData(Keys.X, "Cut")]
    [InlineData(Keys.V, "Paste")]
    public void DoKeyboardAction_CommandClipboardKey_RunsHandler(Keys key, string expectedHandler)
    {
        SpyTextBox textBox = Focused("hello");

        textBox.DoKeyboardAction(new ShortcutKeyboard(key) { IsCommandDown = true });

        textBox.HandlersRun.ShouldBe([expectedHandler]);
    }

    [Theory]
    [InlineData(Keys.X)]
    [InlineData(Keys.V)]
    public void DoKeyboardAction_CommandCutOrPasteWhenReadOnly_RunsNoHandler(Keys key)
    {
        SpyTextBox textBox = Focused("hello");
        textBox.IsReadOnly = true;

        textBox.DoKeyboardAction(new ShortcutKeyboard(key) { IsCommandDown = true });

        textBox.HandlersRun.ShouldBeEmpty();
    }

    [Fact]
    public void DoKeyboardAction_CommandA_SelectsAll()
    {
        SpyTextBox textBox = Focused("hello");

        textBox.DoKeyboardAction(new ShortcutKeyboard(Keys.A) { IsCommandDown = true });

        textBox.SelectionStart.ShouldBe(0);
        textBox.SelectionLength.ShouldBe(5);
    }

    [Fact]
    public void DoKeyboardAction_CommandHeld_IgnoresTypedText()
    {
        SpyTextBox textBox = Focused("hello");

        textBox.DoKeyboardAction(new ShortcutKeyboard(Keys.A) { IsCommandDown = true, StringTyped = "a" });

        textBox.Text.ShouldBe("hello");
        textBox.SelectionLength.ShouldBe(5);
    }

    [Fact]
    public void DoKeyboardAction_CtrlHeld_KeepsTypedText()
    {
        // AltGr arrives as Ctrl+Alt on Windows and types real characters, so Ctrl must not filter text.
        SpyTextBox textBox = Focused("hello");
        textBox.CaretIndex = 5;

        textBox.DoKeyboardAction(new ShortcutKeyboard(Keys.Q) { IsCtrlDown = true, StringTyped = "@" });

        textBox.Text.ShouldBe("hello@");
    }

    [Fact]
    public void DoKeyboardAction_CtrlA_StillSelectsAll()
    {
        SpyTextBox textBox = Focused("hello");

        textBox.DoKeyboardAction(new ShortcutKeyboard(Keys.A) { IsCtrlDown = true });

        textBox.SelectionLength.ShouldBe(5);
    }

    [Fact]
    public void DoKeyboardAction_AWithoutModifier_DoesNotSelect()
    {
        SpyTextBox textBox = Focused("hello");

        textBox.DoKeyboardAction(new ShortcutKeyboard(Keys.A));

        textBox.SelectionLength.ShouldBe(0);
    }

    private static SpyTextBox Focused(string text)
    {
        SpyTextBox textBox = new SpyTextBox();
        textBox.AddToRoot();
        textBox.Text = text;
        InteractiveGue.CurrentInputReceiver = textBox;
        textBox.SelectionLength = 0;
        return textBox;
    }

    private sealed class SpyTextBox : TextBox
    {
        public List<string> HandlersRun { get; } = new List<string>();

        protected override void HandleCopy() => HandlersRun.Add("Copy");
        protected override void HandleCut() => HandlersRun.Add("Cut");
        protected override void HandlePaste() => HandlersRun.Add("Paste");
    }

    private sealed class ShortcutKeyboard(Keys typed) : IInputReceiverKeyboard
    {
        public bool IsShiftDown => false;
        public bool IsCtrlDown { get; init; }
        public bool IsAltDown => false;
        public bool IsCommandDown { get; init; }
        public string StringTyped { get; init; } = string.Empty;
        public IEnumerable<Keys> KeysTyped => [typed];

        public string GetStringTyped() => StringTyped;
        public void Activity(double gameTime) { }
        public bool KeyDown(Keys key) => false;
        public bool KeyPushed(Keys key) => false;
        public bool KeyReleased(Keys key) => false;
        public bool KeyTyped(Keys key) => key == typed;
    }
}
