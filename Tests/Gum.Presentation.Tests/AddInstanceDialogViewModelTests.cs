using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolCommands;
using Gum.ToolStates;
using Moq;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for AddInstanceDialogViewModel, relocated out of Gum.csproj
/// into the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM whose four
/// injected interfaces are all already headless.
/// </summary>
public class AddInstanceDialogViewModelTests
{
    private readonly AddInstanceDialogViewModel _sut;
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
        _sut.Value = "NewInstance";

        _sut.OnAffirmative();

        _elementCommands.Verify(x => x.AddInstance(
            It.IsAny<ElementSave>(),
            "NewInstance",
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<int?>()),
            Times.Once);
    }
}
