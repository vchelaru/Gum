using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A nested binding path resolves each segment on the object it actually holds, not on the declared
/// type of the property that holds it.
/// </summary>
public class BindingNestedPathDeclaredTypeTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
    }

    private class TitledItemViewModel : ItemViewModel
    {
        public string? Title { get => Get<string?>(); set => Set(value); }
    }

    private class ShopViewModel : ViewModel
    {
        public object? SelectedAsObject { get => Get<object?>(); set => Set(value); }
        public ItemViewModel? SelectedAsBase { get => Get<ItemViewModel?>(); set => Set(value); }
    }

    [Fact]
    public void NestedPath_ThroughObjectTypedIntermediate_ReadsRuntimeTypeLeaf()
    {
        ShopViewModel viewModel = new() { SelectedAsObject = new ItemViewModel { Name = "Sword" } };
        Label label = new() { BindingContext = viewModel };

        label.SetBinding(nameof(Label.Text), "SelectedAsObject.Name");

        label.Text.ShouldBe("Sword");
    }

    [Fact]
    public void NestedPath_ThroughBaseTypedIntermediate_ReadsDerivedLeaf()
    {
        ShopViewModel viewModel = new() { SelectedAsBase = new TitledItemViewModel { Title = "Legendary" } };
        Label label = new() { BindingContext = viewModel };

        label.SetBinding(nameof(Label.Text), "SelectedAsBase.Title");

        label.Text.ShouldBe("Legendary");
    }
}
