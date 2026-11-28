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
/// Text written back to a nullable numeric source (int?, float?) converts to the underlying type, as WPF's
/// binding does, including group separators shown by a StringFormat such as "{0:N0}".
/// </summary>
public class NullableNumberWriteBackTests : BaseTestClass
{
    private readonly Action<string>? _previousReporter;
    private readonly CultureInfo _previousCulture;
    private readonly List<string> _reports = new();

    public NullableNumberWriteBackTests()
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

    private class LimitViewModel : ViewModel
    {
        public int? Limit
        {
            get => Get<int?>();
            set => Set(value);
        }

        public float? Scale
        {
            get => Get<float?>();
            set => Set(value);
        }
    }

    [Fact]
    public void NullableIntSource_TypedNumber_WritesNumber()
    {
        LimitViewModel vm = new() { Limit = 3 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(LimitViewModel.Limit));
        textBox.Text.ShouldBe("3");

        textBox.Text = "5";

        vm.Limit.ShouldBe(5);
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void NullableIntSource_N0FormattedText_WritesNumber()
    {
        LimitViewModel vm = new() { Limit = 1234 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(LimitViewModel.Limit)) { StringFormat = "{0:N0}" });
        textBox.Text.ShouldBe("1,234");

        textBox.Text = "1,235";

        vm.Limit.ShouldBe(1235);
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void NullableFloatSource_TypedNumber_WritesNumber()
    {
        LimitViewModel vm = new() { Scale = 1 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(LimitViewModel.Scale));

        textBox.Text = "2.5";

        vm.Scale.ShouldBe(2.5f);
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void NullableIntSource_TextMatchingTargetNullValue_WritesNull()
    {
        LimitViewModel vm = new() { Limit = 3 };
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), new Binding(nameof(LimitViewModel.Limit)) { TargetNullValue = "" });

        textBox.Text = "";

        vm.Limit.ShouldBeNull();
        _reports.ShouldBeEmpty();
    }
}
