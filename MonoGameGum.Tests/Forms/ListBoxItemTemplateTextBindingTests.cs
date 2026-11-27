using System.Collections.ObjectModel;
using System.Linq;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A ListBox row whose template binds its own TextInstance to the item view model shows the bound
/// value, not the item's ToString().
/// </summary>
public class ListBoxItemTemplateTextBindingTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
    }

    private class ShopViewModel : ViewModel
    {
        public ObservableCollection<ItemViewModel> Items { get => Get<ObservableCollection<ItemViewModel>>(); set => Set(value); }
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
            visual.TextInstance.SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));
            return visual;
        });
        listBox.BindingContext = viewModel;
        listBox.SetBinding(nameof(ListBox.Items), nameof(ShopViewModel.Items));
        return (listBox, viewModel);
    }

    private static string?[] RowTexts(ListBox listBox) =>
        listBox.ListBoxItems.Select(item => ((ListBoxItemVisual)item.Visual).TextInstance.Text).ToArray();

    [Fact]
    public void TemplateBindsTextInstance_InitialItems_RowsShowBoundValue()
    {
        (ListBox listBox, _) = CreateBoundListBox("Sword", "Shield");

        RowTexts(listBox).ShouldBe(new[] { "Sword", "Shield" });
    }

    [Fact]
    public void TemplateBindsTextInstance_ItemAdded_NewRowShowsBoundValue()
    {
        (ListBox listBox, ShopViewModel viewModel) = CreateBoundListBox("Sword");

        viewModel.Items.Add(new ItemViewModel { Name = "Bow" });

        RowTexts(listBox).ShouldBe(new[] { "Sword", "Bow" });
    }
}
