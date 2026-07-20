using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Input;
using MonoGameGum.Tests.Input;
using Gum.Input;
using GamePad = Gum.Input.GamePad;
using Moq;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class ListBoxTests : BaseTestClass
{
    #region Children

    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        ListBox listBox = new();
        InteractiveGue visual = listBox.Visual;

        List<ContainerRuntime> children = visual.Descendants().OfType<ContainerRuntime>().ToList();

        foreach(var child in children)
        {
            if(child.Name != "ThumbContainer")
            {
                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }
    }


    [Fact]
    public void Click_ShouldNotSelect_IfDisabled()
    {
        ListBox listBox = new();
        listBox.IsEnabled = false;
        listBox.AddToRoot();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        Mock<ICursor> mockCursor = SetupForPush();

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);

        listBox.SelectedObject.ShouldBeNull();
    }

    [Fact]
    public void Click_ShouldFireSelectionChangedOncePerClick()
    {
        // Repro for issue #2942: SelectionChanged fires once on the first click but
        // twice on every subsequent click that changes which item is selected. The
        // second fire comes from SyncIsSelectedFromSelectedItems detecting that the
        // previously-selected item needs to have its IsSelected cleared.
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < 5; i++)
        {
            listBox.Items!.Add(i);
        }

        int fireCount = 0;
        listBox.SelectionChanged += (_, _) => fireCount++;

        Mock<ICursor> firstCursor = SetupForPushOnItem(listBox, itemIndex: 0);
        firstCursor.SetupProperty(x => x.VisualOver);
        firstCursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            firstCursor.Object,
            null!,
            0);

        fireCount.ShouldBe(1);

        Mock<ICursor> secondCursor = SetupForPushOnItem(listBox, itemIndex: 1);
        secondCursor.SetupProperty(x => x.VisualOver);
        secondCursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            secondCursor.Object,
            null!,
            0);

        fireCount.ShouldBe(2);
    }

    [Fact]
    public void Click_ShouldRaiseSelectionChanged_WithExpectedArgs()
    {
        // Pin args content on the click path so the fix for issue #2942 doesn't
        // accidentally drop the previously-selected item from RemovedItems.
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < 3; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        listBox.SelectedIndex = 0;

        SelectionChangedEventArgs? capturedArgs = null;
        listBox.SelectionChanged += (_, args) => capturedArgs = args;

        Mock<ICursor> cursor = SetupForPushOnItem(listBox, itemIndex: 1);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            cursor.Object,
            null!,
            0);

        capturedArgs.ShouldNotBeNull();
        capturedArgs.AddedItems.Count.ShouldBe(1);
        capturedArgs.AddedItems[0]!.ShouldBe("Item 1");
        capturedArgs.RemovedItems.Count.ShouldBe(1);
        capturedArgs.RemovedItems[0]!.ShouldBe("Item 0");
    }

    [Fact]
    public void MultipleMode_ClickToggle_ShouldFireSelectionChangedOncePerClick()
    {
        // Multiple-mode toggle-off goes through HandleItemSelected and then
        // SyncIsSelectedFromSelectedItems, which is the double-fire vector for #2942.
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.AddToRoot();
        for (int i = 0; i < 3; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        int fireCount = 0;
        listBox.SelectionChanged += (_, _) => fireCount++;

        Mock<ICursor> firstCursor = SetupForPushOnItem(listBox, itemIndex: 0);
        firstCursor.SetupProperty(x => x.VisualOver);
        firstCursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            firstCursor.Object,
            null!,
            0);

        fireCount.ShouldBe(1);

        // Click the same item again to toggle it off — this is where Sync detects
        // a flipped IsSelected and (pre-fix) raises a second SelectionChanged.
        Mock<ICursor> toggleCursor = SetupForPushOnItem(listBox, itemIndex: 0);
        toggleCursor.SetupProperty(x => x.VisualOver);
        toggleCursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            toggleCursor.Object,
            null!,
            0);

        fireCount.ShouldBe(2);
    }

    [Fact]
    public void SelectedIndex_set_ShouldFireSelectionChangedOncePerAssignment()
    {
        ListBox listBox = new();
        for (int i = 0; i < 3; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        int fireCount = 0;
        listBox.SelectionChanged += (_, _) => fireCount++;

        listBox.SelectedIndex = 0;
        fireCount.ShouldBe(1);

        listBox.SelectedIndex = 1;
        fireCount.ShouldBe(2);

        listBox.SelectedIndex = -1;
        fireCount.ShouldBe(3);
    }

    [Fact]
    public void SelectedObject_set_ShouldFireSelectionChangedOncePerAssignment()
    {
        ListBox listBox = new();
        for (int i = 0; i < 3; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        int fireCount = 0;
        listBox.SelectionChanged += (_, _) => fireCount++;

        listBox.SelectedObject = "Item 0";
        fireCount.ShouldBe(1);

        listBox.SelectedObject = "Item 1";
        fireCount.ShouldBe(2);

        listBox.SelectedObject = null;
        fireCount.ShouldBe(3);
    }

    [Fact]
    public void Click_ShouldSelect_IfEnabled()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        Mock<ICursor> mockCursor = SetupForPushOnItem(listBox, itemIndex: 0);

        mockCursor.SetupProperty(x => x.VisualOver);
        mockCursor.SetupProperty(x => x.WindowPushed);


        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);


        if(listBox.SelectedObject == null)
        {
            var firstListBoxItem = listBox.ListBoxItems[0]!;

            var isOverFirst = mockCursor.Object.VisualOver ==
                firstListBoxItem.Visual;

            string diagnostics =
                $"VisualOver: {mockCursor.Object.VisualOver}" +
                $" WindowPushed: {mockCursor.Object.WindowPushed}" +
                $" Is over first: {isOverFirst}";
            throw new Exception(diagnostics);
        }

    }

    #endregion

    [Fact]
    public void InnerPanel_AddListBoxItemVisual_SelectionShouldWork()
    {
        ListBox listBox = new();
        ListBoxItem listBoxItem = new();

        listBox.InnerPanel.Children.Add(listBoxItem.Visual);

        listBoxItem.IsSelected = true;

        listBox.SelectedObject.ShouldBe(listBoxItem);
    }

    [Fact]
    public void InnerPanel_AddListBoxItemVisual_ShouldBeReflectedInItems()
    {
        ListBox listBox = new();
        ListBoxItem listBoxItem = new();

        listBox.InnerPanel.Children.Add(listBoxItem.Visual);

        listBox.ListBoxItems.Count.ShouldBe(1);
        listBox.Items!.Count.ShouldBe(1);
    }

    [Fact]
    public void InnerPanel_AddListBoxItemVisual_ThenRemoveViaItems_ShouldSyncAllCollections()
    {
        ListBox listBox = new();
        ListBoxItem listBoxItem = new();

        listBox.InnerPanel.Children.Add(listBoxItem.Visual);
        listBox.Items!.Count.ShouldBe(1);

        listBox.Items!.Remove(listBoxItem);

        listBox.Items.Count.ShouldBe(0);
        listBox.ListBoxItems.Count.ShouldBe(0);
        listBox.InnerPanel.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void InnerPanel_AddMultipleListBoxItemVisuals_CountsShouldStayInSync()
    {
        ListBox listBox = new();
        ListBoxItem item0 = new();
        ListBoxItem item1 = new();
        ListBoxItem item2 = new();

        listBox.InnerPanel.Children.Add(item0.Visual);
        listBox.InnerPanel.Children.Add(item1.Visual);
        listBox.InnerPanel.Children.Add(item2.Visual);

        listBox.Items!.Count.ShouldBe(3);
        listBox.ListBoxItems.Count.ShouldBe(3);
        listBox.InnerPanel.Children.Count.ShouldBe(3);
    }

    [Fact]
    public void IsEnabled_ShouldSetListBoxItemsDisable_IfSetToFalse()
    {
        bool didSet = false;
        ListBoxItem listBoxItem = new();
        var disabledState = listBoxItem.GetState(FrameworkElement.DisabledStateName);

        disabledState.Clear();
        disabledState.Apply = () =>
        {
            didSet = true;
        };

        ListBox listBox = new ListBox();
        listBox.Items!.Add(listBoxItem);
        didSet.ShouldBeFalse();
        listBox.IsEnabled = false;
        didSet.ShouldBeTrue();

    }

    [Fact]
    public void IsEnabled_ShouldSetListBoxItemAfterDisabled_IfSetToFalse()
    {
        bool didSet = false;
        ListBoxItem listBoxItem = new();
        var disabledState = listBoxItem.GetState(FrameworkElement.DisabledStateName);
        disabledState.Clear();
        disabledState.Apply = () =>
        {
            didSet = true;
        };

        ListBox listBox = new ListBox();
        listBox.IsEnabled = false;
        didSet.ShouldBeFalse();
        listBox.Items!.Add(listBoxItem);
        didSet.ShouldBeTrue();
    }

    [Fact]
    public void Items_ShoudAddListBoxItem_WhenAdding()
    {
        ListBox listBox = new ();
        listBox.Items!.Add(1);
        listBox.ListBoxItems.Count.ShouldBe(1);
        (listBox.ListBoxItems[0]! is ListBoxItem).ShouldBeTrue();

        listBox.InnerPanel.Children.Count.ShouldBe(1);
        (listBox.InnerPanel.Children[0]! is InteractiveGue).ShouldBeTrue();
        (listBox.InnerPanel.Children[0] as InteractiveGue)!.FormsControlAsObject
            .ShouldBeOfType<ListBoxItem>();
    }

    [Fact]
    public void Items_Add_ShouldAddListBoxItems()
    {
        ListBox listBox = new();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        listBox.ListBoxItems.Count.ShouldBe(10);
    }

    [Fact]
    public void Items_Clear_ShouldClearListBoxItems()
    {
        ListBox listBox = new();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        listBox.Items!.Clear();
        listBox.ListBoxItems.Count.ShouldBe(0);
    }

    [Fact]
    public void Items_Clear_ShouldWorkWhenAddingButtonVisual()
    {
        Gum.Forms.DefaultVisuals.ButtonVisual button = new();

        ListBox listBox = new();
        button.Parent.ShouldBeNull();
        listBox.Items!.Add(button);
        button.Parent.ShouldNotBeNull();
        listBox.Items!.Clear(); // should not throw
        button.Parent.ShouldBeNull();
    }

    [Fact]
    public void Items_AddListBoxItemAfterNonListBoxItem_ShouldNotThrowAndStayInSync()
    {
        // Issue #556: a non-ListBoxItem visual (here a Button) added to Items occupies an
        // InnerPanel slot but NOT a ListBoxItemsInternal slot. Adding a real item after it
        // used to call ListBoxItemsInternal.Insert at the InnerPanel index (2) on a
        // 1-element list, throwing ArgumentOutOfRangeException during the add.
        Gum.Forms.DefaultVisuals.ButtonVisual button = new();

        ListBox listBox = new();
        listBox.Items!.Add("A");
        listBox.Items!.Add(button);

        Should.NotThrow(() => listBox.Items!.Add("B"));

        listBox.Items!.Count.ShouldBe(3);
        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.ListBoxItems[0].BindingContext.ShouldBe("A");
        listBox.ListBoxItems[1].BindingContext.ShouldBe("B");
    }

    [Fact]
    public void Items_SelectRowAfterNonListBoxItem_ShouldResolveCorrectDataObject()
    {
        // Issue #556: selection resolved the data object by index alignment between
        // ListBoxItemsInternal and Items (Items[ListBoxItemsInternal.IndexOf(item)]). With a
        // non-ListBoxItem (Button) between two real items, selecting the second real row
        // resolved to the Button instead of "B". Selection must resolve by reference.
        Gum.Forms.DefaultVisuals.ButtonVisual button = new();

        ListBox listBox = new();
        listBox.Items!.Add("A");
        listBox.Items!.Add(button);
        listBox.Items!.Add("B");

        listBox.ListBoxItems[1].IsSelected = true;

        listBox.SelectedObject.ShouldBe("B");
    }

    [Fact]
    public void Items_RemoveRealItemAfterNonListBoxItem_ShouldRemoveTheCorrectRow()
    {
        // Issue #556 (negative path): removing a real item that sits after a non-ListBoxItem used to
        // remove the WRONG ListBoxItem — the #1380 bounds-check band-aid removed at the InnerPanel
        // index, which is offset by the non-ListBoxItem. Removal is now by reference.
        Gum.Forms.DefaultVisuals.ButtonVisual button = new();

        ListBox listBox = new();
        listBox.Items!.Add("A");
        listBox.Items!.Add(button);
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");

        listBox.Items!.Remove("B");

        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.ListBoxItems[0].BindingContext.ShouldBe("A");
        listBox.ListBoxItems[1].BindingContext.ShouldBe("C");
    }

    [Fact]
    public void Items_RemoveNonListBoxItem_ShouldNotRemoveAnyRow()
    {
        // Issue #556 (negative path): removing the non-ListBoxItem itself must not drop a real row.
        // The old index-based removal would RemoveAt the non-ListBoxItem's InnerPanel index and take
        // a real ListBoxItem with it.
        Gum.Forms.DefaultVisuals.ButtonVisual button = new();

        ListBox listBox = new();
        listBox.Items!.Add("A");
        listBox.Items!.Add(button);
        listBox.Items!.Add("B");

        listBox.Items!.Remove(button);

        listBox.Items!.Count.ShouldBe(2);
        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.ListBoxItems[0].BindingContext.ShouldBe("A");
        listBox.ListBoxItems[1].BindingContext.ShouldBe("B");
    }

    [Fact]
    public void Click_OnNonListBoxItem_ShouldNotChangeSelection()
    {
        // Issue #556 (the original worry): a non-ListBoxItem mixed into Items must be inert —
        // clicking it must not deselect the current item or raise SelectionChanged.
        Gum.Forms.DefaultVisuals.ButtonVisual button = new();

        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items!.Add("A");
        listBox.Items!.Add(button);
        listBox.Items!.Add("B");

        listBox.SelectedObject = "A";

        int fireCount = 0;
        listBox.SelectionChanged += (_, _) => fireCount++;

        Mock<ICursor> cursor = SetupForPush();
        var cx = button.GetAbsoluteX() + button.GetAbsoluteWidth() / 2;
        var cy = button.GetAbsoluteY() + button.GetAbsoluteHeight() / 2;
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(cx);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(cy);
        cursor.Setup(c => c.X).Returns((int)cx);
        cursor.Setup(c => c.Y).Returns((int)cy);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(listBox.Visual, cursor.Object, null!, 0);

        listBox.SelectedObject.ShouldBe("A");
        fireCount.ShouldBe(0);
    }

    [Fact]
    public void Items_InsertAt_ShouldProperlyInsert()
    {
        ListBox listBox = new();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        var itemToRemove = listBox.Items![5]!;
        listBox.Items!.RemoveAt(5);
        listBox.Items!.Insert(0, itemToRemove);

        listBox.ListBoxItems.Count.ShouldBe(10);
        var item5 = listBox.ListBoxItems[0]!;
        item5.BindingContext.ShouldBe("Item 5");
    }

    [Fact]
    public void Items_Move_ShouldReorder()
    {
        ObservableCollection<string> values = new();

        for (int i = 0; i < 10; i++)
        {
            values.Add("Item " + i);
        }

        ListBox listBox = new();

        listBox.Items = values;

        listBox.ListBoxItems.Count.ShouldBe(10);

        for(int i = 0; i < 10; i++)
        {
            listBox.ListBoxItems[i]!.BindingContext.ShouldBe("Item " + i);
        }

        values.Move(0, 1);

        listBox.ListBoxItems[0]!.BindingContext.ShouldBe("Item 1");
        listBox.ListBoxItems[1]!.BindingContext.ShouldBe("Item 0");

        var innerPanel = listBox.Visual.FindByName("InnerPanelInstance")!;
        innerPanel.Children.Count.ShouldBe(10);
        for(int i = 0; i < 10; i++)
        {
            innerPanel.Children[i]!.ShouldBe(listBox.ListBoxItems[i]!.Visual);
        }
    }

    [Fact]
    public void Items_AssignTypedCollection_ShouldSyncListBoxItems()
    {
        // Typed ObservableCollection<string> assigned before items are added
        var items = new ObservableCollection<string>();
        ListBox listBox = new();
        listBox.Items = items;

        items.Add("Item 0");
        items.Add("Item 1");
        items.Add("Item 2");

        listBox.ListBoxItems.Count.ShouldBe(3);
        listBox.InnerPanel.Children.Count.ShouldBe(3);
    }

    [Fact]
    public void Items_AssignTypedCollectionWithExistingData_ShouldSyncListBoxItems()
    {
        // Typed ObservableCollection<string> pre-populated before assignment
        var items = new ObservableCollection<string> { "Item 0", "Item 1", "Item 2" };
        ListBox listBox = new();

        listBox.Items = items;

        listBox.ListBoxItems.Count.ShouldBe(3);
        listBox.InnerPanel.Children.Count.ShouldBe(3);
    }

    [Fact]
    public void Items_Remove_ShouldRemoveListBoxItems()
    {

        ListBox listBox = new();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        for(int i = 0; i < 10; i++)
        {
            listBox.Items!.Remove("Item " + i);
            listBox.ListBoxItems.Count.ShouldBe(9 - i);
            if( listBox.ListBoxItems.Count > 0)
            {
                var nextItem = listBox.ListBoxItems[0]!;
                nextItem.BindingContext.ShouldBe("Item " + (i + 1));
            }
        }
    }

    [Fact]
    public void Items_RemoveAt_ShouldRemoveListBoxItems()
    {
        ListBox listBox = new();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        for (int i = 0; i < 10; i++)
        {
            listBox.Items!.RemoveAt(0);
            listBox.ListBoxItems.Count.ShouldBe(9 - i);
        }
    }

    [Fact]
    public void ListBoxItems_ShouldReflectBackingObjects_WhenRemoving()
    {
        ListBox listBox = new();
        for (int i = 0; i < 2; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.Items!.Remove("Item 1");
        listBox.ListBoxItems.Count.ShouldBe(1);
        foreach(var listBoxItem in listBox.ListBoxItems)
        {
            listBoxItem.BindingContext.ShouldNotBe("Item 1");
        }
    }

    #region Visual
    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        ListBox sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }
    #endregion

    #region Multi-Select Tests

    [Fact]
    public void ExtendedMode_ClickAlone_ShouldSelectOnlyClickedItem()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Extended };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        // Select first item
        listBox.ListBoxItems[0].IsSelected = true;

        // Act - click second item without modifiers
        listBox.ListBoxItems[1].IsSelected = true;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe("Item 1");
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[2].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void ExtendedMode_CtrlClickOnEmptyList_ShouldSelectItem()
    {
        // Arrange
        var expectedItem = "Item 0";
        var listBox = new ListBox { SelectionMode = SelectionMode.Extended };
        listBox.AddToRoot();
        listBox.Items!.Add(expectedItem);
        listBox.Items!.Add("Item 1");

        // Act - simulate Ctrl+Click on first item when nothing is selected
        var mockCursor = SetupForPushWithCtrlOnItem(listBox, itemIndex: 0);
        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe(expectedItem);
    }

    [Fact]
    public void ExtendedMode_CtrlClickSelectedItem_ShouldDeselectItem()
    {
        // Arrange
        var firstItem = "Item 0";
        var secondItem = "Item 1";
        var listBox = new ListBox { SelectionMode = SelectionMode.Extended };
        listBox.AddToRoot();
        listBox.Items!.Add(firstItem);
        listBox.Items!.Add(secondItem);

        // Select both items initially
        listBox.SelectedItems.Add(firstItem);
        listBox.SelectedItems.Add(secondItem);
        listBox.SelectedItems.Count.ShouldBe(2);

        // Act - simulate Ctrl+Click on the first selected item
        var mockCursor = SetupForPushWithCtrlOnItem(listBox, itemIndex: 0);
        mockCursor.SetupProperty(x => x.VisualOver);
        mockCursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);

        // Assert - first item should be deselected, second should remain selected
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe(secondItem);
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void ExtendedMode_CtrlClickUnselectedItem_ShouldAddToSelection()
    {
        // Arrange
        var firstItem = "Item 0";
        var secondItem = "Item 1";
        var thirdItem = "Item 2";
        var listBox = new ListBox { SelectionMode = SelectionMode.Extended };
        listBox.AddToRoot();
        listBox.Items!.Add(firstItem);
        listBox.Items!.Add(secondItem);
        listBox.Items!.Add(thirdItem);

        // Select first item initially
        listBox.SelectedItems.Add(firstItem);

        // Act - simulate Ctrl+Click on second item (item at index 1)
        var mockCursor = SetupForPushWithCtrlOnItem(listBox, itemIndex: 1);
        mockCursor.SetupProperty(x => x.VisualOver);
        mockCursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);

        // Assert - both items should be selected
        listBox.SelectedItems.Count.ShouldBe(2);
        listBox.SelectedItems.Cast<object>().ShouldContain(firstItem);
        listBox.SelectedItems.Cast<object>().ShouldContain(secondItem);
        listBox.ListBoxItems[0].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[2].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void ExtendedMode_ShiftClickReverseRange_ShouldSelectCorrectRange()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Extended };
        listBox.AddToRoot();
        for (int i = 0; i < 5; i++)
        {
            listBox.Items!.Add($"Item {i}");
        }

        // Select item at index 3 as anchor
        listBox.SelectedItems.Add("Item 3");

        // Act - simulate Shift+Click on first item (index 0) - reverse direction
        var mockCursor = SetupForPushWithShiftOnItem(listBox, itemIndex: 0);
        mockCursor.SetupProperty(x => x.VisualOver);
        mockCursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);

        // Assert - items 0-3 should be selected (reverse range)
        listBox.SelectedItems.Count.ShouldBe(4);
        for (int i = 0; i < 4; i++)
        {
            listBox.ListBoxItems[i].IsSelected.ShouldBeTrue($"Item {i} should be selected");
        }
        listBox.ListBoxItems[4].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void ExtendedMode_ShiftClickWithAnchor_ShouldSelectRange()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Extended };
        listBox.AddToRoot();
        for (int i = 0; i < 5; i++)
        {
            listBox.Items!.Add($"Item {i}");
        }

        // Select first item as anchor
        listBox.SelectedItems.Add("Item 0");

        // Act - simulate Shift+Click on fourth item (index 3)
        var mockCursor = SetupForPushWithShiftOnItem(listBox, itemIndex: 3);
        mockCursor.SetupProperty(x => x.VisualOver);
        mockCursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);

        // Assert - items 0-3 should be selected
        listBox.SelectedItems.Count.ShouldBe(4);
        for (int i = 0; i < 4; i++)
        {
            listBox.ListBoxItems[i].IsSelected.ShouldBeTrue($"Item {i} should be selected");
        }
        listBox.ListBoxItems[4].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void ExtendedMode_ShiftClickWithNoAnchor_ShouldSelectSingleItem()
    {
        // Arrange
        var expectedItem = "Item 2";
        var listBox = new ListBox { SelectionMode = SelectionMode.Extended };
        listBox.AddToRoot();
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add(expectedItem);

        // Act - simulate Shift+Click when no item is selected
        var mockCursor = SetupForPushWithShiftOnItem(listBox, itemIndex: 2);
        mockCursor.SetupProperty(x => x.VisualOver);
        mockCursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);

        // Assert - only the clicked item should be selected
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe(expectedItem);
    }

    [Fact]
    public void ModifierKeys_ShouldBeCustomizable()
    {
        // Arrange
        var originalToggle = ListBox.ToggleSelectionModifierKey;
        var originalRange = ListBox.RangeSelectionModifierKey;

        // Act
        ListBox.ToggleSelectionModifierKey = Gum.Forms.Input.Keys.LeftAlt;
        ListBox.RangeSelectionModifierKey = Gum.Forms.Input.Keys.LeftControl;

        // Assert
        ListBox.ToggleSelectionModifierKey.ShouldBe(Gum.Forms.Input.Keys.LeftAlt);
        ListBox.RangeSelectionModifierKey.ShouldBe(Gum.Forms.Input.Keys.LeftControl);

        // Cleanup
        ListBox.ToggleSelectionModifierKey = originalToggle;
        ListBox.RangeSelectionModifierKey = originalRange;
    }

    [Fact]
    public void MultipleMode_Click_ShouldToggleSelection()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.AddToRoot();
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        // Act - click first item to select it
        var mockCursor1 = SetupForPushOnItem(listBox, itemIndex: 0);
        mockCursor1.SetupProperty(x => x.VisualOver);
        mockCursor1.SetupProperty(x => x.WindowPushed);
        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor1.Object,
            null!,
            0);

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe("Item 0");

        // Act - click second item (both should be selected in Multiple mode)
        var mockCursor2 = SetupForPushOnItem(listBox, itemIndex: 1);
        mockCursor2.SetupProperty(x => x.VisualOver);
        mockCursor2.SetupProperty(x => x.WindowPushed);
        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor2.Object,
            null!,
            0);

        // Assert
        listBox.SelectedItems.Count.ShouldBe(2);
        listBox.SelectedItems.Cast<object>().ShouldContain("Item 0");
        listBox.SelectedItems.Cast<object>().ShouldContain("Item 1");

        // Act - click first item again to deselect it (toggle off)
        var mockCursor3 = SetupForPushOnItem(listBox, itemIndex: 0);
        mockCursor3.SetupProperty(x => x.VisualOver);
        mockCursor3.SetupProperty(x => x.WindowPushed);
        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor3.Object,
            null!,
            0);

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe("Item 1");
    }

    [Fact]
    public void MultipleMode_ClickSelectedItem_ShouldDeselectItem()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.AddToRoot();
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        // Select first item
        listBox.ListBoxItems[0].IsSelected = true;
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe("Item 0");

        // Act - Click the already-selected item (simulate push)
        Mock<ICursor> mockCursor = SetupForPushOnItem(listBox, itemIndex: 0);
        mockCursor.SetupProperty(x => x.VisualOver);
        mockCursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);

        // Assert - item should be deselected (toggled off)
        listBox.SelectedItems.Count.ShouldBe(0);
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void MultipleMode_SelectAllItems_ShouldAllowAllSelections()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        // Act - select all items
        foreach (var item in listBox.Items!)
        {
            listBox.SelectedItems.Add(item);
        }

        // Assert
        listBox.SelectedItems.Count.ShouldBe(3);
        foreach (var listBoxItem in listBox.ListBoxItems)
        {
            listBoxItem.IsSelected.ShouldBeTrue();
        }
    }

    [Fact]
    public void MultipleMode_SelectOnEmptyList_ShouldNotThrow()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.AddToRoot();

        // Act & Assert - should not throw
        var mockCursor = SetupForPush();
        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);

        listBox.SelectedItems.Count.ShouldBe(0);
    }

    [Fact]
    public void SelectedIndex_Getter_ShouldReturnNegativeOne_WhenItemsIsNull()
    {
        var listBox = new ListBox();
        listBox.Items = null;

        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void SelectedIndex_Getter_ShouldReturnNegativeOne_WhenItemsIsNullAndSelectedObjectIsSet()
    {
        // Reproduce NRE: Items set to null, then SelectedObject set.
        // SelectedObject setter calls SelectedIndex getter internally via SyncIsSelectedFromSelectedItems.
        var listBox = new ListBox();
        listBox.Items = null;

        Should.NotThrow(() => listBox.SelectedObject = "test");
        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void SelectedIndex_Getter_ShouldReturnFirstSelectedItemIndex()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        // Act
        listBox.SelectedItems.Add("Item 1");
        listBox.SelectedItems.Add("Item 2");

        // Assert
        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void SelectedIndex_Getter_ShouldReturnNegativeOneWhenNoSelection()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");

        // Assert
        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void SelectedIndex_Setter_ShouldClearSelectedItemsAndSelectOne()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");

        // Act
        listBox.SelectedIndex = 2;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe("Item 2");
        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void SelectedItems_Add_ShouldSyncIsSelected()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        // Act
        listBox.SelectedItems.Add("Item 1");

        // Assert
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[2].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void SelectedItems_Add_ShouldNotThrow_WhenItemsIsNull()
    {
        // Reproduce NRE: Items set to null, then SelectedItems.Add called.
        // HandleSelectedItemsCollectionChanged calls Items.IndexOf which NREs when Items is null.
        var listBox = new ListBox();
        listBox.Items = null;

        Should.NotThrow(() => listBox.SelectedItems.Add("test"));
    }

    [Fact]
    public void SelectedItems_AddMultipleInSingleMode_ShouldAllowAll()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Single };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        // Act - add multiple items programmatically in Single mode
        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");
        listBox.SelectedItems.Add("Item 2");

        // Assert - Single mode does NOT enforce selection limits when adding programmatically
        // This is different from UI clicking behavior, which enforces Single mode
        // NOTE: This behavior matches WPF ListBox where programmatic SelectedItems.Add()
        // does not enforce Single mode constraints
        listBox.SelectedItems.Count.ShouldBe(3);
        listBox.SelectedItems.Cast<object>().ShouldContain("Item 0");
        listBox.SelectedItems.Cast<object>().ShouldContain("Item 1");
        listBox.SelectedItems.Cast<object>().ShouldContain("Item 2");
    }

    [Fact]
    public void SelectedItems_AddNonExistentItem_ShouldNotAffectSelection()
    {
        // Arrange
        var validItem = "Item 0";
        var invalidItem = "NonExistent Item";
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add(validItem);
        listBox.Items!.Add("Item 1");

        // Act - add a valid item, then try to add an item not in the Items collection
        listBox.SelectedItems.Add(validItem);
        listBox.SelectedItems.Add(invalidItem);

        // Assert - the invalid item is added to SelectedItems but doesn't crash
        // This matches the current behavior where SelectedItems can contain items not in Items
        listBox.SelectedItems.Count.ShouldBe(2);
        listBox.SelectedItems[0]!.ShouldBe(validItem);
        listBox.SelectedItems[1]!.ShouldBe(invalidItem);
    }

    [Fact]
    public void SelectedItems_Clear_ShouldDeselectAllItems()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");

        // Act
        listBox.SelectedItems.Clear();

        // Assert
        listBox.SelectedItems.Count.ShouldBe(0);
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[2].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void SelectedItems_Remove_ShouldSyncIsSelected()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");

        // Act
        listBox.SelectedItems.Remove("Item 0");

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SelectedItems_ShouldNotBeReplaceable()
    {
        // Arrange
        var listBox = new ListBox();

        // Act
        var selectedItems = listBox.SelectedItems;

        // Assert - getting it twice should return the same instance
        listBox.SelectedItems.ShouldBeSameAs(selectedItems);
    }

    [Fact]
    public void SelectedObject_Getter_ShouldReturnNull_WhenItemsIsNull()
    {
        var listBox = new ListBox();
        listBox.Items = null;

        listBox.SelectedObject.ShouldBeNull();
    }

    [Fact]
    public void SelectedObject_Getter_ShouldReturnFirstSelectedItem()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        // Act
        listBox.SelectedItems.Add("Item 1");
        listBox.SelectedItems.Add("Item 2");

        // Assert
        listBox.SelectedObject.ShouldBe("Item 1");
    }

    [Fact]
    public void SelectedObject_Getter_ShouldReturnNullWhenNoSelection()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");

        // Assert
        listBox.SelectedObject.ShouldBeNull();
    }

    [Fact]
    public void SelectedObject_Setter_ShouldClearSelectedItemsAndSelectOne()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");

        // Act
        listBox.SelectedObject = "Item 2";

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe("Item 2");
        listBox.SelectedObject.ShouldBe("Item 2");
    }

    [Fact]
    public void SelectionChanged_ShouldFireOnSelectedItemsAdd()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");

        bool eventFired = false;
        SelectionChangedEventArgs? capturedArgs = null;

        listBox.SelectionChanged += (sender, args) =>
        {
            eventFired = true;
            capturedArgs = args;
        };

        // Act
        listBox.SelectedItems.Add("Item 0");

        // Assert
        eventFired.ShouldBeTrue();
        capturedArgs.ShouldNotBeNull();
        capturedArgs.AddedItems.Count.ShouldBe(1);
        capturedArgs.AddedItems[0]!.ShouldBe("Item 0");
        capturedArgs.RemovedItems.Count.ShouldBe(0);
    }

    [Fact]
    public void SelectionChanged_ShouldFireOnSelectedItemsRemove()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");

        listBox.SelectedItems.Add("Item 0");

        bool eventFired = false;
        SelectionChangedEventArgs? capturedArgs = null;

        listBox.SelectionChanged += (sender, args) =>
        {
            eventFired = true;
            capturedArgs = args;
        };

        // Act
        listBox.SelectedItems.Remove("Item 0");

        // Assert
        eventFired.ShouldBeTrue();
        capturedArgs.ShouldNotBeNull();
        capturedArgs.AddedItems.Count.ShouldBe(0);
        capturedArgs.RemovedItems.Count.ShouldBe(1);
        capturedArgs.RemovedItems[0]!.ShouldBe("Item 0");
    }

    [Fact]
    public void SelectionMode_ChangeToSingle_ShouldKeepOnlyFirstSelection()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");
        listBox.SelectedItems.Add("Item 2");

        // Act
        listBox.SelectionMode = SelectionMode.Single;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe("Item 0");
    }

    [Fact]
    public void SelectionMode_DefaultValue_ShouldBeSingle()
    {
        // Arrange & Act
        var listBox = new ListBox();

        // Assert
        listBox.SelectionMode.ShouldBe(SelectionMode.Single);
    }

    [Fact]
    public void SelectionMode_SwitchBetweenModes_ShouldMaintainValidState()
    {
        // Arrange
        var firstItem = "Item 0";
        var secondItem = "Item 1";
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add(firstItem);
        listBox.Items!.Add(secondItem);
        listBox.Items!.Add("Item 2");

        // Select two items in Multiple mode
        listBox.SelectedItems.Add(firstItem);
        listBox.SelectedItems.Add(secondItem);
        listBox.SelectedItems.Count.ShouldBe(2);

        // Act - switch to Extended mode
        listBox.SelectionMode = SelectionMode.Extended;

        // Assert - both items should still be selected
        listBox.SelectedItems.Count.ShouldBe(2);

        // Act - switch to Single mode
        listBox.SelectionMode = SelectionMode.Single;

        // Assert - only first item should remain
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe(firstItem);

        // Act - switch back to Multiple mode
        listBox.SelectionMode = SelectionMode.Multiple;

        // Assert - the single selection should be maintained
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe(firstItem);
    }

    [Fact]
    public void SelectionMode_SwitchToSingleWithNoSelection_ShouldMaintainEmptySelection()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");

        // Act - switch to Single mode without any selection
        listBox.SelectionMode = SelectionMode.Single;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(0);
    }

    [Fact]
    public void SingleMode_Click_ShouldDeselectOtherItems()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Single };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        // Act
        listBox.ListBoxItems[0].IsSelected = true;
        listBox.ListBoxItems[1].IsSelected = true;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe("Item 1");
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SingleMode_ClickSameItem_ShouldMaintainSelection()
    {
        // Arrange
        var expectedItem = "Item 0";
        var listBox = new ListBox { SelectionMode = SelectionMode.Single };
        listBox.AddToRoot();
        listBox.Items!.Add(expectedItem);
        listBox.Items!.Add("Item 1");

        // Select first item
        listBox.SelectedItems.Add(expectedItem);
        listBox.SelectedItems.Count.ShouldBe(1);

        // Act - click the same item again
        var mockCursor = SetupForPushOnItem(listBox, itemIndex: 0);
        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);

        // Assert - item should still be selected
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0]!.ShouldBe(expectedItem);
    }

    [Fact]
    public void WhenItemRemovedFromItems_ShouldRemoveFromSelectedItems()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");

        // Act
        listBox.Items!.Remove("Item 1");

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems.Cast<object>().ShouldContain("Item 0");
        listBox.SelectedItems.Cast<object>().ShouldNotContain("Item 1");
    }

    [Fact]
    public void WhenItemsCleared_ShouldClearSelectedItems()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items!.Add("Item 0");
        listBox.Items!.Add("Item 1");
        listBox.Items!.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");

        // Act
        listBox.Items!.Clear();

        // Assert
        listBox.SelectedItems.Count.ShouldBe(0);
    }

    #endregion

    #region Setup Methods

    private static Mock<ICursor> SetupForPush()
    {
        Mock<ICursor> mockCursor = new();
        mockCursor.Setup(c => c.XRespectingGumZoomAndBounds())
            .Returns(20);
        mockCursor.Setup(c => c.YRespectingGumZoomAndBounds())
            .Returns(20);
        mockCursor.Setup(c => c.X)
            .Returns(20);
        mockCursor.Setup(c => c.Y)
            .Returns(20);

        mockCursor.Setup(c => c.LastInputDevice)
            .Returns(InputDevice.Mouse);

        FrameworkElement.MainCursor = mockCursor.Object;

        mockCursor.Setup(c => c.PrimaryPush).Returns(true);
        return mockCursor;
    }

    private static Mock<ICursor> SetupForPushOnItem(ListBox listBox, int itemIndex)
    {
        var mockCursor = SetupForPush();

        if (itemIndex >= 0 && itemIndex < listBox.ListBoxItems.Count)
        {
            var targetItemVisual = listBox.ListBoxItems[itemIndex]!.Visual;
            var cursorX = targetItemVisual.GetAbsoluteX() + targetItemVisual.GetAbsoluteWidth() / 2;
            var cursorY = targetItemVisual.GetAbsoluteY() + targetItemVisual.GetAbsoluteHeight() / 2;

            mockCursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(cursorX);
            mockCursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(cursorY);
            mockCursor.Setup(c => c.X).Returns((int)cursorX);
            mockCursor.Setup(c => c.Y).Returns((int)cursorY);
        }

        return mockCursor;
    }

    private static Mock<ICursor> SetupForPushWithCtrl()
    {
        var mockCursor = SetupForPush();

        // Setup a mock keyboard with Ctrl pressed
        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard.Setup(k => k.KeyDown(Gum.Forms.Input.Keys.LeftControl))
            .Returns(true);

        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        return mockCursor;
    }

    private static Mock<ICursor> SetupForPushWithCtrlOnItem(ListBox listBox, int itemIndex)
    {
        var mockCursor = SetupForPushOnItem(listBox, itemIndex);

        // Setup a mock keyboard with Ctrl pressed
        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard.Setup(k => k.KeyDown(Gum.Forms.Input.Keys.LeftControl))
            .Returns(true);

        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        return mockCursor;
    }

    private static Mock<ICursor> SetupForPushWithShift()
    {
        var mockCursor = SetupForPush();

        // Setup a mock keyboard with Shift pressed
        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard.Setup(k => k.KeyDown(Gum.Forms.Input.Keys.LeftShift))
            .Returns(true);

        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        return mockCursor;
    }

    private static Mock<ICursor> SetupForPushWithShiftOnItem(ListBox listBox, int itemIndex)
    {
        var mockCursor = SetupForPushOnItem(listBox, itemIndex);

        // Setup a mock keyboard with Shift pressed
        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard.Setup(k => k.KeyDown(Gum.Forms.Input.Keys.LeftShift))
            .Returns(true);

        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        return mockCursor;
    }

    #endregion

    [Fact]
    public void ListBox_DownArrowPressed_MovesSelectionDown()
    {
        ListBox listBox = new ListBox();
        listBox.AddToRoot();

        listBox.Items!.Add("A");
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");

        listBox.SelectedIndex = 0;
        // ListBox routes keyboard arrow navigation through DoListItemFocusUpdate(),
        // which only runs when items (not the list itself) have focus.
        listBox.DoListItemsHaveFocus = true;

        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyTyped(Gum.Forms.Input.Keys.Down)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeysTyped).Returns(new List<Gum.Forms.Input.Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void ListBox_RightStickDeflection_ScrollsAtTopLevelFocus()
    {
        // Pins that ListBox.DoTopLevelFocusUpdate calls the same ApplyGamepadStickScroll
        // that ScrollViewer.DoTopLevelFocusUpdate uses, rather than duplicating the
        // stick-scroll code (issue #3839).
        ListBox listBox = new ListBox();
        listBox.Visual.Height = 200;
        listBox.AddToRoot();

        for (int i = 0; i < 50; i++)
        {
            listBox.Items!.Add($"Item {i}");
        }

        listBox.VerticalScrollBarMaximum.ShouldBeGreaterThan(0,
            "the test needs scrollable content to observe the scroll");

        listBox.GamepadStickScrollSpeed = 100;
        listBox.VerticalScrollBarValue = 500;
        listBox.DoListItemsHaveFocus = false;

        GamePad gamepad = new();
        // Right stick pushed fully up (XNA/Gum convention: +1 is up).
        gamepad.Activity(
            new GamePadState(
                new GamePadThumbSticks(new Microsoft.Xna.Framework.Vector2(0, 0), new Microsoft.Xna.Framework.Vector2(0, 1f)),
                new GamePadTriggers(0, 0),
                new GamePadButtons(0),
                new GamePadDPad()),
            0);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        InteractiveGue.CurrentGameTime = 0;
        listBox.OnFocusUpdate();

        InteractiveGue.CurrentGameTime = 1;
        listBox.OnFocusUpdate();

        listBox.VerticalScrollBarValue.ShouldBe(400, tolerance: 0.01);
    }

    [Fact]
    public void ListBox_EnterPressedAtTopLevelFocus_DropsFocusIntoItems()
    {
        // Regression test for the Bucket 2 sub-task 4 follow-up: the guard around
        // the Enter-to-drop-focus-into-items block in ListBox.DoTopLevelFocusUpdate
        // was flipped from #if (MONOGAME || KNI || FNA) && !FRB to !FRB. This pins
        // the MonoGame behavior so the flip stays zero-impact on MonoGame.
        ListBox listBox = new ListBox();
        listBox.AddToRoot();

        listBox.Items!.Add("A");
        listBox.Items!.Add("B");

        listBox.DoListItemsHaveFocus = false;

        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        // ClickCombos default contains Enter. IsComboPushed calls keyboard.KeyPushed
        // (on the shared interface, which takes Gum.Forms.Input.Keys) with no held key.
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Enter)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeysTyped).Returns(new List<Gum.Forms.Input.Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.OnFocusUpdate();

        listBox.DoListItemsHaveFocus.ShouldBeTrue();
    }

    private class DisplayItem
    {
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    [Fact]
    public void DisplayMemberPath_WithNonListBoxItemInItems_ShouldUpdateRowsByReference()
    {
        // Issue #556: the DisplayMemberPath setter walked Items[i]/ListBoxItems[i] in parallel,
        // which misaligns (and goes out of range) when a non-ListBoxItem is in Items. It must
        // resolve each row's data object by reference.
        Gum.Forms.DefaultVisuals.ButtonVisual button = new();

        ListBox listBox = new();
        listBox.Items!.Add(new DisplayItem { Name = "Alpha" });
        listBox.Items!.Add(button);
        listBox.Items!.Add(new DisplayItem { Name = "Beta" });

        listBox.DisplayMemberPath = "Name";

        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.ListBoxItems[0].ToString().ShouldBe("Alpha");
        listBox.ListBoxItems[1].ToString().ShouldBe("Beta");
    }

    [Fact]
    public void DoListItemsHaveFocus_SetTrueWhenSelectedRowAfterNonListBoxItem_ShouldNotThrow()
    {
        // Issue #556: SelectedIndex is an Items-space index; with a non-ListBoxItem (Button) in
        // Items it can exceed ListBoxItemsInternal.Count. DoListItemsHaveFocus indexed
        // ListBoxItemsInternal by SelectedIndex and threw. It must resolve the row by reference.
        Gum.Forms.DefaultVisuals.ButtonVisual button = new();

        ListBox listBox = new();
        listBox.Items!.Add("A");
        listBox.Items!.Add(button);
        listBox.Items!.Add("B");

        listBox.SelectedObject = "B"; // SelectedIndex == 2 in Items space, but only 2 rows exist

        Should.NotThrow(() => listBox.DoListItemsHaveFocus = true);

        listBox.ListBoxItems[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void DownArrow_WithNonListBoxItemBetweenRows_ShouldSelectNextRowNotTheNonListBoxItem()
    {
        // Issue #556: arrow navigation moved SelectedIndex in Items space and then indexed
        // ListBoxItemsInternal with it, so a non-ListBoxItem (Button) in Items made the selection
        // land on the unselectable Button instead of the next real row.
        Gum.Forms.DefaultVisuals.ButtonVisual button = new();

        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items!.Add("A");
        listBox.Items!.Add(button);
        listBox.Items!.Add("B");

        listBox.SelectedObject = "A";
        listBox.DoListItemsHaveFocus = true;

        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyTyped(Gum.Forms.Input.Keys.Down)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeysTyped).Returns(new List<Gum.Forms.Input.Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.OnFocusUpdate();

        listBox.SelectedObject.ShouldBe("B");
    }

    [Fact]
    public void ItemsAddedWhileInvisible_ShouldHaveFontsResolved()
    {
        // Regression: ItemsControl.HandleItemsCollectionChanged gates its
        // ResumeLayout(recursive: true) call on IsVisible. That ResumeLayout
        // is what triggers UpdateFontRecursive, which performs the deferred
        // font load for any TextRuntime whose IsFontDirty flag got set
        // while IsAllLayoutSuspended was true (the suspension is set during
        // item creation, so every font assignment on a freshly-created
        // ListBoxItem visual hits the deferral path).
        //
        // Concrete manifestation: a ComboBox's dropdown ListBox is created
        // with Visible=false in V3.ComboBoxVisual.PositionAndAttachListBox.
        // Items added to it have IsFontDirty=true on their TextInstance,
        // and never get resolved — when the dropdown is later opened, the
        // text renders with whatever fallback the font lookup falls back to
        // rather than the styling's configured font.
        //
        // The fix is to drop the IsVisible gate: realization should happen
        // regardless of visibility, mirroring the WireframeObjectManager
        // pattern documented on UpdateFontRecursive.

        ListBox listBox = new();
        listBox.Visual.Visible = false;

        listBox.Items!.Add("First");
        listBox.Items!.Add("Second");

        foreach (var item in listBox.ListBoxItems)
        {
            GraphicalUiElement textInstance = item.Visual.GetGraphicalUiElementByName("TextInstance");
            textInstance.ShouldNotBeNull();
            textInstance.IsFontDirty.ShouldBeFalse(
                "Item TextInstance was constructed while the parent ListBox was invisible. " +
                "Expected the deferred font load to have been processed by ResumeLayout(recursive: true), " +
                "but IsFontDirty is still true.");
        }
    }

    #region Decorations (issue #3305)

    [Fact]
    public void InsertDecorationAfter_RendersBetweenRows_AndKeepsItemsAndSelectionContiguous()
    {
        // Issue #3305: a decoration (here a ColoredRectangle) lives in InnerPanel.Children so it
        // renders between rows, but it is in neither Items nor ListBoxItems. SelectedIndex stays
        // contiguous (the row after the decoration is index 2, not 3) and can never be a decoration.
        ListBox listBox = new();
        listBox.Items!.Add("A");
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");

        ColoredRectangleRuntime separator = new();
        listBox.InsertDecorationAfter("B", separator);

        listBox.Items!.Count.ShouldBe(3);
        listBox.ListBoxItems.Count.ShouldBe(3);

        ObservableCollection<GraphicalUiElement> panel = listBox.InnerPanel.Children;
        panel.Count.ShouldBe(4);
        panel[0].ShouldBe(listBox.ListBoxItems[0].Visual); // A
        panel[1].ShouldBe(listBox.ListBoxItems[1].Visual); // B
        panel[2].ShouldBe(separator);                      // decoration
        panel[3].ShouldBe(listBox.ListBoxItems[2].Visual); // C

        listBox.SelectedObject = "C";
        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void AddDecoration_ThenAddItems_KeepsItemVisualOrderCorrect_WithBoundTypedItems()
    {
        // Issue #3305: the shared ItemsControl base inserts each new item's visual at its Items
        // index into InnerPanel.Children. A decoration occupies a panel slot without an Items slot,
        // so without index translation items added after a decoration land in the wrong panel slot.
        // Also verifies a bound typed ObservableCollection<string> stays valid alongside decorations.
        ObservableCollection<string> items = new() { "A", "B" };
        ListBox listBox = new();
        listBox.Items = items;

        ColoredRectangleRuntime separator = new();
        listBox.AddDecoration(separator); // "add now" -> after the current last item ("B")

        items.Add("C");
        items.Add("D");

        ObservableCollection<GraphicalUiElement> panel = listBox.InnerPanel.Children;
        panel.Count.ShouldBe(5);
        panel[0].ShouldBe(listBox.ListBoxItems[0].Visual); // A
        panel[1].ShouldBe(listBox.ListBoxItems[1].Visual); // B
        panel[2].ShouldBe(separator);
        panel[3].ShouldBe(listBox.ListBoxItems[2].Visual); // C
        panel[4].ShouldBe(listBox.ListBoxItems[3].Visual); // D

        items.ShouldBe(new[] { "A", "B", "C", "D" });
        listBox.ListBoxItems.Count.ShouldBe(4);
        listBox.ListBoxItems[2].BindingContext.ShouldBe("C");
        listBox.ListBoxItems[3].BindingContext.ShouldBe("D");
    }

    [Fact]
    public void Click_OnDecoration_ShouldNotChangeSelection()
    {
        // Issue #3305: clicking a decoration must be inert - it must not deselect the current item
        // or raise SelectionChanged (and, by extension, must not close a parent ComboBox).
        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items!.Add("A");
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");

        ColoredRectangleRuntime separator = new();
        separator.Height = 30;
        listBox.InsertDecorationAfter("A", separator);

        listBox.SelectedObject = "C";

        int fireCount = 0;
        listBox.SelectionChanged += (_, _) => fireCount++;

        Mock<ICursor> cursor = SetupForPush();
        float cx = separator.GetAbsoluteX() + separator.GetAbsoluteWidth() / 2;
        float cy = separator.GetAbsoluteY() + separator.GetAbsoluteHeight() / 2;
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(cx);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(cy);
        cursor.Setup(c => c.X).Returns((int)cx);
        cursor.Setup(c => c.Y).Returns((int)cy);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(listBox.Visual, cursor.Object, null!, 0);

        listBox.SelectedObject.ShouldBe("C");
        fireCount.ShouldBe(0);
    }

    [Fact]
    public void DownArrow_NavigationSkipsDecoration()
    {
        // Issue #3305: keyboard/gamepad navigation moves through rows (ListBoxItems), so a
        // decoration sitting between two rows is skipped - down from "A" selects "B", not the
        // decoration.
        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items!.Add("A");
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");

        ColoredRectangleRuntime separator = new();
        listBox.InsertDecorationAfter("A", separator);

        listBox.SelectedIndex = 0;
        listBox.DoListItemsHaveFocus = true;

        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyTyped(Gum.Forms.Input.Keys.Down)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeysTyped).Returns(new List<Gum.Forms.Input.Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.OnFocusUpdate();

        listBox.SelectedObject.ShouldBe("B");
        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void RemovingAnchorItem_RemovesTheDecoration()
    {
        // Issue #3305: a decoration is anchored to a data item, so removing that item removes the
        // decoration too - it never outlives the row it was attached to.
        ListBox listBox = new();
        listBox.Items!.Add("A");
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");

        ColoredRectangleRuntime separator = new();
        listBox.InsertDecorationAfter("B", separator);

        listBox.InnerPanel.Children.Count.ShouldBe(4);

        listBox.Items!.Remove("B");

        listBox.InnerPanel.Children.Count.ShouldBe(2);
        listBox.InnerPanel.Children.Contains(separator).ShouldBeFalse();
        separator.Parent.ShouldBeNull();
        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.ListBoxItems[0].BindingContext.ShouldBe("A");
        listBox.ListBoxItems[1].BindingContext.ShouldBe("C");
    }

    [Fact]
    public void ReorderItems_KeepsCollectionsInSync_AndDecorationFollowsAnchor()
    {
        // Issue #3305: reordering with a decoration present keeps Items, ListBoxItems, and
        // InnerPanel.Children in sync, and the decoration follows its anchor item to its new spot.
        ObservableCollection<string> items = new() { "A", "B", "C" };
        ListBox listBox = new();
        listBox.Items = items;

        ColoredRectangleRuntime separator = new();
        listBox.InsertDecorationAfter("A", separator); // panel: A, sep, B, C

        items.Move(0, 2); // A -> end. Items: B, C, A

        items.ShouldBe(new[] { "B", "C", "A" });
        listBox.ListBoxItems.Count.ShouldBe(3);
        listBox.ListBoxItems[0].BindingContext.ShouldBe("B");
        listBox.ListBoxItems[1].BindingContext.ShouldBe("C");
        listBox.ListBoxItems[2].BindingContext.ShouldBe("A");

        ObservableCollection<GraphicalUiElement> panel = listBox.InnerPanel.Children;
        panel.Count.ShouldBe(4);
        panel[0].ShouldBe(listBox.ListBoxItems[0].Visual); // B
        panel[1].ShouldBe(listBox.ListBoxItems[1].Visual); // C
        panel[2].ShouldBe(listBox.ListBoxItems[2].Visual); // A
        panel[3].ShouldBe(separator);                      // decoration followed A
    }

    [Fact]
    public void Decoration_IsInPanelButNotAnItemOrRow_AndRemoveDecorationTakesItOut()
    {
        // Issue #3305: a decoration is a plain visual - it renders in the panel but is never data
        // (Items) or a selectable row (ListBoxItems), and RemoveDecoration takes it back out.
        ListBox listBox = new();
        listBox.Items!.Add("A");
        listBox.Items!.Add("B");

        RectangleRuntime decoration = new();
        listBox.AddDecoration(decoration);

        listBox.InnerPanel.Children.Contains(decoration).ShouldBeTrue();
        listBox.Items!.Contains(decoration).ShouldBeFalse();
        listBox.ListBoxItems.Any(item => ReferenceEquals(item.Visual, decoration)).ShouldBeFalse();

        listBox.RemoveDecoration(decoration).ShouldBeTrue();
        listBox.InnerPanel.Children.Contains(decoration).ShouldBeFalse();
        decoration.Parent.ShouldBeNull();
    }

    [Fact]
    public void InsertDecorationBefore_PlacesDecorationAheadOfTheAnchorRow()
    {
        // Issue #3305: the Before placement puts the decoration immediately ahead of its anchor
        // row's visual, while Items/ListBoxItems stay pure.
        ListBox listBox = new();
        listBox.Items!.Add("A");
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");

        ColoredRectangleRuntime separator = new();
        listBox.InsertDecorationBefore("B", separator);

        ObservableCollection<GraphicalUiElement> panel = listBox.InnerPanel.Children;
        panel.Count.ShouldBe(4);
        panel[0].ShouldBe(listBox.ListBoxItems[0].Visual); // A
        panel[1].ShouldBe(separator);                      // decoration before B
        panel[2].ShouldBe(listBox.ListBoxItems[1].Visual); // B
        panel[3].ShouldBe(listBox.ListBoxItems[2].Visual); // C

        listBox.Items!.Count.ShouldBe(3);
        listBox.ListBoxItems.Count.ShouldBe(3);
    }

    [Fact]
    public void InsertDecorationAfter_WithAnchorNotInItems_Throws()
    {
        // Issue #3305: anchoring to an item that is not in Items is a programming error and must
        // be rejected rather than silently dropping the decoration.
        ListBox listBox = new();
        listBox.Items!.Add("A");

        ColoredRectangleRuntime separator = new();

        Should.Throw<ArgumentException>(() => listBox.InsertDecorationAfter("not-in-list", separator));
    }

    #endregion

    #region Pinning: gaps the row-identity rework could plausibly break (issue #3509)

    [Fact]
    public void SelectedIndex_Getter_AfterSetterWithDecorationPresent_ReturnsCorrectItemsIndex()
    {
        // Pin: the existing decoration coverage only round-trips SelectedIndex through the
        // SelectedObject-driven path. The SelectedIndex SETTER must resolve through the same
        // panel-index helpers when a decoration occupies a non-Items panel slot (issue #3305).
        ListBox listBox = new();
        listBox.Items!.Add("A");
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");

        ColoredRectangleRuntime separator = new();
        listBox.InsertDecorationAfter("A", separator);

        listBox.SelectedIndex = 2;

        listBox.SelectedIndex.ShouldBe(2);
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
    }

    #endregion

    #region Pre-existing bug found while investigating #3509 (not duplicate-reference-specific)

    [Fact]
    public void SelectedIndex_Getter_AfterReorder_ReturnsMovedItemsCorrectPosition()
    {
        // Discovered while investigating #3509, but reproducible with NO duplicate values:
        // HandleCollectionItemMoved's SelectedIndex==oldIndex/newIndex comparison reads the (buggy)
        // getter, so moving an unrelated pair of rows around the selected one silently reassigns
        // selection to the wrong item even without any duplicate references involved. With
        // row-identity tracking, the moved row's selection simply follows it to its new position -
        // no index comparison needed at all.
        ObservableCollection<string> values = new() { "A", "B", "C" };
        ListBox listBox = new();
        listBox.Items = values;

        listBox.SelectedObject = "A";

        values.Move(0, 2);

        listBox.SelectedObject.ShouldBe("A");
        listBox.SelectedIndex.ShouldBe(2);
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
    }

    #endregion

    #region Duplicate-reference items (issue #3509)

    private class DuplicatableItem
    {
        public override string ToString() => "Shared";
    }

    [Fact]
    public void SelectedIndex_Getter_WithDuplicateReferenceItems_ShouldReturnActualSelectedOccurrence()
    {
        // Issue #3509: Items[0] and Items[2] are the SAME reference (e.g. a shared inventory Item
        // stacked twice). Selecting the row at index 2 must report SelectedIndex == 2, not 0 (the
        // first occurrence) - the getter previously resolved via Items.IndexOf(value), which
        // always finds the first match.
        DuplicatableItem shared = new();
        ListBox listBox = new();
        listBox.Items!.Add(shared);
        listBox.Items!.Add("B");
        listBox.Items!.Add(shared);

        listBox.SelectedIndex = 2;

        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void SelectedIndex_Setter_WithDuplicateReferenceItems_ShouldSelectOnlyThatRow()
    {
        // Issue #3509 (the reported bug): setting SelectedIndex to one occurrence of a duplicated
        // reference must highlight only that row, not every row sharing the reference.
        DuplicatableItem shared = new();
        ListBox listBox = new();
        listBox.Items!.Add(shared);
        listBox.Items!.Add("B");
        listBox.Items!.Add(shared);

        listBox.SelectedIndex = 2;

        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SelectedObject_set_WithDuplicateReferenceItems_ShouldNotSelectAllOccurrences()
    {
        // The exact AirPig-reported scenario: an inventory ObservableCollection<Item> containing
        // the same shared Item reference more than once. SelectedObject can only disambiguate to a
        // single (first-match) row since it carries no index, but it must never highlight every
        // occurrence.
        DuplicatableItem shared = new();
        ListBox listBox = new();
        listBox.Items!.Add(shared);
        listBox.Items!.Add(shared);
        listBox.Items!.Add(shared);

        listBox.SelectedObject = shared;

        int selectedCount = listBox.ListBoxItems.Count(li => li.IsSelected);
        selectedCount.ShouldBe(1);
    }

    [Fact]
    public void SingleMode_Click_WithDuplicateReferenceItems_ShouldSelectOnlyClickedRow()
    {
        DuplicatableItem shared = new();
        ListBox listBox = new() { SelectionMode = SelectionMode.Single };
        listBox.AddToRoot();
        listBox.Items!.Add(shared);
        listBox.Items!.Add("B");
        listBox.Items!.Add(shared);

        Mock<ICursor> cursor = SetupForPushOnItem(listBox, itemIndex: 2);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(listBox.Visual, cursor.Object, null!, 0);

        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void MultipleMode_Click_WithDuplicateReferenceItems_ShouldToggleOnlyClickedRow()
    {
        // Issue #3509 bug: today, clicking a second row sharing an already-selected row's value
        // takes the "remove" branch (Contains(value) is already true) and deselects the FIRST row
        // instead of adding the second - two rows sharing a value could never both be selected.
        DuplicatableItem shared = new();
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.AddToRoot();
        listBox.Items!.Add(shared);
        listBox.Items!.Add("B");
        listBox.Items!.Add(shared);

        Mock<ICursor> firstClick = SetupForPushOnItem(listBox, itemIndex: 0);
        firstClick.SetupProperty(x => x.VisualOver);
        firstClick.SetupProperty(x => x.WindowPushed);
        GueInteractiveExtensionMethods.DoUiActivityRecursively(listBox.Visual, firstClick.Object, null!, 0);

        Mock<ICursor> secondClick = SetupForPushOnItem(listBox, itemIndex: 2);
        secondClick.SetupProperty(x => x.VisualOver);
        secondClick.SetupProperty(x => x.WindowPushed);
        GueInteractiveExtensionMethods.DoUiActivityRecursively(listBox.Visual, secondClick.Object, null!, 0);

        listBox.ListBoxItems[0].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();

        Mock<ICursor> toggleOffFirst = SetupForPushOnItem(listBox, itemIndex: 0);
        toggleOffFirst.SetupProperty(x => x.VisualOver);
        toggleOffFirst.SetupProperty(x => x.WindowPushed);
        GueInteractiveExtensionMethods.DoUiActivityRecursively(listBox.Visual, toggleOffFirst.Object, null!, 0);

        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void ExtendedMode_CtrlClick_WithDuplicateReferenceItems_ShouldAllowBothRowsSelected()
    {
        // Issue #3509 bug #3: today, Contains(value) is already true for the second row (it shares
        // the first row's value), so Ctrl+Click takes the REMOVE branch and deselects the first row
        // instead of adding the second - two rows sharing a value could never both be selected.
        // SelectedItems is a dedup-by-value projection of the selected rows (documented design
        // decision), so with two duplicate-valued rows selected, SelectedItems holds one entry even
        // though both rows are independently highlighted.
        DuplicatableItem shared = new();
        ListBox listBox = new() { SelectionMode = SelectionMode.Extended };
        listBox.AddToRoot();
        listBox.Items!.Add(shared);
        listBox.Items!.Add("B");
        listBox.Items!.Add(shared);

        Mock<ICursor> firstClick = SetupForPushOnItem(listBox, itemIndex: 0);
        firstClick.SetupProperty(x => x.VisualOver);
        firstClick.SetupProperty(x => x.WindowPushed);
        GueInteractiveExtensionMethods.DoUiActivityRecursively(listBox.Visual, firstClick.Object, null!, 0);

        Mock<ICursor> ctrlClick = SetupForPushWithCtrlOnItem(listBox, itemIndex: 2);
        ctrlClick.SetupProperty(x => x.VisualOver);
        ctrlClick.SetupProperty(x => x.WindowPushed);
        GueInteractiveExtensionMethods.DoUiActivityRecursively(listBox.Visual, ctrlClick.Object, null!, 0);

        listBox.ListBoxItems[0].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
        listBox.SelectedItems.Count.ShouldBe(1);
    }

    [Fact]
    public void DownArrow_WithDuplicateReferenceItems_ShouldAdvanceToNextRowNotJumpToFirstOccurrence()
    {
        // Issue #3509: navigating down from row 0 (value shared) to row 1 (also value shared) must
        // land on SelectedIndex == 1, not snap back to 0 via Items.IndexOf(value)'s first-match.
        DuplicatableItem shared = new();
        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items!.Add(shared);
        listBox.Items!.Add(shared);
        listBox.Items!.Add("C");

        listBox.SelectedIndex = 0;
        listBox.DoListItemsHaveFocus = true;

        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyTyped(Gum.Forms.Input.Keys.Down)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeysTyped).Returns(new List<Gum.Forms.Input.Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void Decoration_WithDuplicateReferenceItemsAroundIt_SelectedIndexResolvesCorrectRow()
    {
        // Issue #3509 x #3305: a decoration sits between the two duplicate-valued rows. SelectedIndex
        // (Items-space) must still resolve to the correct physical row via the panel-index helpers.
        DuplicatableItem shared = new();
        ListBox listBox = new();
        listBox.Items!.Add(shared);
        listBox.Items!.Add("B");
        listBox.Items!.Add(shared);

        ColoredRectangleRuntime separator = new();
        listBox.InsertDecorationAfter("B", separator);

        listBox.SelectedIndex = 2;

        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
        listBox.SelectedIndex.ShouldBe(2);
    }

    #endregion

    #region ShowPopupListBox

    [Fact]
    public void ShowPopupListBox_UsesResolvePopupRootsOverride_WhenSet()
    {
        ContainerRuntime customPopupRoot = new();
        customPopupRoot.AddToManagers(RenderingLibrary.SystemManagers.Default);
        ContainerRuntime customModalRoot = new();
        customModalRoot.AddToManagers(RenderingLibrary.SystemManagers.Default);

        Gum.Wireframe.GraphicalUiElement.ResolvePopupRoots = _ => (customPopupRoot, customModalRoot);

        try
        {
            ComboBox comboBox = new();
            comboBox.AddToRoot();

            comboBox.IsDropDownOpen = true;

            customPopupRoot.Children.ShouldContain(comboBox.ListBox!.Visual);
            Gum.GumService.Default.PopupRoot.Children.ShouldNotContain(comboBox.ListBox!.Visual);
        }
        finally
        {
            Gum.Wireframe.GraphicalUiElement.ResolvePopupRoots = null;
            customPopupRoot.Children.Clear();
            customPopupRoot.RemoveFromManagers();
            customModalRoot.Children.Clear();
            customModalRoot.RemoveFromManagers();
        }
    }

    [Fact]
    public void ShowPopupListBox_FallsBackToGlobalPopupRoot_WhenResolvePopupRootsIsNotSet()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();

        comboBox.IsDropDownOpen = true;

        Gum.GumService.Default.PopupRoot.Children.ShouldContain(comboBox.ListBox!.Visual);
    }

    [Fact]
    public void ShowPopupListBox_ClampsAgainstResolvedPopupRootBounds_NotGlobalCanvas()
    {
        // Issue #3591: reposition-to-keep-in-screen ran before the popup root was resolved
        // and clamped against the global canvas, so a popup routed to a smaller per-camera
        // root (via ResolvePopupRoots) could still overflow that root's bounds.
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;

        ContainerRuntime customPopupRoot = new();
        customPopupRoot.X = 100;
        customPopupRoot.Y = 50;
        customPopupRoot.Width = 200;
        customPopupRoot.Height = 150;
        customPopupRoot.AddToManagers(RenderingLibrary.SystemManagers.Default);
        ContainerRuntime customModalRoot = new();
        customModalRoot.AddToManagers(RenderingLibrary.SystemManagers.Default);

        Gum.Wireframe.GraphicalUiElement.ResolvePopupRoots = _ => (customPopupRoot, customModalRoot);

        try
        {
            ContainerRuntime listBoxParent = new();
            listBoxParent.AddToRoot();

            ScrollViewer popup = new();
            popup.Width = 100;
            popup.Height = 50;
            // Fits within the 800x600 global canvas, but overflows customPopupRoot's
            // right/bottom edges (300, 200) once parented under it:
            popup.X = 250;
            popup.Y = 180;

            ListBox.ShowPopupListBox(popup, listBoxParent);

            (popup.Visual.AbsoluteX + popup.Visual.GetAbsoluteWidth())
                .ShouldBeLessThanOrEqualTo(customPopupRoot.X + customPopupRoot.GetAbsoluteWidth());
            (popup.Visual.AbsoluteY + popup.Visual.GetAbsoluteHeight())
                .ShouldBeLessThanOrEqualTo(customPopupRoot.Y + customPopupRoot.GetAbsoluteHeight());
        }
        finally
        {
            Gum.Wireframe.GraphicalUiElement.ResolvePopupRoots = null;
            customPopupRoot.Children.Clear();
            customPopupRoot.RemoveFromManagers();
            customModalRoot.Children.Clear();
            customModalRoot.RemoveFromManagers();
        }
    }

    #endregion
}
