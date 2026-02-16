using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using Moq;
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
    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        ListBox listBox = new();
        InteractiveGue visual = listBox.Visual;

        List<ContainerRuntime> children = new ();
        visual.FillListWithChildrenByTypeRecursively<ContainerRuntime>(children);

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
    public void IsEnabled_ShouldSetListBoxItemsDisable_IfSetToFalse()
    {
        bool didSet = false;
        ListBoxItem listBoxItem = new ();
        var disabledState = listBoxItem.GetState(FrameworkElement.DisabledStateName);

        disabledState.Clear();
        disabledState.Apply = () =>
        {
            didSet = true;
        };

        ListBox listBox = new ListBox();
        listBox.Items.Add(listBoxItem);
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
        listBox.Items.Add(listBoxItem);
        didSet.ShouldBeTrue();
    }

    [Fact]
    public void Click_ShouldNotSelect_IfDisabled()
    {
        ListBox listBox = new();
        listBox.IsEnabled = false;
        listBox.AddToRoot();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add("Item " + i);
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
    public void Click_ShouldSelect_IfEnabled()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add("Item " + i);
        }

        Mock<ICursor> mockCursor = SetupForPush();

        mockCursor.SetupProperty(x => x.VisualOver);
        mockCursor.SetupProperty(x => x.WindowPushed);


        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null!,
            0);


        if(listBox.SelectedObject == null)
        {
            var firstListBoxItem = listBox.ListBoxItems[0];

            var isOverFirst = mockCursor.Object.VisualOver ==
                firstListBoxItem.Visual;

            string diagnostics =
                $"VisualOver: {mockCursor.Object.VisualOver}" +
                $" WindowPushed: {mockCursor.Object.WindowPushed}" +
                $" Is over first: {isOverFirst}";
            throw new Exception(diagnostics);
        }

    }

    [Fact]
    public void Items_ShoudAddListBoxItem_WhenAdding()
    {
        ListBox listBox = new ();
        listBox.Items.Add(1);
        listBox.ListBoxItems.Count.ShouldBe(1);
        (listBox.ListBoxItems[0] is ListBoxItem).ShouldBeTrue();

        listBox.InnerPanel.Children.Count.ShouldBe(1);
        (listBox.InnerPanel.Children[0] is InteractiveGue).ShouldBeTrue();
        (listBox.InnerPanel.Children[0] as InteractiveGue)!.FormsControlAsObject
            .ShouldBeOfType<ListBoxItem>();
    }

    [Fact]
    public void Items_Add_ShouldAddListBoxItems()
    {
        ListBox listBox = new();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add("Item " + i);
        }
        listBox.ListBoxItems.Count.ShouldBe(10);
    }

    [Fact]
    public void Items_Clear_ShouldClearListBoxItems()
    {
        ListBox listBox = new();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add("Item " + i);
        }
        listBox.Items.Clear();
        listBox.ListBoxItems.Count.ShouldBe(0);
    }

    [Fact]
    public void Items_Clear_ShouldWorkWhenAddingButtonVisual()
    {
        Gum.Forms.DefaultVisuals.ButtonVisual button = new();

        ListBox listBox = new();
        button.Parent.ShouldBeNull();
        listBox.Items.Add(button);
        button.Parent.ShouldNotBeNull();
        listBox.Items.Clear(); // should not throw
        button.Parent.ShouldBeNull();
    }

    [Fact]
    public void Items_InsertAt_ShouldProperlyInsert()
    {
        ListBox listBox = new();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add("Item " + i);
        }

        var itemToRemove = listBox.Items[5];
        listBox.Items.RemoveAt(5);
        listBox.Items.Insert(0, itemToRemove);

        listBox.ListBoxItems.Count.ShouldBe(10);
        var item5 = listBox.ListBoxItems[0];
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
            listBox.ListBoxItems[i].BindingContext.ShouldBe("Item " + i);
        }

        values.Move(0, 1);

        listBox.ListBoxItems[0].BindingContext.ShouldBe("Item 1");
        listBox.ListBoxItems[1].BindingContext.ShouldBe("Item 0");

        var innerPanel = listBox.Visual.GetChildByNameRecursively("InnerPanelInstance")!;
        innerPanel.Children.Count.ShouldBe(10);
        for(int i = 0; i < 10; i++)
        {
            innerPanel.Children[i].ShouldBe(listBox.ListBoxItems[i].Visual);
        }
    }

    [Fact]
    public void Items_Remove_ShouldRemoveListBoxItems()
    {

        ListBox listBox = new();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add("Item " + i);
        }

        for(int i = 0; i < 10; i++)
        {
            listBox.Items.Remove("Item " + i);
            listBox.ListBoxItems.Count.ShouldBe(9 - i);
            if( listBox.ListBoxItems.Count > 0)
            {
                var nextItem = listBox.ListBoxItems[0];
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
            listBox.Items.Add("Item " + i);
        }

        for (int i = 0; i < 10; i++)
        {
            listBox.Items.RemoveAt(0);
            listBox.ListBoxItems.Count.ShouldBe(9 - i);
        }
    }

    [Fact]
    public void ListBoxItems_ShouldReflectBackingObjects_WhenRemoving()
    {
        ListBox listBox = new();
        for (int i = 0; i < 2; i++)
        {
            listBox.Items.Add("Item " + i);
        }
        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.Items.Remove("Item 1");
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
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        // Select first item
        listBox.ListBoxItems[0].IsSelected = true;

        // Act - click second item without modifiers
        listBox.ListBoxItems[1].IsSelected = true;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0].ShouldBe("Item 1");
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[2].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void ModifierKeys_ShouldBeCustomizable()
    {
        // Arrange
        var originalToggle = ListBox.ToggleSelectionModifierKey;
        var originalRange = ListBox.RangeSelectionModifierKey;

        // Act
        ListBox.ToggleSelectionModifierKey = Microsoft.Xna.Framework.Input.Keys.LeftAlt;
        ListBox.RangeSelectionModifierKey = Microsoft.Xna.Framework.Input.Keys.LeftControl;

        // Assert
        ListBox.ToggleSelectionModifierKey.ShouldBe(Microsoft.Xna.Framework.Input.Keys.LeftAlt);
        ListBox.RangeSelectionModifierKey.ShouldBe(Microsoft.Xna.Framework.Input.Keys.LeftControl);

        // Cleanup
        ListBox.ToggleSelectionModifierKey = originalToggle;
        ListBox.RangeSelectionModifierKey = originalRange;
    }

    [Fact]
    public void MultipleMode_Click_ShouldToggleSelection()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        // Act - select first item
        listBox.ListBoxItems[0].IsSelected = true;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0].ShouldBe("Item 0");

        // Act - select second item (both should be selected)
        listBox.ListBoxItems[1].IsSelected = true;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(2);
        listBox.SelectedItems.Cast<object>().ShouldContain("Item 0");
        listBox.SelectedItems.Cast<object>().ShouldContain("Item 1");

        // Act - deselect first item
        listBox.ListBoxItems[0].IsSelected = false;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0].ShouldBe("Item 1");
    }

    [Fact]
    public void MultipleMode_ClickSelectedItem_ShouldDeselectItem()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.AddToRoot();
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        // Select first item
        listBox.ListBoxItems[0].IsSelected = true;
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0].ShouldBe("Item 0");

        // Act - Click the already-selected item (simulate push)
        Mock<ICursor> mockCursor = SetupForPush();
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
    public void SelectedIndex_Getter_ShouldReturnFirstSelectedItemIndex()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

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
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");

        // Assert
        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void SelectedIndex_Setter_ShouldClearSelectedItemsAndSelectOne()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");

        // Act
        listBox.SelectedIndex = 2;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0].ShouldBe("Item 2");
        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void SelectedItems_Add_ShouldSyncIsSelected()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        // Act
        listBox.SelectedItems.Add("Item 1");

        // Assert
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[2].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void SelectedItems_Clear_ShouldDeselectAllItems()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

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
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

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
    public void SelectedObject_Getter_ShouldReturnFirstSelectedItem()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

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
        listBox.Items.Add("Item 0");

        // Assert
        listBox.SelectedObject.ShouldBeNull();
    }

    [Fact]
    public void SelectedObject_Setter_ShouldClearSelectedItemsAndSelectOne()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");

        // Act
        listBox.SelectedObject = "Item 2";

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0].ShouldBe("Item 2");
        listBox.SelectedObject.ShouldBe("Item 2");
    }

    [Fact]
    public void SelectionChanged_ShouldFireOnSelectedItemsAdd()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");

        bool eventFired = false;
        SelectionChangedEventArgs capturedArgs = null;

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
        capturedArgs.AddedItems[0].ShouldBe("Item 0");
        capturedArgs.RemovedItems.Count.ShouldBe(0);
    }

    [Fact]
    public void SelectionChanged_ShouldFireOnSelectedItemsRemove()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");

        listBox.SelectedItems.Add("Item 0");

        bool eventFired = false;
        SelectionChangedEventArgs capturedArgs = null;

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
        capturedArgs.RemovedItems[0].ShouldBe("Item 0");
    }

    [Fact]
    public void SelectionMode_ChangeToSingle_ShouldKeepOnlyFirstSelection()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");
        listBox.SelectedItems.Add("Item 2");

        // Act
        listBox.SelectionMode = SelectionMode.Single;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0].ShouldBe("Item 0");
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
    public void SingleMode_Click_ShouldDeselectOtherItems()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Single };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        // Act
        listBox.ListBoxItems[0].IsSelected = true;
        listBox.ListBoxItems[1].IsSelected = true;

        // Assert
        listBox.SelectedItems.Count.ShouldBe(1);
        listBox.SelectedItems[0].ShouldBe("Item 1");
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void WhenItemRemovedFromItems_ShouldRemoveFromSelectedItems()
    {
        // Arrange
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple };
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");

        // Act
        listBox.Items.Remove("Item 1");

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
        listBox.Items.Add("Item 0");
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        listBox.SelectedItems.Add("Item 0");
        listBox.SelectedItems.Add("Item 1");

        // Act
        listBox.Items.Clear();

        // Assert
        listBox.SelectedItems.Count.ShouldBe(0);
    }

    #endregion

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
}
