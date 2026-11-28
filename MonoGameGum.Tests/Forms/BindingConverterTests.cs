using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// What a Forms binding does with its converter's output in both directions: DoNothing, null, a value
/// the target cannot take, the ConverterParameter, and the order of the converter and StringFormat.
/// </summary>
public class BindingConverterTests : BaseTestClass
{
    private class DelegateConverter : IValueConverter
    {
        public Func<object?, object?> To { get; init; } = v => v;
        public Func<object?, object?> Back { get; init; } = v => v;
        public List<object?> ConvertParameters { get; } = new();
        public List<object?> ConvertBackParameters { get; } = new();

        public object? Convert(object? value, Type targetType, object? parameter)
        {
            ConvertParameters.Add(parameter);
            return To(value);
        }

        public object? ConvertBack(object? value, Type sourceType, object? parameter)
        {
            ConvertBackParameters.Add(parameter);
            return Back(value);
        }
    }

    [Fact]
    public void Convert_ReturnsDoNothing_LeavesTargetUnchanged()
    {
        TestViewModel vm = new() { Text = "ignored" };
        TextBox textBox = new() { Text = "kept", BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(TestViewModel.Text))
        {
            Converter = new DelegateConverter { To = _ => Binding.DoNothing }
        });

        vm.Text = "ignored again";

        textBox.Text.ShouldBe("kept");
    }

    [Fact]
    public void ConvertBack_ReturnsDoNothing_LeavesSourceUnchanged()
    {
        TestViewModel vm = new() { Text = "source" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(TestViewModel.Text))
        {
            Converter = new DelegateConverter { Back = _ => Binding.DoNothing }
        });

        textBox.Text = "typed";

        vm.Text.ShouldBe("source");
    }

    [Fact]
    public void Convert_ReturnsNull_ForNullableTarget_SetsNull()
    {
        TestViewModel vm = new() { IsChecked = true };
        CheckBox checkBox = new() { BindingContext = vm };
        checkBox.SetBinding(nameof(CheckBox.IsChecked), new Binding(nameof(TestViewModel.IsChecked))
        {
            Converter = new DelegateConverter { To = _ => null }
        });

        checkBox.IsChecked.ShouldBeNull();
    }

    [Fact]
    public void ConvertBack_ReturnsNull_ForValueTypeSource_LeavesSourceUnchanged()
    {
        TestViewModel vm = new() { IntValue = 5 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(TestViewModel.IntValue))
        {
            Converter = new DelegateConverter { To = v => v?.ToString(), Back = _ => null }
        });

        textBox.Text = "7";

        vm.IntValue.ShouldBe(5);
    }

    [Fact]
    public void ConverterParameter_ReachesConvertAndConvertBack()
    {
        TestViewModel vm = new() { Text = "source" };
        TextBox textBox = new() { BindingContext = vm };
        DelegateConverter converter = new();
        object parameter = new();
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(TestViewModel.Text))
        {
            Converter = converter,
            ConverterParameter = parameter
        });

        textBox.Text = "typed";

        converter.ConvertParameters.ShouldNotBeEmpty();
        converter.ConvertParameters.ShouldAllBe(p => p == parameter);
        converter.ConvertBackParameters.ShouldNotBeEmpty();
        converter.ConvertBackParameters.ShouldAllBe(p => p == parameter);
    }

    [Fact]
    public void ConverterAndStringFormat_ConverterRunsFirst()
    {
        TestViewModel vm = new() { IntValue = 3 };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), new Binding(nameof(TestViewModel.IntValue))
        {
            Converter = new DelegateConverter { To = v => (int)v! * 10 },
            StringFormat = "Gold: {0}"
        });

        label.Text.ShouldBe("Gold: 30");
    }

    [Fact]
    public void Converter_ReceivesSourceValueOnEveryChange()
    {
        TestViewModel vm = new() { IntValue = 1 };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), new Binding(nameof(TestViewModel.IntValue))
        {
            Converter = new DelegateConverter { To = v => $"#{v}" }
        });

        vm.IntValue = 2;

        label.Text.ShouldBe("#2");
    }
}
