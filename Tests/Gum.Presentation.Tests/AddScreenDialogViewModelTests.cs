using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.ToolStates;
using Moq;

namespace Gum.Presentation.Tests;

/// <summary>
/// AddScreenDialogViewModel had no test coverage before this file. Added alongside narrowing its
/// ProjectCommands/FileLocations dependencies to ICopyPasteProjectCommands/IFileLocations
/// (ADR-0005 Phase 3), which unblocked the VM's move into the headless Gum.Presentation assembly.
/// </summary>
public class AddScreenDialogViewModelTests : BaseTestClass
{
    private readonly AddScreenDialogViewModel _sut;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<ICopyPasteProjectCommands> _projectCommands;
    private readonly Mock<IFileLocations> _fileLocations;
    private readonly Mock<IProjectState> _projectState;

    public AddScreenDialogViewModelTests()
    {
        _nameVerifier = new Mock<INameVerifier>();
        _selectedState = new Mock<ISelectedState>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _projectCommands = new Mock<ICopyPasteProjectCommands>();
        _fileLocations = new Mock<IFileLocations>();
        _projectState = new Mock<IProjectState>();

        // Make the name validation pass so OnAffirmative is not short-circuited by an Error.
        ObjectFinder.Self.GumProjectSave = new GumProjectSave { FullFileName = "C:/project/Test.gumx" };
        string? whyNotValid = null;
        _nameVerifier
            .Setup(x => x.IsElementNameValid(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ElementSave?>(), out whyNotValid))
            .Returns(true);

        _projectState.Setup(x => x.ScreenFilePath).Returns(new ToolsUtilities.FilePath("C:/project/Screens/"));
        _fileLocations.Setup(x => x.ScreensFolder).Returns("C:/project/Screens/");

        _sut = new AddScreenDialogViewModel(
            _nameVerifier.Object,
            _selectedState.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _projectCommands.Object,
            _fileLocations.Object,
            _projectState.Object);
    }

    [Fact]
    public void OnAffirmative_AddsScreenWithNameRelativeToScreensFolder_WhenNoFolderSelected()
    {
        _sut.Value = "NewScreen";

        _sut.OnAffirmative();

        _projectCommands.Verify(x => x.AddScreen(It.Is<ScreenSave>(s => s.Name == "NewScreen")), Times.Once);
        _selectedState.VerifySet(x => x.SelectedScreen = It.Is<ScreenSave>(s => s.Name == "NewScreen"), Times.Once);
    }
}
