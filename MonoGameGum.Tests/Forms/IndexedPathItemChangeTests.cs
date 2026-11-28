using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
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

    [Fact]
    public void NestedIndexedSegments_OuterInsert_OldInnerCollectionNoLongerDrivesTarget()
    {
        PartyViewModel party = new();
        party.Members.Add(InventoryOf("x0", "x1"));
        Label label = new() { BindingContext = party };
        label.SetBinding(nameof(Label.Text), "Members[0].Slots[1].Name");

        InventoryViewModel old = party.Members[0];
        party.Members.Insert(0, InventoryOf("y0", "y1"));
        label.Text.ShouldBe("y1");

        old.Slots.Insert(0, new SlotViewModel { Name = "stale" });
        old.Slots[1].Name = "stale2";
        label.Text.ShouldBe("y1");

        party.Members[0].Slots.RemoveAt(0);
        label.Text.ShouldBeNull();
    }

    [Fact]
    public void NestedIndexedSegments_InnerMove_DisplacedItemNoLongerDrivesTarget()
    {
        PartyViewModel party = new();
        party.Members.Add(InventoryOf("x0", "x1", "x2"));
        Label label = new() { BindingContext = party };
        label.SetBinding(nameof(Label.Text), "Members[0].Slots[1].Name");

        party.Members[0].Slots.Move(2, 1);
        label.Text.ShouldBe("x2");

        party.Members[0].Slots[2].Name = "should-not-show";
        label.Text.ShouldBe("x2");

        party.Members[0].Slots[1].Name = "renamed";
        label.Text.ShouldBe("renamed");
    }

    [Fact]
    public void PlainListSegment_ListReplaced_FollowsNewItem()
    {
        ListInventoryViewModel vm = new()
        {
            Slots = new() { new SlotViewModel { Name = "a" }, new SlotViewModel { Name = "b" } }
        };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Slots[1].Name");

        vm.Slots[1].Name = "b2";
        label.Text.ShouldBe("b2");

        vm.Slots = new() { new SlotViewModel { Name = "z0" }, new SlotViewModel { Name = "z1" } };
        label.Text.ShouldBe("z1");

        vm.Slots[1].Name = "z1b";
        label.Text.ShouldBe("z1b");
    }

    [Fact]
    public void ContextReplaced_OldContextAndCollectionsAreCollected()
    {
        Label label = new();
        label.SetBinding(nameof(Label.Text), "Members[0].Slots[0].Name");

        (WeakReference party, WeakReference members, WeakReference slots) = BindThenReplaceContext(label);
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        party.IsAlive.ShouldBeFalse();
        members.IsAlive.ShouldBeFalse();
        slots.IsAlive.ShouldBeFalse();
        label.Text.ShouldBe("new");
    }

    [Fact]
    public void ClearBinding_LaterCollectionChange_LeavesTarget()
    {
        InventoryViewModel vm = InventoryOf("a");
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Slots[0].Name");

        label.ClearBinding(nameof(Label.Text));
        label.Text = "manual";
        vm.Slots.Insert(0, new SlotViewModel { Name = "x" });
        vm.Slots[0].Name = "y";

        label.Text.ShouldBe("manual");
    }

    [Fact]
    public void ForwardedCollection_RepeatedInserts_FollowsItemWithoutSubscriberGrowth()
    {
        ForwardingInventoryViewModel vm = new() { Slots = new() };
        for (int i = 0; i < 3; i++)
        {
            vm.Slots.Add(new SlotViewModel { Name = "s" + i });
        }
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), "Slots[1].Name");
        int subscribersBefore = CollectionChangedSubscriberCount(vm.Slots);

        for (int i = 0; i < 20; i++)
        {
            vm.Slots.Insert(0, new SlotViewModel { Name = "ins" + i });
        }
        label.Text.ShouldBe("ins18");

        vm.Slots[1].Name = "renamed";
        vm.Slots[2].Name = "not-shown";
        label.Text.ShouldBe("renamed");
        CollectionChangedSubscriberCount(vm.Slots).ShouldBe(subscribersBefore);
    }

    private class ListInventoryViewModel : ViewModel
    {
        public List<SlotViewModel> Slots
        {
            get => Get<List<SlotViewModel>>();
            set => Set(value);
        }
    }

    // Stored through ViewModel.Set, so the view model also forwards the collection's changes as PropertyChanged.
    private class ForwardingInventoryViewModel : ViewModel
    {
        public ObservableCollection<SlotViewModel> Slots
        {
            get => Get<ObservableCollection<SlotViewModel>>();
            set => Set(value);
        }
    }

    private static InventoryViewModel InventoryOf(params string[] names)
    {
        InventoryViewModel vm = new();
        foreach (string name in names)
        {
            vm.Slots.Add(new SlotViewModel { Name = name });
        }
        return vm;
    }

    private static int CollectionChangedSubscriberCount(ObservableCollection<SlotViewModel> collection)
    {
        FieldInfo field = typeof(ObservableCollection<SlotViewModel>).GetField(
            nameof(ObservableCollection<SlotViewModel>.CollectionChanged), BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (field.GetValue(collection) as Delegate)?.GetInvocationList().Length ?? 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference, WeakReference, WeakReference) BindThenReplaceContext(Label label)
    {
        PartyViewModel old = new();
        old.Members.Add(InventoryOf("old"));
        label.BindingContext = old;
        old.Members[0].Slots.Insert(0, new SlotViewModel { Name = "old2" });

        PartyViewModel fresh = new();
        fresh.Members.Add(InventoryOf("new"));
        label.BindingContext = fresh;
        return (new WeakReference(old), new WeakReference(old.Members), new WeakReference(old.Members[0].Slots));
    }
}
