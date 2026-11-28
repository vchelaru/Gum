using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// How a bound value of one type reaches a target or source of another: assignable and convertible values
/// arrive, an unconvertible edit leaves the source alone. Tests marked as pinning current behavior cover
/// cases whose right outcome is a design question.
/// </summary>
public class BindingValueConversionTests : BaseTestClass
{
    public enum Size
    {
        Small,
        Large
    }

    public class ConversionViewModel : ViewModel
    {
        public int Number
        {
            get => Get<int>();
            set => Set(value);
        }

        public Size Size
        {
            get => Get<Size>();
            set => Set(value);
        }

        public string? Text
        {
            get => Get<string?>();
            set => Set(value);
        }

        public List<string>? Strings
        {
            get => Get<List<string>?>();
            set => Set(value);
        }
    }

    [Fact]
    public void ListOfTSource_ToIListItemsTarget_ShowsTheItems()
    {
        ListBox listBox = new();
        ConversionViewModel viewModel = new() { Strings = new List<string> { "a", "b" } };

        listBox.SetBinding(nameof(ListBox.Items), nameof(ConversionViewModel.Strings));
        listBox.BindingContext = viewModel;

        listBox.Items.Count.ShouldBe(2);
        listBox.Items[1].ShouldBe("b");
    }

    [Fact]
    public void ListBoxSelectedItemsOneWayToSource_ToListOfTSource_PinsCurrentBehavior_SourceStaysNull()
    {
        // The selection collection is not a List<string>, so it cannot be assigned, and nothing reports it.
        // Whether this should fail at SetBinding is a design question.
        ListBox listBox = new();
        listBox.Items.Add("a");
        listBox.Items.Add("b");
        ConversionViewModel viewModel = new();
        listBox.SetBinding(nameof(ListBox.SelectedItems),
            new Binding(nameof(ConversionViewModel.Strings)) { Mode = BindingMode.OneWayToSource });

        listBox.BindingContext = viewModel;
        listBox.SelectedIndex = 1;

        viewModel.Strings.ShouldBeNull();
    }

    [Fact]
    public void IntSource_ToStringTarget_WithoutStringFormat_ShowsNumber()
    {
        Label label = new();

        label.SetBinding(nameof(Label.Text), nameof(ConversionViewModel.Number));
        label.BindingContext = new ConversionViewModel { Number = 1234 };

        label.Text.ShouldBe("1234");
    }

    [Fact]
    public void EnumSource_ToStringTarget_ShowsEnumName()
    {
        Label label = new();

        label.SetBinding(nameof(Label.Text), nameof(ConversionViewModel.Size));
        label.BindingContext = new ConversionViewModel { Size = Size.Large };

        label.Text.ShouldBe("Large");
    }

    [Fact]
    public void TextBoxTwoWay_UnparsableTextToIntSource_LeavesSourceUnchanged()
    {
        TextBox textBox = new();
        ConversionViewModel viewModel = new() { Number = 7 };
        textBox.SetBinding(nameof(TextBox.Text), nameof(ConversionViewModel.Number));
        textBox.BindingContext = viewModel;

        textBox.Text = "abc";

        viewModel.Number.ShouldBe(7);
    }

    [Fact]
    public void StringSource_ToEnumTarget_PinsCurrentBehavior_TargetGetsDefault()
    {
        // Convert.ChangeType does not parse enums, so the target falls back to default. Design question.
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };

        listBox.SetBinding(nameof(ListBox.SelectionMode), nameof(ConversionViewModel.Text));
        listBox.BindingContext = new ConversionViewModel { Text = nameof(SelectionMode.Multiple) };

        listBox.SelectionMode.ShouldBe(SelectionMode.Single);
    }

    [Fact]
    public void IntSource_ToEnumTarget_PinsCurrentBehavior_TargetGetsDefault()
    {
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };

        listBox.SetBinding(nameof(ListBox.SelectionMode), nameof(ConversionViewModel.Number));
        listBox.BindingContext = new ConversionViewModel { Number = (int)SelectionMode.Multiple };

        listBox.SelectionMode.ShouldBe(SelectionMode.Single);
    }

    [Fact]
    public void StringFormatWithoutPlaceholder_PinsCurrentBehavior_ShowsFormatLiterally()
    {
        // "$0" is what C# leaves of $"${0:N0}". The value never appears. Whether to warn or throw is a design question.
        Label label = new();

        label.SetBinding(nameof(Label.Text), new Binding(nameof(ConversionViewModel.Number)) { StringFormat = "$0" });
        label.BindingContext = new ConversionViewModel { Number = 1234 };

        label.Text.ShouldBe("$0");
    }

    [Fact]
    public void StringFormatWithPlaceholder_FormatsValue()
    {
        Label label = new();

        label.SetBinding(nameof(Label.Text), new Binding(nameof(ConversionViewModel.Number)) { StringFormat = "${0}" });
        label.BindingContext = new ConversionViewModel { Number = 1234 };

        label.Text.ShouldBe("$1234");
    }
}
