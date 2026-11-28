using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// TargetNullValue maps a null source to a target value and, written back, the target value to null, as WPF
/// does. The placeholder text never reaches the view model.
/// </summary>
public class TargetNullValueWriteBackTests : BaseTestClass
{
    private class NameViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    [Fact]
    public void TargetHoldsTargetNullValue_WritesNullToSource()
    {
        NameViewModel vm = new() { Name = "x" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(NameViewModel.Name))
        {
            TargetNullValue = "(none)"
        });

        textBox.Text = "(none)";

        vm.Name.ShouldBeNull();
    }

    private class UpperBackConverter : IValueConverter
    {
        public object? Convert(object? value, System.Type targetType, object? parameter) => value;

        public object? ConvertBack(object? value, System.Type sourceType, object? parameter) =>
            (value as string)?.ToUpperInvariant();
    }

    [Fact]
    public void TargetHoldsTargetNullValue_WithConverter_WritesNullWithoutConvertBack()
    {
        NameViewModel vm = new() { Name = "x" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(NameViewModel.Name))
        {
            Converter = new UpperBackConverter(),
            TargetNullValue = "(none)"
        });

        textBox.Text = "(none)";

        vm.Name.ShouldBeNull();
    }

    [Fact]
    public void TargetSetToNull_WritesNullToSource()
    {
        NameViewModel vm = new() { Name = "x" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(NameViewModel.Name))
        {
            TargetNullValue = "(none)"
        });

        textBox.Text = null;

        vm.Name.ShouldBeNull();
    }
}
