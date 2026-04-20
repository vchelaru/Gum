using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Keys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// End-to-end Raylib keyboard coverage for <see cref="ListBox"/>. Proves the full
/// chain from a mocked <see cref="IInputReceiverKeyboard"/> through
/// <c>ListBox.OnFocusUpdate</c> → <c>DoTopLevelFocusUpdate</c> →
/// <c>DoListItemFocusUpdate</c> advances <c>SelectedIndex</c> on
/// <c>Keys.Down</c> for Raylib, plus multi-select modifier coverage unlocked by
/// the Bucket A alias flip (which widened the <c>HandleItemSelected</c> modifier
/// check from <c>#if (MONOGAME || KNI || FNA) &amp;&amp; !FRB</c> to <c>#if !FRB</c>).
/// </summary>
public class ListBoxTests : BaseTestClass
{
    [Fact]
    public void ListBox_DownArrowPressed_MovesSelectionDown()
    {
        ListBox listBox = new ListBox();

        listBox.Visual.ShouldNotBeNull();
        listBox.AddToRoot();

        listBox.Items!.Add("A");
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");

        listBox.SelectedIndex = 0;
        // ListBox routes keyboard arrow navigation through DoListItemFocusUpdate(),
        // which only runs when items (not the list itself) have focus. In a real app,
        // Enter/Space on the top-level focused ListBox flips this to true; here we set
        // it directly so the test isolates arrow-key navigation.
        listBox.DoListItemsHaveFocus = true;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyTyped(Keys.Down)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<int>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void ListBox_EnterPressedAtTopLevelFocus_DropsFocusIntoItems()
    {
        // Covers the Bucket 2 sub-task 4 follow-up: the guard at ListBox.cs:~1871
        // was originally #if (MONOGAME || KNI || FNA) && !FRB, which excluded Raylib
        // from the natural Enter-to-drop-focus-into-items transition. This test pins
        // the Raylib behavior so the guard flip to !FRB is covered end-to-end.
        ListBox listBox = new ListBox();

        listBox.Visual.ShouldNotBeNull();
        listBox.AddToRoot();

        listBox.Items!.Add("A");
        listBox.Items!.Add("B");

        // Top-level focus: ListBox is focused, but items are not yet.
        listBox.DoListItemsHaveFocus = false;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        // ClickCombos default contains Enter (PushedKey = Keys.Enter, HeldKey = null).
        // IsComboPushed() calls keyboard.KeyPushed on the pushed key with no held key.
        keyboard.Setup(k => k.KeyPushed(Keys.Enter)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<int>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.OnFocusUpdate();

        listBox.DoListItemsHaveFocus.ShouldBeTrue();
    }

    [Fact]
    public void ListBox_ExtendedMode_CtrlHeldWhileToggling_DoesNotClearOtherSelections()
    {
        // Covers the Bucket A guard flip at ListBox.cs:699 (now #if !FRB, previously
        // excluded Raylib). With LeftControl held, selecting a new item in Extended
        // mode should toggle it on without deselecting the existing selection — that's
        // the Ctrl-toggle behavior. Before the flip, the modifier-detection block was
        // compiled out on Raylib, so new clicks would clear prior selections.
        ListBox listBox = new ListBox { SelectionMode = SelectionMode.Extended };

        listBox.Visual.ShouldNotBeNull();
        listBox.AddToRoot();

        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        listBox.ListBoxItems[0].IsSelected = true;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyDown(ListBox.ToggleSelectionModifierKey)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.ListBoxItems[1].IsSelected = true;

        listBox.SelectedItems.Count.ShouldBe(2);
        listBox.ListBoxItems[0].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void ListBox_ExtendedMode_ShiftHeldWhileSelecting_ExtendsRange()
    {
        // Covers the Bucket A guard flip at ListBox.cs:699 for the range-selection
        // side. With LeftShift held, selecting a later item in Extended mode should
        // select everything from the anchor through the clicked index inclusive.
        ListBox listBox = new ListBox { SelectionMode = SelectionMode.Extended };

        listBox.Visual.ShouldNotBeNull();
        listBox.AddToRoot();

        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");
        listBox.Items!.Add("Item 3");

        listBox.ListBoxItems[0].IsSelected = true;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyDown(ListBox.RangeSelectionModifierKey)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.ListBoxItems[2].IsSelected = true;

        listBox.SelectedItems.Count.ShouldBe(3);
        listBox.ListBoxItems[0].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[3].IsSelected.ShouldBeFalse();
    }
}
