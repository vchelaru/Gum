using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
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
using System.Linq;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Which watcher event sequences queue which files. The sequences are what editors and OSes
/// produce for one save: macOS reports an atomic swap as Created alone, others report Changed,
/// Created+Changed, a rename over the target, or a delete followed by a create.
/// </summary>
public class FileWatchEventMatrixTests : BaseTestClass
{
    public enum Sequence { CreatedOnly, ChangedOnly, CreatedThenChanged, RenamedOver, DeletedThenCreated }

    private readonly string _root;
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly Mock<IFileWatchIgnoreList> _ignoreList = new();
    private readonly Mock<IPluginManager> _pluginManager = new();
    private FileWatchManager _sut = null!;

    public FileWatchEventMatrixTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "FileWatchMatrix_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
        _ignoreList.Setup(i => i.TryGetIgnoreFileChange(It.IsAny<FilePath>())).Returns(false);
    }

    public override void Dispose()
    {
        _sut?.Disable();
        try { Directory.Delete(_root, recursive: true); } catch { /* best-effort */ }
        base.Dispose();
    }

    private sealed class SynchronousDispatcher : IDispatcher
    {
        public void Invoke(Action action) => action();
        public void Post(Action action) => action();
    }

    private void StartProject(bool isJson)
    {
        string gum = isJson ? "j" : "x";
        string projectFile = Path.Combine(_root, "Project.gum" + gum);
        GumProjectSave project = new() { FullFileName = projectFile };
        project.Components.Add(new ComponentSave { Name = "Styles" });
        project.Screens.Add(new ScreenSave { Name = "Main" });
        project.StandardElements.Add(new StandardElementSave { Name = "Rectangle" });
        project.Behaviors.Add(new BehaviorSave { Name = "Clickable" });
        ObjectFinder.Self.GumProjectSave = project;

        Mock<IFileCommands> fileCommands = new();
        fileCommands.Setup(f => f.ProjectDirectory).Returns(new FilePath(_root + "/"));
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.GumProjectSave).Returns(project);
        Mock<IProjectManager> projectManager = new();
        projectManager.Setup(p => p.GumProjectSave).Returns(project);

        FileChangeReactionLogic reaction = new(
            new Mock<ISelectedState>().Object, new Mock<IWireframeCommands>().Object, _guiCommands.Object,
            fileCommands.Object, new Mock<IOutputManager>().Object, new Mock<IWireframeObjectManager>().Object,
            projectState.Object, new Mock<IStandardElementsManagerGumTool>().Object, _pluginManager.Object,
            new Mock<Gum.Services.Dialogs.IDialogService>().Object, new UnsavedChangesTracker(),
            new Mock<Gum.Plugins.InternalPlugins.VariableGrid.IVariableReferenceLogic>().Object);

        _sut = new FileWatchManager(_guiCommands.Object, projectManager.Object, reaction, _ignoreList.Object,
            new SynchronousDispatcher(), new FileSystemWatcherFactory());
        _sut.EnableWithDirectories(new HashSet<FilePath> { new FilePath(_root + "/") });
    }

    private void Feed(string relativePath, Sequence sequence)
    {
        string fullPath = Path.Combine(_root, relativePath);
        string directory = Path.GetDirectoryName(fullPath)!;
        string name = Path.GetFileName(fullPath);
        FileSystemEventArgs Args(WatcherChangeTypes type) => new(type, directory, name);
        switch (sequence)
        {
            case Sequence.CreatedOnly:
                _sut.HandleFileSystemChange(null, Args(WatcherChangeTypes.Created));
                break;
            case Sequence.ChangedOnly:
                _sut.HandleFileSystemChange(null, Args(WatcherChangeTypes.Changed));
                break;
            case Sequence.CreatedThenChanged:
                _sut.HandleFileSystemChange(null, Args(WatcherChangeTypes.Created));
                _sut.HandleFileSystemChange(null, Args(WatcherChangeTypes.Changed));
                break;
            case Sequence.RenamedOver:
                _sut.HandleRename(null, new RenamedEventArgs(WatcherChangeTypes.Renamed, directory, name, name + ".tmp.1"));
                break;
            case Sequence.DeletedThenCreated:
                _sut.HandleFileSystemDelete(null, Args(WatcherChangeTypes.Deleted));
                _sut.HandleFileSystemChange(null, Args(WatcherChangeTypes.Created));
                break;
        }
    }

    private bool IsQueued(string relativePath) =>
        _sut.ChangedFilesWaitingForFlush.Contains(new FilePath(Path.Combine(_root, relativePath)));

    // (isJson, relative path, known to the project, is a Gum file)
    private static IEnumerable<(string Path, bool Known, bool IsGum)> Files(bool isJson)
    {
        string g = isJson ? "j" : "x";
        string[] element = isJson ? new[] { "c", "s", "t" } : new[] { "c", "s", "t" };
        yield return ($"Project.gum{g}", true, true);
        yield return ($"Other.gum{g}", false, true);
        yield return ($"Components/Styles.gu{element[0]}{g}", true, true);
        yield return ($"Components/Brand.gu{element[0]}{g}", false, true);
        yield return ($"Screens/Main.gu{element[1]}{g}", true, true);
        yield return ($"Screens/New.gu{element[1]}{g}", false, true);
        yield return ($"Standards/Rectangle.gu{element[2]}{g}", true, true);
        yield return ($"Components/StylesAnimations.gan{g}", true, true);
        yield return ($"Components/BrandAnimations.gan{g}", false, true);
        yield return ($"Behaviors/Clickable.beh{g}", true, true);
        yield return ($"Behaviors/Fresh.beh{g}", false, true);
        yield return ("Textures/Photo.png", true, false);
        yield return ("FontCache/Font18Arial.fnt", true, false);
        yield return ("Strings.csv", true, false);
        yield return ("Strings.resx", true, false);
        yield return ("ProjectCodeSettings.codsj", true, false);
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (bool isJson in new[] { true, false })
        {
            foreach ((string path, bool known, bool isGum) in Files(isJson))
            {
                foreach (Sequence sequence in Enum.GetValues<Sequence>())
                {
                    yield return new object[] { isJson, path, known, isGum, sequence };
                }
            }
        }
    }

    /// <summary>
    /// Every sequence queues a known file. A Gum file the project does not contain keeps the old
    /// behavior: Created alone is dropped (a new file nothing has loaded), the rest queue as before.
    /// </summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void Sequence_QueuesTheFile_WhenTheProjectKnowsIt(bool isJson, string path, bool known, bool isGum, Sequence sequence)
    {
        StartProject(isJson);

        Feed(path, sequence);

        bool expectQueued = known || !isGum || sequence is not (Sequence.CreatedOnly);
        // An unknown Gum file deleted then recreated is queued by the delete half; a Created alone is not.
        if (!known && isGum && sequence == Sequence.DeletedThenCreated)
        {
            expectQueued = path.Contains("/") && IsElementFile(path) ;
        }
        IsQueued(path).ShouldBe(expectQueued, $"{sequence} on {path}");
    }

    private static bool IsElementFile(string path) =>
        path.EndsWith(".gucj") || path.EndsWith(".gucx") || path.EndsWith(".gusj") || path.EndsWith(".gusx")
        || path.EndsWith(".gutj") || path.EndsWith(".gutx");

    [Theory]
    [InlineData(Sequence.CreatedOnly)]
    [InlineData(Sequence.CreatedThenChanged)]
    [InlineData(Sequence.RenamedOver)]
    [InlineData(Sequence.DeletedThenCreated)]
    public void GumsOwnSave_IsNotQueued_WhateverTheSequence(Sequence sequence)
    {
        StartProject(isJson: true);
        _ignoreList.Setup(i => i.TryGetIgnoreFileChange(new FilePath(Path.Combine(_root, "Components/Styles.gucj")))).Returns(true);

        Feed("Components/Styles.gucj", sequence);

        IsQueued("Components/Styles.gucj").ShouldBeFalse();
    }

    [Fact]
    public void CreatedThenChanged_ReactsOncePerFlush()
    {
        string file = Path.Combine(_root, "Components", "Styles.gucj");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "{}");
        StartProject(isJson: true);

        Feed("Components/Styles.gucj", Sequence.CreatedThenChanged);
        Feed("Components/Styles.gucj", Sequence.CreatedOnly);
        while (_sut.TimeToNextFlush.TotalMilliseconds > 0) System.Threading.Thread.Sleep(20);
        _sut.Flush();

        // A reload of the element is the reaction; two queued events must not reload it twice.
        _pluginManager.Verify(p => p.ElementReloaded(It.IsAny<ElementSave>()), Times.Once);
    }

    private void FlushWhenSettled()
    {
        while (_sut.TimeToNextFlush.TotalMilliseconds > 0) System.Threading.Thread.Sleep(20);
        _sut.Flush();
    }

    private List<string> StartPrinting()
    {
        _sut.PrintFileChangesToOutput = true;
        List<string> printed = new();
        _guiCommands.Setup(g => g.PrintOutput(It.IsAny<string>())).Callback<string>(printed.Add);
        return printed;
    }

    [Theory]
    [InlineData("Components/.Styles.gucj.tmp.123", Sequence.ChangedOnly, "temp file")]
    [InlineData("Components/Styles.gucj", Sequence.CreatedThenChanged, "saved this file itself")]
    [InlineData("Components/Brand.gucj", Sequence.CreatedOnly, "Created")]
    public void WhenPrintingIsOn_EveryDroppedEventPrintsItsReason(string path, Sequence sequence, string reasonFragment)
    {
        StartProject(isJson: true);
        _ignoreList.Setup(i => i.TryGetIgnoreFileChange(new FilePath(Path.Combine(_root, "Components/Styles.gucj")))).Returns(true);
        List<string> printed = StartPrinting();

        Feed(path, sequence);
        FlushWhenSettled();

        printed.ShouldContain(line => line.Contains(reasonFragment) && line.Contains(Path.GetFileName(path)),
            $"printed: [{string.Join(" | ", printed)}]");
    }

    [Theory]
    [InlineData(Sequence.RenamedOver)]
    [InlineData(Sequence.DeletedThenCreated)]
    public void RenameOrDeleteOfAFileTypeGumDoesNotReactTo_PrintsNothing(Sequence sequence)
    {
        StartProject(isJson: true);
        List<string> printed = StartPrinting();

        Feed("bin/App.dll", sequence);
        FlushWhenSettled();

        printed.ShouldBeEmpty();
    }

    [Fact]
    public void ABuildTouchingManyFiles_PrintsFewLines_AndStillExplainsAGumFileDrop()
    {
        StartProject(isJson: true);
        List<string> printed = StartPrinting();
        string elsewhere = Path.Combine(Path.GetTempPath(), "NotWatched_" + Path.GetRandomFileName());
        Directory.CreateDirectory(elsewhere);
        try
        {
            for (int i = 0; i < 300; i++)
            {
                // Watched folder: queued or not reacted to. Unwatched folder: skipped, 3 events each.
                foreach (Sequence sequence in new[] { Sequence.CreatedThenChanged, Sequence.RenamedOver, Sequence.DeletedThenCreated, Sequence.ChangedOnly })
                {
                    Feed($"bin/Out{i}.dll", sequence);
                }
                foreach (Sequence sequence in new[] { Sequence.CreatedThenChanged, Sequence.DeletedThenCreated })
                {
                    Feed(Path.GetRelativePath(_root, Path.Combine(elsewhere, $"Out{i}.dll")), sequence);
                }
            }
            Feed("Components/Brand.gucj", Sequence.CreatedOnly);
            FlushWhenSettled();
        }
        finally
        {
            Directory.Delete(elsewhere, recursive: true);
        }

        printed.Count.ShouldBeLessThan(6, $"printed {printed.Count} lines");
        printed.ShouldContain(line => line.Contains("directory not watched") && line.Contains("x") && line.Contains("e.g."));
        printed.ShouldContain(line => line.Contains("Created event for a Gum file") && line.Contains("Brand.gucj"));
    }

    [Fact]
    public void CreatedWhoseFolderDiffersOnlyByCase_IsKnownToTheProject()
    {
        // macOS volumes are case-insensitive, so a path reported as components/Styles.gucj is Components/Styles.gucj.
        StartProject(isJson: true);

        Feed("components/Styles.gucj", Sequence.CreatedOnly);

        IsQueued("components/Styles.gucj").ShouldBeTrue();
    }
}
