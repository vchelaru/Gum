using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for AddCategoryDialogViewModel, relocated out of Gum.csproj
/// into the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM whose four
/// injected interfaces (ISelectedState, IElementCommands, IUndoManager, INameVerifier) are all
/// already headless.
/// </summary>
public class AddCategoryDialogViewModelTests
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly AddCategoryDialogViewModel _viewModel;

    public AddCategoryDialogViewModelTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _elementCommands = new Mock<IElementCommands>();
        _undoManager = new Mock<IUndoManager>();
        _nameVerifier = new Mock<INameVerifier>();

        _viewModel = new AddCategoryDialogViewModel(
            _selectedState.Object,
            _elementCommands.Object,
            _undoManager.Object,
            _nameVerifier.Object);
    }

    [Fact]
    public void Validate_ShouldValidateComponentCategories_UsingNameVerifier()
    {
        string? errorMessage = "Invalid category name";
        _nameVerifier.Setup(
            x => x.IsCategoryNameValid(
                It.IsAny<string>(),
                It.IsAny<ElementSave>(),
                out errorMessage));

        _selectedState
            .Setup(x => x.SelectedStateContainer)
            .Returns(new ComponentSave());

        _viewModel.Value = "NewCategory";

        _viewModel.Validate();

        _viewModel.Error.ShouldBe("Invalid category name");
    }

    [Fact]
    public void Validate_ShouldValidateBehaviorCategories_UsingNameVerifier()
    {
        string? errorMessage = "Invalid behavior category name";
        _nameVerifier.Setup(
            x => x.IsCategoryNameValid(
                It.IsAny<string>(),
                It.IsAny<BehaviorSave>(),
                out errorMessage));

        _selectedState
            .Setup(x => x.SelectedStateContainer)
            .Returns(new BehaviorSave());

        _viewModel.Value = "NewCategory";
        _viewModel.Validate();

        _viewModel.Error.ShouldBe("Invalid behavior category name");
    }
}
