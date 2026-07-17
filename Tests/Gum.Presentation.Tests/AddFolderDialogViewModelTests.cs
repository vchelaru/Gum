using Gum.Commands;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for AddFolderDialogViewModel, relocated out of Gum.csproj into
/// the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM whose three
/// injected interfaces (ISelectedState, INameVerifier, IGuiCommands) are all already headless.
/// </summary>
public class AddFolderDialogViewModelTests
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly AddFolderDialogViewModel _viewModel;

    public AddFolderDialogViewModelTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _nameVerifier = new Mock<INameVerifier>();
        _guiCommands = new Mock<IGuiCommands>();

        _viewModel = new AddFolderDialogViewModel(_selectedState.Object, _nameVerifier.Object, _guiCommands.Object);
    }

    [Fact]
    public void Validate_ReturnsNameVerifierError_WhenNameIsInvalid()
    {
        string? whyNotValid = "A folder with this name already exists.";
        _nameVerifier
            .Setup(x => x.IsFolderNameValid(It.IsAny<string?>(), out whyNotValid))
            .Returns(false);

        _viewModel.Value = "Existing";
        _viewModel.Validate();

        _viewModel.Error.ShouldBe("A folder with this name already exists.");
    }

    [Fact]
    public void Validate_RejectsSpaces_EvenWhenNameVerifierSaysValid()
    {
        string? whyNotValid = null;
        _nameVerifier
            .Setup(x => x.IsFolderNameValid(It.IsAny<string?>(), out whyNotValid))
            .Returns(true);

        _viewModel.Value = "New Folder";
        _viewModel.Validate();

        _viewModel.Error.ShouldBe("Folders with spaces are not recommended since they can break variable references");
    }

    [Fact]
    public void Validate_HasNoError_WhenNameIsValidAndHasNoSpaces()
    {
        string? whyNotValid = null;
        _nameVerifier
            .Setup(x => x.IsFolderNameValid(It.IsAny<string?>(), out whyNotValid))
            .Returns(true);

        _viewModel.Value = "NewFolder";
        _viewModel.Validate();

        _viewModel.Error.ShouldBeNull();
    }
}
