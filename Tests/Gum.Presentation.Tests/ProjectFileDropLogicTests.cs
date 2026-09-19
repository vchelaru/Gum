using Gum.Commands;
using Gum.Managers;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gum.Presentation.Tests;

public class ProjectFileDropLogicTests
{
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly ProjectFileDropLogic _logic;

    public ProjectFileDropLogicTests()
    {
        _logic = new ProjectFileDropLogic(_fileCommands.Object);
    }

    [Theory]
    [InlineData(@"C:\Projects\MyGame\MyGame.gumx")]
    [InlineData(@"C:\Projects\MyGame\MyGame.gumj")]
    // Windows Explorer hands over whatever casing is on disk, so the check can't be ordinal.
    [InlineData(@"C:\Projects\MyGame\MyGame.GUMX")]
    public async Task TryOpenDroppedProjectAsync_ProjectFile_LoadsIt(string droppedFile)
    {
        (await _logic.TryOpenDroppedProjectAsync(new List<string> { droppedFile })).ShouldBeTrue();

        _fileCommands.Verify(f => f.LoadProjectAsync(droppedFile), Times.Once);
    }

    [Fact]
    public async Task TryOpenDroppedProjectAsync_NonProjectFiles_LoadsNothing()
    {
        List<string> dropped = new() { @"C:\Art\Icon.png", @"C:\Projects\MyGame\Screens\Main.gusx" };

        (await _logic.TryOpenDroppedProjectAsync(dropped)).ShouldBeFalse();

        _fileCommands.Verify(f => f.LoadProjectAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TryOpenDroppedProjectAsync_MixedDrop_LoadsOnlyTheProject()
    {
        List<string> dropped = new() { @"C:\Art\Icon.png", @"C:\Projects\MyGame\MyGame.gumx" };

        (await _logic.TryOpenDroppedProjectAsync(dropped)).ShouldBeTrue();

        _fileCommands.Verify(f => f.LoadProjectAsync(@"C:\Projects\MyGame\MyGame.gumx"), Times.Once);
        _fileCommands.Verify(f => f.LoadProjectAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task TryOpenDroppedProjectAsync_NoFiles_LoadsNothing()
    {
        // A drag of something that isn't a file at all (a tree node, a Standards-palette chip)
        // carries no file list.
        (await _logic.TryOpenDroppedProjectAsync(null)).ShouldBeFalse();
        (await _logic.TryOpenDroppedProjectAsync(new List<string>())).ShouldBeFalse();

        _fileCommands.Verify(f => f.LoadProjectAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetProjectFileToOpen_DoesNotLoad()
    {
        // The drag-over decision runs on every mouse move; it must stay a pure query.
        _logic.GetProjectFileToOpen(new List<string> { @"C:\Projects\MyGame\MyGame.gumx" })
            .ShouldBe(@"C:\Projects\MyGame\MyGame.gumx");

        _fileCommands.Verify(f => f.LoadProjectAsync(It.IsAny<string>()), Times.Never);
    }
}
