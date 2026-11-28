using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A ConvertBack that throws leaves the source unchanged, as WPF does, and is reported once per binding. It
/// never writes FallbackValue or null to the view model.
/// </summary>
public class ConvertBackFailureTests : BaseTestClass
{
    private readonly Action<string>? _previousReporter;
    private readonly List<string> _reports = new();

    public ConvertBackFailureTests()
    {
        _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
        ElementSaveExtensions.ReportPropertyAssignmentError = _reports.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        base.Dispose();
    }

    private class ThrowingBackConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) => value;

        public object? ConvertBack(object? value, Type sourceType, object? parameter) =>
            throw new InvalidOperationException("convert back failed");
    }

    private class NameViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    [Fact]
    public void ConvertBack_Throws_WithFallbackValue_LeavesSourceUnchanged()
    {
        NameViewModel vm = new() { Name = "original" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(NameViewModel.Name))
        {
            Converter = new ThrowingBackConverter(),
            FallbackValue = "fallback"
        });

        textBox.Text = "typed";

        vm.Name.ShouldBe("original");
    }

    [Fact]
    public void ConvertBack_Throws_WithoutFallbackValue_LeavesSourceUnchanged()
    {
        NameViewModel vm = new() { Name = "original" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(NameViewModel.Name))
        {
            Converter = new ThrowingBackConverter()
        });

        textBox.Text = "typed";

        vm.Name.ShouldBe("original");
    }

    [Fact]
    public void ConvertBack_Throws_ReportsOnce()
    {
        NameViewModel vm = new() { Name = "original" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(NameViewModel.Name))
        {
            Converter = new ThrowingBackConverter()
        });

        textBox.Text = "typed";
        textBox.Text = "typed again";

        _reports.Count.ShouldBe(1);
    }
}
