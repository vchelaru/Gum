using System.Collections.ObjectModel;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using Moq;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Splash-damage sweep (#6931, #6932, #6933): gamepad and keyboard behavior of ComboBox, ListBox and
/// ListBoxItem across open/close paths, disabled items and controls, empty and single-item lists,
/// orientation, items changing while open, and focus after each transition.
/// ListBoxes never leaving the list with left/right (#6938) is not covered here.
/// </summary>
public class ComboBoxListBoxGamepadSweepTests : BaseTestClass
{
    private static ComboBox CreateComboBox(int itemCount = 4) =>
        ComboBoxListBoxGamepadTests.CreateComboBox(itemCount);

    private static ListBox CreateListBox(int itemCount = 4) =>
        ComboBoxListBoxGamepadTests.CreateListBox(itemCount);

    private static void EnterItems(ListBox listBox, int selectedIndex = 0)
    {
        listBox.SelectedIndex = selectedIndex;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
    }

    private static Mock<IInputReceiverKeyboardMonoGame> AddKeyboard(Gum.Forms.Input.Keys typedKey)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyTyped(typedKey)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
        return keyboard;
    }

    #region Open and close paths

    [Fact]
    public void OpenWithA_ListBoxHasFocusAndItems()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 1;
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(comboBox.ListBox);
        comboBox.ListBox.DoListItemsHaveFocus.ShouldBeTrue();
        comboBox.ListBox.ListBoxItems[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void OpenFromCode_ListBoxHasFocusAndSelectedRowFocused()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 2;

        comboBox.IsDropDownOpen = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(comboBox.ListBox);
        comboBox.ListBox.ListBoxItems[2].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void OpenWithA_NothingSelected_DownSelectsFirstItem()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Tap(Buttons.DPadDown);

        comboBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void OpenWithA_NothingSelected_ACloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void OpenWithA_NothingSelected_BCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Tap(Buttons.B);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void OpenWithA_ThenReopenAfterClose_StillWorks()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        PadHarness pad = new();

        for (int i = 0; i < 3; i++)
        {
            pad.Tap(Buttons.A);
            pad.Tap(Buttons.DPadDown);
            pad.Tap(Buttons.A);
        }

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(3);
        comboBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void CloseFromCode_AfterOpenFromCode_ListBoxDoesNotKeepFocus()
    {
        ComboBox comboBox = CreateComboBox();

        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        InteractiveGue.CurrentInputReceiver.ShouldNotBe(comboBox.ListBox);
        comboBox.ListBox.IsFocused.ShouldBeFalse();
        comboBox.ListBox.DoListItemsHaveFocus.ShouldBeFalse();
    }

    [Fact]
    public void CloseFromCode_FocusedComboBoxGetsFocusBack()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;

        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        comboBox.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(comboBox);
    }

    [Fact]
    public void CloseFromCode_OtherButtonHadFocus_ItGetsFocusBack()
    {
        ComboBox comboBox = CreateComboBox();
        Button save = new();
        save.AddToRoot();
        save.IsFocused = true;

        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        save.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void CloseFromCode_NothingHadFocus_NothingIsFocusedAfter()
    {
        ComboBox comboBox = CreateComboBox();
        InteractiveGue.CurrentInputReceiver = null;

        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void OpenFromCode_KeyboardEnterCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 1;
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Enter)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        InteractiveGue.CurrentInputReceiver!.OnFocusUpdate();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void OpenFromCode_KeyboardUp_MovesSelectionUp()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 2;
        comboBox.IsDropDownOpen = true;
        AddKeyboard(Gum.Forms.Input.Keys.Up);

        InteractiveGue.CurrentInputReceiver!.OnFocusUpdate();

        comboBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void HiddenWhileOpen_ListBoxIsNotLeftAsInputReceiver()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;

        comboBox.Visual.Visible = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldNotBe(comboBox.ListBox);
    }

    [Fact]
    public void ControlClosedWhileDisabled_OpenFromCode_DoesNotTakeFocus()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsEnabled = false;
        Button save = new();
        save.AddToRoot();
        save.IsFocused = true;

        comboBox.IsDropDownOpen = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(save);
    }

    #endregion

    #region Empty, single and boundaries

    [Fact]
    public void EmptyComboBox_OpenWithA_DownAThenCloses()
    {
        ComboBox comboBox = CreateComboBox(itemCount: 0);
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void EmptyListBox_DownAndUpDoNothing()
    {
        ListBox listBox = CreateListBox(itemCount: 0);
        EnterItems(listBox, selectedIndex: -1);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.DPadUp);

        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void SingleItem_DownAndUpKeepSelection()
    {
        ListBox listBox = CreateListBox(itemCount: 1);
        EnterItems(listBox);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.DPadUp);

        listBox.SelectedIndex.ShouldBe(0);
        listBox.ListBoxItems[0].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void LastItem_DownStays_FirstItem_UpStays()
    {
        ListBox listBox = CreateListBox();
        EnterItems(listBox, selectedIndex: 3);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);
        listBox.SelectedIndex.ShouldBe(3);

        listBox.SelectedIndex = 0;
        listBox.DoListItemsHaveFocus = true;
        pad.Tap(Buttons.DPadUp);
        listBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void NothingSelected_DownSelectsFirst_UpSelectsFirst()
    {
        ListBox down = CreateListBox();
        EnterItems(down, selectedIndex: -1);
        PadHarness pad = new();
        pad.Tap(Buttons.DPadDown);
        down.SelectedIndex.ShouldBe(0);

        ListBox up = CreateListBox();
        EnterItems(up, selectedIndex: -1);
        pad.Tap(Buttons.DPadUp);
        up.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void ListBox_ARunsDownAThenBLeavesItemsAndKeepsListBoxFocus()
    {
        ListBox listBox = CreateListBox();
        listBox.SelectedIndex = 0;
        listBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        listBox.DoListItemsHaveFocus.ShouldBeTrue();
        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.B);

        listBox.SelectedIndex.ShouldBe(1);
        listBox.DoListItemsHaveFocus.ShouldBeFalse();
        listBox.IsFocused.ShouldBeTrue();
    }

    #endregion

    #region Disabled items

    [Fact]
    public void FirstItemDisabled_NothingSelected_DownSelectsFirstEnabled()
    {
        ListBox listBox = CreateListBox();
        listBox.ListBoxItems[0].IsEnabled = false;
        EnterItems(listBox, selectedIndex: -1);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void FirstItemDisabled_UpFromSecondStays()
    {
        ListBox listBox = CreateListBox();
        listBox.ListBoxItems[0].IsEnabled = false;
        EnterItems(listBox, selectedIndex: 1);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadUp);

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void TwoConsecutiveDisabled_DownJumpsBothWays()
    {
        ListBox listBox = CreateListBox(itemCount: 5);
        listBox.ListBoxItems[1].IsEnabled = false;
        listBox.ListBoxItems[2].IsEnabled = false;
        EnterItems(listBox, selectedIndex: 0);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);
        listBox.SelectedIndex.ShouldBe(3);
        pad.Tap(Buttons.DPadUp);
        listBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void AllOtherItemsDisabled_DownAndUpKeepSelection()
    {
        ListBox listBox = CreateListBox();
        listBox.ListBoxItems[1].IsEnabled = false;
        listBox.ListBoxItems[2].IsEnabled = false;
        listBox.ListBoxItems[3].IsEnabled = false;
        EnterItems(listBox, selectedIndex: 0);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.DPadUp);

        listBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void AllItemsDisabled_DownDoesNotSelectAnything()
    {
        ListBox listBox = CreateListBox();
        foreach (ListBoxItem item in listBox.ListBoxItems)
        {
            item.IsEnabled = false;
        }
        EnterItems(listBox, selectedIndex: -1);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);

        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void SelectedItemDisabledFromCode_DownMovesToNextEnabled()
    {
        ListBox listBox = CreateListBox();
        listBox.SelectedIndex = 1;
        listBox.ListBoxItems[1].IsEnabled = false;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);

        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void SelectedItemDisabledFromCode_UpMovesToPreviousEnabled()
    {
        ListBox listBox = CreateListBox();
        listBox.SelectedIndex = 2;
        listBox.ListBoxItems[2].IsEnabled = false;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        PadHarness pad = new();

        pad.Tap(Buttons.DPadUp);

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void SelectedIndexSetOnDisabledItemFromCode_IsLeftAsSet()
    {
        ListBox listBox = CreateListBox();
        listBox.ListBoxItems[2].IsEnabled = false;

        listBox.SelectedIndex = 2;

        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void DisabledItemFocusedByCode_IsNotFocused()
    {
        ListBox listBox = CreateListBox();
        listBox.ListBoxItems[2].IsEnabled = false;

        listBox.ListBoxItems[2].IsFocused = true;

        listBox.ListBoxItems[2].IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void ItemDisabledWhileOpen_ComboBoxDownSkipsIt()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        comboBox.ListBox.ListBoxItems[1].IsEnabled = false;
        pad.Tap(Buttons.DPadDown);

        comboBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void ItemReEnabled_DownReachesIt()
    {
        ListBox listBox = CreateListBox();
        listBox.ListBoxItems[1].IsEnabled = false;
        EnterItems(listBox, selectedIndex: 0);
        PadHarness pad = new();

        listBox.ListBoxItems[1].IsEnabled = true;
        pad.Tap(Buttons.DPadDown);

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void KeyboardUpSkipsDisabledItem()
    {
        ListBox listBox = CreateListBox();
        listBox.ListBoxItems[2].IsEnabled = false;
        EnterItems(listBox, selectedIndex: 3);
        AddKeyboard(Gum.Forms.Input.Keys.Up);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void DataBoundItems_DisabledItemSkipped()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        ObservableCollection<string> items = new() { "a", "b", "c" };
        listBox.Items = items;
        listBox.ListBoxItems[1].IsEnabled = false;
        EnterItems(listBox, selectedIndex: 0);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);

        listBox.SelectedObject.ShouldBe("c");
    }

    [Fact]
    public void FocusMovedToOtherControlWhileOpen_DropDownCloses()
    {
        ComboBox comboBox = CreateComboBox();
        Button other = new();
        other.AddToRoot();
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;

        other.IsFocused = true;

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    #endregion

    #region Disabled controls

    [Fact]
    public void DisabledListBox_AFocusedDoesNotEnterItems()
    {
        ListBox listBox = CreateListBox();
        listBox.AllowFocusWhenDisabled = true;
        listBox.IsEnabled = false;
        listBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Tap(Buttons.DPadDown);

        listBox.DoListItemsHaveFocus.ShouldBeFalse();
        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void DisabledComboBox_AFocusedDoesNotOpen()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.AllowFocusWhenDisabled = true;
        comboBox.IsEnabled = false;
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

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

    #endregion

    #region Items changing while open

    [Fact]
    public void ItemAddedWhileOpen_DownReachesIt()
    {
        ComboBox comboBox = CreateComboBox(itemCount: 2);
        comboBox.SelectedIndex = 1;
        comboBox.IsFocused = true;
        PadHarness pad = new();
        pad.Tap(Buttons.A);

        comboBox.Items!.Add("late");
        pad.Tap(Buttons.DPadDown);

        comboBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void SelectedItemRemovedWhileOpen_DownSelectsFirst()
    {
        ComboBox comboBox = CreateComboBox(itemCount: 3);
        comboBox.SelectedIndex = 1;
        comboBox.IsFocused = true;
        PadHarness pad = new();
        pad.Tap(Buttons.A);

        comboBox.Items!.RemoveAt(1);
        pad.Tap(Buttons.DPadDown);

        comboBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void AllItemsRemovedWhileOpen_ACloses()
    {
        ComboBox comboBox = CreateComboBox(itemCount: 2);
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        PadHarness pad = new();
        pad.Tap(Buttons.A);

        comboBox.Items!.Clear();
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    #endregion

    #region Orientation

    [Fact]
    public void HorizontalListBox_RightAndLeftStepAndSkipDisabled()
    {
        ListBox listBox = CreateListBox();
        listBox.InnerPanel.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        listBox.ListBoxItems[1].IsEnabled = false;
        EnterItems(listBox, selectedIndex: 0);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadRight);
        listBox.SelectedIndex.ShouldBe(2);
        pad.Tap(Buttons.DPadLeft);
        listBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void WrappingListBox_DownSkipsDisabledRowBelow()
    {
        ListBox listBox = CreateListBox(itemCount: 6);
        listBox.InnerPanel.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        listBox.InnerPanel.WrapsChildren = true;
        listBox.InnerPanel.StackSpacing = 0;
        foreach (ListBoxItem item in listBox.ListBoxItems)
        {
            item.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            item.Visual.Width = 100;
            item.Visual.Height = 20;
        }
        listBox.InnerPanel.Width = 200;
        listBox.InnerPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        listBox.InnerPanel.UpdateLayout();
        // 2 columns: rows are (0,1) (2,3) (4,5). Disable the row below item 0.
        listBox.ListBoxItems[2].IsEnabled = false;
        EnterItems(listBox, selectedIndex: 0);
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);

        listBox.SelectedIndex.ShouldBe(4);
    }

    #endregion

    #region State after transitions

    [Fact]
    public void FocusArrivesWhileAHeld_ReleaseUpdatesComboBoxState()
    {
        ComboBox comboBox = CreateComboBox();
        Button other = new();
        other.AddToRoot();
        other.IsFocused = true;
        StateLog log = new(comboBox);
        PadHarness pad = new();

        pad.Frame(Buttons.A);
        comboBox.IsFocused = true;
        pad.Frame();

        log.Last.ShouldBe(FrameworkElement.FocusedStateName);
    }

    [Fact]
    public void ComboBoxStateIsFocusedAfterB_Close()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        StateLog log = new(comboBox);
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Hold(Buttons.B, 5);

        log.Last.ShouldBe(FrameworkElement.FocusedStateName);
    }

    #endregion
}
