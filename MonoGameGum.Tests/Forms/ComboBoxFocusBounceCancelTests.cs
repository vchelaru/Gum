using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using Moq;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// An open drop-down closes on B, confirms on A and Enter, and closes on Escape no matter which of the
/// ComboBox and its own list last took focus (#6968).
/// </summary>
public class ComboBoxFocusBounceCancelTests : BaseTestClass
{
    private static ComboBox CreateComboBox(int itemCount = 4) =>
        ComboBoxListBoxGamepadTests.CreateComboBox(itemCount);

    private static ComboBox OpenByMouse(int selectedIndex = -1)
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = selectedIndex;
        comboBox.Visual.TryCallPush();
        comboBox.IsDropDownOpen.ShouldBeTrue();
        return comboBox;
    }

    private static void BounceFocus(ComboBox comboBox)
    {
        comboBox.IsFocused = true;
        comboBox.ListBox.IsFocused = true;
    }

    [Fact]
    public void OpenedByMouse_FocusBounced_BCloses()
    {
        ComboBox comboBox = OpenByMouse();
        BounceFocus(comboBox);
        PadHarness pad = new();

        pad.Tap(Buttons.B);

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void OpenedByMouse_FocusBounced_ACloses()
    {
        ComboBox comboBox = OpenByMouse();
        BounceFocus(comboBox);
        PadHarness pad = new();

        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void OpenedByMouse_FocusBounced_APicksTheFocusedItem()
    {
        ComboBox comboBox = OpenByMouse(selectedIndex: 0);
        BounceFocus(comboBox);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void OpenedByMouse_FocusBounced_DownStepsSelection()
    {
        ComboBox comboBox = OpenByMouse(selectedIndex: 0);
        BounceFocus(comboBox);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);

        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.IsDropDownOpen.ShouldBeTrue();
    }

    [Fact]
    public void OpenedByMouse_FocusBouncedToComboBoxOnly_BCloses()
    {
        ComboBox comboBox = OpenByMouse();
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.B);

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void OpenedByMouse_ListHasFocusWithoutItems_BCloses()
    {
        ComboBox comboBox = OpenByMouse();
        comboBox.ListBox.IsFocused = true;
        comboBox.ListBox.DoListItemsHaveFocus = false;
        PadHarness pad = new();

        pad.Tap(Buttons.B);

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void OpenedByA_FocusBounced_BCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        PadHarness pad = new();
        pad.Tap(Buttons.A);
        BounceFocus(comboBox);

        pad.Tap(Buttons.B);

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void OpenedFromCode_FocusBounced_BCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        BounceFocus(comboBox);
        PadHarness pad = new();

        pad.Tap(Buttons.B);

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void OpenedByMouse_FocusBounced_BDoesNotChangeSelection()
    {
        ComboBox comboBox = OpenByMouse(selectedIndex: 2);
        BounceFocus(comboBox);
        PadHarness pad = new();

        pad.Tap(Buttons.B);

        comboBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void OpenedByMouse_FocusBounced_KeyboardEnterClosesAndKeepsSelection()
    {
        ComboBox comboBox = OpenByMouse(selectedIndex: 1);
        BounceFocus(comboBox);
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Enter)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        InteractiveGue.CurrentInputReceiver.ShouldNotBeNull();
        InteractiveGue.CurrentInputReceiver.OnFocusUpdate();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
    }
}
