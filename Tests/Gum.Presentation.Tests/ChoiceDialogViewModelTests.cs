using System.Collections.Generic;
using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for ChoiceDialogViewModel, relocated out of Gum.csproj into
/// the headless Gum.Presentation assembly (ADR-0005, #3754). Its View stays in the Gum tool
/// assembly, paired via [Dialog(typeof(ChoiceDialogViewModel))] on ChoiceDialogView - see
/// DialogViewResolverTests (GumToolUnitTests) for the cross-assembly resolution pin.
/// </summary>
public class ChoiceDialogViewModelTests
{
    [Fact]
    public void SetOptions_PopulatesOptionValues_AndDefaultsSelectedValueToFirst()
    {
        ChoiceDialogViewModel viewModel = new();

        DialogChoices<string> choices = new()
        {
            ["reference-current"] = "Reference the file in its current location",
            ["copy-relative"] = "Copy the file relative to the project",
        };
        viewModel.SetOptions(choices);

        viewModel.OptionValues.ShouldBe(new[]
        {
            "Reference the file in its current location",
            "Copy the file relative to the project",
        });
        viewModel.SelectedValue.ShouldBe("Reference the file in its current location");
    }

    [Fact]
    public void SelectedKey_ReturnsTheKeyMatchingSelectedValue()
    {
        ChoiceDialogViewModel viewModel = new();
        viewModel.SetOptions(new DialogChoices<string>
        {
            ["yes"] = "Yes, overwrite the file",
            ["no"] = "No, use the original file",
        });

        viewModel.SelectedValue = "No, use the original file";

        viewModel.SelectedKey.ShouldBe("no");
    }

    [Fact]
    public void CanCancel_True_SetsNegativeTextToCancel()
    {
        ChoiceDialogViewModel viewModel = new() { CanCancel = true };

        viewModel.NegativeText.ShouldBe("Cancel");
    }

    [Fact]
    public void OnNegative_ClearsSelectedValue_WhenCanCancelIsTrue()
    {
        ChoiceDialogViewModel viewModel = new() { CanCancel = true };
        viewModel.SetOptions(new DialogChoices<string> { ["a"] = "A" });

        bool? affirmative = null;
        viewModel.RequestClose += (_, e) => affirmative = e;
        viewModel.NegativeCommand.Execute(null);

        viewModel.SelectedValue.ShouldBeNull();
        affirmative.ShouldBe(false);
    }
}
