using Gum.Commands;
using Gum.DataTypes;
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// An external editor saving a component file nested in Components/ must be queued by the real
/// FileSystemWatcher for each way editors write: in place, temp-then-rename-over, truncate-then-write.
/// </summary>
public class FileWatchSaveStylesTests : IDisposable
{
    private readonly string _projectDirectory;

    // The roots the tool watches for a project like VarRef1: nested roots stay in the set because
    // FileWatchLogic only dedupes against roots already added, and the project directory goes last.
    private static readonly string[] WatchedFolders =
    {
        "Behaviors", "Components/Controls", "Components/Elements", "Components", "Screens", "Standards", "Fonts", "",
    };

    public FileWatchSaveStylesTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "FileWatchSaveStyles_" + Path.GetRandomFileName());
        foreach (string folder in WatchedFolders)
        {
            Directory.CreateDirectory(Path.Combine(_projectDirectory, folder));
        }
    }

    public void Dispose()
    {
        try { Directory.Delete(_projectDirectory, recursive: true); } catch { /* best-effort */ }
    }

    private sealed class SynchronousDispatcher : IDispatcher
    {
        public void Invoke(Action action) => action();
        public void Post(Action action) => action();
    }

    private static bool PollUntil(Func<bool> condition, int timeoutMilliseconds = 6000)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            Thread.Sleep(50);
        }
        return condition();
    }

    private FileWatchManager BuildWatchingProject(FilePath projectDirectory)
    {
        Mock<IFileWatchIgnoreList> ignoreList = new();
        ignoreList.Setup(i => i.TryGetIgnoreFileChange(It.IsAny<FilePath>())).Returns(false);

        Mock<IProjectManager> projectManager = new();
        projectManager.Setup(p => p.GumProjectSave)
            .Returns(new GumProjectSave { FullFileName = projectDirectory + "VarRef1.gumj" });

        FileChangeReactionLogic reaction = new(
            new Mock<ISelectedState>().Object,
            new Mock<IWireframeCommands>().Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IFileCommands>().Object,
            new Mock<IOutputManager>().Object,
            new Mock<IWireframeObjectManager>().Object,
            new Mock<IProjectState>().Object,
            new Mock<IStandardElementsManagerGumTool>().Object,
            new Mock<IPluginManager>().Object,
            new Mock<Gum.Services.Dialogs.IDialogService>().Object,
            new UnsavedChangesTracker(),
            new Mock<Gum.Plugins.InternalPlugins.VariableGrid.IVariableReferenceLogic>().Object);

        FileWatchManager sut = new(
            new Mock<IGuiCommands>().Object,
            projectManager.Object,
            reaction,
            ignoreList.Object,
            new SynchronousDispatcher(),
            new FileSystemWatcherFactory());

        HashSet<FilePath> roots = new();
        foreach (string folder in WatchedFolders)
        {
            roots.Add(new FilePath(Path.Combine(_projectDirectory, folder) + "/"));
        }
        sut.EnableWithDirectories(roots);
        return sut;
    }

    [Theory]
    [InlineData("InPlaceWrite")]
    [InlineData("TempThenRenameOver")]
    [InlineData("TruncateThenWrite")]
    public void ExternalSaveOfNestedComponentFile_IsQueuedForFlush(string saveStyle)
    {
        FilePath projectDirectory = new FilePath(_projectDirectory + "/");
        string target = Path.Combine(_projectDirectory, "Components", "Styles.gucj");
        File.WriteAllText(target, "{ \"Primary.FillRed\": 21 }");
        FileWatchManager sut = BuildWatchingProject(projectDirectory);
        Thread.Sleep(500); // let the watcher settle past its own setup

        string newContents = "{ \"Primary.FillRed\": 200 }";
        switch (saveStyle)
        {
            case "InPlaceWrite":
                File.WriteAllText(target, newContents);
                break;
            case "TempThenRenameOver":
                string temp = target + ".tmp";
                File.WriteAllText(temp, newContents);
                File.Move(temp, target, overwrite: true);
                break;
            case "TruncateThenWrite":
                using (FileStream stream = new(target, FileMode.Truncate, FileAccess.Write))
                {
                    Thread.Sleep(100);
                    stream.Write(System.Text.Encoding.UTF8.GetBytes(newContents));
                }
                break;
        }

        FilePath expected = new FilePath(target);
        PollUntil(() => sut.ChangedFilesWaitingForFlush.Contains(expected)).ShouldBeTrue(
            $"{saveStyle}: Components/Styles.gucj should be queued for flush");
        sut.Disable();
    }
}
