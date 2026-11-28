using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    // Hand-written INotifyPropertyChanged: ViewModel.Set would forward the collection's own changes as
    // PropertyChanged("Slots"), hiding whether the path observer follows the replaced collection itself.
    private class SwappableInventoryViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SlotViewModel>? _slots;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<SlotViewModel>? Slots
        {
            get => _slots;
            set
            {
                _slots = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Slots)));
            }
        }
    }

    private class PartyViewModel : ViewModel
    {
        public ObservableCollection<InventoryViewModel> Members { get; } = new();
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
    public void ItemPropertyChanged_ThenCollectionChange_Updates()
    {
        InventoryViewModel vm = CreateInventory();
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Slots[1].Name");

        vm.Slots[1].Name = "renamed";
        vm.Slots.RemoveAt(0);

        label.Text.ShouldBe("slot2");
    }

    [Fact]
    public void SecondCollectionChange_Updates()
    {
        InventoryViewModel vm = CreateInventory();
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Slots[1].Name");

        vm.Slots.Insert(0, new SlotViewModel { Name = "first insert" });
        label.Text.ShouldBe("slot0");

        vm.Slots.Insert(0, new SlotViewModel { Name = "second insert" });
        label.Text.ShouldBe("first insert");
    }

    [Fact]
    public void CollectionInstanceReplaced_ThenCollectionChange_Updates()
    {
        SwappableInventoryViewModel vm = new()
        {
            Slots = new() { new SlotViewModel { Name = "a" }, new SlotViewModel { Name = "b" } }
        };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Slots[1].Name");

        ObservableCollection<SlotViewModel> replacement = new()
        {
            new SlotViewModel { Name = "c" }, new SlotViewModel { Name = "d" }
        };
        vm.Slots = replacement;
        label.Text.ShouldBe("d");

        replacement.Insert(0, new SlotViewModel { Name = "inserted" });
        label.Text.ShouldBe("c");

        replacement[1].Name = "renamed";
        label.Text.ShouldBe("renamed");
    }

    [Fact]
    public void NestedIndexedSegments_OuterReplaceThenInnerMove_FollowsNewItem()
    {
        PartyViewModel party = new();
        party.Members.Add(CreateInventory());
        TextRuntime text = new() { BindingContext = party };
        text.SetBinding(nameof(TextRuntime.Text), "Members[0].Slots[1].Name");

        InventoryViewModel other = CreateInventory();
        party.Members[0] = other;
        other.Slots.Move(2, 1);
        text.Text.ShouldBe("slot2");

        other.Slots[1].Name = "moved";
        text.Text.ShouldBe("moved");
    }

    [Fact]
    public void ItemRemovedFromIndex_IsCollectedWhileBindingLives()
    {
        InventoryViewModel vm = CreateInventory();
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Slots[1].Name");

        WeakReference removed = RemoveSlotWeakly(vm, 1);
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        removed.IsAlive.ShouldBeFalse("because the binding must not keep an item it no longer points at alive");
        label.Text.ShouldBe("slot2");
        GC.KeepAlive(vm);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RemoveSlotWeakly(InventoryViewModel vm, int index)
    {
        WeakReference reference = new(vm.Slots[index]);
        vm.Slots.RemoveAt(index);
        return reference;
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
