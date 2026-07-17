using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for GetUserStringDialogViewModel (and its
/// GetUserStringDialogBaseViewModel base), relocated out of Gum.csproj into the headless
/// Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM with zero injected interfaces.
/// </summary>
public class GetUserStringDialogViewModelTests
{
    [Fact]
    public void Constructor_AppliesOptions()
    {
        GetUserStringDialogViewModel viewModel = new(new GetUserStringOptions
        {
            InitialValue = "Foo",
            PreSelect = true,
        });

        viewModel.Value.ShouldBe("Foo");
        viewModel.PreSelect.ShouldBeTrue();
    }

    [Fact]
    public void Validate_UsesCustomValidator_WhenProvided()
    {
        GetUserStringDialogViewModel viewModel = new(new GetUserStringOptions
        {
            Validator = value => value == "Taken" ? "Name already in use." : null,
        });

        viewModel.Value = "Taken";
        viewModel.Validate();

        viewModel.Error.ShouldBe("Name already in use.");
    }

    [Fact]
    public void Validate_FallsBackToEnsureNotEmpty_WhenNoValidatorProvided()
    {
        GetUserStringDialogViewModel viewModel = new();

        viewModel.Value = "";
        viewModel.Validate();

        viewModel.Error.ShouldBe("Cannot be empty.");
    }

    [Fact]
    public void CanExecuteAffirmative_IsFalse_WhileErrorIsSet()
    {
        GetUserStringDialogViewModel viewModel = new();

        viewModel.Value = "";
        viewModel.Validate();

        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }
}
