using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// An open ComboBox drop-down closes when its owner stops being a valid interactive owner: it becomes
/// disabled (#6957) or focus moves to a control outside the ComboBox and its own list (#6958).
/// </summary>
public class ComboBoxPopupClosesTests : BaseTestClass
{
    private static ComboBox CreateComboBox(int itemCount = 4) =>
        ComboBoxListBoxGamepadTests.CreateComboBox(itemCount);

    private static Button CreateButton()
    {
        Button button = new();
        button.AddToRoot();
        return button;
    }

    #region Existing behavior, pinned

    [Fact]
    public void EnabledComboBox_AOpensThenBCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        comboBox.IsDropDownOpen.ShouldBeTrue();

        pad.Tap(Buttons.B);
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void EnabledComboBox_PickWithA_ClosesAndKeepsSelection()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void EnabledComboBox_OpenedFromCode_StaysOpenAndClosesFromCode()
    {
        ComboBox comboBox = CreateComboBox();

        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen.ShouldBeTrue();

        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    #endregion

    #region 6957: disabled while open

    [Fact]
    public void ComboBoxDisabledWhileOpen_GamepadDoesNotChangeSelection()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        PadHarness pad = new();
        pad.Tap(Buttons.A);

        comboBox.IsEnabled = false;
        pad.Tap(Buttons.DPadDown);

        comboBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void DisabledWhileOpenByGamepad_DropDownCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        PadHarness pad = new();
        pad.Tap(Buttons.A);

        comboBox.IsEnabled = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        FrameworkElement.PopupRoot.Children!.ShouldNotContain(comboBox.ListBox.Visual);
    }

    [Fact]
    public void DisabledWhileOpenFromCode_DropDownCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;

        comboBox.IsEnabled = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void DisabledWhileOpenByMousePush_DropDownCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.Visual.TryCallPush();
        comboBox.IsDropDownOpen.ShouldBeTrue();

        comboBox.Visual.IsEnabled = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void DisabledWhileOpen_SelectionIsUnchangedByTheClose()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 2;
        comboBox.IsDropDownOpen = true;

        comboBox.IsEnabled = false;

        comboBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void DisabledWhileOpen_ReEnabledThenOpensAgain()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        comboBox.IsEnabled = false;

        comboBox.IsEnabled = true;
        comboBox.IsDropDownOpen = true;

        comboBox.IsDropDownOpen.ShouldBeTrue();
        FrameworkElement.PopupRoot.Children!.ShouldContain(comboBox.ListBox.Visual);
    }

    [Fact]
    public void DisabledWhileClosed_StaysClosed()
    {
        ComboBox comboBox = CreateComboBox();

        comboBox.IsEnabled = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void HiddenWhileOpen_DropDownClosesAndPopupLeavesFocus()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;

        comboBox.IsVisible = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldNotBe(comboBox.ListBox);
    }

    [Fact]
    public void RemovedFromRootWhileOpen_DropDownCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;

        comboBox.RemoveFromRoot();

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    #endregion

    #region 6958: focus moves outside

    [Fact]
    public void FocusMovedToOtherControlWhileOpen_DropDownCloses()
    {
        ComboBox comboBox = CreateComboBox();
        Button other = CreateButton();
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;

        other.IsFocused = true;

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void FocusMovedByTabWhileOpen_DropDownCloses()
    {
        ComboBox comboBox = CreateComboBox();
        Button other = CreateButton();
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;

        comboBox.HandleTab(TabDirection.Down);

        other.IsFocused.ShouldBeTrue();
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void FocusMovedWhileOpenedWithoutFocus_DropDownClosesAndFocusStaysOnTarget()
    {
        ComboBox comboBox = CreateComboBox();
        Button other = CreateButton();
        comboBox.IsDropDownOpen = true;

        other.IsFocused = true;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        other.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(other);
    }

    [Fact]
    public void FocusMoveClose_DoesNotStealFocusBack()
    {
        ComboBox comboBox = CreateComboBox();
        Button other = CreateButton();
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;

        other.IsFocused = true;

        comboBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBe(other);
    }

    [Fact]
    public void FocusMovedWhileOpen_SelectionIsUnchanged()
    {
        ComboBox comboBox = CreateComboBox();
        Button other = CreateButton();
        comboBox.SelectedIndex = 1;
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;

        other.IsFocused = true;

        comboBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void FocusMovingBetweenComboBoxAndItsListDoesNotClose()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;

        comboBox.IsFocused = true;
        comboBox.ListBox.IsFocused = true;
        comboBox.IsFocused = true;
        comboBox.ListBox.IsFocused = true;

        comboBox.IsDropDownOpen.ShouldBeTrue();
    }

    [Fact]
    public void OpenWithAThenNavigateRows_StaysOpen()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.DPadDown);

        comboBox.IsDropDownOpen.ShouldBeTrue();
    }

    [Fact]
    public void ReopenAfterFocusClose_Works()
    {
        ComboBox comboBox = CreateComboBox();
        Button other = CreateButton();
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;
        other.IsFocused = true;

        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen.ShouldBeTrue();

        other.IsFocused = true;
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void FocusMovedAfterClose_DoesNothing()
    {
        ComboBox comboBox = CreateComboBox();
        Button other = CreateButton();
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        other.IsFocused = true;

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    #endregion
}
