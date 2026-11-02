using System.Collections.Generic;
using System.Linq;
using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// ListBox and ComboBox filled with strings the way the tutorial does, then edited while a
/// selection exists: remove, clear, reselect, select out of range, remove inside the handler.
/// </summary>
public class ListBoxComboBoxFirstHourTests : BeginnerDriverTestBase
{
    private static ListBox CreateListBox(int count)
    {
        ListBox listBox = new();
        for (int i = 0; i < count; i++)
        {
            listBox.Items.Add($"Item {i}");
        }
        return listBox;
    }

    [Fact]
    public void SelectedObject_ShouldBeClickedItem_WhenItemIsClicked()
    {
        ListBox listBox = CreateListBox(5);
        listBox.Visual.Width = 150;
        listBox.Visual.Height = 300;
        AddToRoot(listBox, "Lb");
        Driver.Step();
        object? selected = null;
        listBox.SelectionChanged += (_, _) => selected = listBox.SelectedObject;
        Gum.Wireframe.GraphicalUiElement row = listBox.ListBoxItems[2].Visual;

        Driver.Click((int)(row.AbsoluteLeft + row.AbsoluteWidth / 2), (int)(row.AbsoluteTop + row.AbsoluteHeight / 2));

        selected.ShouldBe("Item 2");
        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void SelectedIndex_ShouldBeMinusOne_WhenSelectedItemIsRemoved()
    {
        ListBox listBox = CreateListBox(3);
        listBox.SelectedIndex = 1;

        listBox.Items.RemoveAt(1);

        listBox.SelectedIndex.ShouldBe(-1);
        listBox.SelectedObject.ShouldBeNull();
        listBox.ListBoxItems.Count.ShouldBe(2);
    }

    [Fact]
    public void SelectionChanged_ShouldReportRemovedItem_WhenSelectedItemIsRemoved()
    {
        ListBox listBox = CreateListBox(3);
        listBox.SelectedIndex = 1;
        List<object> removed = new();
        listBox.SelectionChanged += (_, args) => removed.AddRange(args.RemovedItems.Cast<object>());

        listBox.Items.RemoveAt(1);

        removed.ShouldBe(new object[] { "Item 1" });
    }

    [Fact]
    public void Items_Clear_ShouldEmptyRowsAndSelection_WhenItemIsSelected()
    {
        ListBox listBox = CreateListBox(4);
        listBox.SelectedIndex = 2;

        listBox.Items.Clear();

        listBox.ListBoxItems.Count.ShouldBe(0);
        listBox.SelectedIndex.ShouldBe(-1);
        listBox.SelectedObject.ShouldBeNull();
    }

    [Fact]
    public void SelectedIndex_ShouldClearSelection_WhenSetToMinusOne()
    {
        ListBox listBox = CreateListBox(3);
        listBox.SelectedIndex = 1;

        listBox.SelectedIndex = -1;

        listBox.SelectedObject.ShouldBeNull();
        listBox.ListBoxItems.Any(i => i.IsSelected).ShouldBeFalse();
    }

    [Fact]
    public void SelectedIndex_ShouldNotThrow_WhenSetPastTheLastItem()
    {
        ListBox listBox = CreateListBox(3);

        Should.NotThrow(() => listBox.SelectedIndex = 10);

        listBox.SelectedObject.ShouldBeNull();
    }

    [Fact]
    public void Items_Add_ShouldCreateTwoRows_WhenSameStringIsAddedTwice()
    {
        ListBox listBox = new();

        listBox.Items.Add("Sword");
        listBox.Items.Add("Sword");

        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.SelectedIndex = 1;
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SelectedObject_ShouldStayOnSameItem_WhenItemInsertedBeforeIt()
    {
        ListBox listBox = CreateListBox(3);
        listBox.SelectedIndex = 1;

        listBox.Items.Insert(0, "New");

        listBox.SelectedObject.ShouldBe("Item 1");
        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void Items_Remove_ShouldNotThrow_WhenCalledInsideSelectionChanged()
    {
        ListBox listBox = CreateListBox(5);
        listBox.SelectionChanged += (_, _) =>
        {
            if (listBox.SelectedObject is string s)
            {
                listBox.Items.Remove(s);
            }
        };

        Should.NotThrow(() => listBox.SelectedIndex = 2);

        listBox.ListBoxItems.Count.ShouldBe(4);
        listBox.Items.Count.ShouldBe(4);
    }

    [Fact]
    public void ListBoxItems_ShouldMatchItems_WhenItemsAreAddedAndRemovedRepeatedly()
    {
        ListBox listBox = CreateListBox(3);

        for (int i = 0; i < 20; i++)
        {
            listBox.Items.Add($"Extra {i}");
            listBox.Items.RemoveAt(0);
        }

        listBox.ListBoxItems.Count.ShouldBe(listBox.Items.Count);
        listBox.Items.Count.ShouldBe(3);
    }

    [Fact]
    public void ComboBox_Text_ShouldShowSelectedItem_WhenSelectedIndexIsSet()
    {
        ComboBox combo = new();
        combo.Items.Add("Low");
        combo.Items.Add("High");

        combo.SelectedIndex = 1;

        combo.SelectedObject.ShouldBe("High");
        combo.Text.ShouldBe("High");
    }

    [Fact]
    public void ComboBox_Text_ShouldBeEmpty_WhenItemsAreCleared()
    {
        ComboBox combo = new();
        combo.Items.Add("Low");
        combo.SelectedIndex = 0;

        combo.Items.Clear();

        combo.SelectedIndex.ShouldBe(-1);
        combo.Text.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void ComboBox_Click_ShouldNotThrow_WhenThereAreNoItems()
    {
        ComboBox combo = AddToRoot(new ComboBox(), "Combo");

        Should.NotThrow(() => Driver.Click("Combo"));
        Should.NotThrow(() => Driver.Click("Combo"));
    }

    [Fact]
    public void ComboBox_SelectionChanged_ShouldRaiseOnce_WhenItemChosenFromDropDown()
    {
        ComboBox combo = new() { Name = "Combo" };
        for (int i = 0; i < 5; i++)
        {
            combo.Items.Add($"Opt {i}");
        }
        AddToRoot(combo, "Combo");
        int changes = 0;
        combo.SelectionChanged += (_, _) => changes++;

        Driver.Click("Combo");
        Gum.Wireframe.GraphicalUiElement row = combo.ListBox.ListBoxItems[3].Visual;
        Driver.Click((int)(row.AbsoluteLeft + row.AbsoluteWidth / 2), (int)(row.AbsoluteTop + row.AbsoluteHeight / 2));

        changes.ShouldBe(1);
        combo.SelectedObject.ShouldBe("Opt 3");
    }

    [Fact]
    public void ComboBox_Items_ShouldAllowRemovingTheSelectedItem_AfterSelection()
    {
        ComboBox combo = new();
        combo.Items.Add("A");
        combo.Items.Add("B");
        combo.SelectedIndex = 0;

        Should.NotThrow(() => combo.Items.Remove("A"));

        combo.SelectedObject.ShouldBeNull();
    }
}
