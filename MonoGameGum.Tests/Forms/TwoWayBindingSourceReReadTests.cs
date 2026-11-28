using System;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// After a two-way binding writes to its source it reads the source back, as WPF does since .NET 4, and shows
/// the value the view model kept when it differs from what was written, even when the setter raised no
/// PropertyChanged. A source that accepts the value, a write that never happens and OneWayToSource are left alone.
/// </summary>
public class TwoWayBindingSourceReReadTests : BaseTestClass
{
    private class CoercingViewModel : ViewModel
    {
        public double Clamped { get => Get<double>(); set => Set(Math.Min(value, 10)); }
    }

    private class PlainViewModel
    {
        public int Number { get; set; }
        public string FullName => "Ada Lovelace";
    }

    private class CountingConverter : IValueConverter
    {
        public int ConvertCalls { get; private set; }

        public object? Convert(object? value, Type targetType, object? parameter)
        {
            ConvertCalls++;
            return value?.ToString();
        }

        public object? ConvertBack(object? value, Type sourceType, object? parameter) =>
            int.TryParse(value as string, out int parsed) ? parsed : Binding.DoNothing;
    }

    [Fact]
    public void ViewModelClampsToValueItAlreadyHolds_ControlShowsTheHeldValue()
    {
        CoercingViewModel vm = new() { Clamped = 10 };
        Slider slider = new() { Minimum = 0, Maximum = 100, BindingContext = vm };
        slider.SetBinding(nameof(Slider.Value), nameof(CoercingViewModel.Clamped));
        slider.Value.ShouldBe(10);

        slider.Value = 50;

        vm.Clamped.ShouldBe(10);
        slider.Value.ShouldBe(10, "because a two-way binding shows what the view model kept, as WPF re-reads the source after writing it");
    }

    [Fact]
    public void ViewModelAcceptsValue_ConverterDoesNotRunAgain()
    {
        PlainViewModel vm = new() { Number = 1 };
        TextBox textBox = new() { BindingContext = vm };
        CountingConverter converter = new();
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(PlainViewModel.Number)) { Converter = converter });
        int callsBefore = converter.ConvertCalls;

        textBox.Text = "7";

        vm.Number.ShouldBe(7);
        converter.ConvertCalls.ShouldBe(callsBefore);
        textBox.Text.ShouldBe("7");
    }

    [Fact]
    public void OneWayToSource_ViewModelCoerces_ControlKeepsItsValue()
    {
        CoercingViewModel vm = new() { Clamped = 10 };
        Slider slider = new() { Minimum = 0, Maximum = 100, Value = 5, BindingContext = vm };
        slider.SetBinding(nameof(Slider.Value), new Binding(nameof(CoercingViewModel.Clamped)) { Mode = BindingMode.OneWayToSource });

        slider.Value = 50;

        vm.Clamped.ShouldBe(10);
        slider.Value.ShouldBe(50, "because OneWayToSource never shows the source value");
    }

    [Fact]
    public void GetOnlySource_EditIsNotWritten_ControlKeepsTheEdit()
    {
        PlainViewModel vm = new();
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(PlainViewModel.FullName));

        textBox.Text = "edited";

        textBox.Text.ShouldBe("edited");
    }

    [Fact]
    public void LostFocusTrigger_ContextClearedWhileEditing_ViewModelCoerces_ControlResets()
    {
        CoercingViewModel vm = new() { Clamped = 10 };
        Slider slider = new() { Minimum = 0, Maximum = 100, BindingContext = vm };
        slider.SetBinding(nameof(Slider.Value),
            new Binding(nameof(CoercingViewModel.Clamped)) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });

        slider.IsFocused = true;
        slider.Value = 50;
        slider.BindingContext = null;

        vm.Clamped.ShouldBe(10, "because clearing the context commits the pending edit to the outgoing view model");
        slider.Value.ShouldBe(ContextResetExpectation.Of(0d),
            "because a target whose BindingContext becomes null resets (#8543), and is not refreshed from the outgoing view model");
    }

    [Fact]
    public void LostFocusTrigger_FocusLeaves_ViewModelCoerces_ControlShowsTheHeldValue()
    {
        CoercingViewModel vm = new() { Clamped = 10 };
        Slider slider = new() { Minimum = 0, Maximum = 100, BindingContext = vm };
        slider.SetBinding(nameof(Slider.Value),
            new Binding(nameof(CoercingViewModel.Clamped)) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });

        slider.IsFocused = true;
        slider.Value = 50;
        slider.IsFocused = false;

        vm.Clamped.ShouldBe(10);
        slider.Value.ShouldBe(10);
    }
}
