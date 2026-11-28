using System;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A value that cannot reach the target (a converter result of the wrong type, a converter that throws, a
/// StringFormat the value cannot take, a name no enum member has) shows the binding's FallbackValue when one
/// is set, as WPF does. Without a FallbackValue each case keeps its own outcome.
/// </summary>
public class BindingConversionFallbackTests : BaseTestClass
{
    private class DelegateConverter : IValueConverter
    {
        public Func<object?, object?> To { get; init; } = v => v;

        public object? Convert(object? value, Type targetType, object? parameter) => To(value);

        public object? ConvertBack(object? value, Type sourceType, object? parameter) => value;
    }

    private class NumberViewModel
    {
        public double Number { get; set; }
        public string? Text { get; set; }
    }

    private static Func<object?, object?> ConverterOutput(string output) => output switch
    {
        "null" => _ => null,
        "wrongType" => _ => "not a bool",
        _ => _ => throw new InvalidOperationException("converter failed"),
    };

    [Theory]
    [InlineData("null")]
    [InlineData("wrongType")]
    [InlineData("throws")]
    public void Convert_ReturnsValueTargetCannotTake_ShowsFallbackValue(string output)
    {
        TestViewModel vm = new() { Text = "anything" };
        Button button = new() { BindingContext = vm };
        button.SetBinding(nameof(Button.IsEnabled), new Binding(nameof(TestViewModel.Text))
        {
            Converter = new DelegateConverter { To = ConverterOutput(output) },
            FallbackValue = true
        });

        button.IsEnabled = false;
        vm.Text = "changed";

        button.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Convert_ReturnsValueTargetCannotTake_WithoutFallbackValue_ShowsTargetDefault()
    {
        TestViewModel vm = new() { Text = "anything" };
        Button button = new() { BindingContext = vm };
        button.IsEnabled.ShouldBeTrue();

        button.SetBinding(nameof(Button.IsEnabled), new Binding(nameof(TestViewModel.Text))
        {
            Converter = new DelegateConverter { To = _ => null }
        });

        button.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void StringFormat_ValueCannotTakeFormat_ShowsFallbackValue()
    {
        NumberViewModel vm = new() { Number = 3 };
        Label label = new() { BindingContext = vm };

        // "Q" is not a numeric format specifier, so double.ToString throws FormatException.
        label.SetBinding(nameof(Label.Text), new Binding(nameof(NumberViewModel.Number))
        {
            StringFormat = "{0:Q}",
            FallbackValue = "n/a"
        });

        label.Text.ShouldBe("n/a");
    }

    [Fact]
    public void EnumTarget_UnknownName_WithFallbackValue_ShowsFallbackValue()
    {
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.SetBinding(nameof(ListBox.SelectionMode), new Binding(nameof(NumberViewModel.Text))
        {
            FallbackValue = SelectionMode.Single
        });

        listBox.BindingContext = new NumberViewModel { Text = "Huge" };

        listBox.SelectionMode.ShouldBe(SelectionMode.Single);
    }
}
