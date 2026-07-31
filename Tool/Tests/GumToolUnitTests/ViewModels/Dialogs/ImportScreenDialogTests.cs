using System;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.ViewModels.Dialogs;

public class ImportScreenDialogTests : BaseTestClass
{
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IImportLogic> _importLogic;
    private readonly Mock<IProjectState> _projectState;
    private readonly GumProjectSave _gumProjectSave;
    private string? _screensDirectory;

    public ImportScreenDialogTests()
    {
        _dialogService = new Mock<IDialogService>();
        _importLogic = new Mock<IImportLogic>();
        _projectState = new Mock<IProjectState>();
        _gumProjectSave = new GumProjectSave { FullFileName = "C:/project/Test.gumx" };

        // The constructor scans the screens folder (returns empty for a non-existent dir)
        // and reads the project's existing screens, so those reads must not throw.
        _projectState.Setup(x => x.ScreenFilePath).Returns(new FilePath("C:/project/Screens/"));
        _projectState.Setup(x => x.GumProjectSave).Returns(_gumProjectSave);
    }

    public override void Dispose()
    {
        if (_screensDirectory != null && Directory.Exists(_screensDirectory))
        {
            Directory.Delete(_screensDirectory, recursive: true);
        }
        base.Dispose();
    }

    private ImportScreenDialog CreateSut() => new(
        _dialogService.Object,
        _importLogic.Object,
        _projectState.Object);

    [Fact]
    public void Constructor_ListsBothXmlAndJsonScreenFiles_ThatAreNotYetInProject()
    {
        // A JSON-converted screen (issue #4182) must be offered for import the same way a .gusx
        // one is.
        _screensDirectory = Path.Combine(Path.GetTempPath(), "ImportScreenDialogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_screensDirectory);
        File.WriteAllText(Path.Combine(_screensDirectory, "XmlOnly.gusx"), "");
        File.WriteAllText(Path.Combine(_screensDirectory, "JsonOnly.gusj"), "{}");
        _projectState.Setup(x => x.ScreenFilePath).Returns(new FilePath(_screensDirectory + "/"));

        ImportScreenDialog sut = CreateSut();

        sut.UnfilteredFiles.Any(f => f.EndsWith("XmlOnly.gusx")).ShouldBeTrue();
        sut.UnfilteredFiles.Any(f => f.EndsWith("JsonOnly.gusj")).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_ExcludesScreenAlreadyInProject()
    {
        // Boyscout pin (issue #4182): the exclusion check previously compared against
        // ComponentFilePath instead of ScreenFilePath, so an already-imported screen never matched
        // and always re-appeared in the "available to import" list.
        _screensDirectory = Path.Combine(Path.GetTempPath(), "ImportScreenDialogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_screensDirectory);
        File.WriteAllText(Path.Combine(_screensDirectory, "AlreadyImported.gusx"), "");
        _projectState.Setup(x => x.ScreenFilePath).Returns(new FilePath(_screensDirectory + "/"));
        _gumProjectSave.Screens.Add(new ScreenSave { Name = "AlreadyImported" });

        ImportScreenDialog sut = CreateSut();

        sut.UnfilteredFiles.Any(f => f.EndsWith("AlreadyImported.gusx")).ShouldBeFalse();
    }

    [Fact]
    public void BrowseFileFilter_AcceptsBothXmlAndJsonScreenFiles()
    {
        ImportScreenDialog sut = CreateSut();

        sut.BrowseFileFilter.ShouldContain("*.gusx");
        sut.BrowseFileFilter.ShouldContain("*.gusj");
    }
}
