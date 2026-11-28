using System.Collections.ObjectModel;
using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A lambda binding's indexer follows a collection change at that index: a replaced item, an earlier
/// item removed, or the item that moved away changing later.
/// </summary>
public class LambdaBindingIndexerTests : BaseTestClass
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
    public void TypedLambda_LiteralIndex_Binds()
    {
        InventoryViewModel vm = CreateInventory();
        Label label = new() { BindingContext = vm };

        label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Slots[2].Name);

        label.Text.ShouldBe("slot2");
    }

    [Fact]
    public void TypedLambda_LiteralIndex_SlotReplaced_Updates()
    {
        InventoryViewModel vm = CreateInventory();
        Label label = new() { BindingContext = vm };
        label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Slots[0].Name);

        vm.Slots[0] = new SlotViewModel { Name = "sword" };

        label.Text.ShouldBe("sword");
    }

    [Fact]
    public void TypedLambda_LiteralIndex_EarlierSlotRemoved_ShowsShiftedItem()
    {
        InventoryViewModel vm = CreateInventory();
        Label label = new() { BindingContext = vm };
        label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Slots[1].Name);

        vm.Slots.RemoveAt(0);

        label.Text.ShouldBe("slot2");
    }

    [Fact]
    public void TypedLambda_LiteralIndex_ItemShiftedOut_StaleItemChange_ShowsCurrentItem()
    {
        InventoryViewModel vm = CreateInventory();
        Label label = new() { BindingContext = vm };
        label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Slots[1].Name);
        SlotViewModel original = vm.Slots[1];

        vm.Slots.RemoveAt(0);
        original.Name = "stale";

        label.Text.ShouldBe("slot2");
    }
}
