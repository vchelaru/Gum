using System.ComponentModel;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Forms.Input;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A TextBox or PasswordBox edit pushed to the view model with the PropertyChanged trigger is not rewritten by
/// the value the view model sends back while the user is typing; the text is reconciled when the edit ends
/// (focus lost or Enter), as in WPF.
/// </summary>
public class TextBoxPropertyChangedEditTests : BaseTestClass
{
    // Raises PropertyChanged on every set, even when the value is unchanged.
    private class AlwaysRaisingViewModel : INotifyPropertyChanged
    {
        private float _floatValue;
        public event PropertyChangedEventHandler? PropertyChanged;

        public float FloatValue
        {
            get => _floatValue;
            set
            {
                _floatValue = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FloatValue)));
            }
        }
    }

    private static TextBox CreateFocusedFloatTextBox(AlwaysRaisingViewModel vm)
    {
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(AlwaysRaisingViewModel.FloatValue)) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        textBox.IsFocused = true;
        textBox.Text = "";
        return textBox;
    }

    private static void Type(TextBoxBase textBox, string text)
    {
        foreach (char character in text)
        {
            textBox.HandleCharEntered(character);
        }
    }

    [Fact]
    public void Typing_TrailingZero_KeepsTypedText()
    {
        AlwaysRaisingViewModel vm = new();
        TextBox textBox = CreateFocusedFloatTextBox(vm);

        Type(textBox, "1.50");

        textBox.Text.ShouldBe("1.50");
        vm.FloatValue.ShouldBe(1.5f);
    }

    [Fact]
    public void Typing_TrailingDot_KeepsDot()
    {
        AlwaysRaisingViewModel vm = new();
        TextBox textBox = CreateFocusedFloatTextBox(vm);

        Type(textBox, "1.");

        textBox.Text.ShouldBe("1.");
        vm.FloatValue.ShouldBe(1f);
    }

    [Fact]
    public void FocusLost_AfterEdit_ReconcilesToViewModelValue()
    {
        AlwaysRaisingViewModel vm = new();
        TextBox textBox = CreateFocusedFloatTextBox(vm);
        Type(textBox, "1.50");

        textBox.IsFocused = false;

        textBox.Text.ShouldBe("1.5");
    }

    [Fact]
    public void EnterPressed_AfterEdit_ReconcilesToViewModelValue()
    {
        AlwaysRaisingViewModel vm = new();
        TextBox textBox = CreateFocusedFloatTextBox(vm);
        Type(textBox, "1.50");

        textBox.HandleKeyDown(Keys.Enter, isShiftDown: false, isAltDown: false, isCtrlDown: false);

        textBox.Text.ShouldBe("1.5");
    }

    [Fact]
    public void ViewModelChangedElsewhere_WhileFocused_UpdatesText()
    {
        AlwaysRaisingViewModel vm = new();
        TextBox textBox = CreateFocusedFloatTextBox(vm);
        Type(textBox, "1.50");

        vm.FloatValue = 3;

        textBox.Text.ShouldBe("3");
    }

    [Fact]
    public void TextSetInCode_WhileNotFocused_ShowsViewModelValueAtOnce()
    {
        AlwaysRaisingViewModel vm = new();
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(AlwaysRaisingViewModel.FloatValue)) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        textBox.Text = "1.50";

        textBox.Text.ShouldBe("1.5");
    }

    [Fact]
    public void PasswordBox_TypingWithReformattingViewModel_KeepsTypedText()
    {
        AlwaysRaisingViewModel vm = new();
        PasswordBox passwordBox = new() { BindingContext = vm };
        passwordBox.SetBinding(nameof(PasswordBox.Password),
            new Binding(nameof(AlwaysRaisingViewModel.FloatValue)) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        passwordBox.IsFocused = true;
        passwordBox.Password = "";

        Type(passwordBox, "2.0");

        passwordBox.Password.ShouldBe("2.0");
        vm.FloatValue.ShouldBe(2f);
    }
}
