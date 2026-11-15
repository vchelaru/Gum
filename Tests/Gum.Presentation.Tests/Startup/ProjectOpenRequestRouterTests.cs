using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Startup;
using Gum.Startup.SingleInstance;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Startup;

/// <summary>
/// An OS open-document request (macOS Finder double-click, #5130) can arrive before the tool has
/// finished starting or while it is running; the router holds or opens it accordingly.
/// </summary>
public class ProjectOpenRequestRouterTests : BaseTestClass
{
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IProjectState> _projectState;
    private readonly ProjectOpenRequestRouter _router;

    public ProjectOpenRequestRouterTests()
    {
        _fileCommands = new Mock<IFileCommands>();
        _fileCommands.Setup(f => f.LoadProjectAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _selectedState = new Mock<ISelectedState>();
        _projectState = new Mock<IProjectState>();
        _router = new ProjectOpenRequestRouter(
            _selectedState.Object,
            new Lazy<IFileCommands>(() => _fileCommands.Object),
            new Lazy<IProjectState>(() => _projectState.Object));
    }

    [Fact]
    public async Task RequestOpenAsync_BeforeStartup_IsHeldForStartupInsteadOfLoaded()
    {
        string project = "/Users/me/Game/Game.gumx";

        await _router.RequestOpenAsync(new[] { project });

        _fileCommands.Verify(f => f.LoadProjectAsync(It.IsAny<string>()), Times.Never);
        _router.TakePendingStartupProject().ShouldBe(project);
        _router.TakePendingStartupProject().ShouldBeNull();
    }

    [Fact]
    public async Task CompleteStartupAsync_OpensRequestNotTakenByStartup()
    {
        string project = "/Users/me/Game/Game.gumx";
        await _router.RequestOpenAsync(new[] { project });

        await _router.CompleteStartupAsync();

        _fileCommands.Verify(f => f.LoadProjectAsync(project), Times.Once);
    }

    [Fact]
    public async Task CompleteStartupAsync_DoesNotReopenRequestTakenByStartup()
    {
        await _router.RequestOpenAsync(new[] { "/Users/me/Game/Game.gumx" });
        _router.TakePendingStartupProject();

        await _router.CompleteStartupAsync();

        _fileCommands.Verify(f => f.LoadProjectAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestOpenAsync_AfterStartup_ClearsSelectionAndLoadsFirstProjectFile()
    {
        string project = "/Users/me/Game/Game.GUMJ";
        await _router.CompleteStartupAsync();

        await _router.RequestOpenAsync(new[] { "/Users/me/notes.txt", project, "/Users/me/Other/Other.gumx" });

        _selectedState.VerifySet(s => s.SelectedElement = null);
        _fileCommands.Verify(f => f.LoadProjectAsync(project), Times.Once);
        _fileCommands.Verify(f => f.LoadProjectAsync(It.IsAny<string>()), Times.Once);
        _router.TakePendingStartupProject().ShouldBeNull();
    }

    [Fact]
    public async Task RequestOpenAsync_IgnoresRequestWithNoProjectFile()
    {
        await _router.RequestOpenAsync(new[] { "/Users/me/Game/Screens/Main.gusx" });
        await _router.CompleteStartupAsync();
        await _router.RequestOpenAsync(new[] { "/Users/me/notes.txt" });

        _router.TakePendingStartupProject().ShouldBeNull();
        _fileCommands.Verify(f => f.LoadProjectAsync(It.IsAny<string>()), Times.Never);
    }

    private ScreenSave LoadedScreen(string projectFile, string screenName)
    {
        ScreenSave screen = new ScreenSave { Name = screenName };
        GumProjectSave project = new GumProjectSave { FullFileName = projectFile };
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;
        _projectState.SetupGet(p => p.GumProjectSave).Returns(project);
        return screen;
    }

    [Fact]
    public async Task RequestOpenAsync_AfterStartupForTheLoadedProject_SelectsTheElementWithoutReloading()
    {
        string projectFile = Path.Combine(Path.GetTempPath(), "Game", "Game.gumx");
        ScreenSave screen = LoadedScreen(projectFile, "Menus/Main");
        await _router.CompleteStartupAsync();

        await _router.RequestOpenAsync(new OpenRequest(projectFile.ToUpperInvariant().Replace("GAME.GUMX", "Game.gumx"), "Menus/Main"));

        _fileCommands.Verify(f => f.LoadProjectAsync(It.IsAny<string>()), Times.Never);
        _selectedState.VerifySet(s => s.SelectedElement = screen);
    }

    [Fact]
    public async Task RequestOpenAsync_AfterStartupForAnotherProject_LoadsItThenSelectsTheElement()
    {
        string loaded = Path.Combine(Path.GetTempPath(), "Game", "Game.gumx");
        string other = Path.Combine(Path.GetTempPath(), "Other", "Other.gumx");
        LoadedScreen(loaded, "Main");
        await _router.CompleteStartupAsync();
        ScreenSave otherScreen = new ScreenSave { Name = "Intro" };
        _fileCommands.Setup(f => f.LoadProjectAsync(other)).Returns(() =>
        {
            GumProjectSave project = new GumProjectSave { FullFileName = other };
            project.Screens.Add(otherScreen);
            ObjectFinder.Self.GumProjectSave = project;
            return Task.CompletedTask;
        });

        await _router.RequestOpenAsync(new OpenRequest(other, "Intro"));

        _fileCommands.Verify(f => f.LoadProjectAsync(other), Times.Once);
        _selectedState.VerifySet(s => s.SelectedElement = otherScreen);
    }

    [Fact]
    public async Task RequestOpenAsync_ElementThatDoesNotExist_LeavesSelectionAlone()
    {
        string projectFile = Path.Combine(Path.GetTempPath(), "Game", "Game.gumx");
        LoadedScreen(projectFile, "Main");
        await _router.CompleteStartupAsync();

        await _router.RequestOpenAsync(new OpenRequest(projectFile, "Missing"));

        _selectedState.VerifySet(s => s.SelectedElement = It.IsAny<ElementSave?>(), Times.Never);
    }

    [Fact]
    public async Task RequestOpenAsync_DuringStartup_KeepsTheElementForAfterTheStartupLoad()
    {
        string projectFile = Path.Combine(Path.GetTempPath(), "Game", "Game.gumx");
        await _router.RequestOpenAsync(new OpenRequest(projectFile, "Main"));

        _router.TakePendingStartupProject().ShouldBe(projectFile);
        ScreenSave screen = LoadedScreen(projectFile, "Main");
        await _router.CompleteStartupAsync();

        _fileCommands.Verify(f => f.LoadProjectAsync(It.IsAny<string>()), Times.Never);
        _selectedState.VerifySet(s => s.SelectedElement = screen);
    }

    [Fact]
    public async Task RequestOpenAsync_DuringStartup_LatestRequestWins()
    {
        string projectFile = Path.Combine(Path.GetTempPath(), "Game", "Game.gumx");
        await _router.RequestOpenAsync(new OpenRequest(projectFile, "First"));
        await _router.RequestOpenAsync(new OpenRequest(projectFile, "Second"));
        _router.TakePendingStartupProject();
        ScreenSave second = LoadedScreen(projectFile, "Second");
        await _router.CompleteStartupAsync();

        _selectedState.VerifySet(s => s.SelectedElement = second);
    }
}
