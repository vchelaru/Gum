using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins;
using Gum.Plugins.Errors;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.Errors;

public class FileCaseMismatchFixerTests : BaseTestClass, IDisposable
{
    private readonly string _directory;
    private readonly GumProjectSave _project;
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly FileCaseMismatchFixer _sut;
    private int _refreshRequestsIncludingProject;
    private int _undoLocksOpened;
    private bool _twinExists;

    public FileCaseMismatchFixerTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GumCaseFix_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _project = new GumProjectSave { FullFileName = Path.Combine(_directory, "Project.gumx") };
        ObjectFinder.Self.GumProjectSave = _project;

        Mock<IProjectState> projectState = new();
        projectState.SetupGet(x => x.GumProjectSave).Returns(_project);
        Mock<IUndoManager> undoManager = new();
        undoManager.Setup(x => x.RequestLock(It.IsAny<ElementSave>())).Returns(() =>
        {
            _undoLocksOpened++;
            return new UndoLock(() => { });
        });
        _messenger.Register<RequestErrorRefreshMessage>(this, (_, message) =>
        {
            if (message.IncludesProject)
            {
                _refreshRequestsIncludingProject++;
            }
        });

        _sut = new FileCaseMismatchFixer(projectState.Object, _dialogService.Object, undoManager.Object,
            _pluginManager.Object, Mock.Of<IGuiCommands>(), _fileCommands.Object, _messenger, (_, _) => _twinExists);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static FileNameCaseMismatchDetails Details(string referenced, string onDisk) =>
        new FileNameCaseMismatchDetails { ReferencedPath = referenced, OnDiskPath = onDisk };

    private ComponentSave AddSpriteComponent(string name, string sourceFile)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.Instances.Add(new InstanceSave { Name = "Icon", BaseType = "Sprite" });
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        state.Variables.Add(new VariableSave { Name = "Icon.SourceFile", Type = "string", Value = sourceFile, IsFile = true });
        component.States.Add(state);
        _project.Components.Add(component);
        return component;
    }

    [Fact]
    public void Fix_ChangesEveryVariableHoldingThePath_AndSavesAndRefreshesTheErrors()
    {
        ComponentSave first = AddSpriteComponent("First", "../ui_sheet.png");
        ComponentSave second = AddSpriteComponent("Second", "../ui_sheet.png");
        ComponentSave other = AddSpriteComponent("Other", "other.png");

        _sut.Fix(Details("../ui_sheet.png", "../UI_Sheet.png"));

        first.DefaultState.GetValue("Icon.SourceFile").ShouldBe("../UI_Sheet.png");
        second.DefaultState.GetValue("Icon.SourceFile").ShouldBe("../UI_Sheet.png");
        other.DefaultState.GetValue("Icon.SourceFile").ShouldBe("other.png");
        _undoLocksOpened.ShouldBe(2);
        _fileCommands.Verify(x => x.TryAutoSaveElement(first), Times.Once);
        _fileCommands.Verify(x => x.TryAutoSaveElement(second), Times.Once);
        _pluginManager.Verify(x => x.VariableSet(first, first.GetInstance("Icon"), "SourceFile", "../ui_sheet.png", It.IsAny<bool>()), Times.Once);
        _refreshRequestsIncludingProject.ShouldBe(1);
    }

    [Fact]
    public void Fix_ChangesTheSinglePixelTextureSetting_AndSavesTheProject()
    {
        _project.SinglePixelTextureFile = "ui_sheet.png";

        _sut.Fix(Details("ui_sheet.png", "UI_Sheet.png"));

        _project.SinglePixelTextureFile.ShouldBe("UI_Sheet.png");
        _fileCommands.Verify(x => x.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public void Fix_ChangesNothingAndExplains_WhenTwoFilesDifferOnlyByCase()
    {
        // Injected, since a case-insensitive file system cannot hold both files.
        _twinExists = true;
        ComponentSave component = AddSpriteComponent("First", "UI_SHEET.png");

        _sut.Fix(Details("UI_SHEET.png", "UI_Sheet.png"));

        component.DefaultState.GetValue("Icon.SourceFile").ShouldBe("UI_SHEET.png");
        _dialogService.Verify(x => x.ShowMessage(It.Is<string>(m => m.Contains("differing only by letter case")),
            It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()), Times.Once);
        _fileCommands.Verify(x => x.TryAutoSaveElement(It.IsAny<ElementSave>()), Times.Never);
    }

    [Fact]
    public void CanFix_IsFalse_WhenNoVariableOrSettingHoldsThePath()
    {
        AddSpriteComponent("First", "other.png");

        _sut.CanFix(Details("../ui_sheet.png", "../UI_Sheet.png")).ShouldBeFalse();
    }

    [Fact]
    public void CanFix_IsTrue_WhenAVariableHoldsThePath()
    {
        AddSpriteComponent("First", "../ui_sheet.png");

        _sut.CanFix(Details("../ui_sheet.png", "../UI_Sheet.png")).ShouldBeTrue();
    }
}
