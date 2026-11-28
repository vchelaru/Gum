using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A view model's collection shown by an items control (ItemsControl, ListBox, ComboBox, Menu) must not
/// keep the control alive, and a control whose bound Items lost its BindingContext (for example by
/// leaving the root) stops following that collection until it is given a context again.
/// </summary>
public class ItemsCollectionLifetimeTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
        public override string ToString() => Name ?? string.Empty;
    }

    private class ListViewModel : ViewModel
    {
        public ObservableCollection<ItemViewModel> Items { get => Get<ObservableCollection<ItemViewModel>>(); set => Set(value); }
        public ItemViewModel? Selected { get => Get<ItemViewModel?>(); set => Set(value); }
    }

    public override void Dispose()
    {
        ComboBox.FromFileDropDownTemplate = null;
        base.Dispose();
    }

    [Theory]
    [InlineData(typeof(ListBox))]
    [InlineData(typeof(ComboBox))]
    [InlineData(typeof(ItemsControl))]
    [InlineData(typeof(Menu))]
    public void BoundItems_RemovedFromRoot_IsCollectedWhileViewModelLives(Type controlType)
    {
        ListViewModel vm = new() { Items = new() { new ItemViewModel { Name = "one" } } };

        WeakReference control = BindInRootRemoveAndDrop(controlType, vm);
        CollectGarbage();

        control.IsAlive.ShouldBeFalse("because the view model's collection must not keep a removed control alive");
        GC.KeepAlive(vm);
    }

    [Theory]
    [InlineData(typeof(ListBox))]
    [InlineData(typeof(ComboBox))]
    [InlineData(typeof(ItemsControl))]
    [InlineData(typeof(Menu))]
    public void BoundItems_RemovedFromRoot_ReleasesTheViewModelCollection(Type controlType)
    {
        ListViewModel vm = new() { Items = new() { new ItemViewModel { Name = "one" } } };
        FrameworkElement control = CreateBoundInRoot(controlType, vm);
        GetItems(control).ShouldBeSameAs(vm.Items);

        control.RemoveFromRoot();
        vm.Items.Add(new ItemViewModel { Name = "two" });

        GetItems(control).ShouldNotBeSameAs(vm.Items);
        GetItems(control).Count.ShouldBe(0);
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

        listBox.ListBoxItems.Count.ShouldBe(0, "because a control that left the root stops following its view model");
    }

    [Fact]
    public void BoundListBox_ReAddedWithSameViewModel_ShowsAndFollowsTheCollectionAgain()
    {
        ListViewModel vm = new() { Items = new() { new ItemViewModel { Name = "one" } } };
        ListBox listBox = new();
        listBox.SetBinding(nameof(ListBox.Items), nameof(ListViewModel.Items));
        listBox.AddToRoot();
        listBox.BindingContext = vm;
        listBox.RemoveFromRoot();
        vm.Items.Add(new ItemViewModel { Name = "added while removed" });

        listBox.AddToRoot();
        listBox.BindingContext = vm;
        listBox.ListBoxItems.Count.ShouldBe(2);
        vm.Items.Add(new ItemViewModel { Name = "added after re-add" });

        listBox.Items.ShouldBeSameAs(vm.Items);
        listBox.ListBoxItems.Count.ShouldBe(3);
    }

    [Fact]
    public void BoundComboBox_ReAddedWithSameViewModel_FollowsTheCollectionAgain()
    {
        ListViewModel vm = new() { Items = new() { new ItemViewModel { Name = "one" } } };
        ComboBox comboBox = new();
        comboBox.SetBinding(nameof(ComboBox.Items), nameof(ListViewModel.Items));
        comboBox.AddToRoot();
        comboBox.BindingContext = vm;
        comboBox.RemoveFromRoot();

        comboBox.AddToRoot();
        comboBox.BindingContext = vm;
        vm.Items.Add(new ItemViewModel { Name = "two" });
        comboBox.SelectedIndex = 1;

        comboBox.Items.ShouldBeSameAs(vm.Items);
        comboBox.SelectedObject.ShouldBe(vm.Items[1]);
    }

    [Theory]
    [InlineData(typeof(ListBox))]
    [InlineData(typeof(ComboBox))]
    [InlineData(typeof(ItemsControl))]
    [InlineData(typeof(Menu))]
    public void ItemsAssignedInCode_ControlDropped_IsCollectedWhileCollectionLives(Type controlType)
    {
        ObservableCollection<ItemViewModel> longLived = new() { new ItemViewModel { Name = "one" } };

        WeakReference control = AssignItemsAndDrop(controlType, longLived);
        CollectGarbage();

        control.IsAlive.ShouldBeFalse("because a collection the control only shows must not keep it alive");
        longLived.Add(new ItemViewModel { Name = "after collection" });
        GC.KeepAlive(longLived);
    }

    [Fact]
    public void ItemsAssignedInCode_ComboBoxDropDownNotBuiltYet_IsCollectedWhileCollectionLives()
    {
        ComboBox.FromFileDropDownTemplate = new VisualTemplate((_, _) => new ListBoxVisual(tryCreateFormsObject: false));
        ObservableCollection<ItemViewModel> longLived = new() { new ItemViewModel { Name = "one" } };

        WeakReference control = AssignItemsToLazyComboBoxAndDrop(longLived);
        CollectGarbage();

        control.IsAlive.ShouldBeFalse("because a collection the drop-down will show must not keep the combo box alive");
        GC.KeepAlive(longLived);
    }

    [Fact]
    public void ItemsAssignedInCode_NoBindingContext_FollowsTheCollection()
    {
        ObservableCollection<ItemViewModel> items = new() { new ItemViewModel { Name = "one" } };
        ListBox listBox = new() { Items = items };
        listBox.AddToRoot();

        listBox.RemoveFromRoot();
        items.Add(new ItemViewModel { Name = "two" });

        listBox.Items.ShouldBeSameAs(items);
        listBox.ListBoxItems.Count.ShouldBe(2);
    }

    [Fact]
    public void BoundListBox_ReparentedThroughChildrenRemoveAndAdd_LosesUnboundSelectionAndScroll()
    {
        (ListBox listBox, StackPanel _, StackPanel b) = CreateScrolledBoundListBoxInPanel(selectedIndex: 2, scrollValue: 100);

        listBox.Visual.Parent!.Children.Remove(listBox.Visual);
        b.Visual.Children.Add(listBox.Visual);

        listBox.ListBoxItems.Count.ShouldBe(20, "because the new parent's context binds Items again");
        listBox.SelectedIndex.ShouldBe(-1, "because Items was released while the context was null");
        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void BoundListBox_ReparentedByAssigningParent_KeepsSelectionAndScroll()
    {
        (ListBox listBox, StackPanel _, StackPanel b) = CreateScrolledBoundListBoxInPanel(selectedIndex: 2, scrollValue: 100);

        listBox.Visual.Parent = b.Visual;

        listBox.SelectedIndex.ShouldBe(2);
        listBox.VerticalScrollBarValue.ShouldBe(100);
    }

    // Two panels sharing one view model; the ListBox starts in the first and inherits its context.
    private static (ListBox, StackPanel, StackPanel) CreateScrolledBoundListBoxInPanel(int selectedIndex, double scrollValue)
    {
        ListViewModel vm = new() { Items = new() };
        for (int i = 0; i < 20; i++)
        {
            vm.Items.Add(new ItemViewModel { Name = "item " + i });
        }
        StackPanel a = new() { BindingContext = vm };
        StackPanel b = new() { BindingContext = vm };
        ListBox listBox = new() { Height = 100 };
        listBox.SetBinding(nameof(ListBox.Items), nameof(ListViewModel.Items));
        a.AddChild(listBox);
        listBox.SelectedIndex = selectedIndex;
        listBox.VerticalScrollBarValue = scrollValue;
        listBox.SelectedIndex.ShouldBe(selectedIndex);
        listBox.VerticalScrollBarValue.ShouldBe(scrollValue);
        return (listBox, a, b);
    }

    private static IList GetItems(FrameworkElement control) => control switch
    {
        ComboBox comboBox => comboBox.Items,
        ItemsControl itemsControl => itemsControl.Items,
        _ => throw new ArgumentException(control.GetType().Name)
    };

    private static FrameworkElement CreateBoundInRoot(Type controlType, ListViewModel vm)
    {
        FrameworkElement control = (FrameworkElement)Activator.CreateInstance(controlType)!;
        control.SetBinding("Items", nameof(ListViewModel.Items));
        if (control is ListBox or ComboBox)
        {
            control.SetBinding("SelectedObject", nameof(ListViewModel.Selected));
        }
        control.AddToRoot();
        control.BindingContext = vm;
        return control;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference BindInRootRemoveAndDrop(Type controlType, ListViewModel vm)
    {
        FrameworkElement control = CreateBoundInRoot(controlType, vm);
        control.RemoveFromRoot();
        return new WeakReference(control);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AssignItemsAndDrop(Type controlType, IList items)
    {
        FrameworkElement control = (FrameworkElement)Activator.CreateInstance(controlType)!;
        if (control is ComboBox comboBox)
        {
            comboBox.Items = items;
        }
        else
        {
            ((ItemsControl)control).Items = items;
        }
        return new WeakReference(control);
    }

    // A from-file ComboBox whose component has no ListBoxInstance keeps its Items until the drop-down
    // is first built.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AssignItemsToLazyComboBoxAndDrop(IList items)
    {
        ComboBoxVisual visual = new(tryCreateFormsObject: false);
        visual.Children.Remove(visual.GetGraphicalUiElementByName("ListBoxInstance")!);
        ComboBox comboBox = new(visual) { Items = items };
        return new WeakReference(comboBox);
    }

    private static void CollectGarbage()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
