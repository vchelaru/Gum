using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Exercises FileWatchManager end-to-end against a real temp directory and real
/// FileSystemWatcher, since its whole job is wiring OS file-change events to the queue/flush
/// pipeline - mocking FileSystemWatcher itself would test nothing. Polls with a generous
/// timeout rather than a fixed sleep to absorb OS event-delivery jitter.
/// </summary>
public class FileWatchManagerTests : IDisposable
{
    private readonly string _tempDirectory;

    public FileWatchManagerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "FileWatchManagerTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { /* best-effort */ }
    }

    private static FileWatchManager BuildSut(
        out Mock<IGuiCommands> guiCommandsMock,
        out Mock<IPluginManager> pluginManagerMock,
        out Mock<IFileWatchIgnoreList> ignoreListMock,
        FilePath projectDirectory)
    {
        guiCommandsMock = new Mock<IGuiCommands>();
        pluginManagerMock = new Mock<IPluginManager>();

        ignoreListMock = new Mock<IFileWatchIgnoreList>();
        ignoreListMock.Setup(i => i.TryGetIgnoreFileChange(It.IsAny<FilePath>())).Returns(false);

        var projectManagerMock = new Mock<IProjectManager>();
        var gumProject = new GumProjectSave { FullFileName = projectDirectory + "Project.gumx" };
        projectManagerMock.Setup(p => p.GumProjectSave).Returns(gumProject);

        var fileChangeReactionLogic = new FileChangeReactionLogic(
            new Mock<ISelectedState>().Object,
            new Mock<IWireframeCommands>().Object,
            guiCommandsMock.Object,
            new Mock<IFileCommands>().Object,
            new Mock<IOutputManager>().Object,
            new Mock<IWireframeObjectManager>().Object,
            new Mock<IProjectState>().Object,
            new Mock<IStandardElementsManagerGumTool>().Object,
            pluginManagerMock.Object);

        return new FileWatchManager(
            guiCommandsMock.Object,
            projectManagerMock.Object,
            fileChangeReactionLogic,
            ignoreListMock.Object);
    }

    private static bool PollUntil(Func<bool> condition, int timeoutMilliseconds = 4000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            Thread.Sleep(50);
        }
        return condition();
    }

    [Fact]
    public void EnableWithDirectories_ThenFlush_ShouldReactToFileCreatedInWatchedDirectory()
    {
        FilePath watchedDirectory = new FilePath(_tempDirectory + "/");
        FileWatchManager sut = BuildSut(
            out Mock<IGuiCommands> guiCommandsMock,
            out Mock<IPluginManager> pluginManagerMock,
            out _,
            watchedDirectory);

        sut.EnableWithDirectories(new HashSet<FilePath> { watchedDirectory });
        sut.Enabled.ShouldBeTrue();

        FilePath createdFile = new FilePath(Path.Combine(_tempDirectory, "NewFile.txt"));
        File.WriteAllText(createdFile.FullPath, "contents");

        PollUntil(() => sut.ChangedFilesWaitingForFlush.Contains(createdFile)).ShouldBeTrue(
            "the watcher should queue the created file for flush");

        // Flush debounces for 500ms after the last change; wait it out so Flush() doesn't early-out.
        PollUntil(() => sut.TimeToNextFlush.TotalSeconds <= 0).ShouldBeTrue();

        sut.Flush();

        sut.ChangedFilesWaitingForFlush.ShouldNotContain(createdFile);
        pluginManagerMock.Verify(p => p.ReactToFileChanged(createdFile), Times.Once);
    }

    [Fact]
    public void IgnoreNextChangeUntil_ShouldSuppressQueuedChange_ForIgnoredFile()
    {
        FilePath watchedDirectory = new FilePath(_tempDirectory + "/");
        FileWatchManager sut = BuildSut(
            out _,
            out Mock<IPluginManager> pluginManagerMock,
            out Mock<IFileWatchIgnoreList> ignoreListMock,
            watchedDirectory);

        FilePath ignoredFile = new FilePath(Path.Combine(_tempDirectory, "Ignored.txt"));
        ignoreListMock.Setup(i => i.TryGetIgnoreFileChange(ignoredFile)).Returns(true);

        sut.EnableWithDirectories(new HashSet<FilePath> { watchedDirectory });

        File.WriteAllText(ignoredFile.FullPath, "contents");

        // Negative assertion: poll for the un-ignored behavior (queued) and expect it NOT to
        // happen within the window, rather than a fixed sleep guessing at "long enough".
        PollUntil(() => sut.ChangedFilesWaitingForFlush.Contains(ignoredFile), timeoutMilliseconds: 1000)
            .ShouldBeFalse("an ignored file change should never be queued for flush");

        pluginManagerMock.Verify(p => p.ReactToFileChanged(ignoredFile), Times.Never);
    }
}
