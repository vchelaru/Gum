using System;
using System.Collections;
using System.Linq;
using System.ComponentModel;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A OneWayToSource binding copies the control's value to the view model, including the value the control
/// already holds when the binding attaches to a BindingContext.
/// </summary>
public class OneWayToSourceAttachTests : BaseTestClass
{
    public class SelectionViewModel : ViewModel
    {
        public IList? SelectedItems
        {
            get => Get<IList?>();
            set => Set(value);
        }

        public string? Text
        {
            get => Get<string?>();
            set => Set(value);
        }

        public bool IsChecked
        {
            get => Get<bool>();
            set => Set(value);
        }
    }

    private static ListBox CreateListBoxWithSelection()
    {
        ListBox listBox = new();
        listBox.Items.Add("a");
        listBox.Items.Add("b");
        listBox.SelectedIndex = 1;
        return listBox;
    }

    [Fact]
    public void Attach_TextBoxText_WritesCurrentTextToViewModel()
    {
        TextBox textBox = new() { Text = "typed" };
        SelectionViewModel viewModel = new();
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(SelectionViewModel.Text)) { Mode = BindingMode.OneWayToSource });

        textBox.BindingContext = viewModel;

        viewModel.Text.ShouldBe("typed");
    }

    [Fact]
    public void Attach_CheckBoxIsChecked_WritesCurrentValueToViewModel()
    {
        CheckBox checkBox = new() { IsChecked = true };
        SelectionViewModel viewModel = new();
        checkBox.SetBinding(nameof(CheckBox.IsChecked),
            new Binding(nameof(SelectionViewModel.IsChecked)) { Mode = BindingMode.OneWayToSource });

        checkBox.BindingContext = viewModel;

        viewModel.IsChecked.ShouldBeTrue();
    }

    [Fact]
    public void Attach_ListBoxSelectedItems_GivesViewModelTheSelectionCollection()
    {
        ListBox listBox = CreateListBoxWithSelection();
        SelectionViewModel viewModel = new();
        listBox.SetBinding(nameof(ListBox.SelectedItems),
            new Binding(nameof(SelectionViewModel.SelectedItems)) { Mode = BindingMode.OneWayToSource });

        listBox.BindingContext = viewModel;

        viewModel.SelectedItems.ShouldBeSameAs(listBox.SelectedItems);
    }

    [Fact]
    public void Attach_ListBoxSelectedItems_ViewModelSeesLaterSelectionChangesThroughLiveCollection()
    {
        ListBox listBox = CreateListBoxWithSelection();
        SelectionViewModel viewModel = new();
        listBox.SetBinding(nameof(ListBox.SelectedItems),
            new Binding(nameof(SelectionViewModel.SelectedItems)) { Mode = BindingMode.OneWayToSource });
        listBox.BindingContext = viewModel;

        listBox.SelectedIndex = 0;

        viewModel.SelectedItems.ShouldNotBeNull();
        viewModel.SelectedItems.Cast<object>().ShouldBe(new object[] { "a" });
    }

    [Fact]
    public void Attach_BindingContextSetBeforeSetBinding_WritesCurrentValueToViewModel()
    {
        SelectionViewModel viewModel = new() { Text = "from view model" };
        TextBox textBox = new() { Text = "typed", BindingContext = viewModel };

        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(SelectionViewModel.Text)) { Mode = BindingMode.OneWayToSource });

        viewModel.Text.ShouldBe("typed");
        textBox.Text.ShouldBe("typed");
    }

    [Fact]
    public void Attach_InheritedBindingContext_WritesCurrentValueToViewModel()
    {
        StackPanel parent = new();
        CheckBox checkBox = new() { IsChecked = true };
        parent.AddChild(checkBox);
        SelectionViewModel viewModel = new();
        checkBox.SetBinding(nameof(CheckBox.IsChecked),
            new Binding(nameof(SelectionViewModel.IsChecked)) { Mode = BindingMode.OneWayToSource });

        parent.BindingContext = viewModel;

        viewModel.IsChecked.ShouldBeTrue();
    }

    [Fact]
    public void Attach_NewBindingContext_WritesCurrentValueToNewViewModelOnly()
    {
        TextBox textBox = new() { Text = "first" };
        SelectionViewModel firstViewModel = new();
        SelectionViewModel secondViewModel = new();
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(SelectionViewModel.Text)) { Mode = BindingMode.OneWayToSource });
        textBox.BindingContext = firstViewModel;
        textBox.Text = "second";

        textBox.BindingContext = secondViewModel;

        firstViewModel.Text.ShouldBe("second");
        secondViewModel.Text.ShouldBe("second");
    }

    [Fact]
    public void Attach_WithConverter_WritesConvertedBackValue()
    {
        TextBox textBox = new() { Text = "typed" };
        SelectionViewModel viewModel = new();
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(SelectionViewModel.Text))
            {
                Mode = BindingMode.OneWayToSource,
                Converter = new UpperCaseBackConverter()
            });

        textBox.BindingContext = viewModel;

        viewModel.Text.ShouldBe("TYPED");
    }

    [Fact]
    public void Attach_OneWay_DoesNotWriteToViewModel()
    {
        TextBox textBox = new() { Text = "typed" };
        SelectionViewModel viewModel = new() { Text = "from view model" };
        textBox.SetBinding(nameof(TextBox.Text),
            new Binding(nameof(SelectionViewModel.Text)) { Mode = BindingMode.OneWay });

        textBox.BindingContext = viewModel;

        viewModel.Text.ShouldBe("from view model");
        textBox.Text.ShouldBe("from view model");
    }

    private class UpperCaseBackConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) => value;

        public object? ConvertBack(object? value, Type sourceType, object? parameter) =>
            (value as string)?.ToUpperInvariant();
    }
}
