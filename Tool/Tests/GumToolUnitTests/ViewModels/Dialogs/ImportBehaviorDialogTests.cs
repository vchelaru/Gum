using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using ToolsUtilities;

namespace GumToolUnitTests.ViewModels.Dialogs;

public class ImportBehaviorDialogTests : BaseTestClass
{
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IImportLogic> _importLogic;
    private readonly Mock<IProjectState> _projectState;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly GumProjectSave _gumProjectSave;

    public ImportBehaviorDialogTests()
    {
        _fileCommands = new Mock<IFileCommands>();
        _guiCommands = new Mock<IGuiCommands>();
        _selectedState = new Mock<ISelectedState>();
        _dialogService = new Mock<IDialogService>();
        _importLogic = new Mock<IImportLogic>();
        _projectState = new Mock<IProjectState>();
        _projectManager = new Mock<IProjectManager>();
        _gumProjectSave = new GumProjectSave { FullFileName = "C:/project/Test.gumx" };

        // The constructor scans the behaviors folder (returns empty for a non-existent dir)
        // and reads the project's existing behaviors, so those reads must not throw.
        _projectState.Setup(x => x.BehaviorFilePath).Returns(new FilePath("C:/project/Behaviors/"));
        _projectState.Setup(x => x.GumProjectSave).Returns(_gumProjectSave);
        _projectManager.Setup(x => x.GumProjectSave).Returns(_gumProjectSave);
    }

    private ImportBehaviorDialog CreateSut() => new(
        _fileCommands.Object,
        _guiCommands.Object,
        _selectedState.Object,
        _dialogService.Object,
        _importLogic.Object,
        _projectState.Object,
        _projectManager.Object);

    [Fact]
    public void OnAffirmative_ReadsGumProjectSaveFromInjectedProjectManager()
    {
        ImportBehaviorDialog sut = CreateSut();

        sut.OnAffirmative();

        _projectManager.Verify(x => x.GumProjectSave, Times.Once);
    }
}
