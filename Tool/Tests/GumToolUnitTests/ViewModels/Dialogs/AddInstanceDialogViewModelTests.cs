using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Moq;

namespace GumToolUnitTests.ViewModels.Dialogs;

public class AddInstanceDialogViewModelTests
{
    AddInstanceDialogViewModel _sut;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly Mock<ISetVariableLogic> _setVariableLogic;

    public AddInstanceDialogViewModelTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _nameVerifier = new Mock<INameVerifier>();
        _elementCommands = new Mock<IElementCommands>();
        _setVariableLogic = new Mock<ISetVariableLogic>();

        _sut = new AddInstanceDialogViewModel(
            _selectedState.Object,
            _nameVerifier.Object,
            _elementCommands.Object,
            _setVariableLogic.Object);
    }

    [Fact]
    public void OnAffirmative_ShouldCreateInstance()
    {
        // arrange
        _sut.Value = "NewInstance";

        // act
        _sut.OnAffirmative();

        // verify
        _elementCommands.Verify(x => x.AddInstance(
            It.IsAny<ElementSave>(), 
            "NewInstance", 
            It.IsAny<string?>(), 
            It.IsAny<string?>(), 
            It.IsAny<int?>()),
            Times.Once);
            
    }
}
