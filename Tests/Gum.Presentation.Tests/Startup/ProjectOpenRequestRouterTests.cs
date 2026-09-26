using Gum.Commands;
using Gum.Startup;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Startup;

/// <summary>
/// An OS open-document request (macOS Finder double-click, #5130) can arrive before the tool has
/// finished starting or while it is running; the router holds or opens it accordingly.
/// </summary>
public class ProjectOpenRequestRouterTests
{
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly ProjectOpenRequestRouter _router;

    public ProjectOpenRequestRouterTests()
    {
        _fileCommands = new Mock<IFileCommands>();
        _fileCommands.Setup(f => f.LoadProjectAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _selectedState = new Mock<ISelectedState>();
        _router = new ProjectOpenRequestRouter(
            _selectedState.Object,
            new Lazy<IFileCommands>(() => _fileCommands.Object));
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
}
