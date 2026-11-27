using System.Collections.ObjectModel;
using System.Linq;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Mvvm;
using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A ListBox whose Items is bound to an ObservableCollection of item view models, with a visual
/// template whose inner text binds to the item view model: collection changes, item property changes,
/// item replacement and selection binding.
/// </summary>
public class ListBoxTemplatedItemBindingTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
    }

    private class ShopViewModel : ViewModel
    {
        public ObservableCollection<ItemViewModel> Items { get => Get<ObservableCollection<ItemViewModel>>(); set => Set(value); }
        public ItemViewModel? SelectedItem { get => Get<ItemViewModel?>(); set => Set(value); }
        public int SelectedIndex { get => Get<int>(); set => Set(value); }
    }

    private static (ListBox listBox, ShopViewModel viewModel) CreateBoundListBox(params string[] names)
    {
        ShopViewModel viewModel = new()
        {
            Items = new ObservableCollection<ItemViewModel>(names.Select(item => new ItemViewModel { Name = item }))
        };
        ListBox listBox = new();
        listBox.VisualTemplate = new VisualTemplate(() =>
        {
            ListBoxItemVisual visual = new();
            // A separate child: ListBoxItem writes the item's ToString() into TextInstance.
            TextRuntime nameText = new() { Name = "NameText" };
            nameText.SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));
            visual.AddChild(nameText);
            return visual;
        });
        listBox.BindingContext = viewModel;
        listBox.SetBinding(nameof(ListBox.Items), nameof(ShopViewModel.Items));
        return (listBox, viewModel);
    }

    private static string?[] RowTexts(ListBox listBox) =>
        listBox.ListBoxItems.Select(item => ((TextRuntime)item.Visual.GetGraphicalUiElementByName("NameText")!).Text).ToArray();


    [Fact]
    public void InitialItems_RowsShowItemViewModelValues()
    {
        (ListBox listBox, _) = CreateBoundListBox("Sword", "Shield");

        RowTexts(listBox).ShouldBe(new[] { "Sword", "Shield" });
    }

    [Fact]
    public void ItemPropertyChanges_RowFollows()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");

        viewModel.Items[1].Name = "Tower Shield";

        RowTexts(listBox).ShouldBe(new[] { "Sword", "Tower Shield" });
    }

    [Fact]
    public void ItemAdded_NewRowShowsItsValue()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword");

        viewModel.Items.Add(new ItemViewModel { Name = "Bow" });

        RowTexts(listBox).ShouldBe(new[] { "Sword", "Bow" });
    }

    [Fact]
    public void ItemInserted_RowsShowValuesInOrder()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Bow");

        viewModel.Items.Insert(1, new ItemViewModel { Name = "Shield" });

        RowTexts(listBox).ShouldBe(new[] { "Sword", "Shield", "Bow" });
    }

    [Fact]
    public void ItemRemoved_RemainingRowsShowValues()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield", "Bow");

        viewModel.Items.RemoveAt(1);

        RowTexts(listBox).ShouldBe(new[] { "Sword", "Bow" });
    }

    [Fact]
    public void ItemReplaced_RowShowsNewItemAndIgnoresOldItem()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");
        ItemViewModel old = viewModel.Items[0];

        viewModel.Items[0] = new ItemViewModel { Name = "Axe" };
        old.Name = "Stale";

        RowTexts(listBox).ShouldBe(new[] { "Axe", "Shield" });
    }

    [Fact]
    public void ItemReplaced_NewItemPropertyChanges_RowFollows()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");

        ItemViewModel replacement = new() { Name = "Axe" };
        viewModel.Items[0] = replacement;
        replacement.Name = "Great Axe";

        RowTexts(listBox).ShouldBe(new[] { "Great Axe", "Shield" });
    }

    [Fact]
    public void ItemMoved_RowsShowValuesInNewOrder()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield", "Bow");

        viewModel.Items.Move(0, 2);

        RowTexts(listBox).ShouldBe(new[] { "Shield", "Bow", "Sword" });
    }

    [Fact]
    public void ItemsCleared_ThenAdded_RowsShowNewValues()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");

        viewModel.Items.Clear();
        viewModel.Items.Add(new ItemViewModel { Name = "Bow" });

        RowTexts(listBox).ShouldBe(new[] { "Bow" });
    }

    [Fact]
    public void ItemsCollectionReplaced_RowsShowNewCollection()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");

        viewModel.Items = new ObservableCollection<ItemViewModel> { new() { Name = "Potion" } };

        RowTexts(listBox).ShouldBe(new[] { "Potion" });
    }

    [Fact]
    public void ItemsCollectionReplaced_OldCollectionChanges_AreIgnored()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");
        ObservableCollection<ItemViewModel> old = viewModel.Items;

        viewModel.Items = new ObservableCollection<ItemViewModel> { new() { Name = "Potion" } };
        old.Add(new ItemViewModel { Name = "Stale" });

        RowTexts(listBox).ShouldBe(new[] { "Potion" });
    }

    [Fact]
    public void RemovedItem_PropertyChanges_DoNotThrow()
    {
        (_, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");
        ItemViewModel removed = viewModel.Items[0];

        viewModel.Items.RemoveAt(0);

        Should.NotThrow(() => removed.Name = "Gone");
    }

    [Fact]
    public void SelectedItemBinding_ViewModelSetsSelection_ListBoxSelects()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");
        listBox.SetBinding(nameof(ListBox.SelectedObject), nameof(ShopViewModel.SelectedItem));

        viewModel.SelectedItem = viewModel.Items[1];

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void SelectedItemBinding_ListBoxSelects_ViewModelFollows()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");
        listBox.SetBinding(nameof(ListBox.SelectedObject), nameof(ShopViewModel.SelectedItem));

        listBox.SelectedIndex = 1;

        viewModel.SelectedItem.ShouldBe(viewModel.Items[1]);
    }

    [Fact]
    public void SelectedItemBinding_SelectedItemRemovedFromCollection_ViewModelSelectionCleared()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");
        listBox.SetBinding(nameof(ListBox.SelectedObject), nameof(ShopViewModel.SelectedItem));
        listBox.SelectedIndex = 1;

        viewModel.Items.RemoveAt(1);

        viewModel.SelectedItem.ShouldBeNull();
    }

    [Fact]
    public void SelectedIndexBinding_ListBoxSelects_ViewModelFollows()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword", "Shield");
        viewModel.SelectedIndex = -1;
        listBox.SetBinding(nameof(ListBox.SelectedIndex), nameof(ShopViewModel.SelectedIndex));

        listBox.SelectedIndex = 1;

        viewModel.SelectedIndex.ShouldBe(1);
    }
}
