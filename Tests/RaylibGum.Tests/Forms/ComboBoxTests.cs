using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Keys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// End-to-end Raylib keyboard coverage for <see cref="ComboBox"/>. Proves the full
/// chain from a mocked <see cref="IInputReceiverKeyboard"/> through
/// <c>ComboBox.OnFocusUpdate</c> → <c>KeyCombo.IsComboPushed</c> →
/// <c>OpenAndFocusListBox</c> works on Raylib's shared Forms code path.
/// </summary>
public class ComboBoxTests : BaseTestClass
{
    [Fact]
    public void ComboBox_EnterPressed_Opens()
    {
        ComboBox comboBox = new ComboBox();

        comboBox.Visual.ShouldNotBeNull();
        comboBox.AddToRoot();

        comboBox.IsDropDownOpen.ShouldBeFalse();

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyPushed(Keys.Enter)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        comboBox.OnFocusUpdate();

        comboBox.IsDropDownOpen.ShouldBeTrue();
    }
}
