using Gum.Forms.Controls;
using Gum.Forms.Data;
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

    private class WeaponViewModel : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
        public ItemViewModel? Inner { get => Get<ItemViewModel?>(); set => Set(value); }
    }

    [Fact]
    public void DeepPath_ThroughObjectTypedIntermediate_TwoWay_WritesToRuntimeTypeLeaf()
    {
        ItemViewModel inner = new() { Name = "Sword" };
        ShopViewModel viewModel = new() { SelectedAsObject = new WeaponViewModel { Inner = inner } };
        TextBox textBox = new() { BindingContext = viewModel };
        textBox.SetBinding(nameof(TextBox.Text), "SelectedAsObject.Inner.Name");
        textBox.Text.ShouldBe("Sword");

        textBox.Text = "Shield";

        inner.Name.ShouldBe("Shield");
    }

    [Fact]
    public void NestedPath_ThroughObjectTypedIntermediate_FollowsChangeToUnrelatedRuntimeType()
    {
        ShopViewModel viewModel = new() { SelectedAsObject = new ItemViewModel { Name = "Sword" } };
        Label label = new() { BindingContext = viewModel };
        label.SetBinding(nameof(Label.Text), "SelectedAsObject.Name");

        viewModel.SelectedAsObject = new WeaponViewModel { Name = "Axe" };

        label.Text.ShouldBe("Axe");
    }

    [Fact]
    public void NestedPath_ThroughObjectTypedIntermediate_RuntimeTypeLacksMember_UsesFallbackValue()
    {
        ShopViewModel viewModel = new() { SelectedAsObject = new ItemViewModel { Name = "Sword" } };
        Label label = new() { BindingContext = viewModel };
        label.SetBinding(nameof(Label.Text), new Binding("SelectedAsObject.Name") { FallbackValue = "fallback" });

        viewModel.SelectedAsObject = "not an item";

        label.Text.ShouldBe("fallback");
    }

    [Fact]
    public void NestedPath_ThroughObjectTypedIntermediate_TwoWay_WritesToRuntimeTypeLeaf()
    {
        ItemViewModel item = new() { Name = "Sword" };
        ShopViewModel viewModel = new() { SelectedAsObject = item };
        TextBox textBox = new() { BindingContext = viewModel };
        textBox.SetBinding(nameof(TextBox.Text), "SelectedAsObject.Name");

        textBox.Text = "Shield";

        item.Name.ShouldBe("Shield");
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
