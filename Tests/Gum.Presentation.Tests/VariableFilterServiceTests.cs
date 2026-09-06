using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.VariableGrid;
using Gum.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class VariableFilterServiceTests
{
    [Fact]
    public void MainControlViewModel_ShouldHideFilterWatermarkOnceTextIsTyped()
    {
        MainControlViewModel viewModel = new MainControlViewModel(
            new Mock<IDeleteVariableService>().Object,
            new Mock<IEditVariableService>().Object);

        viewModel.VariableFilterText.ShouldBe("");
        viewModel.IsFilterWatermarkVisible.ShouldBeTrue();

        viewModel.VariableFilterText = "vis";

        viewModel.IsFilterWatermarkVisible.ShouldBeFalse();
    }

    private VariableFilterService CreateService() => new();

    [Fact]
    public void HasFilter_ShouldBeFalseForNullOrWhitespace()
    {
        VariableFilterService service = CreateService();

        service.HasFilter(null).ShouldBeFalse();
        service.HasFilter("").ShouldBeFalse();
        service.HasFilter("   ").ShouldBeFalse();
        service.HasFilter("vis").ShouldBeTrue();
    }

    [Fact]
    public void IsMatch_ShouldMatchCaseInsensitiveSubstringOfNameOrDisplayName()
    {
        VariableFilterService service = CreateService();

        // Substring, not prefix: "let" has to find "MaxLettersToShow".
        service.IsMatch("VIS", "Visible", displayName: null).ShouldBeTrue();
        service.IsMatch("let", "MaxLettersToShow", displayName: null).ShouldBeTrue();

        // DisplayName can differ from Name, so a match on either one keeps the row.
        service.IsMatch("width", "Width", displayName: "Texture Width").ShouldBeTrue();
        service.IsMatch("texture", "Width", displayName: "Texture Width").ShouldBeTrue();
    }

    [Fact]
    public void IsMatch_ShouldRejectNamesNotContainingTheFilter()
    {
        VariableFilterService service = CreateService();

        service.IsMatch("visible", "X", displayName: null).ShouldBeFalse();
        service.IsMatch("zzz", "Width", displayName: "Texture Width").ShouldBeFalse();
    }

    [Fact]
    public void IsMatch_ShouldTrimFilterAndTreatBlankAsMatchEverything()
    {
        VariableFilterService service = CreateService();

        // A blank box is not a filter, so every row survives it.
        service.IsMatch(null, "Visible", displayName: null).ShouldBeTrue();
        service.IsMatch("", "Visible", displayName: null).ShouldBeTrue();
        service.IsMatch("   ", "Visible", displayName: null).ShouldBeTrue();

        // Trailing space from typing must not make a real filter stop matching.
        service.IsMatch(" vis ", "Visible", displayName: null).ShouldBeTrue();
    }
}
