using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using SkiaGum.Tests.Forms.Linked;

namespace SkiaGum.Tests.Forms;

/// <summary>
/// Skia twins of the Button, ListBox and Slider gamepad tests that MonoGameGum.Tests builds from
/// Microsoft.Xna <c>GamePadState</c> (compiled out of the linked files by SKIA_LINKED_TESTS). These
/// drive the platform-neutral <see cref="GamePad"/> directly (#6511).
/// </summary>
public class GamepadLinkedGapTests : SkiaFormsTestBase
{
    [Fact]
    public void Button_OnFocusUpdate_ShouldPush_OnGamepadA()
    {
        Button button = new();
        button.IsFocused = true;
        bool wasPushed = false;
        button.Push += (_, _) => wasPushed = true;

        GamePad gamepad = new();
        gamepad.Activity(0);
        gamepad.SetButtonState(GamepadButton.A, true);
        InteractiveGue.CurrentGameTime = 1;
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        button.OnFocusUpdate();

        wasPushed.ShouldBeTrue();
    }

    [Fact]
    public void Slider_DPadRightPressed_RaisesValueChangedByUi()
    {
        Slider slider = new() { Minimum = 0, Maximum = 100, SmallChange = 5, Value = 50 };
        slider.IsFocused = true;

        GamePad gamepad = new();
        InteractiveGue.CurrentGameTime = 0;
        gamepad.Activity(0);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);
        bool wasRaised = false;
        slider.ValueChangedByUi += (_, _) => wasRaised = true;

        gamepad.SetButtonState(GamepadButton.DPadRight, true);
        InteractiveGue.CurrentGameTime = 1;
        gamepad.Activity(1);
        slider.OnFocusUpdate();

        slider.Value.ShouldBe(55);
        wasRaised.ShouldBeTrue();
    }

    [Fact]
    public void ListBox_RightStickUp_ScrollsAtTopLevelFocus()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < 50; i++)
        {
            listBox.Items!.Add($"Item {i}");
        }
        listBox.GamepadStickScrollSpeed = 100;
        listBox.VerticalScrollBarValue = 500;
        listBox.DoListItemsHaveFocus = false;

        GamePad gamepad = new();
        gamepad.SetRightStickPosition(0, 1f);
        gamepad.Activity(0);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        InteractiveGue.CurrentGameTime = 0;
        listBox.OnFocusUpdate();
        InteractiveGue.CurrentGameTime = 1;
        listBox.OnFocusUpdate();

        listBox.VerticalScrollBarValue.ShouldBe(400, tolerance: 0.01);
    }
}
