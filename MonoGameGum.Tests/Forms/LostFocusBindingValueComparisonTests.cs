using Gum.Forms.Controls;
using Gum.Forms.Data;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A LostFocus binding writes the source only when the control's value changed since the binding last
/// showed or wrote it, compared by value: a getter that builds a new string (PasswordBox.Password) or boxes
/// a number (Slider.Value) on every read is not an edit.
/// </summary>
public class LostFocusBindingValueComparisonTests : BaseTestClass
{
    private class CountingViewModel
    {
        private string? _text;
        private double _number;
        public int TimesSet { get; private set; }
        public string? Text { get => _text; set { TimesSet++; _text = value; } }
        public double Number { get => _number; set { TimesSet++; _number = value; } }
    }

    private static FrameworkElement CreateBoundControl(string control, CountingViewModel vm)
    {
        Binding Bind(string path) => new(path) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus };
        switch (control)
        {
            case nameof(PasswordBox):
                PasswordBox passwordBox = new() { BindingContext = vm };
                passwordBox.SetBinding(nameof(PasswordBox.Password), Bind(nameof(CountingViewModel.Text)));
                return passwordBox;
            case nameof(Slider):
                Slider slider = new() { Minimum = 0, Maximum = 100, BindingContext = vm };
                slider.SetBinding(nameof(Slider.Value), Bind(nameof(CountingViewModel.Number)));
                return slider;
            default:
                TextBox textBox = new() { BindingContext = vm };
                textBox.SetBinding(nameof(TextBox.Text), Bind(nameof(CountingViewModel.Text)));
                return textBox;
        }
    }

    [Theory]
    [InlineData(nameof(TextBox))]
    [InlineData(nameof(PasswordBox))]
    [InlineData(nameof(Slider))]
    public void LostFocusTrigger_FocusLeavesWithoutEdit_DoesNotWriteSource(string control)
    {
        CountingViewModel vm = new() { Text = "initial", Number = 5 };
        FrameworkElement element = CreateBoundControl(control, vm);
        int setsBefore = vm.TimesSet;

        element.IsFocused = true;
        element.IsFocused = false;

        vm.TimesSet.ShouldBe(setsBefore);
    }

    [Fact]
    public void LostFocusTrigger_FocusLeavesAgainAfterCommit_DoesNotWriteAgain()
    {
        CountingViewModel vm = new() { Text = "initial" };
        TextBox textBox = (TextBox)CreateBoundControl(nameof(TextBox), vm);
        textBox.IsFocused = true;
        textBox.Text = "typed";
        textBox.IsFocused = false;
        vm.Text.ShouldBe("typed");
        int setsAfterCommit = vm.TimesSet;

        textBox.IsFocused = true;
        textBox.IsFocused = false;

        vm.TimesSet.ShouldBe(setsAfterCommit, "because the view model raises no PropertyChanged, so the binding must remember what it wrote");
    }
}
