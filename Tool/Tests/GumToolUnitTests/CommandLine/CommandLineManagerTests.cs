using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.CommandLine;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.FontGeneration;
using Moq;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.CommandLine;

public class CommandLineManagerTests
{
    private readonly Mock<IHeadlessFontGenerationService> _fontGenerationService;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IMessenger> _messenger;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly CommandLineManager _commandLineManager;

    public CommandLineManagerTests()
    {
        _fontGenerationService = new Mock<IHeadlessFontGenerationService>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _messenger = new Mock<IMessenger>();
        _projectManager = new Mock<IProjectManager>();

        _commandLineManager = new CommandLineManager(
            _fontGenerationService.Object,
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
        // The --rebuildfonts path must go through the headless font service (no UI callbacks), so
        // it works before app.Run() without touching the WPF dispatcher.
        _fileCommands.Setup(f => f.ProjectDirectory).Returns(new FilePath("MyProject.gumx"));
        _fontGenerationService
            .Setup(f => f.CreateAllMissingFontFiles(It.IsAny<GumProjectSave>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        await _commandLineManager.ReadCommandLine(new[] { "Gum.exe", "--rebuildfonts", "MyProject.gumx" });

        _commandLineManager.ShouldExitImmediately.ShouldBeTrue();
        _fileCommands.Verify(f => f.LoadProject("MyProject.gumx"), Times.Once);
        _fontGenerationService.Verify(
            f => f.CreateAllMissingFontFiles(It.IsAny<GumProjectSave>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Once);
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
