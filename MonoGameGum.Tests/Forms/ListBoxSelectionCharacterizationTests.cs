using Gum.Forms.Controls;
using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Tests.Input;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pins ListBox selection behavior that no other test covered before the selection logic moved into a
/// shared helper (#7843): direct binding, collection edits next to a selected row, the Extended-to-Single
/// collapse, gamepad button events, and a subclass overriding the protected hooks.
/// </summary>
public class ListBoxSelectionCharacterizationTests : BaseTestClass
{
    #region Binding

    [Fact]
    public void Binding_SelectedObject_ControlToViewModel()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = new();
        listBox.BindingContext = viewModel;
        listBox.SetBinding(nameof(listBox.SelectedObject), nameof(viewModel.Selected));

        listBox.ListBoxItems[2].IsSelected = true;

        viewModel.Selected.ShouldBe("C");
    }

    [Fact]
    public void Binding_SelectedObject_ViewModelToControl()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = new();
        listBox.BindingContext = viewModel;
        listBox.SetBinding(nameof(listBox.SelectedObject), nameof(viewModel.Selected));

        viewModel.Selected = "B";

        listBox.SelectedIndex.ShouldBe(1);
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void Binding_SelectedObject_RemovingSelectedItemPushesNull()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = new();
        listBox.BindingContext = viewModel;
        listBox.SetBinding(nameof(listBox.SelectedObject), nameof(viewModel.Selected));
        listBox.SelectedIndex = 1;

        listBox.Items!.RemoveAt(1);

        viewModel.Selected.ShouldBeNull();
        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void Binding_SelectedIndex_ControlToViewModel()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = new();
        listBox.BindingContext = viewModel;
        listBox.SetBinding(nameof(listBox.SelectedIndex), nameof(viewModel.SelectedIndex));

        listBox.ListBoxItems[2].IsSelected = true;

        viewModel.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void Binding_SelectedIndex_ViewModelToControl()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = new();
        listBox.BindingContext = viewModel;
        listBox.SetBinding(nameof(listBox.SelectedIndex), nameof(viewModel.SelectedIndex));

        viewModel.SelectedIndex = 1;

        listBox.SelectedObject.ShouldBe("B");
    }

    [Fact]
    public void Binding_SelectedIndex_SetInCodeAndInsertBeforeSelection_DoNotPush()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = new();
        listBox.BindingContext = viewModel;
        listBox.SetBinding(nameof(listBox.SelectedIndex), nameof(viewModel.SelectedIndex));
        listBox.SelectedIndex = 1;

        listBox.Items!.Insert(0, "Z");

        // Pins today's behavior: neither the SelectedIndex setter nor an Insert pushes SelectedIndex
        // to the view model. Only a row's IsSelected change and removals push it.
        listBox.SelectedIndex.ShouldBe(2);
        viewModel.SelectedIndex.ShouldBe(0);
    }

    #endregion

    #region Collection edits next to the selection

    [Fact]
    public void Move_SelectedItem_SelectionFollowsTheItem()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        listBox.SelectedIndex = 0;
        List<SelectionChangedEventArgs> raised = TrackSelectionChanged(listBox);

        ((System.Collections.ObjectModel.ObservableCollection<object>)listBox.Items!).Move(0, 2);

        listBox.SelectedObject.ShouldBe("A");
        listBox.SelectedIndex.ShouldBe(2);
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        raised.Count.ShouldBe(0);
    }

    [Fact]
    public void Move_OtherItemAcrossSelection_SelectedIndexShifts()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        listBox.SelectedIndex = 1;

        ((System.Collections.ObjectModel.ObservableCollection<object>)listBox.Items!).Move(0, 2);

        listBox.SelectedObject.ShouldBe("B");
        listBox.SelectedIndex.ShouldBe(0);
        listBox.ListBoxItems[0].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void Replace_SelectedItem_ClearsSelectionAndReportsRemoval()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        listBox.SelectedIndex = 1;
        List<SelectionChangedEventArgs> raised = TrackSelectionChanged(listBox);

        listBox.Items![1] = "Q";

        listBox.SelectedIndex.ShouldBe(-1);
        listBox.SelectedObject.ShouldBeNull();
        listBox.ListBoxItems.Any(item => item.IsSelected).ShouldBeFalse();
        raised.Count.ShouldBe(1);
        raised[0].RemovedItems.Cast<object>().ShouldBe(new object[] { "B" });
        raised[0].AddedItems.Count.ShouldBe(0);
    }

    [Fact]
    public void Replace_UnselectedItem_KeepsSelection()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        listBox.SelectedIndex = 1;
        List<SelectionChangedEventArgs> raised = TrackSelectionChanged(listBox);

        listBox.Items![2] = "Q";

        listBox.SelectedIndex.ShouldBe(1);
        listBox.SelectedObject.ShouldBe("B");
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
        raised.Count.ShouldBe(0);
    }

    [Fact]
    public void Insert_BeforeSelection_ShiftsIndexWithoutRaising()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        listBox.SelectedIndex = 1;
        List<SelectionChangedEventArgs> raised = TrackSelectionChanged(listBox);

        listBox.Items!.Insert(1, "Z");

        listBox.SelectedObject.ShouldBe("B");
        listBox.SelectedIndex.ShouldBe(2);
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[1].IsSelected.ShouldBeFalse();
        raised.Count.ShouldBe(0);
    }

    [Fact]
    public void Insert_AfterSelection_KeepsIndex()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        listBox.SelectedIndex = 1;

        listBox.Items!.Insert(2, "Z");

        listBox.SelectedIndex.ShouldBe(1);
        listBox.SelectedObject.ShouldBe("B");
    }

    [Fact]
    public void DuplicateDataObjects_SelectedByIndex_SelectsThatRow()
    {
        ListBox listBox = CreateListBox("A", "B", "A");

        listBox.SelectedIndex = 2;

        listBox.SelectedIndex.ShouldBe(2);
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
    }

    #endregion

    #region Selection mode

    [Fact]
    public void ExtendedRange_SwitchToSingle_KeepsOnlyFirstSelectedRow()
    {
        ListBox listBox = CreateListBox("A", "B", "C", "D");
        listBox.SelectionMode = SelectionMode.Extended;
        listBox.SelectedItems.Add("C");
        listBox.SelectedItems.Add("B");
        listBox.SelectedItems.Add("D");

        listBox.SelectionMode = SelectionMode.Single;

        listBox.SelectedItems.Cast<object>().ShouldBe(new object[] { "C" });
        listBox.SelectedIndex.ShouldBe(2);
        listBox.ListBoxItems[1].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[3].IsSelected.ShouldBeFalse();
    }

    #endregion

    #region Gamepad

#if !SKIA_LINKED_TESTS
    [Fact]
    public void ControllerButtonPushed_TopLevelFocus_RaisesForPushedButton()
    {
        ListBox listBox = CreateListBox("A", "B");
        listBox.IsFocused = true;
        List<Gum.Input.GamepadButton> pushed = new();
        listBox.ControllerButtonPushed += pushed.Add;

        PushGamepad(Microsoft.Xna.Framework.Input.Buttons.X);
        listBox.OnFocusUpdate();

        pushed.ShouldBe(new[] { Gum.Input.GamepadButton.X });
    }

    [Fact]
    public void ControllerButtonPushed_ItemFocus_RaisesForPushedButton()
    {
        ListBox listBox = CreateListBox("A", "B");
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        listBox.SelectedIndex = 0;
        List<Gum.Input.GamepadButton> pushed = new();
        listBox.ControllerButtonPushed += pushed.Add;

        PushGamepad(Microsoft.Xna.Framework.Input.Buttons.Y);
        listBox.OnFocusUpdate();

        pushed.ShouldBe(new[] { Gum.Input.GamepadButton.Y });
    }

    private static void PushGamepad(Microsoft.Xna.Framework.Input.Buttons buttons)
    {
        Gum.Input.GamePad gamepad = new();
        gamepad.Activity(
            new Microsoft.Xna.Framework.Input.GamePadState(
                Microsoft.Xna.Framework.Vector2.Zero,
                Microsoft.Xna.Framework.Vector2.Zero,
                0, 0,
                buttons),
            0);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);
    }
#endif

    #endregion

    #region Subclass

    [Fact]
    public void Subclass_OverridingRemovalHook_SeesRowsAndSelectionStaysConsistent()
    {
        RecordingListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items!.Add("A");
        listBox.Items.Add("B");
        listBox.Items.Add("C");
        listBox.SelectedIndex = 2;

        listBox.Items.RemoveAt(0);

        listBox.RowCountsSeenOnRemove.ShouldBe(new[] { 2 });
        listBox.RowsInternal.Count.ShouldBe(2);
        listBox.SelectedObject.ShouldBe("C");
        listBox.SelectedIndex.ShouldBe(1);
    }

    private class RecordingListBox : ListBox
    {
        public List<int> RowCountsSeenOnRemove { get; } = new();

        public List<ListBoxItem> RowsInternal => ListBoxItemsInternal;

        protected override void HandleCollectionItemRemoved(int indexToRemoveFrom, FrameworkElement removedItem)
        {
            base.HandleCollectionItemRemoved(indexToRemoveFrom, removedItem);
            RowCountsSeenOnRemove.Add(ListBoxItemsInternal.Count);
        }
    }

    #endregion

    private static ListBox CreateListBox(params string[] items)
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        foreach (string item in items)
        {
            listBox.Items!.Add(item);
        }
        return listBox;
    }

    private static List<SelectionChangedEventArgs> TrackSelectionChanged(ListBox listBox)
    {
        List<SelectionChangedEventArgs> raised = new();
        listBox.SelectionChanged += (_, args) => raised.Add(args);
        return raised;
    }

    private class SelectionViewModel : ViewModel
    {
        public string? Selected
        {
            get => Get<string?>();
            set => Set(value);
        }

        public int SelectedIndex
        {
            get => Get<int>();
            set => Set(value);
        }
    }
}
