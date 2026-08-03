using System.Collections.Generic;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Shouldly;
using ToolsUtilities;
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
    public void RefreshRootDirectory_WatchesProjectDirectoryAndOutOfProjectFileDirectories()
    {
        // Font-cache files always live under "FontCache/" inside the project directory, which is
        // watched with IncludeSubdirectories, so they never contribute a directory of their own.
        // Files referenced from outside the project do, and are the only thing that can actually be
        // lost by narrowing what the dependency walk enumerates. A custom font is the subtle case:
        // it is a font, but it is classified as an external file rather than a font-cache file, so
        // it must survive a walk that excludes font-cache enumeration.
        const string outOfProjectTexture = "C:/Elsewhere/Art/bg.png";
        const string outOfProjectFont = "C:/OutsideFonts/Fancy.ttf";

        GumProjectSave project = new GumProjectSave { FullFileName = "C:/FakeGumProject/MyProject.gumx" };
        ScreenSave screen = new ScreenSave { Name = "MainMenu" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave sprite = new InstanceSave { Name = "Sprite1", BaseType = "Sprite", ParentContainer = screen };
        screen.Instances.Add(sprite);
        screen.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Sprite1.SourceFile",
            Type = "string",
            Value = outOfProjectTexture,
            IsFile = true,
            SetsValue = true,
        });
        InstanceSave text = new InstanceSave { Name = "Text1", BaseType = "Text", ParentContainer = screen };
        screen.Instances.Add(text);
        screen.DefaultState.Variables.Add(new VariableSave { Name = "Text1.UseCustomFont", Type = "bool", Value = false, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "Text1.Font", Type = "string", Value = "Arial", IsFont = true, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "Text1.FontSize", Type = "int", Value = 18, SetsValue = true });
        InstanceSave customFontText = new InstanceSave { Name = "Text2", BaseType = "Text", ParentContainer = screen };
        screen.Instances.Add(customFontText);
        screen.DefaultState.Variables.Add(new VariableSave { Name = "Text2.UseCustomFont", Type = "bool", Value = true, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Text2.CustomFontFile",
            Type = "string",
            Value = outOfProjectFont,
            IsFile = true,
            SetsValue = true,
        });
        project.Screens.Add(screen);

        ObjectFinder.Self.GumProjectSave = project;
        _projectManager.SetupGet(m => m.GumProjectSave).Returns(project);
        _projectState.SetupGet(m => m.GumProjectSave).Returns(project);

        HashSet<FilePath>? watched = null;
        _fileWatchManager
            .Setup(m => m.EnableWithDirectories(It.IsAny<HashSet<FilePath>>()))
            .Callback<HashSet<FilePath>>(directories => watched = directories);

        _fileWatchLogic.RefreshRootDirectory();

        watched.ShouldNotBeNull();
        watched.ShouldContain(new FilePath("C:/FakeGumProject/"));
        watched.ShouldContain(new FilePath("C:/Elsewhere/Art/"));
        watched.ShouldContain(new FilePath("C:/OutsideFonts/"));
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
