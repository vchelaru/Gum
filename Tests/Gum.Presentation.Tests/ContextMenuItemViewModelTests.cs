using Gum.ViewModels;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for ContextMenuItemViewModel, relocated out of Gum.csproj
/// into the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM with zero
/// injected interfaces.
/// </summary>
public class ContextMenuItemViewModelTests
{
    [Fact]
    public void Defaults_AreEmptyNonSeparatorWithNoChildren()
    {
        ContextMenuItemViewModel viewModel = new();

        viewModel.Text.ShouldBe(string.Empty);
        viewModel.Action.ShouldBeNull();
        viewModel.IsSeparator.ShouldBeFalse();
        viewModel.Children.ShouldBeEmpty();
    }
}
