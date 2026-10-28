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
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// A shader source file (.fx, .slang) edited, renamed over or deleted on disk while the tool is open
/// (#7017): the tool recompiles the effect the canvas cached by path and refreshes what it shows.
/// </summary>
public class FileChangeShaderReactionTests : BaseTestClass
{
    private readonly string _root;
    private readonly string _projectFolder;
    private readonly Mock<IWireframeObjectManager> _wireframe = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IFileWatchIgnoreList> _ignoreList = new();
    private readonly FileChangeReactionLogic _reaction;
    private FileWatchManager? _watch;
    private readonly GumProjectSave _project;

    public FileChangeShaderReactionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "Gum7017_" + Path.GetRandomFileName());
        _projectFolder = Path.Combine(_root, "Project");
        Directory.CreateDirectory(Path.Combine(_projectFolder, "Shaders"));
        Directory.CreateDirectory(Path.Combine(_root, "Shared"));

        _project = new GumProjectSave { FullFileName = Path.Combine(_projectFolder, "Project.gumx") };
        ObjectFinder.Self.GumProjectSave = _project;

        _wireframe.Setup(w => w.AllIpsos).Returns(new List<GraphicalUiElement>());
        _ignoreList.Setup(i => i.TryGetIgnoreFileChange(It.IsAny<FilePath>())).Returns(false);

        Mock<IFileCommands> fileCommands = new();
        fileCommands.Setup(f => f.ProjectDirectory).Returns(new FilePath(_projectFolder + "/"));
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.GumProjectSave).Returns(_project);

        _reaction = new FileChangeReactionLogic(
            _selectedState.Object, new Mock<IWireframeCommands>().Object, new Mock<IGuiCommands>().Object,
            fileCommands.Object, new Mock<IOutputManager>().Object, _wireframe.Object,
            projectState.Object, new Mock<IStandardElementsManagerGumTool>().Object, new Mock<IPluginManager>().Object,
            new Mock<Gum.Services.Dialogs.IDialogService>().Object, new UnsavedChangesTracker(),
            new Mock<IVariableReferenceLogic>().Object, new Mock<Gum.Undo.IUndoManager>().Object);
    }

    public override void Dispose()
    {
        _watch?.Disable();
        try { Directory.Delete(_root, recursive: true); } catch { /* best-effort */ }
        base.Dispose();
    }

    private ScreenSave AddScreenWithShader(string screenName, string shaderFile)
    {
        ScreenSave screen = new ScreenSave { Name = screenName };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = "Container", ParentContainer = screen });
        defaultState.Variables.Add(new VariableSave
        {
            Name = "Box.SourceShaderFile", Type = "string", Value = shaderFile, IsFile = true, SetsValue = true,
        });
        _project.Screens.Add(screen);
        return screen;
    }

    private string WriteShader(string relativePath)
    {
        string path = Path.GetFullPath(Path.Combine(_projectFolder, relativePath));
        File.WriteAllText(path, "float Thickness = 1.0;");
        return path;
    }

    private void VerifyRefreshedWithTextureReload(Times times) =>
        _wireframe.Verify(w => w.RefreshAll(true, true), times);

    [Theory]
    [InlineData("Shaders/Blur.slang")]
    [InlineData("Shaders/Blur.fx")]
    [InlineData("Shaders/Blur.SLANG")]
    public void ReactToFileChanged_RefreshesTheCanvasAndDropsTheCachedEffect_WhenTheSelectedElementUsesTheShader(string shader)
    {
        ScreenSave screen = AddScreenWithShader("Main", shader);
        _selectedState.Setup(s => s.SelectedElement).Returns(screen);

        _reaction.ReactToFileChanged(new FilePath(WriteShader(shader)));

        VerifyRefreshedWithTextureReload(Times.Once());
    }

    [Fact]
    public void ReactToFileChanged_DropsTheCachedEffect_WhenOnlyAnotherElementUsesTheShader()
    {
        AddScreenWithShader("Other", "Shaders/Blur.slang");
        ScreenSave selected = new ScreenSave { Name = "Selected" };
        _project.Screens.Add(selected);
        _selectedState.Setup(s => s.SelectedElement).Returns(selected);

        _reaction.ReactToFileChanged(new FilePath(WriteShader("Shaders/Blur.slang")));

        // Selecting the other element later must not draw the old cached effect.
        VerifyRefreshedWithTextureReload(Times.Once());
    }

    [Fact]
    public void ReactToFileChanged_Refreshes_WhenTheShaderLivesOutsideTheProjectFolder()
    {
        ScreenSave screen = AddScreenWithShader("Main", "../Shared/Far.slang");
        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
        string outside = Path.Combine(_root, "Shared", "Far.slang");
        File.WriteAllText(outside, "float Thickness = 1.0;");

        _reaction.ReactToFileChanged(new FilePath(outside));

        VerifyRefreshedWithTextureReload(Times.Once());
    }

    [Fact]
    public void ReactToFileChanged_DoesNothing_WhenNoElementUsesTheShader()
    {
        ScreenSave screen = AddScreenWithShader("Main", "Shaders/Blur.slang");
        _selectedState.Setup(s => s.SelectedElement).Returns(screen);

        _reaction.ReactToFileChanged(new FilePath(WriteShader("Shaders/Unused.slang")));

        VerifyRefreshedWithTextureReload(Times.Never());
    }

    [Fact]
    public void ReactToFileDeleted_RefreshesSoTheMissingFileIsReported_WhenAnElementUsesTheShader()
    {
        ScreenSave screen = AddScreenWithShader("Main", "Shaders/Blur.slang");
        _selectedState.Setup(s => s.SelectedElement).Returns(screen);

        _reaction.ReactToFileDeleted(new FilePath(Path.Combine(_projectFolder, "Shaders", "Blur.slang")));

        VerifyRefreshedWithTextureReload(Times.Once());
    }

    [Fact]
    public void ReactToFileDeleted_DoesNothing_WhenNoElementUsesTheShader()
    {
        AddScreenWithShader("Main", "Shaders/Blur.slang");

        _reaction.ReactToFileDeleted(new FilePath(Path.Combine(_projectFolder, "Shaders", "Unused.fx")));

        VerifyRefreshedWithTextureReload(Times.Never());
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
    [InlineData("Blur.slang")]
    [InlineData("Blur.fx")]
    public void ASaveByRenamingATempFileOverTheShader_Refreshes(string name)
    {
        AddScreenWithShader("Main", "Shaders/" + name);
        FileWatchManager watch = StartWatching();
        string path = WriteShader("Shaders/" + name);

        watch.HandleRename(null, new RenamedEventArgs(WatcherChangeTypes.Renamed, Path.GetDirectoryName(path)!, name, name + ".tmp.1"));
        FlushWhenSettled(watch);

        VerifyRefreshedWithTextureReload(Times.Once());
    }

    [Fact]
    public void DeletingTheShader_RefreshesOnce_AndDoesNotThrow()
    {
        AddScreenWithShader("Main", "Shaders/Blur.slang");
        FileWatchManager watch = StartWatching();
        string path = Path.Combine(_projectFolder, "Shaders", "Blur.slang");

        watch.HandleFileSystemDelete(null, Args(path, WatcherChangeTypes.Deleted));
        FlushWhenSettled(watch);

        VerifyRefreshedWithTextureReload(Times.Once());
    }

    [Fact]
    public void DeletingTheShaderThenRecreatingIt_ReloadsLikeAnEdit()
    {
        AddScreenWithShader("Main", "Shaders/Blur.slang");
        FileWatchManager watch = StartWatching();
        string path = WriteShader("Shaders/Blur.slang");

        watch.HandleFileSystemDelete(null, Args(path, WatcherChangeTypes.Deleted));
        watch.HandleFileSystemChange(null, Args(path, WatcherChangeTypes.Created));
        FlushWhenSettled(watch);

        VerifyRefreshedWithTextureReload(Times.Once());
    }

    [Fact]
    public void AnEditorWritingTwice_RefreshesOnce()
    {
        AddScreenWithShader("Main", "Shaders/Blur.slang");
        FileWatchManager watch = StartWatching();
        string path = WriteShader("Shaders/Blur.slang");

        watch.HandleFileSystemChange(null, Args(path, WatcherChangeTypes.Changed));
        watch.HandleFileSystemChange(null, Args(path, WatcherChangeTypes.Changed));
        FlushWhenSettled(watch);

        VerifyRefreshedWithTextureReload(Times.Once());
    }

    [Fact]
    public void AShaderTheToolWroteItself_IsNotQueued()
    {
        AddScreenWithShader("Main", "Shaders/Blur.slang");
        FileWatchManager watch = StartWatching();
        string path = WriteShader("Shaders/Blur.slang");
        _ignoreList.Setup(i => i.TryGetIgnoreFileChange(new FilePath(path))).Returns(true);

        watch.HandleFileSystemChange(null, Args(path, WatcherChangeTypes.Created));
        FlushWhenSettled(watch);

        VerifyRefreshedWithTextureReload(Times.Never());
    }

    [Fact]
    public void CopyingABuiltInShaderIntoTheProject_TellsTheWatcherToIgnoreTheCopy()
    {
        string builtIn = Path.Combine(_root, "BuiltIn");
        Directory.CreateDirectory(builtIn);
        File.WriteAllText(Path.Combine(builtIn, "Blur.slang"), "shipped");
        Mock<IProjectState> projectState = new();
        projectState.SetupGet(p => p.ProjectDirectory).Returns(_projectFolder + "/");
        Mock<IFileWatchManager> watch = new();
        BuiltInShaderFiles sut = new BuiltInShaderFiles(projectState.Object, builtIn, new Lazy<IFileWatchManager>(() => watch.Object));

        sut.CopyIntoProject("Blur.slang");

        watch.Verify(w => w.IgnoreNextChangeUntil(
            new FilePath(Path.Combine(_projectFolder, "Shaders", "Blur.slang")), It.IsAny<DateTime?>()), Times.Once);
    }
}
