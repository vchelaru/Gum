using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A TwoWay binding with a StringFormat still writes the target's value back, as WPF does. The format is not
/// reversed: the text is written as is, converted to the source's type when the types differ.
/// </summary>
public class StringFormatWriteBackTests : BaseTestClass
{
    private class EntryViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }

        public int Count
        {
            get => Get<int>();
            set => Set(value);
        }
    }

    [Fact]
    public void StringSource_TargetEdited_WritesTextAsIs()
    {
        EntryViewModel vm = new() { Name = "Ann" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(EntryViewModel.Name)) { StringFormat = "Name: {0}" });

        textBox.Text = "Name: Bob";

        vm.Name.ShouldBe("Name: Bob");
    }

    [Fact]
    public void IntSource_TargetEditedWithNumber_WritesConvertedNumber()
    {
        EntryViewModel vm = new() { Count = 5 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(EntryViewModel.Count)) { StringFormat = "{0:N0}" });

        textBox.Text = "12";

        vm.Count.ShouldBe(12);
    }
}
