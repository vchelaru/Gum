using System;
using System.Collections.Generic;
using System.Globalization;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Text written back to a numeric source accepts group separators, so a number shown with a StringFormat such as
/// "{0:N0}" round-trips. Text that still does not parse (a currency symbol, letters) leaves the source unchanged
/// and is reported once per binding.
/// </summary>
public class FormattedNumberWriteBackTests : BaseTestClass
{
    private readonly Action<string>? _previousReporter;
    private readonly CultureInfo _previousCulture;
    private readonly List<string> _reports = new();

    public FormattedNumberWriteBackTests()
    {
        _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
        ElementSaveExtensions.ReportPropertyAssignmentError = _reports.Add;
        _previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    public override void Dispose()
    {
        CultureInfo.CurrentCulture = _previousCulture;
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        base.Dispose();
    }

    private class CountViewModel : ViewModel
    {
        public int Count
        {
            get => Get<int>();
            set => Set(value);
        }
    }

    private class ScoreViewModel : ViewModel
    {
        public int Count
        {
            get => Get<int>();
            set => Set(value);
        }
    }

    [Fact]
    public void IntSource_N0Format_EditedFormattedText_WritesNumber()
    {
        CountViewModel vm = new() { Count = 1234 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(CountViewModel.Count)) { StringFormat = "{0:N0}" });
        textBox.Text.ShouldBe("1,234");

        textBox.Text = "1,235";

        vm.Count.ShouldBe(1235);
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void IntSource_CurrencyFormat_EditedText_LeavesSourceUnchangedAndReportsOnce()
    {
        CountViewModel vm = new() { Count = 1234 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(CountViewModel.Count)) { StringFormat = "{0:C0}" });
        textBox.Text.ShouldBe("$1,234");

        textBox.Text = "$1,235";
        textBox.Text = "$1,236";

        vm.Count.ShouldBe(1234);
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void Typing_TransientTextThenNumber_WritesNumberWithoutReport()
    {
        CountViewModel vm = new() { Count = 5 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(CountViewModel.Count));
        textBox.IsFocused = true;

        textBox.Text = "";
        textBox.Text = "-";
        textBox.Text = "-7";
        textBox.IsFocused = false;

        vm.Count.ShouldBe(-7);
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void Typing_TextThatDoesNotParse_ReportsOnlyWhenEditingEnds()
    {
        CountViewModel vm = new() { Count = 5 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(CountViewModel.Count));
        textBox.IsFocused = true;

        textBox.Text = "abc";
        _reports.ShouldBeEmpty();
        textBox.IsFocused = false;

        vm.Count.ShouldBe(5);
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void Typing_EmptyTextAtEditEnd_LeavesSourceUnchangedAndReports()
    {
        CountViewModel vm = new() { Count = 5 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(CountViewModel.Count));
        textBox.IsFocused = true;

        textBox.Text = "";
        textBox.IsFocused = false;

        vm.Count.ShouldBe(5);
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void LostFocusTrigger_TextThatDoesNotParse_ReportsAtEditEnd()
    {
        CountViewModel vm = new() { Count = 5 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(CountViewModel.Count))
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        });
        textBox.IsFocused = true;

        textBox.Text = "abc";
        _reports.ShouldBeEmpty();
        textBox.IsFocused = false;

        vm.Count.ShouldBe(5);
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void IntSource_TextThatDoesNotParse_LeavesSourceUnchangedAndReportsOnce()
    {
        CountViewModel vm = new() { Count = 5 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(CountViewModel.Count));

        textBox.Text = "abc";
        textBox.Text = "abcd";

        vm.Count.ShouldBe(5);
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void IntSource_TextThatDoesNotParse_OnSourcesOfTwoTypes_ReportsOncePerType()
    {
        TextBox textBox = new() { BindingContext = new CountViewModel() };
        textBox.SetBinding(nameof(TextBox.Text), nameof(CountViewModel.Count));

        textBox.Text = "abc";
        textBox.BindingContext = new ScoreViewModel();
        textBox.Text = "def";
        textBox.BindingContext = new CountViewModel();
        textBox.Text = "ghi";

        _reports.Count.ShouldBe(2);
        _reports[0].ShouldContain("for CountViewModel.Count");
        _reports[1].ShouldContain("for ScoreViewModel.Count");
    }
}
