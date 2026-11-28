using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// What a Forms binding does after it was set up: clearing or replacing it, swapping the BindingContext,
/// and the control leaving its parent. A binding that is gone, or a control that left, must neither follow
/// the view model nor be kept alive by it.
/// </summary>
public class BindingLifecycleTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
    }

    private class ListViewModel : ViewModel
    {
        public ObservableCollection<ItemViewModel> Items { get => Get<ObservableCollection<ItemViewModel>>(); set => Set(value); }
        public ItemViewModel? Selected { get => Get<ItemViewModel?>(); set => Set(value); }
    }

    [Fact]
    public void ClearBinding_LaterViewModelChange_DoesNotReachTarget()
    {
        TestViewModel vm = new() { Text = "first" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        textBox.ClearBinding(nameof(TextBox.Text));
        textBox.Text = "typed";
        vm.Text = "second";

        textBox.Text.ShouldBe("typed");
    }

    [Fact]
    public void ClearBinding_LaterTargetEdit_DoesNotReachSource()
    {
        TestViewModel vm = new() { Text = "first" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        textBox.ClearBinding(nameof(TextBox.Text));
        textBox.Text = "typed";

        vm.Text.ShouldBe("first");
    }

    [Fact]
    public void SetBinding_SamePropertyRebound_OnlyTheNewPathFollowsAndReceivesEdits()
    {
        TestViewModel vm = new() { Text = "old path", AuxVm = new AuxTestViewModel { Text = "new path" } };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        textBox.SetBinding(nameof(TextBox.Text), "AuxVm.Text");
        textBox.Text.ShouldBe("new path");

        vm.Text = "old path changed";
        textBox.Text.ShouldBe("new path");

        textBox.Text = "typed";
        vm.AuxVm!.Text.ShouldBe("typed");
        vm.Text.ShouldBe("old path changed");
    }

    [Fact]
    public void BindingContextSwap_AToBToNull_OnlyTheCurrentContextReachesTarget()
    {
        TestViewModel a = new() { Text = "a" };
        TestViewModel b = new() { Text = "b" };
        TextBox textBox = new() { BindingContext = a };
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        textBox.BindingContext = b;
        textBox.Text.ShouldBe("b");
        a.Text = "a changed";
        textBox.Text.ShouldBe("b");

        textBox.BindingContext = null;
        b.Text = "b changed";
        a.Text = "a changed again";
        textBox.Text.ShouldBe("b");
    }

    [Fact]
    public void BindingContextSwap_AToB_TargetEditWritesOnlyB()
    {
        TestViewModel a = new() { Text = "a" };
        TestViewModel b = new() { Text = "b" };
        TextBox textBox = new() { BindingContext = a };
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        textBox.BindingContext = b;
        textBox.Text = "typed";

        b.Text.ShouldBe("typed");
        a.Text.ShouldBe("a");
    }

    [Fact]
    public void BindingContextSwapToNull_TargetEdit_WritesNoViewModel()
    {
        TestViewModel a = new() { Text = "a" };
        TextBox textBox = new() { BindingContext = a };
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        textBox.BindingContext = null;
        textBox.Text = "typed";

        a.Text.ShouldBe("a");
    }

    [Fact]
    public void RemovedFromNonRootParent_InheritedContext_StopsFollowingViewModel()
    {
        TestViewModel vm = new() { Text = "first" };
        StackPanel panel = new() { BindingContext = vm };
        TextBox textBox = new();
        panel.AddChild(textBox);
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        panel.Visual.Children.Remove(textBox.Visual);
        vm.Text = "second";

        textBox.BindingContext.ShouldBeNull();
        textBox.Text.ShouldBe("first");
    }

    [Fact]
    public void RemovedFromNonRootParent_InheritedContext_TargetEditDoesNotReachViewModel()
    {
        TestViewModel vm = new() { Text = "first" };
        StackPanel panel = new() { BindingContext = vm };
        TextBox textBox = new();
        panel.AddChild(textBox);
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        panel.Visual.Children.Remove(textBox.Visual);
        textBox.Text = "typed";

        vm.Text.ShouldBe("first");
    }

    [Fact]
    public void BoundTextBox_DroppedWithoutRemoval_IsCollectedWhileViewModelLives()
    {
        TestViewModel vm = new() { Text = "first", AuxVm = new AuxTestViewModel { Text = "nested" } };

        WeakReference control = BindTextBoxAndDrop(vm);
        CollectGarbage();

        control.IsAlive.ShouldBeFalse("because the view model must not keep a bound control alive");
        GC.KeepAlive(vm);
    }

    [Fact]
    public void BoundListBox_RemovedFromRoot_IsCollectedWhileViewModelLives()
    {
        ListViewModel vm = new() { Items = new() { new ItemViewModel { Name = "one" } } };

        WeakReference control = BindListBoxInRootRemoveAndDrop(vm);
        CollectGarbage();

        control.IsAlive.ShouldBeFalse("because the view model's collection must not keep a removed ListBox alive");
        GC.KeepAlive(vm);
    }

    [Fact]
    public void BoundListBox_RemovedFromRoot_LaterCollectionAddDoesNotReachIt()
    {
        ListViewModel vm = new() { Items = new() { new ItemViewModel { Name = "one" } } };
        ListBox listBox = new();
        listBox.SetBinding(nameof(ListBox.Items), nameof(ListViewModel.Items));
        listBox.AddToRoot();
        listBox.BindingContext = vm;
        listBox.ListBoxItems.Count.ShouldBe(1);

        listBox.RemoveFromRoot();
        vm.Items.Add(new ItemViewModel { Name = "two" });

        listBox.ListBoxItems.Count.ShouldBe(1, "because a control that left the root stops following its view model");
    }

    [Fact]
    public void ListBoxRow_ItemRemovedFromBoundCollection_RowIsCollectedWhileItemViewModelLives()
    {
        ItemViewModel kept = new() { Name = "kept" };
        ListViewModel vm = new() { Items = new() { kept } };
        ListBox listBox = new() { BindingContext = vm };
        listBox.SetBinding(nameof(ListBox.Items), nameof(ListViewModel.Items));

        WeakReference row = TakeRowWeakly(listBox);
        vm.Items.Remove(kept);
        CollectGarbage();

        row.IsAlive.ShouldBeFalse("because the item view model must not keep its discarded row alive");
        GC.KeepAlive(kept);
        GC.KeepAlive(listBox);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference BindTextBoxAndDrop(TestViewModel vm)
    {
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));
        TextBox nested = new() { BindingContext = vm };
        nested.SetBinding(nameof(TextBox.Text), "AuxVm.Text");
        return new WeakReference(textBox);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference BindListBoxInRootRemoveAndDrop(ListViewModel vm)
    {
        ListBox listBox = new();
        listBox.SetBinding(nameof(ListBox.Items), nameof(ListViewModel.Items));
        listBox.SetBinding(nameof(ListBox.SelectedObject), nameof(ListViewModel.Selected));
        listBox.AddToRoot();
        listBox.BindingContext = vm;
        listBox.RemoveFromRoot();
        return new WeakReference(listBox);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference TakeRowWeakly(ListBox listBox) => new(listBox.ListBoxItems[0]);

    private static void CollectGarbage()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
