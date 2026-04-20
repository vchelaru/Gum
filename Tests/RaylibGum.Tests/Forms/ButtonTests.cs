using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using Keys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// End-to-end Raylib keyboard coverage for <see cref="Button"/>. Proves the full
/// chain from a mocked <see cref="IInputReceiverKeyboard"/> through
/// <c>ButtonBase.OnFocusUpdate</c> → <c>KeyCombo.IsComboPushed</c> →
/// <c>HandleClick</c> works on Raylib's shared Forms code path.
/// </summary>
public class ButtonTests : BaseTestClass
{
    [Fact]
    public void Button_EnterPressed_FiresClick()
    {
        Button button = new Button();

        button.Visual.ShouldNotBeNull(
            "Raylib FormsUtilities.InitializeDefaults should have registered a V2 ButtonVisual template, " +
            "so new Button() returns a control with a non-null Visual.");

        bool wasClicked = false;
        button.Click += (_, _) => wasClicked = true;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyPushed(Keys.Enter)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<int>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        button.OnFocusUpdate();

        wasClicked.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_ShouldAssignVisual()
    {
        Button button = new Button();

        button.Visual.ShouldNotBeNull();
    }
}
