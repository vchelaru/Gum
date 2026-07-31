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

public class ImportComponentDialogTests : BaseTestClass
{
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IImportLogic> _importLogic;
    private readonly Mock<IProjectState> _projectState;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly GumProjectSave _gumProjectSave;
    private string? _componentsDirectory;

    public ImportComponentDialogTests()
    {
        _fileCommands = new Mock<IFileCommands>();
        _guiCommands = new Mock<IGuiCommands>();
        _selectedState = new Mock<ISelectedState>();
        _dialogService = new Mock<IDialogService>();
        _importLogic = new Mock<IImportLogic>();
        _projectState = new Mock<IProjectState>();
        _projectManager = new Mock<IProjectManager>();
        _gumProjectSave = new GumProjectSave { FullFileName = "C:/project/Test.gumx" };

        // The constructor scans the components folder (returns empty for a non-existent dir)
        // and reads the project's existing components, so those reads must not throw.
        _projectState.Setup(x => x.ComponentFilePath).Returns(new FilePath("C:/project/Components/"));
        _projectState.Setup(x => x.GumProjectSave).Returns(_gumProjectSave);
        _projectManager.Setup(x => x.GumProjectSave).Returns(_gumProjectSave);
    }

    public override void Dispose()
    {
        if (_componentsDirectory != null && Directory.Exists(_componentsDirectory))
        {
            Directory.Delete(_componentsDirectory, recursive: true);
        }
        base.Dispose();
    }

    private ImportComponentDialog CreateSut() => new(
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
        ImportComponentDialog sut = CreateSut();

        sut.OnAffirmative();

        _projectManager.Verify(x => x.GumProjectSave, Times.Once);
    }

    [Fact]
    public void Constructor_ListsBothXmlAndJsonComponentFiles_ThatAreNotYetInProject()
    {
        // A JSON-converted component (issue #4182) must be offered for import the same way a .gucx
        // one is.
        _componentsDirectory = Path.Combine(Path.GetTempPath(), "ImportComponentDialogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_componentsDirectory);
        File.WriteAllText(Path.Combine(_componentsDirectory, "XmlOnly.gucx"), "");
        File.WriteAllText(Path.Combine(_componentsDirectory, "JsonOnly.gucj"), "{}");
        _projectState.Setup(x => x.ComponentFilePath).Returns(new FilePath(_componentsDirectory + "/"));

        ImportComponentDialog sut = CreateSut();

        sut.UnfilteredFiles.Any(f => f.EndsWith("XmlOnly.gucx")).ShouldBeTrue();
        sut.UnfilteredFiles.Any(f => f.EndsWith("JsonOnly.gucj")).ShouldBeTrue();
    }

    [Fact]
    public void BrowseFileFilter_AcceptsBothXmlAndJsonComponentFiles()
    {
        ImportComponentDialog sut = CreateSut();

        sut.BrowseFileFilter.ShouldContain("*.gucx");
        sut.BrowseFileFilter.ShouldContain("*.gucj");
    }
}
