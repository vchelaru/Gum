using Gum.Commands;
using Gum.DataTypes;
using Gum.Diagnostics;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Concurrent;
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
        FilePath projectDirectory,
        IDispatcher? dispatcher = null,
        ICrashReporter? crashReporter = null)
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
            pluginManagerMock.Object,
            new Mock<Gum.Services.Dialogs.IDialogService>().Object,
            new UnsavedChangesTracker());

        return new FileWatchManager(
            guiCommandsMock.Object,
            projectManagerMock.Object,
            fileChangeReactionLogic,
            ignoreListMock.Object,
            dispatcher ?? new SynchronousDispatcher(),
            crashReporter);
    }

    private sealed class SynchronousDispatcher : IDispatcher
    {
        public void Invoke(Action action) => action();
        public void Post(Action action) => action();
    }

    /// <summary>Stands in for the UI thread: posted work waits until the test drains it.</summary>
    private sealed class QueuedDispatcher : IDispatcher
    {
        private readonly ConcurrentQueue<Action> _queue = new();
        public void Invoke(Action action) => action();
        public void Post(Action action) => _queue.Enqueue(action);
        public void RunPending()
        {
            while (_queue.TryDequeue(out Action? action)) action();
        }
    }

    private sealed class ThrowingDispatcher : IDispatcher
    {
        public void Invoke(Action action) => throw new InvalidOperationException("dispatcher shut down");
        public void Post(Action action) => throw new InvalidOperationException("dispatcher shut down");
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
    public void HandleWatcherError_ShouldReportTheWatchedDirectoryToOutput()
    {
        FilePath watchedDirectory = new FilePath(_tempDirectory + "/");
        FileWatchManager sut = BuildSut(out Mock<IGuiCommands> guiCommandsMock, out _, out _, watchedDirectory);
        using FileSystemWatcher watcher = new FileSystemWatcher(_tempDirectory);

        sut.HandleWatcherError(watcher, new ErrorEventArgs(new InternalBufferOverflowException("too many changes")));

        guiCommandsMock.Verify(g => g.PrintOutput(It.Is<string>(s =>
            s.Contains(_tempDirectory) && s.Contains("too many changes"))), Times.Once);
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

    [Theory]
    [InlineData("gusx")]
    [InlineData("gusj")]
    [InlineData("gucx")]
    [InlineData("gucj")]
    [InlineData("gutx")]
    [InlineData("gutj")]
    public void IsElementFileExtension_ShouldReturnTrue_ForXmlAndJsonElementExtensions(string extension)
    {
        // Issue #4182: a JSON-converted project's element files must be recognized the same way
        // as their XML counterparts (used to flag missing/reappeared elements on external delete).
        FilePath file = new FilePath($@"C:\proj\Components\MyButton.{extension}");

        FileWatchManager.IsElementFileExtension(file).ShouldBeTrue();
    }

    [Fact]
    public void IsElementFileExtension_ShouldReturnFalse_ForNonElementExtension()
    {
        FilePath file = new FilePath(@"C:\proj\Components\MyButtonAnimations.ganx");

        FileWatchManager.IsElementFileExtension(file).ShouldBeFalse();
    }

    [Fact]
    public void EnableWithDirectories_ThenFlush_ShouldReactToJsonComponentRenamedIntoWatchedDirectory()
    {
        // A rename (the atomic-save pattern many editors/tools use) into a .gucj name must be
        // recognized the same way as a .gucx rename (issue #4182).
        FilePath watchedDirectory = new FilePath(_tempDirectory + "/");
        FileWatchManager sut = BuildSut(
            out Mock<IGuiCommands> guiCommandsMock,
            out Mock<IPluginManager> pluginManagerMock,
            out _,
            watchedDirectory);

        sut.EnableWithDirectories(new HashSet<FilePath> { watchedDirectory });

        string tempName = Path.Combine(_tempDirectory, "MyComponent.gucj.tmp");
        File.WriteAllText(tempName, "{}");
        FilePath renamedFile = new FilePath(Path.Combine(_tempDirectory, "MyComponent.gucj"));
        File.Move(tempName, renamedFile.FullPath);

        PollUntil(() => sut.ChangedFilesWaitingForFlush.Contains(renamedFile)).ShouldBeTrue(
            "a rename onto a .gucj file should be treated like any other recognized Gum file change");
    }

    [Fact]
    public void EnableWithDirectories_ShouldCreateDirectory_WhenWatchedDirectoryDoesNotExistYet()
    {
        // Issue #4259: watching a directory that hasn't been created yet (e.g. FontCache/ before
        // the first font is generated) used to throw and log a spurious error. The directory
        // should simply be created so the watch can proceed.
        string missingDirectory = Path.Combine(_tempDirectory, "FontCache");
        FilePath watchedDirectory = new FilePath(missingDirectory + "/");
        FileWatchManager sut = BuildSut(
            out Mock<IGuiCommands> guiCommandsMock,
            out _,
            out _,
            watchedDirectory);

        sut.EnableWithDirectories(new HashSet<FilePath> { watchedDirectory });

        Directory.Exists(missingDirectory).ShouldBeTrue();
        sut.Enabled.ShouldBeTrue();
        guiCommandsMock.Verify(g => g.PrintOutput(It.IsAny<string>()), Times.Never);
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

    [Fact]
    public void HandleFileSystemChange_RaisedOffUiThreadWhileWatchedDirectoriesChange_DoesNotThrow()
    {
        // FileSystemWatcher raises events on a thread-pool thread while the UI thread replaces the
        // watcher list on project load. Hammer both sides at once for a fixed number of events; the
        // unfixed code threw within a few hundred of them, so a regression fails reliably.
        FilePath watchedDirectory = new FilePath(_tempDirectory + "/");
        var uiThread = new QueuedDispatcher();
        var crashReporter = new Mock<ICrashReporter>();
        FileWatchManager sut = BuildSut(out _, out _, out _, watchedDirectory, uiThread, crashReporter.Object);
        var args = new FileSystemEventArgs(WatcherChangeTypes.Changed, _tempDirectory, "Texture.png");
        var watcherThreadExceptions = new List<Exception>();
        using var startTogether = new Barrier(2);
        const int eventCount = 20_000;

        var watcherThread = new Thread(() =>
        {
            startTogether.SignalAndWait();
            for (int i = 0; i < eventCount && watcherThreadExceptions.Count == 0; i++)
            {
                try
                {
                    sut.HandleFileSystemChange(null, args);
                }
                catch (Exception e)
                {
                    watcherThreadExceptions.Add(e);
                }
            }
        });
        watcherThread.Start();

        startTogether.SignalAndWait();
        while (watcherThread.IsAlive)
        {
            sut.EnableWithDirectories(new HashSet<FilePath> { watchedDirectory });
            uiThread.RunPending();
            sut.Disable();
        }
        watcherThread.Join();

        watcherThreadExceptions.ShouldBeEmpty();
        // The callbacks' catch-all must not be what kept the exceptions away.
        crashReporter.Verify(c => c.ReportRecoverable(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);

        // The marshaled events still reach the queue once the UI thread runs them.
        sut.EnableWithDirectories(new HashSet<FilePath> { watchedDirectory });
        sut.HandleFileSystemChange(null, args);
        uiThread.RunPending();
        sut.ChangedFilesWaitingForFlush.ShouldContain(new FilePath(args.FullPath));
    }

    [Fact]
    public void HandleFileSystemChange_IgnoredWhenRaisedButUiThreadRunsAfterWindow_StaysIgnored()
    {
        // Gum's own save must stay ignored even when the UI thread is busy past the ignore window.
        FilePath watchedDirectory = new FilePath(_tempDirectory + "/");
        var uiThread = new QueuedDispatcher();
        FileWatchManager sut = BuildSut(
            out _, out _, out Mock<IFileWatchIgnoreList> ignoreListMock, watchedDirectory, uiThread);
        sut.EnableWithDirectories(new HashSet<FilePath> { watchedDirectory });
        var args = new FileSystemEventArgs(WatcherChangeTypes.Changed, _tempDirectory, "Saved.png");
        ignoreListMock.Setup(i => i.TryGetIgnoreFileChange(It.IsAny<FilePath>())).Returns(true);

        sut.HandleFileSystemChange(null, args);
        ignoreListMock.Setup(i => i.TryGetIgnoreFileChange(It.IsAny<FilePath>())).Returns(false);
        uiThread.RunPending();

        sut.ChangedFilesWaitingForFlush.ShouldNotContain(new FilePath(args.FullPath));
    }

    [Fact]
    public void HandleFileSystemChange_WhenReactionThrows_ReportsInsteadOfThrowing()
    {
        FilePath watchedDirectory = new FilePath(_tempDirectory + "/");
        var crashReporter = new Mock<ICrashReporter>();
        FileWatchManager sut = BuildSut(
            out Mock<IGuiCommands> guiCommandsMock, out _, out Mock<IFileWatchIgnoreList> ignoreListMock,
            watchedDirectory, crashReporter: crashReporter.Object);
        sut.PrintFileChangesToOutput = true;
        ignoreListMock.Setup(i => i.TryGetIgnoreFileChange(It.IsAny<FilePath>())).Returns(true);
        var failure = new InvalidOperationException("reaction failed");
        guiCommandsMock.Setup(g => g.PrintOutput(It.Is<string>(s => s.StartsWith("File change skipped")))).Throws(failure);
        var args = new FileSystemEventArgs(WatcherChangeTypes.Changed, _tempDirectory, "Texture.png");

        Should.NotThrow(() => sut.HandleFileSystemChange(null, args));

        crashReporter.Verify(c => c.ReportRecoverable(failure, It.IsAny<string>()), Times.Once);
        guiCommandsMock.Verify(g => g.PrintOutput(It.Is<string>(s => s.Contains("reaction failed"))), Times.Once);
    }

    [Fact]
    public void HandleFileSystemChange_WhenDispatcherRejectsWork_ReportsInsteadOfThrowing()
    {
        FilePath watchedDirectory = new FilePath(_tempDirectory + "/");
        var crashReporter = new Mock<ICrashReporter>();
        FileWatchManager sut = BuildSut(
            out _, out _, out _, watchedDirectory, new ThrowingDispatcher(), crashReporter.Object);
        var args = new FileSystemEventArgs(WatcherChangeTypes.Changed, _tempDirectory, "Texture.png");

        Should.NotThrow(() => sut.HandleFileSystemChange(null, args));

        crashReporter.Verify(c => c.ReportRecoverable(
            It.Is<Exception>(e => e.Message == "dispatcher shut down"), It.IsAny<string>()), Times.Once);
    }
}
