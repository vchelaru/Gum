using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.CommandLine;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Fonts;
using Moq;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.CommandLine;

public class CommandLineManagerTests
{
    private readonly Mock<IFontManager> _fontManager;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IMessenger> _messenger;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly CommandLineManager _commandLineManager;

    public CommandLineManagerTests()
    {
        _fontManager = new Mock<IFontManager>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _messenger = new Mock<IMessenger>();
        _projectManager = new Mock<IProjectManager>();

        _commandLineManager = new CommandLineManager(
            _fontManager.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _messenger.Object,
            _projectManager.Object);
    }

    [Fact]
    public async Task ReadCommandLine_DoesNotSetExitOrLoad_WhenNoRecognizedArgs()
    {
        await _commandLineManager.ReadCommandLine(new[] { "Gum.exe" });

        _commandLineManager.ShouldExitImmediately.ShouldBeFalse();
        _commandLineManager.ShouldCodeGenAll.ShouldBeFalse();
        _commandLineManager.GlueProjectToLoad.ShouldBeNull();
        _commandLineManager.ElementName.ShouldBeNull();
    }

    [Fact]
    public async Task ReadCommandLine_SetsExitAndCodeGen_WhenGenerateCodeArg()
    {
        await _commandLineManager.ReadCommandLine(new[] { "Gum.exe", "--generatecode" });

        _commandLineManager.ShouldCodeGenAll.ShouldBeTrue();
        _commandLineManager.ShouldExitImmediately.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadCommandLine_SetsExitAndRebuildsFonts_WhenRebuildFontsArg()
    {
        _fontManager
            .Setup(f => f.CreateAllMissingFontFiles(It.IsAny<GumProjectSave>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        await _commandLineManager.ReadCommandLine(new[] { "Gum.exe", "--rebuildfonts", "MyProject.gumx" });

        _commandLineManager.ShouldExitImmediately.ShouldBeTrue();
        _fileCommands.Verify(f => f.LoadProject("MyProject.gumx"), Times.Once);
        _fontManager.Verify(f => f.CreateAllMissingFontFiles(It.IsAny<GumProjectSave>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task ReadCommandLine_SetsGlueProjectToLoad_WhenGumxArg()
    {
        await _commandLineManager.ReadCommandLine(new[] { "Gum.exe", "MyProject.gumx" });

        _commandLineManager.GlueProjectToLoad.ShouldBe("MyProject.gumx");
    }

    [Fact]
    public async Task ReadCommandLine_SetsGlueProjectToLoad_WhenGumjArg()
    {
        // A JSON-converted project (issue #4182) must be launchable the same way as a .gumx.
        await _commandLineManager.ReadCommandLine(new[] { "Gum.exe", "MyProject.gumj" });

        _commandLineManager.GlueProjectToLoad.ShouldBe("MyProject.gumj");
    }

    [Fact]
    public async Task ReadCommandLine_FindsSiblingGumjProject_WhenGucjElementArg()
    {
        // Double-clicking (or scripting a launch against) a JSON-converted element file must still
        // resolve the containing project - here the project itself was also converted, so only the
        // .gumj sibling exists on disk (issue #4182).
        string tempDirectory = Path.Combine(Path.GetTempPath(), "CommandLineManagerTests_" + Guid.NewGuid().ToString("N"));
        string componentsDirectory = Path.Combine(tempDirectory, "Components");
        Directory.CreateDirectory(componentsDirectory);
        string projectPath = Path.Combine(tempDirectory, "MyProject.gumj");
        string componentPath = Path.Combine(componentsDirectory, "Foo.gucj");
        File.WriteAllText(projectPath, "{}");
        File.WriteAllText(componentPath, "{}");

        try
        {
            await _commandLineManager.ReadCommandLine(new[] { "Gum.exe", componentPath });

            _commandLineManager.ElementName.ShouldBe("Foo");
            _commandLineManager.GlueProjectToLoad.ShouldBe(projectPath);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
