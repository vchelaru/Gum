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
/// A Convert that throws is reported once per binding through the [Gum] channel, whether or not a FallbackValue
/// hides it, instead of silently showing the target's default.
/// </summary>
public class ConverterExceptionReportTests : BaseTestClass
{
    private readonly Action<string>? _previousReporter;
    private readonly List<string> _reports = new();

    public ConverterExceptionReportTests()
    {
        _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
        ElementSaveExtensions.ReportPropertyAssignmentError = _reports.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        base.Dispose();
    }

    private class ThrowingConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) =>
            throw new InvalidOperationException("convert failed");

        public object? ConvertBack(object? value, Type sourceType, object? parameter) => value;
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
    public void Convert_Throws_WithoutFallbackValue_ReportsOnce()
    {
        NameViewModel vm = new() { Name = "a" };
        Label label = new() { BindingContext = vm };

        label.SetBinding(nameof(Label.Text), new Binding(nameof(NameViewModel.Name))
        {
            Converter = new ThrowingConverter()
        });
        vm.Name = "b";

        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void Convert_Throws_WithFallbackValue_ReportsOnce()
    {
        NameViewModel vm = new() { Name = "a" };
        Label label = new() { BindingContext = vm };

        label.SetBinding(nameof(Label.Text), new Binding(nameof(NameViewModel.Name))
        {
            Converter = new ThrowingConverter(),
            FallbackValue = "?"
        });
        vm.Name = "b";

        label.Text.ShouldBe("?");
        _reports.Count.ShouldBe(1);
    }

    private class OtherNameViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    [Fact]
    public void Convert_Throws_OnContextsOfTwoTypes_ReportsOncePerType()
    {
        Label label = new();
        label.SetBinding(nameof(Label.Text), new Binding(nameof(NameViewModel.Name))
        {
            Converter = new ThrowingConverter()
        });

        label.BindingContext = new NameViewModel { Name = "a" };
        label.BindingContext = new OtherNameViewModel { Name = "b" };
        label.BindingContext = new NameViewModel { Name = "c" };

        _reports.Count.ShouldBe(2);
    }

    private class BothWaysThrowingConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) =>
            throw new InvalidOperationException("convert failed");

        public object? ConvertBack(object? value, Type sourceType, object? parameter) =>
            throw new InvalidOperationException("convert back failed");
    }

    [Fact]
    public void ConvertAndConvertBackThrow_ReportsOncePerDirection()
    {
        NameViewModel vm = new() { Name = "a" };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(NameViewModel.Name))
        {
            Converter = new BothWaysThrowingConverter()
        });

        textBox.Text = "b";
        textBox.Text = "c";
        vm.Name = "d";

        vm.Name.ShouldBe("d");
        _reports.Count.ShouldBe(2);
        _reports.ShouldContain(r => r.Contains("BothWaysThrowingConverter.Convert threw"));
        _reports.ShouldContain(r => r.Contains("BothWaysThrowingConverter.ConvertBack threw"));
    }

    [Fact]
    public void Convert_ThrowsOnTwoBindings_ReportsOncePerBinding()
    {
        NameViewModel vm = new() { Name = "a" };
        Label first = new() { BindingContext = vm };
        Label second = new() { BindingContext = vm };

        first.SetBinding(nameof(Label.Text), new Binding(nameof(NameViewModel.Name)) { Converter = new ThrowingConverter() });
        second.SetBinding(nameof(Label.Text), new Binding(nameof(NameViewModel.Name)) { Converter = new ThrowingConverter() });
        vm.Name = "b";

        _reports.Count.ShouldBe(2);
    }

    private class ThrowOnValueConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) =>
            Equals(value, "bad") ? throw new InvalidOperationException("bad value") : value;

        public object? ConvertBack(object? value, Type sourceType, object? parameter) => value;
    }

    [Fact]
    public void Convert_ThrowsOnLaterValue_ShowsFallbackValueAndReports()
    {
        NameViewModel vm = new() { Name = "good" };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), new Binding(nameof(NameViewModel.Name))
        {
            Converter = new ThrowOnValueConverter(),
            FallbackValue = "?"
        });
        label.Text.ShouldBe("good");
        _reports.ShouldBeEmpty();

        vm.Name = "bad";

        label.Text.ShouldBe("?");
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void VisualBinding_ConvertThrows_ReportsOnce()
    {
        NameViewModel vm = new() { Name = "a" };
        Gum.GueDeriving.TextRuntime text = new() { BindingContext = vm };

        text.SetBinding(nameof(Gum.GueDeriving.TextRuntime.Text), new Binding(nameof(NameViewModel.Name))
        {
            Converter = new ThrowingConverter()
        });
        vm.Name = "b";

        _reports.Count.ShouldBe(1);
    }
}
