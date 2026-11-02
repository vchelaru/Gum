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
/// A file a shader <c>#include</c>s is edited, renamed over or deleted while the tool is open (#7034): the shaders that
/// read it recompile through the same reaction a shader's own edit gets. The canvas records what each shader read
/// in the <see cref="IShaderIncludeMap"/> when it compiles.
/// </summary>
public class FileChangeShaderIncludeReactionTests : BaseTestClass
{
    private readonly string _root;
    private readonly string _projectFolder;
    private readonly Mock<IWireframeObjectManager> _wireframe = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IFileWatchIgnoreList> _ignoreList = new();
    private readonly ShaderIncludeMap _includes = new();
    private readonly FileChangeReactionLogic _reaction;
    private FileWatchManager? _watch;
    private readonly GumProjectSave _project;

    public FileChangeShaderIncludeReactionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "Gum7034i_" + Path.GetRandomFileName());
        _projectFolder = Path.Combine(_root, "Project");
        Directory.CreateDirectory(Path.Combine(_projectFolder, "Shaders"));

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
            new Mock<IVariableReferenceLogic>().Object, new Mock<Gum.Undo.IUndoManager>().Object,
            shaderIncludes: _includes);

        global::RenderingLibrary.Content.LoaderManager.Self.DisposeAndClear();
    }

    public override void Dispose()
    {
        _watch?.Disable();
        global::RenderingLibrary.Content.LoaderManager.Self.DisposeAndClear();
        try { Directory.Delete(_root, recursive: true); } catch { /* best-effort */ }
        base.Dispose();
    }

    private sealed class CachedEffect : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    private void AddScreenWithShader(string screenName, string shaderFile)
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
        _selectedState.Setup(s => s.SelectedElement).Returns(screen);
    }

    private string PathInProject(string relativePath) => Path.GetFullPath(Path.Combine(_projectFolder, relativePath));

    // What the canvas does when it compiles a shader: caches the effect by path and records what the shader read.
    private CachedEffect Compiled(string shader, params string[] included)
    {
        CachedEffect effect = new CachedEffect();
        if (global::RenderingLibrary.Content.LoaderManager.Self.CachedDisposables.ContainsKey(PathInProject(shader)))
        {
            // Compiling again happens after the cache entry was dropped.
            global::RenderingLibrary.Content.LoaderManager.Self.Dispose(PathInProject(shader));
        }
        global::RenderingLibrary.Content.LoaderManager.Self.AddDisposable(PathInProject(shader), effect);
        _includes.SetIncludes(new FilePath(PathInProject(shader)), Array.ConvertAll(included, i => new FilePath(PathInProject(i))));
        return effect;
    }

    private void VerifyRebuilt(Times times) =>
        _wireframe.Verify(w => w.RefreshAll(true, false), times);

    [Fact]
    public void ReactToFileChanged_DropsTheShadersEffectAndRebuilds_WhenAnIncludeOfAnUsedShaderChanges()
    {
        AddScreenWithShader("Main", "Shaders/Blur.fx");
        CachedEffect blur = Compiled("Shaders/Blur.fx", "Shaders/Common.hlsli");
        CachedEffect other = Compiled("Shaders/Other.fx");

        _reaction.ReactToFileChanged(new FilePath(PathInProject("Shaders/Common.hlsli")));

        blur.Disposed.ShouldBeTrue("the next build must compile the shader again");
        other.Disposed.ShouldBeFalse();
        VerifyRebuilt(Times.Once());
        _wireframe.Verify(w => w.RefreshAll(It.IsAny<bool>(), true), Times.Never());
    }

    [Fact]
    public void ReactToFileChanged_ReloadsEveryShaderThatReadsTheInclude_WithOneRebuild()
    {
        AddScreenWithShader("Main", "Shaders/Blur.fx");
        AddScreenWithShader("Second", "Shaders/Glow.fx");
        CachedEffect blur = Compiled("Shaders/Blur.fx", "Shaders/Common.hlsli");
        CachedEffect glow = Compiled("Shaders/Glow.fx", "Shaders/Common.hlsli");

        _reaction.ReactToFileChanged(new FilePath(PathInProject("Shaders/Common.hlsli")));

        blur.Disposed.ShouldBeTrue();
        glow.Disposed.ShouldBeTrue();
        VerifyRebuilt(Times.Once());
    }

    [Fact]
    public void ReactToFileChanged_DoesNothing_WhenNoElementUsesTheShaderThatReadsTheInclude()
    {
        AddScreenWithShader("Main", "Shaders/Blur.fx");
        CachedEffect unused = Compiled("Shaders/Unused.fx", "Shaders/Common.hlsli");

        _reaction.ReactToFileChanged(new FilePath(PathInProject("Shaders/Common.hlsli")));

        unused.Disposed.ShouldBeFalse();
        VerifyRebuilt(Times.Never());
    }

    [Fact]
    public void ReactToFileChanged_DoesNothing_WhenNoShaderReadTheFile()
    {
        AddScreenWithShader("Main", "Shaders/Blur.fx");
        CachedEffect blur = Compiled("Shaders/Blur.fx", "Shaders/Common.hlsli");

        _reaction.ReactToFileChanged(new FilePath(PathInProject("Shaders/Elsewhere.hlsli")));

        blur.Disposed.ShouldBeFalse();
        VerifyRebuilt(Times.Never());
    }

    [Fact]
    public void ReactToFileChanged_DoesNothing_AfterTheShaderCompilesWithoutTheInclude()
    {
        AddScreenWithShader("Main", "Shaders/Blur.fx");
        Compiled("Shaders/Blur.fx", "Shaders/Common.hlsli");
        CachedEffect recompiled = Compiled("Shaders/Blur.fx");

        _reaction.ReactToFileChanged(new FilePath(PathInProject("Shaders/Common.hlsli")));

        recompiled.Disposed.ShouldBeFalse();
        VerifyRebuilt(Times.Never());
    }

    [Fact]
    public void ReactToFileChanged_ReachesAnIncludeNestedInAnotherInclude()
    {
        AddScreenWithShader("Main", "Shaders/Blur.fx");
        // ShadowDusk reports nested includes as well, so the shader lists both.
        CachedEffect blur = Compiled("Shaders/Blur.fx", "Shaders/Common.hlsli", "Shaders/lib/Weights.hlsli");

        _reaction.ReactToFileChanged(new FilePath(PathInProject("Shaders/lib/Weights.hlsli")));

        blur.Disposed.ShouldBeTrue();
        VerifyRebuilt(Times.Once());
    }

    [Fact]
    public void ReactToFileDeleted_ReloadsTheShader_SoTheMissingIncludeIsReported()
    {
        AddScreenWithShader("Main", "Shaders/Blur.fx");
        CachedEffect blur = Compiled("Shaders/Blur.fx", "Shaders/Common.hlsli");

        _reaction.ReactToFileDeleted(new FilePath(PathInProject("Shaders/Common.hlsli")));

        blur.Disposed.ShouldBeTrue();
        VerifyRebuilt(Times.Once());
    }

    [Fact]
    public void IsReferencedFile_IsTrueForAFileAShaderReads_SoTheWatcherQueuesIt()
    {
        AddScreenWithShader("Main", "Shaders/Blur.fx");
        Compiled("Shaders/Blur.fx", "Shaders/Common.hlsli");

        _reaction.IsReferencedFile(new FilePath(PathInProject("Shaders/Common.hlsli"))).ShouldBeTrue();
        _reaction.IsReferencedFile(new FilePath(PathInProject("Shaders/Elsewhere.hlsli"))).ShouldBeFalse();
    }

    [Fact]
    public void SavingAnIncludeByRenamingATempFileOverIt_ReloadsTheShader()
    {
        AddScreenWithShader("Main", "Shaders/Blur.fx");
        CachedEffect blur = Compiled("Shaders/Blur.fx", "Shaders/Common.hlsli");
        FileWatchManager watch = StartWatching();
        string include = PathInProject("Shaders/Common.hlsli");
        File.WriteAllText(include, "float Thickness = 1.0;");

        watch.HandleRename(null, new RenamedEventArgs(WatcherChangeTypes.Renamed, Path.GetDirectoryName(include)!, "Common.hlsli", "Common.hlsli.tmp.1"));
        FlushWhenSettled(watch);

        blur.Disposed.ShouldBeTrue();
        VerifyRebuilt(Times.Once());
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
}
