using ConvertToJsonPlugin;
using Gum.Commands;
using Gum.DataTypes;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Business logic behind the "Convert to JSON" menu item (issues #4175, #4926), relocated out of the
/// WPF-hosted <c>MainConvertToJsonPlugin</c> so it is unit testable (mirrors
/// <c>ImportFromGumxLogicTests</c>).
/// </summary>
public class ConvertToJsonLogicTests
{
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IConvertProjectToJsonService> _convertService = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly ConvertToJsonLogic _logic;
    private string? _summary;

    public ConvertToJsonLogicTests()
    {
        _logic = new ConvertToJsonLogic(
            _projectState.Object, _convertService.Object, _fileCommands.Object, _dialogService.Object);
    }

    [Fact]
    public void CanConvert_NoProjectLoaded_ReturnsFalse()
    {
        _projectState.Setup(x => x.GumProjectSave).Returns((GumProjectSave?)null);
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);

        _logic.CanConvert.ShouldBeFalse();
    }

    [Fact]
    public void CanConvert_ProjectLoadedAndSaved_ReturnsTrue()
    {
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);

        _logic.CanConvert.ShouldBeTrue();
    }

    [Fact]
    public void CanConvert_ProjectNeedsToSave_ReturnsFalse()
    {
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(true);

        _logic.CanConvert.ShouldBeFalse();
    }

    [Fact]
    public async Task ConvertCurrentProjectAsync_CannotConvert_ShowsMessageAndDoesNotCallConvertService()
    {
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(true);
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());

        await _logic.ConvertCurrentProjectAsync();

        _convertService.Verify(x => x.ConvertToJson(It.IsAny<GumProjectSave>()), Times.Never);
        _dialogService.Verify(x => x.ShowMessage(It.IsAny<string>(), null, null), Times.Once);
    }

    [Fact]
    public async Task ConvertCurrentProjectAsync_ConvertServiceThrows_ShowsErrorMessageAndDoesNotReload()
    {
        GumProjectSave project = new GumProjectSave { FullFileName = "/Projects/MyProject.gumx" };
        _projectState.Setup(x => x.GumProjectSave).Returns(project);
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);
        SetUpConfirmation(confirmed: true, recycleXmlFiles: true);
        _convertService
            .Setup(x => x.ConvertToJson(project))
            .Throws(new InvalidOperationException("The project is already in JSON format."));

        await _logic.ConvertCurrentProjectAsync();

        _fileCommands.Verify(x => x.LoadProjectAsync(It.IsAny<string>()), Times.Never);
        _fileCommands.Verify(x => x.MoveToRecycleBin(It.IsAny<IReadOnlyList<FilePath>>()), Times.Never);
        _dialogService.Verify(
            x => x.ShowMessage("The project is already in JSON format.", "Convert to JSON", null),
            Times.Once);
    }

    [Fact]
    public async Task ConvertCurrentProjectAsync_JsonProjectDidNotLoad_KeepsTheXmlFiles()
    {
        GumProjectSave project = new GumProjectSave { FullFileName = "/Projects/MyProject.gumx" };
        _projectState.Setup(x => x.GumProjectSave).Returns(project);
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);
        SetUpConfirmation(confirmed: true, recycleXmlFiles: true);
        _convertService
            .Setup(x => x.ConvertToJson(project))
            .Returns(new ConvertProjectToJsonResult
            {
                ProjectFilePath = "/Projects/MyProject.gumj",
                ConvertedXmlFiles = new List<FilePath> { "/Projects/MyProject.gumx" },
            });
        // LoadProjectAsync is a no-op here, so the XML project is still the one open.

        await _logic.ConvertCurrentProjectAsync();

        _fileCommands.Verify(x => x.MoveToRecycleBin(It.IsAny<IReadOnlyList<FilePath>>()), Times.Never);
    }

    [Fact]
    public async Task ConvertCurrentProjectAsync_RecycleFails_ReportsItInTheSummary()
    {
        GumProjectSave project = new GumProjectSave { FullFileName = "/Projects/MyProject.gumx" };
        SetUpConvertAndReload(project, "/Projects/MyProject.gumj", new List<FilePath> { "/Projects/MyProject.gumx" });
        SetUpConfirmation(confirmed: true, recycleXmlFiles: true);
        _fileCommands
            .Setup(x => x.MoveToRecycleBin(It.IsAny<IReadOnlyList<FilePath>>()))
            .Throws(new IOException("gio is not installed"));
        CaptureSummary();

        await _logic.ConvertCurrentProjectAsync();

        _summary.ShouldNotBeNull();
        _summary.ShouldContain("gio is not installed");
    }

    [Fact]
    public async Task ConvertCurrentProjectAsync_UserConfirmsWithoutRecycling_ReloadsAndKeepsTheXmlFiles()
    {
        GumProjectSave project = new GumProjectSave { FullFileName = "/Projects/MyProject.gumx" };
        SetUpConvertAndReload(project, "/Projects/MyProject.gumj", new List<FilePath> { "/Projects/MyProject.gumx" });
        SetUpConfirmation(confirmed: true, recycleXmlFiles: false);

        await _logic.ConvertCurrentProjectAsync();

        _fileCommands.Verify(x => x.LoadProjectAsync("/Projects/MyProject.gumj"), Times.Once);
        _fileCommands.Verify(x => x.MoveToRecycleBin(It.IsAny<IReadOnlyList<FilePath>>()), Times.Never);
    }

    [Fact]
    public async Task ConvertCurrentProjectAsync_UserConfirmsWithRecycling_RecyclesTheConvertedXmlAfterReloading()
    {
        GumProjectSave project = new GumProjectSave { FullFileName = "/Projects/MyProject.gumx" };
        List<FilePath> convertedXml = new List<FilePath> { "/Projects/MyProject.gumx", "/Projects/Components/Button.gucx" };
        SetUpConvertAndReload(project, "/Projects/MyProject.gumj", convertedXml);
        SetUpConfirmation(confirmed: true, recycleXmlFiles: true);
        CaptureSummary();

        await _logic.ConvertCurrentProjectAsync();

        _fileCommands.Verify(x => x.MoveToRecycleBin(convertedXml), Times.Once);
        _summary.ShouldNotBeNull();
        _summary.ShouldContain("Moved 2 XML file(s)");
        _summary.ShouldContain("MyProject.gumj");
    }

    [Fact]
    public async Task ConvertCurrentProjectAsync_UserDeclinesConfirmation_DoesNotCallConvertService()
    {
        GumProjectSave project = new GumProjectSave { FullFileName = "/Projects/MyProject.gumx" };
        _projectState.Setup(x => x.GumProjectSave).Returns(project);
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);
        SetUpConfirmation(confirmed: false, recycleXmlFiles: true);

        await _logic.ConvertCurrentProjectAsync();

        _convertService.Verify(x => x.ConvertToJson(It.IsAny<GumProjectSave>()), Times.Never);
        _fileCommands.Verify(x => x.LoadProjectAsync(It.IsAny<string>()), Times.Never);
    }

    private void CaptureSummary()
    {
        _dialogService
            .Setup(x => x.ShowMessage(It.IsAny<string>(), "Convert to JSON", null))
            .Callback<string, string?, MessageDialogStyle?>((message, _, _) => _summary = message);
    }

    private void SetUpConfirmation(bool confirmed, bool recycleXmlFiles)
    {
        _dialogService
            .Setup(x => x.Show(It.IsAny<ConvertToJsonDialogViewModel>()))
            .Callback<ConvertToJsonDialogViewModel>(dialog => dialog.ShouldRecycleXmlFiles = recycleXmlFiles)
            .Returns(confirmed);
    }

    /// <summary>
    /// Makes <paramref name="xmlProject"/> the open project until LoadProjectAsync(<paramref name="jsonPath"/>)
    /// runs, after which a project at <paramref name="jsonPath"/> is open.
    /// </summary>
    private void SetUpConvertAndReload(GumProjectSave xmlProject, string jsonPath, List<FilePath> convertedXml)
    {
        GumProjectSave current = xmlProject;
        _projectState.Setup(x => x.GumProjectSave).Returns(() => current);
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);
        _convertService
            .Setup(x => x.ConvertToJson(xmlProject))
            .Returns(new ConvertProjectToJsonResult { ProjectFilePath = jsonPath, ConvertedXmlFiles = convertedXml });
        _fileCommands
            .Setup(x => x.LoadProjectAsync(jsonPath))
            .Callback(() => current = new GumProjectSave { FullFileName = jsonPath })
            .Returns(Task.CompletedTask);
    }
}
