using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Xunit;

namespace GumToolUnitTests.Logic.FileWatch;

public class FileWatchLogicTests
{
    private readonly Mock<IFileWatchManager> _fileWatchManager;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IProjectState> _projectState;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly FileWatchLogic _fileWatchLogic;

    public FileWatchLogicTests()
    {
        _fileWatchManager = new Mock<IFileWatchManager>();
        _guiCommands = new Mock<IGuiCommands>();
        _projectState = new Mock<IProjectState>();
        _projectManager = new Mock<IProjectManager>();

        _fileWatchLogic = new FileWatchLogic(
            _fileWatchManager.Object,
            _guiCommands.Object,
            _projectState.Object,
            _projectManager.Object);
    }

    [Fact]
    public void HandleProjectUnloaded_DisablesWatcher()
    {
        _fileWatchLogic.HandleProjectUnloaded();

        _fileWatchManager.Verify(m => m.Disable(), Times.Once);
    }

    [Fact]
    public void RefreshRootDirectory_ClearsIgnoredFilesAndDisables_WhenNoProjectLoaded()
    {
        // GumProjectSave defaults to null on the mock, so GumProjectSave?.FullFileName
        // is null and RefreshRootDirectory takes the "no project" branch. This avoids
        // GetFileWatchRootDirectories, which would require heavy ObjectFinder.Self setup.
        _fileWatchLogic.RefreshRootDirectory();

        _fileWatchManager.Verify(m => m.ClearIgnoredFiles(), Times.Once);
        _fileWatchManager.Verify(m => m.Disable(), Times.Once);
    }
}
