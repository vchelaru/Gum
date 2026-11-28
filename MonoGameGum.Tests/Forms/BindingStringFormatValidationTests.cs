using System;
using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.GueDeriving;
using Gum.Mvvm;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A StringFormat that string.Format can never apply fails at the SetBinding call that supplied it, not later
/// when a BindingContext arrives or the value changes. A specifier the value's type rejects ({0:Q} on an int) fails
/// at SetBinding when the source member's type is known then; otherwise it is reported once and the target shows
/// its default (or FallbackValue), never throwing from a BindingContext setter or a PropertyChanged raise.
/// </summary>
public class BindingStringFormatValidationTests : BaseTestClass
{
    private readonly Action<string>? _previousReporter;
    private readonly List<string> _reports = new();

    public BindingStringFormatValidationTests()
    {
        _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
        ElementSaveExtensions.ReportPropertyAssignmentError = _reports.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        base.Dispose();
    }

    private class GoldViewModel : ViewModel
    {
        public int Gold
        {
            get => Get<int>();
            set => Set(value);
        }

        public int? NullableGold
        {
            get => Get<int?>();
            set => Set(value);
        }

        public object? Anything
        {
            get => Get<object?>();
            set => Set(value);
        }

        public DateTime When
        {
            get => Get<DateTime>();
            set => Set(value);
        }
    }

    [Theory]
    [InlineData("{0} of {1}")]
    [InlineData("Gold: {0:N0")]
    [InlineData("Gold: {0}}")]
    public void SetBinding_StringFormatStringFormatRejects_ThrowsAtSetBinding(string format)
    {
        Label label = new();

        Should.Throw<Exception>(() =>
            label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = format }));
    }

    [Fact]
    public void SetBinding_StringFormatStringFormatRejects_ContextAssignedLater_DoesNotThrowFromContextSetter()
    {
        Label label = new();
        try
        {
            label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "{0} of {1}" });
        }
        catch (Exception)
        {
            // Refused at SetBinding, which is where the error belongs.
            return;
        }

        Should.NotThrow(() => label.BindingContext = new GoldViewModel { Gold = 5 });
    }

    [Fact]
    public void SetBinding_ValidStringFormat_DoesNotThrow()
    {
        Label label = new() { BindingContext = new GoldViewModel { Gold = 1234 } };

        label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "Gold: {0:N0}" });

        label.Text.ShouldBe("Gold: 1,234");
    }

    [Fact]
    public void VisualSetBinding_CompositeFormatPassedAsSpecifier_IsUsedAsIs()
    {
        // The Forms overload takes "{0:N0}"; a user moving a binding to a visual passes the same string.
        TextRuntime text = new();

        text.SetBinding(nameof(TextRuntime.Text), nameof(GoldViewModel.Gold), "{0:N0}");
        text.BindingContext = new GoldViewModel { Gold = 1234 };

        text.Text.ShouldBe("1,234");
    }

    [Fact]
    public void VisualSetBinding_NeitherSpecifierNorCompositeFormat_ThrowsArgumentException()
    {
        TextRuntime text = new();

        Should.Throw<ArgumentException>(() =>
            text.SetBinding(nameof(TextRuntime.Text), nameof(GoldViewModel.Gold), "Gold: {0:N0"));
    }

    [Fact]
    public void VisualSetBinding_InvalidSpecifier_FailsAtSetBinding()
    {
        TextRuntime text = new();
        try
        {
            text.SetBinding(nameof(TextRuntime.Text), nameof(GoldViewModel.Gold), "N0}");
        }
        catch (Exception)
        {
            return;
        }

        Should.NotThrow(() => text.BindingContext = new GoldViewModel { Gold = 1234 });
    }

    [Fact]
    public void SetBinding_EscapedPlaceholderOnly_ThrowsAtSetBinding()
    {
        Label label = new();

        Should.Throw<InvalidOperationException>(() =>
            label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "{{0}}" }));
    }

    [Fact]
    public void SetBinding_AlignedPlaceholder_ShowsValue()
    {
        Label label = new() { BindingContext = new GoldViewModel { Gold = 12 } };

        label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "[{0,4}]" });

        label.Text.ShouldBe("[  12]");
    }

    [Fact]
    public void VisualSetBinding_BindingObjectWithBadStringFormat_ThrowsAtSetBinding()
    {
        TextRuntime text = new();

        Should.Throw<InvalidOperationException>(() =>
            text.SetBinding(nameof(TextRuntime.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "{0} of {1}" }));
    }

    [Fact]
    public void SetBinding_RefusedFormat_LeavesPreviousBindingWorking()
    {
        GoldViewModel vm = new() { Gold = 1 };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), nameof(GoldViewModel.Gold));

        Should.Throw<InvalidOperationException>(() =>
            label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "{1}" }));
        vm.Gold = 2;

        label.Text.ShouldBe("2");
    }

    [Fact]
    public void SpecifierTheValueTypeRejects_ContextAssigned_DoesNotThrowFromContextSetter()
    {
        Label label = new();
        label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "{0:Q}" });

        Should.NotThrow(() => label.BindingContext = new GoldViewModel { Gold = 5 });
    }

    [Fact]
    public void SpecifierTheValueTypeRejects_ContextAssignedLater_ReportsOnceAndShowsDefault()
    {
        GoldViewModel vm = new() { Gold = 5 };
        Label label = new();
        label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "{0:Q}" });

        label.BindingContext = vm;
        vm.Gold = 6;

        label.Text.ShouldBeNullOrEmpty();
        _reports.Count.ShouldBe(1);
        _reports[0].ShouldContain("{0:Q}");
    }

    [Fact]
    public void SpecifierTheValueTypeRejects_ObjectTypedSource_DoesNotThrowFromPropertyChanged()
    {
        GoldViewModel vm = new() { Anything = "text" };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Anything)) { StringFormat = "{0:Q}" });

        Should.NotThrow(() => vm.Anything = 5);
        label.Text.ShouldBeNullOrEmpty();
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void SpecifierTheValueTypeRejects_ContextAssignedLater_WithFallbackValue_ShowsFallbackAndReportsOnce()
    {
        GoldViewModel vm = new() { Gold = 5 };
        Label label = new();
        label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold))
        {
            StringFormat = "{0:Q}",
            FallbackValue = "n/a"
        });

        label.BindingContext = vm;
        vm.Gold = 6;

        label.Text.ShouldBe("n/a");
        _reports.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(nameof(GoldViewModel.Gold))]
    [InlineData(nameof(GoldViewModel.NullableGold))]
    public void SpecifierTheValueTypeRejects_ContextAlreadySet_ThrowsAtSetBinding(string path)
    {
        Label label = new() { BindingContext = new GoldViewModel { Gold = 5, NullableGold = 5 } };

        Should.Throw<InvalidOperationException>(() =>
            label.SetBinding(nameof(Label.Text), new Binding(path) { StringFormat = "{0:Q}" }));
    }

    [Fact]
    public void SpecifierTheValueTypeRejects_ContextAlreadySet_WithFallbackValue_ShowsFallbackAndReportsOnce()
    {
        Label label = new() { BindingContext = new GoldViewModel { Gold = 5 } };

        label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold))
        {
            StringFormat = "{0:Q}",
            FallbackValue = "n/a"
        });

        label.Text.ShouldBe("n/a");
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void SpecifierTheValueTypeRejects_ContextAlreadySet_LeavesPreviousBindingWorking()
    {
        GoldViewModel vm = new() { Gold = 1 };
        Label label = new() { BindingContext = vm };
        label.SetBinding(nameof(Label.Text), nameof(GoldViewModel.Gold));

        Should.Throw<InvalidOperationException>(() =>
            label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "{0:Q}" }));
        vm.Gold = 2;

        label.Text.ShouldBe("2");
    }

    [Fact]
    public void SpecifierTheValueTypeAccepts_ContextAlreadySet_ShowsFormattedValue()
    {
        Label label = new() { BindingContext = new GoldViewModel { When = new DateTime(2026, 12, 1) } };

        label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.When)) { StringFormat = "{0:yyyy-MM-dd}" });

        label.Text.ShouldBe("2026-12-01");
    }

    [Fact]
    public void VisualSetBinding_SpecifierTheValueTypeRejects_ContextAlreadySet_ThrowsAtSetBinding()
    {
        TextRuntime text = new() { BindingContext = new GoldViewModel { Gold = 5 } };

        Should.Throw<InvalidOperationException>(() =>
            text.SetBinding(nameof(TextRuntime.Text), nameof(GoldViewModel.Gold), "Q"));
    }

    [Fact]
    public void VisualSetBinding_Lambda_SpecifierTheValueTypeRejects_ThrowsAtSetBindingWithoutContext()
    {
        TextRuntime text = new();

        Should.Throw<InvalidOperationException>(() =>
            text.SetBinding<GoldViewModel>(nameof(TextRuntime.Text), vm => vm.Gold, "Q"));
    }

    [Fact]
    public void VisualSetBinding_SpecifierTheValueTypeRejects_ContextAssignedLater_ReportsOnce()
    {
        GoldViewModel vm = new() { Gold = 5 };
        TextRuntime text = new();
        text.SetBinding(nameof(TextRuntime.Text), nameof(GoldViewModel.Gold), "Q");

        Should.NotThrow(() => text.BindingContext = vm);
        Should.NotThrow(() => vm.Gold = 6);
        _reports.Count.ShouldBe(1);
    }

    private class PassThroughConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) => value;

        public object? ConvertBack(object? value, Type sourceType, object? parameter) => value;
    }

    [Fact]
    public void SpecifierTheValueTypeRejects_WithConverter_ReportsOnceInsteadOfThrowingAtSetBinding()
    {
        GoldViewModel vm = new() { Gold = 5 };
        Label label = new() { BindingContext = vm };

        Should.NotThrow(() => label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold))
        {
            StringFormat = "{0:Q}",
            Converter = new PassThroughConverter()
        }));
        vm.Gold = 6;

        label.Text.ShouldBeNullOrEmpty();
        _reports.Count.ShouldBe(1);
    }

    [Fact]
    public void SpecifierTheValueTypeRejects_OneWayToSource_DoesNotThrowAtSetBinding()
    {
        Label label = new() { BindingContext = new GoldViewModel { Gold = 5 } };

        Should.NotThrow(() => label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold))
        {
            StringFormat = "{0:Q}",
            Mode = BindingMode.OneWayToSource
        }));
    }

    [Fact]
    public void SpecifierTheValueTypeRejects_NonStringTarget_IsNotApplied()
    {
        Label label = new() { BindingContext = new GoldViewModel { Gold = 5 } };

        label.SetBinding(nameof(Label.Width), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "{0:Q}" });

        label.Width.ShouldBe(5);
    }

    [Fact]
    public void VisualSetBinding_Lambda_NonStringTarget_DoesNotThrow()
    {
        TextRuntime text = new() { BindingContext = new GoldViewModel { Gold = 5 } };

        text.SetBinding<GoldViewModel>(nameof(TextRuntime.Width), vm => vm.Gold, "Q");

        text.Width.ShouldBe(5);
    }

    [Fact]
    public void DateFormat_DefaultDateOutsideCultureCalendar_DoesNotThrowAtSetBinding()
    {
        // The probe formats default(DateTime), which the Um Al Qura calendar cannot show; that says nothing about
        // the format, so SetBinding accepts it and the real date formats normally.
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo culture = (System.Globalization.CultureInfo)
            new System.Globalization.CultureInfo("ar-SA").Clone();
        culture.DateTimeFormat.Calendar = new System.Globalization.UmAlQuraCalendar();
        Should.Throw<ArgumentOutOfRangeException>(() => string.Format(culture, "{0:d}", default(DateTime)));
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
            Label label = new() { BindingContext = new GoldViewModel { When = new DateTime(2026, 12, 1) } };

            Should.NotThrow(() =>
                label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.When)) { StringFormat = "{0:d}" }));
            label.Text.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
