using Gum.Commands;
using Gum.DataTypes;
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
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Every file an element references (#7027): an image, svg, lottie json, animation chain, font or any other
/// file the project refers to refreshes the tool when it changes, is renamed over or is deleted on disk.
/// A sentinel disposable in <see cref="LoaderManager"/> stands in for the cached texture, font or document,
/// since a graphics device cannot be created in the test host on macOS.
/// </summary>
public class FileChangeReferencedFileReactionTests : BaseTestClass
{
    private readonly string _root;
    private readonly string _projectFolder;
    private readonly Mock<IWireframeObjectManager> _wireframe = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly Mock<IFileWatchIgnoreList> _ignoreList = new();
    private readonly FileChangeReactionLogic _reaction;
    private readonly GumProjectSave _project;
    private FileWatchManager? _watch;

    public FileChangeReferencedFileReactionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "Gum7027_" + Path.GetRandomFileName());
        _projectFolder = Path.Combine(_root, "Project");
        Directory.CreateDirectory(Path.Combine(_projectFolder, "Textures"));

        _project = new GumProjectSave { FullFileName = Path.Combine(_projectFolder, "Project.gumx") };
        ObjectFinder.Self.GumProjectSave = _project;

        _wireframe.Setup(w => w.AllIpsos).Returns(new List<GraphicalUiElement>());
        _ignoreList.Setup(i => i.TryGetIgnoreFileChange(It.IsAny<FilePath>())).Returns(false);

        Mock<IFileCommands> fileCommands = new();
        fileCommands.Setup(f => f.ProjectDirectory).Returns(new FilePath(_projectFolder + "/"));
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.GumProjectSave).Returns(_project);

        _reaction = new FileChangeReactionLogic(
            _selectedState.Object, new Mock<IWireframeCommands>().Object, _guiCommands.Object,
            fileCommands.Object, new Mock<IOutputManager>().Object, _wireframe.Object,
            projectState.Object, new Mock<IStandardElementsManagerGumTool>().Object, new Mock<IPluginManager>().Object,
            new Mock<Gum.Services.Dialogs.IDialogService>().Object, new UnsavedChangesTracker(),
            new Mock<IVariableReferenceLogic>().Object, new Mock<Gum.Undo.IUndoManager>().Object);

        LoaderManager.Self.DisposeAndClear();
    }

    public override void Dispose()
    {
        _watch?.Disable();
        LoaderManager.Self.DisposeAndClear();
        try { Directory.Delete(_root, recursive: true); } catch { /* best-effort */ }
        base.Dispose();
    }

    private sealed class Cached : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    private ComponentSave AddComponent(string name, string? baseType = null)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
        return component;
    }

    private ScreenSave AddScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);
        return screen;
    }

    private static void AddInstance(ElementSave element, string name, string baseType) =>
        element.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = element });

    private static void AddFileVariable(ElementSave element, string instance, string variable, string file)
    {
        element.DefaultState.Variables.Add(new VariableSave
        {
            Name = instance + "." + variable, Type = "string", Value = file, IsFile = true, SetsValue = true,
        });
        if (variable == "CustomFontFile")
        {
            // The font file only applies when the text says to use it.
            element.DefaultState.Variables.Add(new VariableSave
            {
                Name = instance + ".UseCustomFont", Type = "bool", Value = true, SetsValue = true,
            });
        }
    }

    private string WriteFile(string relativePath, string contents = "content")
    {
        string path = Path.GetFullPath(Path.Combine(_projectFolder, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    private void Select(ElementSave? element) =>
        _selectedState.Setup(s => s.SelectedElement).Returns(element);

    private void VerifyReloaded(Times times) =>
        _wireframe.Verify(w => w.RefreshAll(true, true), times);

    // (instance base type, variable, file) of every standard element variable marked as a file.
    public static IEnumerable<object[]> FileTypes()
    {
        yield return new object[] { "Sprite", "SourceFile", "Textures/Photo.png" };
        yield return new object[] { "Sprite", "SourceFile", "Textures/Photo.jpg" };
        yield return new object[] { "Sprite", "SourceFile", "Textures/Photo.gif" };
        yield return new object[] { "Sprite", "SourceFile", "Textures/Photo.bmp" };
        yield return new object[] { "Sprite", "SourceFile", "Textures/Photo.webp" };
        yield return new object[] { "Sprite", "AnimationChains", "Animations/Idle.achx" };
        yield return new object[] { "NineSlice", "SourceFile", "Textures/Frame.png" };
        yield return new object[] { "Svg", "SourceFile", "Art/Icon.svg" };
        yield return new object[] { "LottieAnimation", "SourceFile", "Art/Spinner.json" };
        yield return new object[] { "Text", "CustomFontFile", "Fonts/Custom.fnt" };
    }

    [Theory]
    [MemberData(nameof(FileTypes))]
    public void ReactToFileChanged_ReloadsTheCanvas_WhenTheSelectedElementReferencesTheFile(string baseType, string variable, string file)
    {
        ScreenSave screen = AddScreen("Main");
        AddInstance(screen, "Item", baseType);
        AddFileVariable(screen, "Item", variable, file);
        Select(screen);

        _reaction.ReactToFileChanged(new FilePath(WriteFile(file)));

        VerifyReloaded(Times.Once());
    }

    [Theory]
    [MemberData(nameof(FileTypes))]
    public void ReactToFileChanged_ReloadsTheCanvas_WhenTheFileIsUsedThroughANestedComponentInstance(string baseType, string variable, string file)
    {
        ComponentSave card = AddComponent("Card", "Container");
        AddInstance(card, "Item", baseType);
        AddFileVariable(card, "Item", variable, file);
        ScreenSave screen = AddScreen("Main");
        AddInstance(screen, "Card1", "Card");
        Select(screen);

        _reaction.ReactToFileChanged(new FilePath(WriteFile(file)));

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void ReactToFileChanged_ReloadsTheCanvas_WhenTheFileIsUsedByTheBaseComponent()
    {
        ComponentSave card = AddComponent("Card", "Container");
        AddInstance(card, "Item", "Sprite");
        AddFileVariable(card, "Item", "SourceFile", "Textures/Photo.png");
        ComponentSave derived = AddComponent("Derived", "Card");
        Select(derived);

        _reaction.ReactToFileChanged(new FilePath(WriteFile("Textures/Photo.png")));

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void ReactToFileChanged_ReloadsTheCanvas_WhenOnlyAnotherElementUsesTheFile()
    {
        // The reload drops the whole cache, or selecting that element later would draw the old picture.
        ScreenSave other = AddScreen("Other");
        AddInstance(other, "Item", "Sprite");
        AddFileVariable(other, "Item", "SourceFile", "Textures/Photo.png");
        ScreenSave selected = AddScreen("Selected");
        Select(selected);
        string path = WriteFile("Textures/Photo.png");
        LoaderManager.Self.AddDisposable(path, new Cached());

        _reaction.ReactToFileChanged(new FilePath(path));

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void ReactToFileChanged_ReloadsTheCanvas_WhenNothingIsSelected()
    {
        // The reload drops the whole cache even with nothing shown, so the next selection loads fresh.
        ScreenSave other = AddScreen("Other");
        AddInstance(other, "Item", "Sprite");
        AddFileVariable(other, "Item", "SourceFile", "Textures/Photo.png");
        Select(null);

        _reaction.ReactToFileChanged(new FilePath(WriteFile("Textures/Photo.png")));

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void ReactToFileChanged_ReloadsTheCanvas_WhenACachedFileNoVariableNamesChanges()
    {
        // A font the tool generated into FontCache is cached by path but is not a variable's value.
        ScreenSave screen = AddScreen("Main");
        Select(screen);
        string path = WriteFile("FontCache/Font18Arial.fnt");
        LoaderManager.Self.AddDisposable(path, new Cached());

        _reaction.ReactToFileChanged(new FilePath(path));

        VerifyReloaded(Times.Once());
    }

    [Theory]
    [InlineData("Textures/Photo.png")]
    [InlineData("Textures/Unused.svg")]
    [InlineData("bin/App.dll")]
    public void ReactToFileChanged_DoesNothing_WhenNoElementReferencesTheFileAndNothingCachedIt(string file)
    {
        ScreenSave screen = AddScreen("Main");
        AddInstance(screen, "Item", "Sprite");
        AddFileVariable(screen, "Item", "SourceFile", "Textures/Other.png");
        Select(screen);

        _reaction.ReactToFileChanged(new FilePath(WriteFile(file)));

        VerifyReloaded(Times.Never());
        _wireframe.Verify(w => w.RefreshAll(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
    }

    [Fact]
    public void ReactToFileChanged_ReloadsTheTexture_WhenAFramePictureOfAReferencedAnimationChainChanges()
    {
        ScreenSave screen = AddScreen("Main");
        AddInstance(screen, "Item", "Sprite");
        AddFileVariable(screen, "Item", "AnimationChains", "Animations/Idle.achx");
        Select(screen);
        WriteFile("Animations/Idle.achx",
            "<?xml version=\"1.0\"?><AnimationChainArraySave><FileRelativeTextures>false</FileRelativeTextures>" +
            "<AnimationChain><Name>Idle</Name><Frame><TextureName>Textures/Frame1.png</TextureName>" +
            "<FrameLength>0.1</FrameLength></Frame></AnimationChain></AnimationChainArraySave>");

        _reaction.ReactToFileChanged(new FilePath(WriteFile("Textures/Frame1.png")));

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void ReactToFileChanged_ReloadsTheTexture_WhenAPagePictureOfAReferencedFontChanges()
    {
        ScreenSave screen = AddScreen("Main");
        AddInstance(screen, "Item", "Text");
        AddFileVariable(screen, "Item", "CustomFontFile", "Fonts/Custom.fnt");
        Select(screen);
        WriteFile("Fonts/Custom.fnt",
            "info face=\"Arial\" size=32\ncommon lineHeight=32 base=26 scaleW=256 scaleH=256 pages=1\n" +
            "page id=0 file=\"Custom_0.png\"\nchars count=0\n");

        _reaction.ReactToFileChanged(new FilePath(WriteFile("Fonts/Custom_0.png")));

        VerifyReloaded(Times.Once());
    }

    [Theory]
    [MemberData(nameof(FileTypes))]
    public void ReactToFileDeleted_ReloadsTheCanvas_WhenAnElementReferencesTheFile(string baseType, string variable, string file)
    {
        // The rebuilt element reports the missing file the way it does when the file is picked.
        ScreenSave screen = AddScreen("Main");
        AddInstance(screen, "Item", baseType);
        AddFileVariable(screen, "Item", variable, file);
        Select(screen);

        _reaction.ReactToFileDeleted(new FilePath(Path.Combine(_projectFolder, file)));

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void ReactToFileDeleted_DoesNothing_WhenNoElementReferencesTheFile()
    {
        ScreenSave screen = AddScreen("Main");
        Select(screen);

        _reaction.ReactToFileDeleted(new FilePath(Path.Combine(_projectFolder, "bin", "App.dll")));

        _wireframe.Verify(w => w.RefreshAll(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
    }

    [Fact]
    public void ReactToFileChanged_AlsoRefreshesTheVariables_SoRowsBuiltFromTheFileFollowIt()
    {
        // An animation chain file feeds the chain-name drop-down (#4485).
        ScreenSave screen = AddScreen("Main");
        AddInstance(screen, "Item", "Sprite");
        AddFileVariable(screen, "Item", "AnimationChains", "Animations/Idle.achx");
        Select(screen);

        _reaction.ReactToFileChanged(new FilePath(WriteFile("Animations/Idle.achx")));

        _guiCommands.Verify(g => g.RefreshVariables(true), Times.Once());
    }

    [Fact]
    public void AFlushOfSeveralReferencedFiles_ReloadsTheCanvasOnce()
    {
        ScreenSave screen = AddScreen("Main");
        for (int i = 0; i < 3; i++)
        {
            AddInstance(screen, "Item" + i, "Sprite");
            AddFileVariable(screen, "Item" + i, "SourceFile", $"Textures/Photo{i}.png");
        }
        Select(screen);

        using (_reaction.DeferDependentUpdates())
        {
            for (int i = 0; i < 3; i++)
            {
                _reaction.ReactToFileChanged(new FilePath(WriteFile($"Textures/Photo{i}.png")));
            }
        }

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void ALocalizationFileTheProjectLists_ReloadsTheLanguageFileAndTheCanvas()
    {
        _project.LocalizationFiles.Add("Strings.csv");
        Mock<IFileCommands> fileCommands = new();
        fileCommands.Setup(f => f.ProjectDirectory).Returns(new FilePath(_projectFolder + "/"));
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.GumProjectSave).Returns(_project);
        projectState.Setup(p => p.ProjectDirectory).Returns(_projectFolder + "/");
        Mock<IWireframeCommands> wireframeCommands = new();
        FileChangeReactionLogic reaction = new FileChangeReactionLogic(
            _selectedState.Object, wireframeCommands.Object, _guiCommands.Object,
            fileCommands.Object, new Mock<IOutputManager>().Object, _wireframe.Object,
            projectState.Object, new Mock<IStandardElementsManagerGumTool>().Object, new Mock<IPluginManager>().Object,
            new Mock<Gum.Services.Dialogs.IDialogService>().Object, new UnsavedChangesTracker(),
            new Mock<IVariableReferenceLogic>().Object, new Mock<Gum.Undo.IUndoManager>().Object);

        reaction.ReactToFileChanged(new FilePath(WriteFile("Strings.csv")));

        fileCommands.Verify(f => f.LoadLocalizationFile(), Times.Once());
        wireframeCommands.Verify(w => w.Refresh(It.IsAny<bool>(), It.IsAny<bool>()), Times.Once());
    }

    private sealed class SynchronousDispatcher : IDispatcher
    {
        public void Invoke(Action action) => action();
        public void Post(Action action) => action();
    }

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

    private static FileSystemEventArgs Args(string fullPath, WatcherChangeTypes type) =>
        new(type, Path.GetDirectoryName(fullPath)!, Path.GetFileName(fullPath));

    [Theory]
    [InlineData("Textures/Photo.webp")]
    [InlineData("Art/Spinner.json")]
    [InlineData("Art/Icon.svg")]
    public void ASaveByRenamingATempFileOverAReferencedFile_ReloadsTheCanvas(string file)
    {
        ScreenSave screen = AddScreen("Main");
        AddInstance(screen, "Item", "Sprite");
        AddFileVariable(screen, "Item", "SourceFile", file);
        Select(screen);
        FileWatchManager watch = StartWatching();
        string path = WriteFile(file);

        watch.HandleRename(null, new RenamedEventArgs(
            WatcherChangeTypes.Renamed, Path.GetDirectoryName(path)!, Path.GetFileName(path), Path.GetFileName(path) + ".tmp.1"));
        FlushWhenSettled(watch);

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void ARenameOfAFileNothingReferences_IsNotQueued()
    {
        Select(AddScreen("Main"));
        FileWatchManager watch = StartWatching();
        string path = WriteFile("bin/App.dll");

        watch.HandleRename(null, new RenamedEventArgs(
            WatcherChangeTypes.Renamed, Path.GetDirectoryName(path)!, "App.dll", "App.dll.tmp.1"));

        watch.ChangedFilesWaitingForFlush.ShouldBeEmpty();
    }

    [Fact]
    public void DeletingAReferencedPicture_ReloadsTheCanvasOnce()
    {
        ScreenSave screen = AddScreen("Main");
        AddInstance(screen, "Item", "Sprite");
        AddFileVariable(screen, "Item", "SourceFile", "Textures/Photo.png");
        Select(screen);
        FileWatchManager watch = StartWatching();

        watch.HandleFileSystemDelete(null, Args(Path.Combine(_projectFolder, "Textures", "Photo.png"), WatcherChangeTypes.Deleted));
        FlushWhenSettled(watch);

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void DeletingThenRecreatingAReferencedPicture_ReloadsLikeAnEdit()
    {
        ScreenSave screen = AddScreen("Main");
        AddInstance(screen, "Item", "Sprite");
        AddFileVariable(screen, "Item", "SourceFile", "Textures/Photo.png");
        Select(screen);
        FileWatchManager watch = StartWatching();
        string path = WriteFile("Textures/Photo.png");

        watch.HandleFileSystemDelete(null, Args(path, WatcherChangeTypes.Deleted));
        watch.HandleFileSystemChange(null, Args(path, WatcherChangeTypes.Created));
        FlushWhenSettled(watch);

        VerifyReloaded(Times.Once());
    }

    [Fact]
    public void DeletingAFileNothingReferences_IsNotQueued()
    {
        Select(AddScreen("Main"));
        FileWatchManager watch = StartWatching();

        watch.HandleFileSystemDelete(null, Args(Path.Combine(_projectFolder, "bin", "App.dll"), WatcherChangeTypes.Deleted));

        watch.ChangedFilesWaitingForFlush.ShouldBeEmpty();
    }
}
