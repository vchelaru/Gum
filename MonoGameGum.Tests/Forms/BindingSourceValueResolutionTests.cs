using System;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// How a Forms binding turns the source value into the target value: null source values,
/// TargetNullValue, FallbackValue, converters, StringFormat, and intermediates whose declared type is
/// less specific than the object they hold.
/// </summary>
public class BindingSourceValueResolutionTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
        public int Price { get => Get<int>(); set => Set(value); }
    }

    private interface IHasTitle
    {
        string? Title { get; }
    }

    private class TitledItemViewModel : ItemViewModel, IHasTitle
    {
        public string? Title { get => Get<string?>(); set => Set(value); }
    }

    private class ShopViewModel : ViewModel
    {
        public IHasTitle? SelectedAsInterface { get => Get<IHasTitle?>(); set => Set(value); }
        public ItemViewModel? Selected { get => Get<ItemViewModel?>(); set => Set(value); }
        public string? Caption { get => Get<string?>(); set => Set(value); }
        public int Gold { get => Get<int>(); set => Set(value); }
    }

    private class ThrowingConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) =>
            throw new InvalidOperationException("convert failed");

        public object? ConvertBack(object? value, Type sourceType, object? parameter) =>
            throw new InvalidOperationException("convert back failed");
    }

    private class GoldToEnabledConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) =>
            value is int gold && gold >= (int)parameter!;

        public object? ConvertBack(object? value, Type sourceType, object? parameter) => Binding.DoNothing;
    }

    [Fact]
    public void SourceChangesFromNullToValue_WithTargetNullValue_TargetShowsValue()
    {
        ShopViewModel viewModel = new() { Caption = null };
        Label label = new() { BindingContext = viewModel };
        label.SetBinding(nameof(Label.Text), new Binding(nameof(ShopViewModel.Caption)) { TargetNullValue = "(none)" });

        viewModel.Caption = "Sword";

        label.Text.ShouldBe("Sword");
    }

    [Fact]
    public void FallbackValue_BrokenIntermediate_IsUsed()
    {
        ShopViewModel viewModel = new() { Selected = null };
        Label label = new() { BindingContext = viewModel };

        label.SetBinding(nameof(Label.Text), new Binding("Selected.Name") { FallbackValue = "nothing selected" });

        label.Text.ShouldBe("nothing selected");
    }

    [Fact]
    public void FallbackValue_IntermediateBecomesNonNull_TargetShowsLeaf()
    {
        ShopViewModel viewModel = new() { Selected = null };
        Label label = new() { BindingContext = viewModel };
        label.SetBinding(nameof(Label.Text), new Binding("Selected.Name") { FallbackValue = "nothing selected" });

        viewModel.Selected = new ItemViewModel { Name = "Shield" };

        label.Text.ShouldBe("Shield");
    }

    [Fact]
    public void IntermediateReplaced_OldIntermediateChanges_TargetIgnoresIt()
    {
        ItemViewModel first = new() { Name = "Sword" };
        ShopViewModel viewModel = new() { Selected = first };
        Label label = new() { BindingContext = viewModel };
        label.SetBinding(nameof(Label.Text), "Selected.Name");

        viewModel.Selected = new ItemViewModel { Name = "Shield" };
        first.Name = "Stale";

        label.Text.ShouldBe("Shield");
    }

    [Fact]
    public void TwoWayNestedPath_TargetEdit_WritesToCurrentIntermediate()
    {
        ItemViewModel first = new() { Name = "Sword" };
        ItemViewModel second = new() { Name = "Shield" };
        ShopViewModel viewModel = new() { Selected = first };
        TextBox textBox = new() { BindingContext = viewModel };
        textBox.SetBinding(nameof(TextBox.Text), "Selected.Name");

        viewModel.Selected = second;
        textBox.Text = "Bow";

        second.Name.ShouldBe("Bow");
        first.Name.ShouldBe("Sword");
    }

    [Fact]
    public void Converter_WithParameter_DrivesIsEnabled()
    {
        ShopViewModel viewModel = new() { Gold = 5 };
        Button button = new() { BindingContext = viewModel };
        button.SetBinding(nameof(Button.IsEnabled), new Binding(nameof(ShopViewModel.Gold))
        {
            Mode = BindingMode.OneWay,
            Converter = new GoldToEnabledConverter(),
            ConverterParameter = 10
        });

        button.IsEnabled.ShouldBeFalse();
        viewModel.Gold = 12;
        button.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Converter_Throws_DoesNotThrowOutOfViewModelSetter()
    {
        ShopViewModel viewModel = new() { Caption = "a" };
        Label label = new() { BindingContext = viewModel };
        label.SetBinding(nameof(Label.Text), new Binding(nameof(ShopViewModel.Caption)) { Converter = new ThrowingConverter() });

        Should.NotThrow(() => viewModel.Caption = "b");
    }

    [Fact]
    public void StringFormat_IntSource_FormatsOnEveryChange()
    {
        ShopViewModel viewModel = new() { Gold = 5 };
        Label label = new() { BindingContext = viewModel };
        label.SetBinding(nameof(Label.Text), new Binding(nameof(ShopViewModel.Gold)) { StringFormat = "Gold: {0:N0}" });

        label.Text.ShouldBe("Gold: 5");
        viewModel.Gold = 1234;
        label.Text.ShouldBe($"Gold: {1234:N0}");
    }
    [Fact]
    public void NestedPath_ThroughInterfaceTypedIntermediate_ReadsInterfaceLeaf()
    {
        ShopViewModel viewModel = new() { SelectedAsInterface = new TitledItemViewModel { Title = "Legendary" } };
        Label label = new() { BindingContext = viewModel };

        label.SetBinding(nameof(Label.Text), "SelectedAsInterface.Title");

        label.Text.ShouldBe("Legendary");
    }

    [Fact]
    public void BindingContextNull_AfterValue_LeavesTargetAndDoesNotThrow()
    {
        ShopViewModel viewModel = new() { Caption = "Sword" };
        Label label = new() { BindingContext = viewModel };
        label.SetBinding(nameof(Label.Text), nameof(ShopViewModel.Caption));

        Should.NotThrow(() => label.BindingContext = null);
        Should.NotThrow(() => viewModel.Caption = "after");

        label.Text.ShouldNotBe("after");
    }

    [Fact]
    public void ClearBinding_IsEnabled_StopsFollowingViewModel()
    {
        ShopViewModel viewModel = new() { Gold = 50 };
        Button button = new() { BindingContext = viewModel };
        button.SetBinding(nameof(Button.IsEnabled), new Binding(nameof(ShopViewModel.Gold))
        {
            Mode = BindingMode.OneWay,
            Converter = new GoldToEnabledConverter(),
            ConverterParameter = 10
        });

        button.ClearBinding(nameof(Button.IsEnabled));
        viewModel.Gold = 0;

        button.IsDataBound(nameof(Button.IsEnabled)).ShouldBeFalse();
    }
}
