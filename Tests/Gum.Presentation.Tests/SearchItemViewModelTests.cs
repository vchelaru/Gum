using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for SearchItemViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754).
/// </summary>
public class SearchItemViewModelTests
{
    [Fact]
    public void Display_FallsBackToBackingObjectToString_WhenCustomTextIsEmpty()
    {
        SearchItemViewModel viewModel = new() { BackingObject = 42 };

        viewModel.Display.ShouldBe("42");
    }

    [Fact]
    public void Display_ReturnsCustomText_WhenCustomTextIsSet()
    {
        SearchItemViewModel viewModel = new() { CustomText = "custom", BackingObject = "backing" };

        viewModel.Display.ShouldBe("custom");
    }

    [Fact]
    public void Display_ReturnsEmptyString_WhenCustomTextAndBackingObjectAreBothUnset()
    {
        SearchItemViewModel viewModel = new();

        viewModel.Display.ShouldBe(string.Empty);
    }
}
