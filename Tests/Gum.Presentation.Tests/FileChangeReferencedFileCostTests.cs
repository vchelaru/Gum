using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// What the referenced-file reaction (#7027) costs and which files it leaves to other reactions: the project
/// is walked once for many file events rather than once per event, a file the canvas cached counts the same as
/// a referenced one, and animation files keep their own reload.
/// </summary>
public class FileChangeReferencedFileCostTests : BaseTestClass
{
    private readonly string _root;
    private readonly string _projectFolder;
    private readonly Mock<IWireframeObjectManager> _wireframe = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IFileWatchIgnoreList> _ignoreList = new();
    private readonly GumProjectSave _project;
    private readonly SpyReferencedFiles _referencedFiles;
    private FileChangeReactionLogic _reaction;
    private FileWatchManager? _watch;

    /// <summary>The real index, with the walks it makes counted and the calls to forget it recorded.</summary>
    private sealed class SpyReferencedFiles : IProjectReferencedFiles
    {
        private readonly ProjectReferencedFiles _inner;

        public int Walks;
        public int Invalidations;

        public SpyReferencedFiles() => _inner = new ProjectReferencedFiles(() =>
        {
            Walks++;
            return ObjectFinder.Self.GetAllFilesInProject(
                Gum.Bundle.GumBundleInclusion.Core | Gum.Bundle.GumBundleInclusion.ExternalFiles);
        });

        public bool Contains(FilePath file) => _inner.Contains(file);

        public void Invalidate()
        {
            Invalidations++;
            _inner.Invalidate();
        }
    }

    public FileChangeReferencedFileCostTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "Gum7027Cost_" + Path.GetRandomFileName());
        _projectFolder = Path.Combine(_root, "Project");
        Directory.CreateDirectory(Path.Combine(_projectFolder, "Textures"));

        _project = new GumProjectSave { FullFileName = Path.Combine(_projectFolder, "Project.gumx") };
        ObjectFinder.Self.GumProjectSave = _project;

        _wireframe.Setup(w => w.AllIpsos).Returns(new List<GraphicalUiElement>());
        _ignoreList.Setup(i => i.TryGetIgnoreFileChange(It.IsAny<FilePath>())).Returns(false);
        _fileCommands.Setup(f => f.ProjectDirectory).Returns(new FilePath(_projectFolder + "/"));
        _projectState.Setup(p => p.GumProjectSave).Returns(_project);

        _referencedFiles = new SpyReferencedFiles();
        _reaction = NewReaction();

        LoaderManager.Self.DisposeAndClear();
    }

    public override void Dispose()
    {
        _watch?.Disable();
        LoaderManager.Self.DisposeAndClear();
        try { Directory.Delete(_root, recursive: true); } catch { /* best-effort */ }
        base.Dispose();
    }

    private FileChangeReactionLogic NewReaction() => new FileChangeReactionLogic(
        _selectedState.Object, new Mock<IWireframeCommands>().Object, new Mock<IGuiCommands>().Object,
        _fileCommands.Object, new Mock<IOutputManager>().Object, _wireframe.Object,
        _projectState.Object, new Mock<IStandardElementsManagerGumTool>().Object, new Mock<IPluginManager>().Object,
        new Mock<Gum.Services.Dialogs.IDialogService>().Object, new UnsavedChangesTracker(),
        new Mock<IVariableReferenceLogic>().Object, new Mock<Gum.Undo.IUndoManager>().Object, _referencedFiles);

    private sealed class Cached : IDisposable
    {
        public void Dispose() { }
    }

    private sealed class SynchronousDispatcher : IDispatcher
    {
        public void Invoke(Action action) => action();
        public void Post(Action action) => action();
    }

    private ScreenSave AddScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);
        return screen;
    }

    private static void AddPicture(ElementSave element, string instance, string file)
    {
        element.Instances.Add(new InstanceSave { Name = instance, BaseType = "Sprite", ParentContainer = element });
        element.DefaultState.Variables.Add(new VariableSave
        {
            Name = instance + ".SourceFile", Type = "string", Value = file, IsFile = true, SetsValue = true,
        });
    }

    private string WriteFile(string relativePath, string contents = "content")
    {
        string path = Path.GetFullPath(Path.Combine(_projectFolder, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    private string PathOf(string relativePath) => Path.GetFullPath(Path.Combine(_projectFolder, relativePath));

    private FileWatchManager StartWatching()
    {
        Mock<IProjectManager> projectManager = new();
        projectManager.Setup(p => p.GumProjectSave).Returns(_project);
        _watch = new FileWatchManager(new Mock<IGuiCommands>().Object, projectManager.Object, _reaction, _ignoreList.Object,
            new SynchronousDispatcher(), new FileSystemWatcherFactory());
        _watch.EnableWithDirectories(new HashSet<FilePath> { new FilePath(_projectFolder + "/") });
        return _watch;
    }

    private static void FlushWhenSettled(FileWatchManager watch)
    {
        while (watch.TimeToNextFlush.TotalMilliseconds > 0) System.Threading.Thread.Sleep(20);
        watch.Flush();
    }

    private static FileSystemEventArgs Deleted(string fullPath) =>
        new(WatcherChangeTypes.Deleted, Path.GetDirectoryName(fullPath)!, Path.GetFileName(fullPath));

    private static FileSystemEventArgs Changed(string fullPath) =>
        new(WatcherChangeTypes.Changed, Path.GetDirectoryName(fullPath)!, Path.GetFileName(fullPath));

    private static RenamedEventArgs Renamed(string fullPath) =>
        new(WatcherChangeTypes.Renamed, Path.GetDirectoryName(fullPath)!, Path.GetFileName(fullPath), Path.GetFileName(fullPath) + ".tmp.1");

    private void VerifyReloaded(Times times) => _wireframe.Verify(w => w.RefreshAll(true, true), times);

    [Fact]
    public void ManyRenamesAndDeletesOfUnrelatedFiles_WalkTheProjectAtMostOnce()
    {
        ScreenSave screen = AddScreen("Main");
        AddPicture(screen, "Item", "Textures/Photo.png");
        FileWatchManager watch = StartWatching();

        for (int i = 0; i < 40; i++)
        {
            watch.HandleRename(null, Renamed(PathOf($"bin/Debug/Lib{i}.dll")));
            watch.HandleFileSystemDelete(null, Deleted(PathOf($"obj/Debug/Gen{i}.cache")));
        }

        _referencedFiles.Walks.ShouldBeLessThanOrEqualTo(1);
        watch.ChangedFilesWaitingForFlush.ShouldBeEmpty();
    }

    [Fact]
    public void ARenameOrDeleteOfAReferencedFile_IsStillQueued_AfterManyUnrelatedEvents()
    {
        ScreenSave screen = AddScreen("Main");
        AddPicture(screen, "Item", "Textures/Photo.webp");
        AddPicture(screen, "Other", "Textures/Other.webp");
        FileWatchManager watch = StartWatching();
        for (int i = 0; i < 20; i++)
        {
            watch.HandleFileSystemDelete(null, Deleted(PathOf($"bin/Gen{i}.cache")));
        }

        watch.HandleRename(null, Renamed(PathOf("Textures/Photo.webp")));
        watch.HandleFileSystemDelete(null, Deleted(PathOf("Textures/Other.webp")));

        watch.ChangedFilesWaitingForFlush.Select(f => Path.GetFileName(f.FullPath))
            .OrderBy(name => name).ShouldBe(new[] { "Other.webp", "Photo.webp" });
        _referencedFiles.Walks.ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public void AFlushOfManyFilesNothingReferences_WalksTheProjectAtMostOnce()
    {
        ScreenSave screen = AddScreen("Main");
        AddPicture(screen, "Item", "Textures/Photo.png");
        FileWatchManager watch = StartWatching();
        for (int i = 0; i < 20; i++)
        {
            watch.HandleFileSystemChange(null, Changed(WriteFile($"bin/Lib{i}.dll")));
        }

        FlushWhenSettled(watch);

        watch.ChangedFilesWaitingForFlush.ShouldBeEmpty();
        _referencedFiles.Walks.ShouldBeLessThanOrEqualTo(1);
        VerifyReloaded(Times.Never());
    }

    [Fact]
    public void AFlushOfSeveralDeletedPictures_ReloadsTheCanvasOnce()
    {
        ScreenSave screen = AddScreen("Main");
        for (int i = 0; i < 3; i++)
        {
            AddPicture(screen, "Item" + i, $"Textures/Photo{i}.png");
        }
        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
        FileWatchManager watch = StartWatching();
        for (int i = 0; i < 3; i++)
        {
            watch.HandleFileSystemDelete(null, Deleted(PathOf($"Textures/Photo{i}.png")));
        }

        FlushWhenSettled(watch);

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void ARenameOfAFileTheCanvasCachedButNoElementReferences_IsQueued()
    {
        AddScreen("Main");
        string cachedFile = WriteFile("Generated/Thing.xyz");
        LoaderManager.Self.AddDisposable(cachedFile, new Cached());
        FileWatchManager watch = StartWatching();

        watch.HandleRename(null, Renamed(cachedFile));

        watch.ChangedFilesWaitingForFlush.ShouldContain(new FilePath(cachedFile));
    }

    [Fact]
    public void ADeleteOfAFileTheCanvasCachedButNoElementReferences_ReloadsTheCanvas()
    {
        // A font the tool generated into FontCache is cached by path but no variable names it.
        _selectedState.Setup(s => s.SelectedElement).Returns(AddScreen("Main"));
        string cachedFile = PathOf("FontCache/Font18Arial.fnt");
        LoaderManager.Self.AddDisposable(cachedFile, new Cached());
        FileWatchManager watch = StartWatching();

        watch.HandleFileSystemDelete(null, Deleted(cachedFile));
        FlushWhenSettled(watch);

        VerifyReloaded(Times.Once());
    }

    private void ArrangeAnimationFileOfAReferencedElement(out string animationFile)
    {
        ComponentSave card = new ComponentSave { Name = "Card", BaseType = "Container" };
        card.States.Add(new StateSave { Name = "Default", ParentContainer = card });
        _project.Components.Add(card);
        _project.ComponentReferences.Add(new ElementReference { Name = "Card", ElementType = ElementType.Component });
        _selectedState.Setup(s => s.SelectedElement).Returns(card);
        animationFile = WriteFile("Components/CardAnimations.ganx");

        // The animation sidecar is part of what the project lists, which is what once made it look referenced.
        _reaction.IsReferencedByProject(new FilePath(animationFile)).ShouldBeTrue();
        _referencedFiles.Invalidate();
    }

    [Fact]
    public void AChangedAnimationFile_DoesNotReloadTheCanvas_BecauseTheAnimationPluginHandlesIt()
    {
        ArrangeAnimationFileOfAReferencedElement(out string animationFile);

        _reaction.ReactToFileChanged(new FilePath(animationFile));

        _wireframe.Verify(w => w.RefreshAll(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
    }

    [Fact]
    public void ADeletedAnimationFile_IsNotQueuedAndDoesNotReloadTheCanvas()
    {
        ArrangeAnimationFileOfAReferencedElement(out string animationFile);
        FileWatchManager watch = StartWatching();

        watch.HandleFileSystemDelete(null, Deleted(animationFile));
        _reaction.ReactToFileDeleted(new FilePath(animationFile));

        watch.ChangedFilesWaitingForFlush.ShouldBeEmpty();
        _wireframe.Verify(w => w.RefreshAll(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
    }

    [Fact]
    public void AFlushOfSeveralPictureEdits_WalksTheProjectOnce()
    {
        ScreenSave screen = AddScreen("Main");
        for (int i = 0; i < 5; i++)
        {
            AddPicture(screen, "Item" + i, $"Textures/Photo{i}.png");
        }
        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
        FileWatchManager watch = StartWatching();
        for (int i = 0; i < 5; i++)
        {
            watch.HandleFileSystemChange(null, Changed(WriteFile($"Textures/Photo{i}.png")));
        }

        FlushWhenSettled(watch);

        VerifyReloaded(Times.Once());
        _referencedFiles.Walks.ShouldBe(1);
    }

    [Fact]
    public void AnAnimationChainEdit_MakesTheNewFramePicturesCountAsReferenced()
    {
        // The set lists the pictures an animation chain file names, so an edit of that file changes it.
        ScreenSave screen = AddScreen("Main");
        screen.Instances.Add(new InstanceSave { Name = "Item", BaseType = "Sprite", ParentContainer = screen });
        screen.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Item.AnimationChains", Type = "string", Value = "Animations/Idle.achx", IsFile = true, SetsValue = true,
        });
        string FrameXml(string texture) =>
            "<?xml version=\"1.0\"?><AnimationChainArraySave><FileRelativeTextures>false</FileRelativeTextures>" +
            "<AnimationChain><Name>Idle</Name><Frame><TextureName>" + texture + "</TextureName>" +
            "<FrameLength>0.1</FrameLength></Frame></AnimationChain></AnimationChainArraySave>";
        string chainFile = WriteFile("Animations/Idle.achx", FrameXml("Textures/Old.png"));
        FilePath newFrame = new FilePath(PathOf("Textures/New.png"));
        _reaction.IsReferencedByProject(newFrame).ShouldBeFalse();

        WriteFile("Animations/Idle.achx", FrameXml("Textures/New.png"));
        _reaction.ReactToFileChanged(new FilePath(chainFile));

        _reaction.IsReferencedByProject(newFrame).ShouldBeTrue();
    }

    #region Invalidation by a reload of an element, behavior or the project

    private string _loadedFolder = "";
    private SpyReferencedFiles _loadedReferences = null!;
    private FileChangeReactionLogic _loadedReaction = null!;

    /// <summary>Saves a project holding a component with a picture and a behavior, loads it back, and reacts to it.</summary>
    private void ArrangeLoadedProject()
    {
        _loadedFolder = Path.Combine(_root, "Loaded");
        Directory.CreateDirectory(_loadedFolder);
        GumProjectSave authored = new GumProjectSave { FullFileName = Path.Combine(_loadedFolder, "Project.gumj") };
        ComponentSave card = new ComponentSave { Name = "Card", BaseType = "Container" };
        card.States.Add(new StateSave { Name = "Default", ParentContainer = card });
        AddPicture(card, "Item", "Textures/Old.png");
        authored.Components.Add(card);
        authored.ComponentReferences.Add(new ElementReference { Name = "Card", ElementType = ElementType.Component });
        authored.BehaviorReferences.Add(new BehaviorReference { Name = "Check" });
        authored.Behaviors.Add(new BehaviorSave { Name = "Check" });
        ObjectFinder.Self.GumProjectSave = authored;
        authored.Initialize();
        authored.Save(authored.FullFileName, saveElements: true);
        Directory.CreateDirectory(Path.Combine(_loadedFolder, "Behaviors"));
        authored.Behaviors[0].Save(Path.Combine(_loadedFolder, "Behaviors", "Check.behj"));

        GumProjectSave loaded = GumProjectSave.Load(authored.FullFileName);
        ObjectFinder.Self.GumProjectSave = loaded;
        loaded.Initialize();

        Mock<IFileCommands> fileCommands = new();
        fileCommands.Setup(f => f.ProjectDirectory).Returns(new FilePath(_loadedFolder + Path.DirectorySeparatorChar));
        fileCommands.Setup(f => f.LoadProjectAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.GumProjectSave).Returns(loaded);
        _loadedReferences = new SpyReferencedFiles();
        _loadedReaction = new FileChangeReactionLogic(
            _selectedState.Object, new Mock<IWireframeCommands>().Object, new Mock<IGuiCommands>().Object,
            fileCommands.Object, new Mock<IOutputManager>().Object, _wireframe.Object,
            projectState.Object, new Mock<IStandardElementsManagerGumTool>().Object, new Mock<IPluginManager>().Object,
            new Mock<Gum.Services.Dialogs.IDialogService>().Object, new UnsavedChangesTracker(),
            new Mock<IVariableReferenceLogic>().Object, new Mock<Gum.Undo.IUndoManager>().Object, _loadedReferences);
    }

    [Fact]
    public void AnElementReloadedDuringAFlush_MakesTheFilesItNowReferencesCountAsReferenced()
    {
        ArrangeLoadedProject();
        FilePath newPicture = new FilePath(Path.Combine(_loadedFolder, "Textures", "New.png"));
        _loadedReaction.IsReferencedByProject(newPicture).ShouldBeFalse();
        string componentFile = Path.Combine(_loadedFolder, "Components", "Card.gucj");
        File.WriteAllText(componentFile, File.ReadAllText(componentFile).Replace("Textures/Old.png", "Textures/New.png"));

        using (_loadedReaction.DeferDependentUpdates())
        {
            _loadedReaction.ReactToFileChanged(new FilePath(componentFile));

            _loadedReaction.IsReferencedByProject(newPicture).ShouldBeTrue();
        }
    }

    [Fact]
    public void ABehaviorReloaded_ForgetsWhatTheProjectReferences()
    {
        ArrangeLoadedProject();
        _loadedReferences.Invalidations = 0;

        _loadedReaction.ReactToFileChanged(new FilePath(Path.Combine(_loadedFolder, "Behaviors", "Check.behj")));

        _loadedReferences.Invalidations.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TheProjectReloaded_ForgetsWhatTheProjectReferences()
    {
        ArrangeLoadedProject();
        _loadedReferences.Invalidations = 0;

        _loadedReaction.ReactToFileChanged(new FilePath(Path.Combine(_loadedFolder, "Project.gumj")));

        _loadedReferences.Invalidations.ShouldBeGreaterThan(0);
    }

    #endregion
}
