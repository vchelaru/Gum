using System;
using System.IO;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Shouldly;
using ToolsUtilities;
using Xunit;

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
    private string? _behaviorsDirectory;

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

    public override void Dispose()
    {
        if (_behaviorsDirectory != null && Directory.Exists(_behaviorsDirectory))
        {
            Directory.Delete(_behaviorsDirectory, recursive: true);
        }
        base.Dispose();
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

    [Fact]
    public void Constructor_ListsBothXmlAndJsonBehaviorFiles_ThatAreNotYetInProject()
    {
        // A JSON-converted behavior (issue #4182) must be offered for import the same way a .behx
        // one is.
        _behaviorsDirectory = Path.Combine(Path.GetTempPath(), "ImportBehaviorDialogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_behaviorsDirectory);
        File.WriteAllText(Path.Combine(_behaviorsDirectory, "XmlOnly.behx"), "");
        File.WriteAllText(Path.Combine(_behaviorsDirectory, "JsonOnly.behj"), "{}");
        _projectState.Setup(x => x.BehaviorFilePath).Returns(new FilePath(_behaviorsDirectory + "/"));

        ImportBehaviorDialog sut = CreateSut();

        sut.UnfilteredFiles.Any(f => f.EndsWith("XmlOnly.behx")).ShouldBeTrue();
        sut.UnfilteredFiles.Any(f => f.EndsWith("JsonOnly.behj")).ShouldBeTrue();
    }

    [Fact]
    public void BrowseFileFilter_AcceptsBothXmlAndJsonBehaviorFiles()
    {
        ImportBehaviorDialog sut = CreateSut();

        sut.BrowseFileFilter.ShouldContain("*.behx");
        sut.BrowseFileFilter.ShouldContain("*.behj");
    }
}
