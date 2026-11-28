using System;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A StringFormat that string.Format can never apply fails at the SetBinding call that supplied it, not later
/// when a BindingContext arrives or the value changes.
/// </summary>
public class BindingStringFormatValidationTests : BaseTestClass
{
    private class GoldViewModel : ViewModel
    {
        public int Gold
        {
            get => Get<int>();
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
    public void SpecifierTheValueTypeRejects_ContextAssigned_DoesNotThrowFromContextSetter()
    {
        Label label = new();
        label.SetBinding(nameof(Label.Text), new Binding(nameof(GoldViewModel.Gold)) { StringFormat = "{0:Q}" });

        Should.NotThrow(() => label.BindingContext = new GoldViewModel { Gold = 5 });
    }
}
