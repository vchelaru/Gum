using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A lambda binding's indexer may be a captured variable, such as a loop counter (inventory slots bound in a
/// for loop); the path takes the variable's value at the SetBinding call.
/// </summary>
public class LambdaCapturedIndexBindingTests : BaseTestClass
{
    private class SlotViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }

        public List<string> Tags { get; } = new();
    }

    private class InventoryViewModel : ViewModel
    {
        public ObservableCollection<SlotViewModel> Slots { get; } = new();
        public List<string> Names { get; } = new() { "name0", "name1", "name2" };
        public string[] Tags { get; set; } = new[] { "t0", "t1", "t2" };
        public Dictionary<string, string> Lookup { get; } = new() { ["k"] = "v" };
        public int Selected { get; set; } = 1;
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
    public void TypedLambda_CapturedLoopIndex_BindsEachSlot()
    {
        InventoryViewModel vm = CreateInventory();
        List<Label> labels = new();
        for (int i = 0; i < 3; i++)
        {
            Label label = new() { BindingContext = vm };
            label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Slots[i].Name);
            labels.Add(label);
        }

        labels[0].Text.ShouldBe("slot0");
        labels[1].Text.ShouldBe("slot1");
        labels[2].Text.ShouldBe("slot2");
    }

    [Fact]
    public void TypedLambda_CapturedIndexIntoList_Binds()
    {
        InventoryViewModel vm = CreateInventory();
        int index = 1;
        Label label = new() { BindingContext = vm };

        label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Names[index]);

        label.Text.ShouldBe("name1");
    }

    [Fact]
    public void TypedLambda_CapturedIndexIntoArray_Binds()
    {
        InventoryViewModel vm = CreateInventory();
        int index = 2;
        Label label = new() { BindingContext = vm };

        label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Tags[index]);

        label.Text.ShouldBe("t2");
    }

    [Fact]
    public void VisualTypedLambda_CapturedIndex_Binds()
    {
        InventoryViewModel vm = CreateInventory();
        int index = 1;
        TextRuntime text = new() { BindingContext = vm };

        text.SetBinding<InventoryViewModel>(nameof(TextRuntime.Text), v => v.Slots[index].Name);

        text.Text.ShouldBe("slot1");
    }

    [Fact]
    public void VisualTypedLambda_LiteralIndex_Binds()
    {
        InventoryViewModel vm = CreateInventory();
        TextRuntime text = new() { BindingContext = vm };

        text.SetBinding<InventoryViewModel>(nameof(TextRuntime.Text), v => v.Slots[2].Name);

        text.Text.ShouldBe("slot2");
    }

    [Fact]
    public void ParameterlessLambda_CapturedIndexAndCapturedViewModel_Binds()
    {
        InventoryViewModel vm = CreateInventory();
        int index = 2;
        Label label = new() { BindingContext = vm };

        label.SetBinding(nameof(Label.Text), () => vm.Slots[index].Name);

        label.Text.ShouldBe("slot2");
    }

    [Fact]
    public void TypedLambda_TwoCapturedIndexesInOnePath_Binds()
    {
        InventoryViewModel vm = CreateInventory();
        vm.Slots[1].Tags.AddRange(new[] { "a", "b" });
        int slot = 1;
        int tag = 1;
        Label label = new() { BindingContext = vm };

        label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Slots[slot].Tags[tag]);

        label.Text.ShouldBe("b");
    }

    [Fact]
    public void TypedLambda_CapturedIndexChangedAfterSetBinding_KeepsValueAtCall()
    {
        InventoryViewModel vm = CreateInventory();
        int index = 0;
        Label label = new() { BindingContext = vm };

        label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Slots[index].Name);
        index = 2;
        vm.Slots[0].Name = "changed";

        label.Text.ShouldBe("changed");
    }

    [Fact]
    public void TypedLambda_IndexNotACapturedValue_ThrowsNamingIndex()
    {
        InventoryViewModel vm = CreateInventory();
        string key = "k";
        Label label = new() { BindingContext = vm };

        Should.Throw<InvalidOperationException>(() =>
            label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Names[v.Selected]))
            .Message.ShouldContain("v.Selected");
        Should.Throw<InvalidOperationException>(() =>
            label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Names[IndexOfSecond()]))
            .Message.ShouldContain("IndexOfSecond()");
        Should.Throw<InvalidOperationException>(() =>
            label.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Lookup[key]))
            .Message.ShouldContain("String");
    }

    [Fact]
    public void TypedLambda_StaticFieldAndPropertyIndexes_Bind()
    {
        InventoryViewModel vm = CreateInventory();
        Label fieldLabel = new() { BindingContext = vm };
        Label propertyLabel = new() { BindingContext = vm };

        fieldLabel.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Tags[FirstIndexField]);
        propertyLabel.SetBinding<InventoryViewModel>(nameof(Label.Text), v => v.Tags[LastIndexProperty]);

        fieldLabel.Text.ShouldBe("t0");
        propertyLabel.Text.ShouldBe("t2");
    }

    private static readonly int FirstIndexField = 0;
    private static int LastIndexProperty => 2;

    private static int IndexOfSecond() => 1;
}
