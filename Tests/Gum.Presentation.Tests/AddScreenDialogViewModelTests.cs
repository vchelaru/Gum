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

        // A leading-slash path (not a Windows drive letter) is rooted per Path.IsPathRooted on
        // both Windows and Unix; a "C:/..." literal is only rooted on Windows, so on macOS/Linux
        // CI it's treated as relative and gets the CI runner's actual working directory prepended.
        _projectState.Setup(x => x.ScreenFilePath).Returns(new ToolsUtilities.FilePath("/project/Screens/"));
        _fileLocations.Setup(x => x.ScreensFolder).Returns("/project/Screens/");

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

    [Fact]
    public void OnAffirmative_PreservesFolderCasing_WhenFolderSelected()
    {
        // Regression for #4481: MakeRelative's 2-arg overload lowercases by default, so a mixed-case
        // selected folder ("MyFolder") used to mangle the new screen's Name to "myfolder/NewScreen".
        var screensRoot = new Mock<ITreeNode>();
        screensRoot.Setup(x => x.Parent).Returns((ITreeNode?)null);
        screensRoot.Setup(x => x.Text).Returns("Screens");

        var folderNode = new Mock<ITreeNode>();
        folderNode.Setup(x => x.Tag).Returns((object?)null);
        folderNode.Setup(x => x.Parent).Returns(screensRoot.Object);
        folderNode.Setup(x => x.GetFullFilePath()).Returns(new ToolsUtilities.FilePath("/project/Screens/MyFolder/"));

        _selectedState.Setup(x => x.SelectedTreeNode).Returns(folderNode.Object);
        _sut.Value = "NewScreen";

        _sut.OnAffirmative();

        _projectCommands.Verify(x => x.AddScreen(It.Is<ScreenSave>(s => s.Name == "MyFolder/NewScreen")), Times.Once);
    }
}
