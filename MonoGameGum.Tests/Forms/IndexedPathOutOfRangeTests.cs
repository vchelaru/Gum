using System.Collections.ObjectModel;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A collection change that leaves an indexed path segment (Items[1].Name) out of range shows the fallback
/// instead of throwing, and the binding picks the item up again once the index exists.
/// </summary>
public class IndexedPathOutOfRangeTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    private class ListViewModel : ViewModel
    {
        public ObservableCollection<ItemViewModel> Items { get; } = new();
    }

    [Fact]
    public void RemoveLeavesIndexOutOfRange_ShowsFallback()
    {
        ListViewModel vm = new();
        vm.Items.Add(new ItemViewModel { Name = "a" });
        vm.Items.Add(new ItemViewModel { Name = "b" });
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), new Binding("Items[1].Name") { FallbackValue = "none" });

        ItemViewModel removed = vm.Items[1];
        Should.NotThrow(() => vm.Items.RemoveAt(1));
        label.Text.ShouldBe("none");

        removed.Name = "after-remove";
        label.Text.ShouldBe("none");

        vm.Items.Add(new ItemViewModel { Name = "back" });
        label.Text.ShouldBe("back");
    }

    [Fact]
    public void Visual_ClearLeavesIndexOutOfRange_DoesNotThrow()
    {
        ListViewModel vm = new();
        vm.Items.Add(new ItemViewModel { Name = "a" });
        TextRuntime text = new() { BindingContext = vm };
        text.SetBinding(nameof(TextRuntime.Text), "Items[0].Name");

        Should.NotThrow(() => vm.Items.Clear());
        Should.NotThrow(() => vm.Items.Add(new ItemViewModel { Name = "b" }));

        text.Text.ShouldBe("b");
    }
}
