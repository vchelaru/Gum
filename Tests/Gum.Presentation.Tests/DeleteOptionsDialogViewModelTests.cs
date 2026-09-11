using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Presentation.Tests;

public class DeleteOptionsDialogViewModelTests
{
    [Fact]
    public void AffirmativeCommand_ClosesAffirmed_SoTheDeleteGoesAhead()
    {
        DeleteOptionsDialogViewModel viewModel = new DeleteOptionsDialogViewModel();
        bool? closedWith = null;
        viewModel.RequestClose += (_, affirmed) => closedWith = affirmed;

        viewModel.AffirmativeCommand.Execute(null);

        closedWith.ShouldBe(true);
    }

    [Fact]
    public void Constructor_IsAYesNoConfirmationWithNoOptions()
    {
        DeleteOptionsDialogViewModel viewModel = new DeleteOptionsDialogViewModel();

        viewModel.AffirmativeText.ShouldBe("Yes");
        viewModel.NegativeText.ShouldBe("No");
        viewModel.CheckBoxes.ShouldBeEmpty();
        viewModel.Choices.ShouldBeEmpty();
    }

    [Fact]
    public void DeleteOptionChoiceViewModel_KeepsItsOptionsInOrder()
    {
        DeleteOptionCheckboxViewModel first = new DeleteOptionCheckboxViewModel { Label = "Only parent", IsChecked = true };
        DeleteOptionCheckboxViewModel second = new DeleteOptionCheckboxViewModel { Label = "Parent and children" };

        DeleteOptionChoiceViewModel choice = new DeleteOptionChoiceViewModel("Delete children?", new[] { first, second });

        choice.Header.ShouldBe("Delete children?");
        choice.Options.ShouldBe(new[] { first, second });
    }
}
