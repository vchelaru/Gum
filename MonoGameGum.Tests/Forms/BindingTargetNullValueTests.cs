using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// TargetNullValue is what the target shows when the source value is null.
/// </summary>
public class BindingTargetNullValueTests : BaseTestClass
{
    private class CaptionViewModel : ViewModel
    {
        public string? Caption { get => Get<string?>(); set => Set(value); }
    }

    [Fact]
    public void SourceNull_WithTargetNullValue_TargetShowsTargetNullValue()
    {
        CaptionViewModel viewModel = new() { Caption = null };
        Label label = new() { BindingContext = viewModel };

        label.SetBinding(nameof(Label.Text), new Binding(nameof(CaptionViewModel.Caption)) { TargetNullValue = "(none)" });

        label.Text.ShouldBe("(none)");
    }

    [Fact]
    public void SourceChangesToNull_WithTargetNullValue_TargetShowsTargetNullValue()
    {
        CaptionViewModel viewModel = new() { Caption = "Sword" };
        Label label = new() { BindingContext = viewModel };
        label.SetBinding(nameof(Label.Text), new Binding(nameof(CaptionViewModel.Caption)) { TargetNullValue = "(none)" });

        viewModel.Caption = null;

        label.Text.ShouldBe("(none)");
    }
}
