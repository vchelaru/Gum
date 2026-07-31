using ConvertToJsonPlugin;
using Gum.Commands;
using Gum.DataTypes;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Business logic behind the "Convert to JSON" menu item (issue #4175), relocated out of the
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
    public void CanConvert_ProjectNeedsToSave_ReturnsFalse()
    {
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(true);

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
    public void ConvertCurrentProject_CannotConvert_ShowsMessageAndDoesNotCallConvertService()
    {
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(true);
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());

        _logic.ConvertCurrentProject();

        _convertService.Verify(x => x.ConvertToJson(It.IsAny<GumProjectSave>()), Times.Never);
        _dialogService.Verify(x => x.ShowMessage(It.IsAny<string>(), null, null), Times.Once);
    }

    [Fact]
    public void ConvertCurrentProject_UserDeclinesConfirmation_DoesNotCallConvertService()
    {
        GumProjectSave project = new GumProjectSave();
        _projectState.Setup(x => x.GumProjectSave).Returns(project);
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);
        _dialogService
            .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.Is<MessageDialogStyle?>(s => s != null)))
            .Returns(MessageDialogResult.Negative);

        _logic.ConvertCurrentProject();

        _convertService.Verify(x => x.ConvertToJson(It.IsAny<GumProjectSave>()), Times.Never);
        _fileCommands.Verify(x => x.LoadProject(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ConvertCurrentProject_UserConfirms_CallsConvertServiceAndReloadsProject()
    {
        GumProjectSave project = new GumProjectSave { FullFileName = "C:/Projects/MyProject.gumx" };
        _projectState.Setup(x => x.GumProjectSave).Returns(project);
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);
        _dialogService
            .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.Is<MessageDialogStyle?>(s => s != null)))
            .Returns(MessageDialogResult.Affirmative);
        _convertService
            .Setup(x => x.ConvertToJson(project))
            .Returns(new ConvertProjectToJsonResult { ProjectFilePath = "C:/Projects/MyProject.gumj" });

        _logic.ConvertCurrentProject();

        _fileCommands.Verify(x => x.LoadProject("C:/Projects/MyProject.gumj"), Times.Once);
    }

    [Fact]
    public void ConvertCurrentProject_ConvertServiceThrows_ShowsErrorMessageAndDoesNotReload()
    {
        GumProjectSave project = new GumProjectSave();
        _projectState.Setup(x => x.GumProjectSave).Returns(project);
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);
        _dialogService
            .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.Is<MessageDialogStyle?>(s => s != null)))
            .Returns(MessageDialogResult.Affirmative);
        _convertService
            .Setup(x => x.ConvertToJson(project))
            .Throws(new InvalidOperationException("The project is already in JSON format."));

        _logic.ConvertCurrentProject();

        _fileCommands.Verify(x => x.LoadProject(It.IsAny<string>()), Times.Never);
        _dialogService.Verify(
            x => x.ShowMessage("The project is already in JSON format.", "Convert to JSON", null),
            Times.Once);
    }
}
