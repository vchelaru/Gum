using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Keys = Gum.Forms.Input.Keys;

namespace SilkNetGum.Tests.Forms;

/// <summary>
/// Behavioral integration tests for the Silk.NET-backed Forms input pipeline (#3652): mouse
/// click-through, TextBox character entry, and Tab focus traversal, each driven through the same
/// production seams the real Silk.NET game loop uses (<see cref="FormsUtilities.SetCursor"/> +
/// <see cref="GumService.Update(double)"/> for mouse, and the real
/// <c>KeyCombo</c>/<c>FrameworkElement.KeyboardsForUiControl</c> pipeline via
/// <see cref="FrameworkElement.OnFocusUpdate"/> for keyboard), rather than only the isolated
/// <see cref="Keyboard.GetStringTyped"/> seam already covered by <c>KeyboardSilkTests</c>.
/// </summary>
public class FormsInteractionTests : BaseTestClass
{
    [Fact]
    public void DoUiActivityRecursively_KeyCharsTyped_AppendsToFocusedTextBoxText()
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.IsFocused = true;

        Mock<IInputReceiverKeyboard> keyboard = new();
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys>());
        keyboard.Setup(k => k.GetStringTyped()).Returns("hi");

        // The same extension method FormsUtilities.Update delegates to each frame -- exercising it
        // directly drives the real DoKeyboardAction -> HandleCharEntered -> Text pipeline rather
        // than just Keyboard.GetStringTyped() in isolation.
        GumService.Default.Root.DoUiActivityRecursively(FrameworkElement.MainCursor, keyboard.Object, 0);

        textBox.Text.ShouldBe("hi");
    }

    [Fact]
    public void OnFocusUpdate_TabKeyPushed_MovesFocusToNextControl()
    {
        StackPanel stackPanel = new();
        stackPanel.AddToRoot();

        Button button1 = new();
        stackPanel.AddChild(button1);
        Button button2 = new();
        stackPanel.AddChild(button2);

        button1.IsFocused = true;

        Mock<IInputReceiverKeyboard> keyboard = new();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        // Routes through the real TabKeyCombos / KeyCombo.IsComboPushed pipeline (the same one
        // GumUI.UseKeyboardDefaults wires up for a real Silk keyboard), not a direct HandleTab call.
        button1.OnFocusUpdate();

        button1.IsFocused.ShouldBeFalse();
        button2.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Update_MouseClickOverButton_FiresClick()
    {
        Button button = new();
        button.AddToRoot();

        bool wasClicked = false;
        button.Click += (_, _) => wasClicked = true;

        Mock<ICursor> cursor = new();
        cursor.Setup(c => c.PrimaryClick).Returns(true);
        cursor.Setup(c => c.WindowPushed).Returns(button.Visual);
        FormsUtilities.SetCursor(cursor.Object);

        GumService.Default.Update(0);

        wasClicked.ShouldBeTrue();
    }
}
