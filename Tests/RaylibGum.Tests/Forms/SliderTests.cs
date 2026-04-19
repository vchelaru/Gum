using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Keys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// End-to-end Raylib keyboard coverage for <see cref="Slider"/>. Proves the full
/// chain from a mocked <see cref="IInputReceiverKeyboard"/> through
/// <c>Slider.OnFocusUpdate</c> → <c>KeyTyped(Keys.Right)</c> advances
/// <c>Value</c> by <c>SmallChange</c> on Raylib.
/// </summary>
public class SliderTests : BaseTestClass
{
    [Fact]
    public void Slider_RightArrowPressed_IncreasesValue()
    {
        Slider slider = new Slider();

        slider.Visual.ShouldNotBeNull();
        slider.AddToRoot();

        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.SmallChange = 5;
        slider.Value = 50;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyTyped(Keys.Right)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<int>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        slider.OnFocusUpdate();

        slider.Value.ShouldBe(55);
    }
}
