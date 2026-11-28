using System.Collections.ObjectModel;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// After a collection change moves a different item under an indexed path segment (Slots[1].Name), the binding
/// follows that item's PropertyChanged, not the item that used to be there.
/// </summary>
public class IndexedPathItemChangeTests : BaseTestClass
{
    private class SlotViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    private class InventoryViewModel : ViewModel
    {
        public ObservableCollection<SlotViewModel> Slots { get; } = new();
    }

    private static InventoryViewModel CreateInventory()
    {
        InventoryViewModel vm = new();
        for (int i = 0; i < 3; i++)
        {
            vm.Slots.Add(new SlotViewModel { Name = "slot" + i });
        }
        return vm;
    }

    [Fact]
    public void ItemShiftedIntoIndex_PropertyChange_Updates()
    {
        InventoryViewModel vm = CreateInventory();
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Slots[1].Name");

        vm.Slots.RemoveAt(0);
        vm.Slots[1].Name = "renamed";

        label.Text.ShouldBe("renamed");
    }

    [Fact]
    public void ItemReplacedAtIndex_NewItemPropertyChange_Updates()
    {
        InventoryViewModel vm = CreateInventory();
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Slots[0].Name");

        SlotViewModel replacement = new() { Name = "shield" };
        vm.Slots[0] = replacement;
        replacement.Name = "broken shield";

        label.Text.ShouldBe("broken shield");
    }

    [Fact]
    public void ItemAddedAtEmptyIndex_LaterPropertyChange_Updates()
    {
        InventoryViewModel vm = new();
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Slots[0].Name");

        SlotViewModel added = new() { Name = "potion" };
        vm.Slots.Add(added);
        added.Name = "empty flask";

        label.Text.ShouldBe("empty flask");
    }

    [Fact]
    public void Visual_ItemReplacedAtIndex_NewItemPropertyChange_Updates()
    {
        InventoryViewModel vm = CreateInventory();
        TextRuntime text = new() { BindingContext = vm };
        text.SetBinding(nameof(TextRuntime.Text), "Slots[0].Name");

        SlotViewModel replacement = new() { Name = "shield" };
        vm.Slots[0] = replacement;
        replacement.Name = "broken shield";

        text.Text.ShouldBe("broken shield");
    }
}
