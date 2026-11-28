using Gum.Forms.Controls;
using Gum.Forms.Data;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// When a two-way binding on a text-entry control writes back to its source: on every change
/// (PropertyChanged) or only when the edit ends (LostFocus), including the control leaving the root
/// or the view model changing while an edit is pending.
/// </summary>
public class UpdateSourceTriggerTests : BaseTestClass
{
    private class CountingViewModel
    {
        private string? _text;
        public int TimesSet { get; private set; }
        public string? Text { get => _text; set { TimesSet++; _text = value; } }
    }

    private static TextBoxBase CreateTextEntry(string control) => control switch
    {
        nameof(PasswordBox) => new PasswordBox(),
        _ => new TextBox(),
    };

    private static string TextPropertyOf(TextBoxBase control) =>
        control is PasswordBox ? nameof(PasswordBox.Password) : nameof(TextBox.Text);

    private static void SetText(TextBoxBase control, string value)
    {
        if (control is PasswordBox passwordBox)
        {
            passwordBox.Password = value;
        }
        else
        {
            ((TextBox)control).Text = value;
        }
    }

    [Theory]
    [InlineData(nameof(TextBox))]
    [InlineData(nameof(PasswordBox))]
    public void PropertyChangedTrigger_EachEditReachesSource(string control)
    {
        TestViewModel vm = new() { Text = "initial" };
        TextBoxBase textEntry = CreateTextEntry(control);
        textEntry.BindingContext = vm;
        textEntry.SetBinding(TextPropertyOf(textEntry),
            new Binding(nameof(TestViewModel.Text)) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        SetText(textEntry, "a");
        vm.Text.ShouldBe("a");
        SetText(textEntry, "ab");
        vm.Text.ShouldBe("ab");
    }

    [Theory]
    [InlineData(nameof(TextBox))]
    [InlineData(nameof(PasswordBox))]
    public void LostFocusTrigger_EditReachesSourceOnlyWhenFocusLeaves(string control)
    {
        TestViewModel vm = new() { Text = "initial" };
        TextBoxBase textEntry = CreateTextEntry(control);
        textEntry.BindingContext = vm;
        textEntry.SetBinding(TextPropertyOf(textEntry),
            new Binding(nameof(TestViewModel.Text)) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });

        textEntry.IsFocused = true;
        SetText(textEntry, "typed");
        vm.Text.ShouldBe("initial");

        textEntry.IsFocused = false;
        vm.Text.ShouldBe("typed");
    }

    [Theory]
    [InlineData(nameof(TextBox))]
    [InlineData(nameof(PasswordBox))]
    public void LostFocusTrigger_FocusLeavesWithoutEdit_DoesNotWriteSource(string control)
    {
        CountingViewModel vm = new() { Text = "initial" };
        TextBoxBase textEntry = CreateTextEntry(control);
        textEntry.BindingContext = vm;
        textEntry.SetBinding(TextPropertyOf(textEntry),
            new Binding(nameof(CountingViewModel.Text)) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });
        int setsBefore = vm.TimesSet;

        textEntry.IsFocused = true;
        textEntry.IsFocused = false;

        vm.TimesSet.ShouldBe(setsBefore);
    }

    [Fact]
    public void LostFocusTrigger_RemovedFromRootWhileEditing_CommitsEditToViewModel()
    {
        TestViewModel vm = new() { Text = "initial" };
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.BindingContext = vm;
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(TestViewModel.Text)) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });

        textBox.IsFocused = true;
        textBox.Text = "typed";
        textBox.RemoveFromRoot();

        vm.Text.ShouldBe("typed");
    }

    [Fact]
    public void LostFocusTrigger_ContextSwappedWhileEditing_NewViewModelKeepsItsValue()
    {
        TestViewModel first = new() { Text = "first" };
        TestViewModel second = new() { Text = "second" };
        TextBox textBox = new() { BindingContext = first };
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(TestViewModel.Text)) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });

        textBox.IsFocused = true;
        textBox.Text = "typed";
        textBox.BindingContext = second;
        textBox.IsFocused = false;

        textBox.Text.ShouldBe("second");
        second.Text.ShouldBe("second");
    }

    [Fact]
    public void LostFocusTrigger_SourceChangesWhileEditing_TargetShowsSourceAndFocusLossWritesNothing()
    {
        TestViewModel vm = new() { Text = "initial" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(TestViewModel.Text)) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });

        textBox.IsFocused = true;
        textBox.Text = "typed";
        vm.Text = "from code";
        textBox.Text.ShouldBe("from code");

        textBox.IsFocused = false;
        vm.Text.ShouldBe("from code");
    }

    [Fact]
    public void LostFocusTrigger_OneWayBinding_NeverWritesSource()
    {
        TestViewModel vm = new() { Text = "initial" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(TestViewModel.Text))
        {
            Mode = BindingMode.OneWay,
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        });

        textBox.IsFocused = true;
        textBox.Text = "typed";
        textBox.IsFocused = false;

        vm.Text.ShouldBe("initial");
    }
}
