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
    public void Constructor_IsAYesNoConfirmationWithAccessKeysAndNoOptions()
    {
        DeleteOptionsDialogViewModel viewModel = new DeleteOptionsDialogViewModel();

        // The underscores mark Y and N as the buttons' access keys (Alt+Y, Alt+N), as in WPF.
        viewModel.AffirmativeText.ShouldBe("_Yes");
        viewModel.NegativeText.ShouldBe("_No");
        viewModel.CheckBoxes.ShouldBeEmpty();
        viewModel.Choices.ShouldBeEmpty();
    }

    [Theory]
    [InlineData('y', true)]
    [InlineData('Y', true)]
    [InlineData('n', false)]
    public void TryAnswerFromAccessKey_YesOrNoLetterAlone_AnswersTheDialog(char letter, bool expectedAffirmed)
    {
        DeleteOptionsDialogViewModel viewModel = new DeleteOptionsDialogViewModel();
        bool? closedWith = null;
        viewModel.RequestClose += (_, affirmed) => closedWith = affirmed;

        bool answered = viewModel.TryAnswerFromAccessKey(letter);

        answered.ShouldBeTrue();
        closedWith.ShouldBe(expectedAffirmed);
    }

    [Fact]
    public void TryAnswerFromAccessKey_OtherLetter_LeavesTheDialogOpen()
    {
        DeleteOptionsDialogViewModel viewModel = new DeleteOptionsDialogViewModel();
        bool closed = false;
        viewModel.RequestClose += (_, _) => closed = true;

        viewModel.TryAnswerFromAccessKey('x').ShouldBeFalse();

        closed.ShouldBeFalse();
    }

    [Fact]
    public void TryAnswerFromAccessKey_DialogThatDoesNotOptIn_IgnoresTheLetter()
    {
        // Only dialogs with nothing to type into answer on a bare letter; elsewhere it is text entry.
        MessageDialogViewModel viewModel = new MessageDialogViewModel { AffirmativeText = "_Yes", NegativeText = "_No" };
        bool closed = false;
        viewModel.RequestClose += (_, _) => closed = true;

        viewModel.TryAnswerFromAccessKey('y').ShouldBeFalse();

        closed.ShouldBeFalse();
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
