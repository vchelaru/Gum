using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A ListBox or ComboBox whose Items is bound to a view-model collection: each collection change shows up
/// as the matching rows, each row's BindingContext is its item, and a bound SelectedObject follows
/// removal of the selected item.
/// </summary>
public class BoundItemsCollectionTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
        public override string ToString() => Name ?? string.Empty;
    }

    private class ListViewModel : ViewModel
    {
        public ObservableCollection<ItemViewModel> Items { get => Get<ObservableCollection<ItemViewModel>>(); set => Set(value); }
        public List<ItemViewModel>? PlainItems { get => Get<List<ItemViewModel>?>(); set => Set(value); }
        public ItemViewModel? Selected { get => Get<ItemViewModel?>(); set => Set(value); }
    }

    private static ItemViewModel Item(string name) => new() { Name = name };

    private static List<object?> RowContexts(ListBox listBox) =>
        listBox.ListBoxItems.Select(row => row.BindingContext).ToList();

    private static (ListViewModel vm, ListBox listBox) CreateBoundListBox(params string[] names)
    {
        ListViewModel vm = new() { Items = new(names.Select(Item)) };
        ListBox listBox = new() { BindingContext = vm };
        listBox.SetBinding(nameof(ListBox.Items), nameof(ListViewModel.Items));
        listBox.SetBinding(nameof(ListBox.SelectedObject), nameof(ListViewModel.Selected));
        return (vm, listBox);
    }

    [Fact]
    public void ItemsBoundToPlainList_ShowsItsItems()
    {
        ListViewModel vm = new() { PlainItems = new() { Item("a"), Item("b") } };
        ListBox listBox = new() { BindingContext = vm };

        listBox.SetBinding(nameof(ListBox.Items), nameof(ListViewModel.PlainItems));

        RowContexts(listBox).ShouldBe(vm.PlainItems.Cast<object?>());
    }

    [Theory]
    [InlineData("Add")]
    [InlineData("Insert")]
    [InlineData("Remove")]
    [InlineData("Move")]
    [InlineData("Replace")]
    [InlineData("Clear")]
    public void ObservableCollectionChange_RowsMatchItemsInOrderWithEachItemAsContext(string change)
    {
        (ListViewModel vm, ListBox listBox) = CreateBoundListBox("a", "b", "c");

        switch (change)
        {
            case "Add": vm.Items.Add(Item("d")); break;
            case "Insert": vm.Items.Insert(1, Item("x")); break;
            case "Remove": vm.Items.RemoveAt(1); break;
            case "Move": vm.Items.Move(0, 2); break;
            case "Replace": vm.Items[1] = Item("y"); break;
            case "Clear": vm.Items.Clear(); break;
        }

        RowContexts(listBox).ShouldBe(vm.Items.Cast<object?>());
    }

    [Fact]
    public void CollectionPropertyReplaced_ShowsNewCollectionAndStopsFollowingOldOne()
    {
        (ListViewModel vm, ListBox listBox) = CreateBoundListBox("a", "b");
        ObservableCollection<ItemViewModel> old = vm.Items;

        vm.Items = new() { Item("new") };
        RowContexts(listBox).ShouldBe(vm.Items.Cast<object?>());

        old.Add(Item("stale"));
        RowContexts(listBox).ShouldBe(vm.Items.Cast<object?>());

        vm.Items.Add(Item("new 2"));
        RowContexts(listBox).ShouldBe(vm.Items.Cast<object?>());
    }

    [Fact]
    public void SelectedObjectBound_SelectedItemRemoved_ViewModelSelectionBecomesNull()
    {
        (ListViewModel vm, ListBox listBox) = CreateBoundListBox("a", "b", "c");
        ItemViewModel selected = vm.Items[1];
        vm.Selected = selected;
        listBox.SelectedObject.ShouldBe(selected);

        vm.Items.Remove(selected);

        listBox.SelectedObject.ShouldBeNull();
        vm.Selected.ShouldBeNull();
    }

    [Fact]
    public void SelectedObjectBound_CollectionCleared_ViewModelSelectionBecomesNull()
    {
        (ListViewModel vm, ListBox listBox) = CreateBoundListBox("a", "b");
        vm.Selected = vm.Items[0];

        vm.Items.Clear();

        vm.Selected.ShouldBeNull();
    }

    [Fact]
    public void SelectedObjectBound_SelectedItemMoved_SelectionFollowsItem()
    {
        (ListViewModel vm, ListBox listBox) = CreateBoundListBox("a", "b", "c");
        ItemViewModel selected = vm.Items[0];
        vm.Selected = selected;

        vm.Items.Move(0, 2);

        vm.Selected.ShouldBe(selected);
        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void SelectedObjectBound_RowSelectedInControl_ReachesViewModel()
    {
        (ListViewModel vm, ListBox listBox) = CreateBoundListBox("a", "b", "c");

        listBox.SelectedIndex = 2;

        vm.Selected.ShouldBe(vm.Items[2]);
    }

    [Fact]
    public void ComboBoxItemsBound_CollectionChanges_ItemsMatch()
    {
        ListViewModel vm = new() { Items = new() { Item("a") } };
        ComboBox comboBox = new() { BindingContext = vm };
        comboBox.SetBinding(nameof(ComboBox.Items), nameof(ListViewModel.Items));

        vm.Items.Add(Item("b"));
        vm.Items.RemoveAt(0);

        comboBox.Items.Cast<object>().ShouldBe(vm.Items);
    }

    [Fact]
    public void ComboBoxSelectedObjectBound_SelectedItemRemoved_ViewModelSelectionBecomesNull()
    {
        ListViewModel vm = new() { Items = new() { Item("a"), Item("b") } };
        ComboBox comboBox = new() { BindingContext = vm };
        comboBox.SetBinding(nameof(ComboBox.Items), nameof(ListViewModel.Items));
        comboBox.SetBinding(nameof(ComboBox.SelectedObject), nameof(ListViewModel.Selected));
        ItemViewModel selected = vm.Items[1];
        vm.Selected = selected;

        vm.Items.Remove(selected);

        comboBox.SelectedObject.ShouldBeNull();
        vm.Selected.ShouldBeNull();
    }

    [Fact]
    public void ItemMoved_RowAtNewIndexHasTheItemAsContext()
    {
        (ListViewModel vm, ListBox listBox) = CreateBoundListBox("a", "b", "c");
        ItemViewModel moved = vm.Items[0];

        vm.Items.Move(0, 2);

        listBox.ListBoxItems[2].BindingContext.ShouldBe(moved);
    }
}
