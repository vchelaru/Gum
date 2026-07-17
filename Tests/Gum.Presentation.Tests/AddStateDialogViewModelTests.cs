using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for AddStateDialogViewModel, relocated out of Gum.csproj into
/// the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM whose four
/// injected interfaces are all already headless.
/// </summary>
public class AddStateDialogViewModelTests
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly AddStateDialogViewModel _viewModel;

    public AddStateDialogViewModelTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _nameVerifier = new Mock<INameVerifier>();
        _undoManager = new Mock<IUndoManager>();
        _elementCommands = new Mock<IElementCommands>();

        _undoManager.Setup(x => x.RequestLock()).Returns(new UndoLock(() => { }));

        _viewModel = new AddStateDialogViewModel(
            _selectedState.Object,
            _nameVerifier.Object,
            _undoManager.Object,
            _elementCommands.Object);
    }

    [Fact]
    public void OnAffirmative_AddsState_AndSelectsIt()
    {
        StateSaveCategory category = new StateSaveCategory();
        StateSave newState = new StateSave();
        _selectedState.Setup(x => x.SelectedStateContainer).Returns((IStateContainer?)null);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);
        _elementCommands
            .Setup(x => x.AddState(null, category, "NewState"))
            .Returns(newState);

        _viewModel.Value = "NewState";
        _viewModel.OnAffirmative();

        _selectedState.VerifySet(x => x.SelectedStateSave = newState, Times.Once);
    }

    [Fact]
    public void Validate_RequiresCategoryToBeSelected()
    {
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns((StateSaveCategory?)null);

        _viewModel.Value = "NewState";
        _viewModel.Validate();

        _viewModel.Error.ShouldBe("You must first select an element or a behavior category to add a state");
    }
}
