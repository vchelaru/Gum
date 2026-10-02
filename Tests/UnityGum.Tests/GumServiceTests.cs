using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Keys = Gum.Forms.Input.Keys;

namespace UnityGum.Tests;

/// <summary>
/// The Unity <see cref="GumService"/> end to end: input the host pushes reaches Forms controls
/// through <see cref="GumService.Update"/>.
/// </summary>
public class GumServiceTests : BaseTestClass
{
    [Fact]
    public void Initialize_CreatesPushedCursorAndKeyboard()
    {
        IGumService.Default.ShouldBeSameAs(GumService.Default);
        GumService.Default.Cursor.ShouldNotBeNull();
        GumService.Default.Keyboard.ShouldNotBeNull();
    }

    [Fact]
    public void Update_PushedPressAndRelease_ClicksButton()
    {
        Button button = new Button();
        button.X = 100;
        button.Y = 50;
        button.Width = 200;
        button.Height = 60;
        button.AddToRoot();
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        Cursor cursor = GumService.Default.Cursor;

        cursor.SetMouseState(150, 70, leftDown: false, middleDown: false, rightDown: false);
        GumService.Default.Update(0);
        cursor.SetMouseState(150, 70, leftDown: true, middleDown: false, rightDown: false);
        GumService.Default.Update(0.016);
        cursor.SetMouseState(150, 70, leftDown: false, middleDown: false, rightDown: false);
        GumService.Default.Update(0.032);

        clickCount.ShouldBe(1);
    }

    [Fact]
    public void Update_PushedTypedText_ReachesFocusedTextBox()
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        GumService.Default.UseKeyboardDefaults();
        textBox.IsFocused = true;

        GumService.Default.Keyboard.AddTypedText('o');
        GumService.Default.Keyboard.AddTypedText('k');
        GumService.Default.Update(0);

        textBox.Text.ShouldBe("ok");
    }

    [Fact]
    public void Update_PushedGamepadState_ConnectsGamepadWithButtonsAndSticks()
    {
        GamePad gamepad = GumService.Default.Gamepads[1];

        gamepad.SetConnected(true);
        gamepad.SetButtonState(GamepadButton.A, isDown: true);
        gamepad.SetLeftStickPosition(0.75f, -0.5f);
        GumService.Default.Update(0);

        gamepad.IsConnected.ShouldBeTrue();
        gamepad.ButtonPushed(GamepadButton.A).ShouldBeTrue();
        gamepad.ButtonDown(GamepadButton.B).ShouldBeFalse();
        gamepad.LeftStick.X.ShouldBeGreaterThan(0);
        gamepad.LeftStick.Y.ShouldBeLessThan(0);
        GumService.Default.Gamepads[0].IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void UseClipboard_SetsClipboard()
    {
        IGumClipboard clipboard = new FakeClipboard();

        GumService.Default.UseClipboard(clipboard);

        GumService.Default.Clipboard.ShouldBeSameAs(clipboard);
    }

    private sealed class FakeClipboard : IGumClipboard
    {
        public string? GetText(Action? callback) => "";

        public void SetText(string text)
        {
        }
    }
}
