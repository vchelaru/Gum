using System.Collections.Generic;
using ImportFromGumxPlugin.ViewModels;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for StandardDiffDetailsViewModel, relocated out of
/// ImportFromGumxPlugin.csproj into the headless Gum.Presentation assembly (ADR-0005, #3754). Its
/// View stays in the (dynamically-loaded) ImportFromGumxPlugin assembly - see
/// DialogViewResolverTests (GumToolUnitTests) for the cross-assembly resolution pin, exercised
/// there specifically because it's a third assembly distinct from both this view model's own
/// assembly and the main Gum tool assembly.
/// </summary>
public class StandardDiffDetailsViewModelTests
{
    [Fact]
    public void Constructor_SetsStandardNameAndRows_AndDisablesCancel()
    {
        List<StandardDiffRowViewModel> rows = new()
        {
            new StandardDiffRowViewModel("Changed", "Rotation · SetsValue: True → False"),
        };

        StandardDiffDetailsViewModel viewModel = new("MyStandard", rows);

        viewModel.StandardName.ShouldBe("MyStandard");
        viewModel.Rows.ShouldBe(rows);
        viewModel.AffirmativeText.ShouldBe("Close");
        viewModel.NegativeText.ShouldBeNull();
    }
}
