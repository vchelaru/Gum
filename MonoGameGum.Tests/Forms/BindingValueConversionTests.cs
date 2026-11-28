using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// How a bound value of one type reaches a target or source of another: assignable and convertible values
/// arrive, an unconvertible edit leaves the source alone.
/// </summary>
public class BindingValueConversionTests : BaseTestClass
{
    public enum Size
    {
        Small,
        Large
    }

    [Flags]
    public enum Sides
    {
        None = 0,
        Left = 1,
        Right = 2
    }

    private class SizeElement : FrameworkElement
    {
        private Size _size;
        private Sides _flags;

        public SizeElement() : base(new Gum.Wireframe.InteractiveGue())
        {
        }

        public Size Size
        {
            get => _size;
            set
            {
                _size = value;
                PushValueToViewModel();
            }
        }

        public Sides Flags
        {
            get => _flags;
            set
            {
                _flags = value;
                PushValueToViewModel();
            }
        }
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
    public void StringSource_ToEnumTarget_SetsMatchingMember()
    {
        ListBox listBox = new();

        listBox.SetBinding(nameof(ListBox.SelectionMode), nameof(ConversionViewModel.Text));
        listBox.BindingContext = new ConversionViewModel { Text = nameof(SelectionMode.Multiple) };

        listBox.SelectionMode.ShouldBe(SelectionMode.Multiple);
    }

    [Fact]
    public void StringSource_ToEnumTarget_IgnoresCase()
    {
        // WPF parses enum names through EnumConverter, which ignores case.
        ListBox listBox = new();

        listBox.SetBinding(nameof(ListBox.SelectionMode), nameof(ConversionViewModel.Text));
        listBox.BindingContext = new ConversionViewModel { Text = "multiple" };

        listBox.SelectionMode.ShouldBe(SelectionMode.Multiple);
    }

    [Fact]
    public void IntSource_ToEnumTarget_SetsMatchingMember()
    {
        ListBox listBox = new();

        listBox.SetBinding(nameof(ListBox.SelectionMode), nameof(ConversionViewModel.Number));
        listBox.BindingContext = new ConversionViewModel { Number = (int)SelectionMode.Multiple };

        listBox.SelectionMode.ShouldBe(SelectionMode.Multiple);
    }

    [Fact]
    public void StringSource_ToEnumTarget_UnknownName_ThrowsNamingEnumAndValue()
    {
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.SetBinding(nameof(ListBox.SelectionMode), nameof(ConversionViewModel.Text));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            listBox.BindingContext = new ConversionViewModel { Text = "Huge" });

        exception.Message.ShouldContain("'Huge'");
        exception.Message.ShouldContain(nameof(SelectionMode));
        listBox.SelectionMode.ShouldBe(SelectionMode.Multiple);
    }

    [Fact]
    public void IntSource_ToEnumTarget_UndefinedNumber_ThrowsNamingEnumAndValue()
    {
        ListBox listBox = new() { SelectionMode = SelectionMode.Multiple };
        listBox.SetBinding(nameof(ListBox.SelectionMode), nameof(ConversionViewModel.Number));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            listBox.BindingContext = new ConversionViewModel { Number = 99 });

        exception.Message.ShouldContain("'99'");
        exception.Message.ShouldContain(nameof(SelectionMode));
        listBox.SelectionMode.ShouldBe(SelectionMode.Multiple);
    }

    [Fact]
    public void IntSource_ToFlagsEnumTarget_CombinationOfMembers_SetsCombination()
    {
        SizeElement element = new();

        element.SetBinding(nameof(SizeElement.Flags), nameof(ConversionViewModel.Number));
        element.BindingContext = new ConversionViewModel { Number = (int)(Sides.Left | Sides.Right) };

        element.Flags.ShouldBe(Sides.Left | Sides.Right);
    }

    [Fact]
    public void EnumTargetTwoWay_ToStringAndIntSources_WritesBack()
    {
        SizeElement toText = new();
        SizeElement toNumber = new();
        ConversionViewModel viewModel = new() { Text = nameof(Size.Small), Number = (int)Size.Small };
        toText.SetBinding(nameof(SizeElement.Size), nameof(ConversionViewModel.Text));
        toNumber.SetBinding(nameof(SizeElement.Size), nameof(ConversionViewModel.Number));
        toText.BindingContext = viewModel;
        toNumber.BindingContext = viewModel;

        toText.Size = Size.Large;
        toNumber.Size = Size.Large;

        viewModel.Text.ShouldBe(nameof(Size.Large));
        viewModel.Number.ShouldBe((int)Size.Large);
    }

    [Fact]
    public void TextBoxTwoWay_TextToEnumSource_ParsesNameAndLeavesSourceOnUnknownName()
    {
        TextBox textBox = new();
        ConversionViewModel viewModel = new() { Size = Size.Small };
        textBox.SetBinding(nameof(TextBox.Text), nameof(ConversionViewModel.Size));
        textBox.BindingContext = viewModel;

        textBox.Text = "large";
        viewModel.Size.ShouldBe(Size.Large);

        textBox.Text = "Huge";
        viewModel.Size.ShouldBe(Size.Large);
    }

    [Fact]
    public void StringFormatWithoutPlaceholder_ThrowsNamingFormat()
    {
        // "$0" is what C# leaves of $"${0:N0}".
        Label label = new();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            label.SetBinding(nameof(Label.Text), new Binding(nameof(ConversionViewModel.Number)) { StringFormat = "$0" }));

        exception.Message.ShouldContain("'$0'");
        exception.Message.ShouldContain("{0}");
    }

    [Theory]
    [InlineData("{{0}}")]
    [InlineData("")]
    public void StringFormatWithOnlyEscapedOrNoText_Throws(string format)
    {
        Label label = new();

        Should.Throw<InvalidOperationException>(() =>
            label.SetBinding(nameof(Label.Text), new Binding(nameof(ConversionViewModel.Number)) { StringFormat = format }));
    }

    [Fact]
    public void StringFormatWithoutPlaceholder_OnNonStringTarget_Throws()
    {
        // StringFormat only applies to a string target, but a format with no {0} is a mistake on any target.
        SizeElement element = new();

        Should.Throw<InvalidOperationException>(() =>
            element.SetBinding(nameof(SizeElement.Size), new Binding(nameof(ConversionViewModel.Size)) { StringFormat = "$0" }));
    }

    [Theory]
    [InlineData("{0:N0}", "1,234")]
    [InlineData("[{0,6}]", "[  1234]")]
    [InlineData("{{{0}}}", "{1234}")]
    public void StringFormatWithPlaceholderVariants_FormatsValue(string format, string expected)
    {
        Label label = new();

        label.SetBinding(nameof(Label.Text), new Binding(nameof(ConversionViewModel.Number)) { StringFormat = format });
        label.BindingContext = new ConversionViewModel { Number = 1234 };

        label.Text.ShouldBe(expected);
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
