using Gum.Cli.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

public class SvgCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public SvgCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliSvgTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Svg_WithValidProject_WritesSvgFile()
    {
        string projectPath = CreateProjectWithScreen();
        string outputPath = Path.Combine(_tempDirectory, "Screen.svg");

        CliTestHelper result = CliTestHelper.Run("svg", projectPath, "Screen", "--output", outputPath);

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.Exists(outputPath).ShouldBeTrue();
    }

    [Fact]
    public void Svg_WithMissingProject_ReturnsExitCode2()
    {
        string projectPath = Path.Combine(_tempDirectory, "DoesNotExist.gumx");

        CliTestHelper result = CliTestHelper.Run("svg", projectPath, "Screen");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("Project file not found");
    }

    // ExportSvgInProcess (issue #4723) is the reflection entry point Gum.Services.IsolatedPluginHost
    // calls from the Avalonia head, so its signature and behavior are pinned directly here rather
    // than only exercised indirectly through the head.
    [Fact]
    public void ExportSvgInProcess_WithValidProject_ReturnsNullAndWritesSvgFile()
    {
        string projectPath = CreateProjectWithScreen();
        string outputPath = Path.Combine(_tempDirectory, "Screen.svg");

        string? error = SvgCommand.ExportSvgInProcess(projectPath, "Screen", outputPath);

        error.ShouldBeNull();
        File.Exists(outputPath).ShouldBeTrue();
    }

    [Fact]
    public void ExportSvgInProcess_WithUnknownElement_ReturnsErrorMessage()
    {
        string projectPath = CreateProjectWithScreen();
        string outputPath = Path.Combine(_tempDirectory, "Screen.svg");

        string? error = SvgCommand.ExportSvgInProcess(projectPath, "NoSuchScreen", outputPath);

        error.ShouldNotBeNull();
        error.ShouldContain("NoSuchScreen");
        File.Exists(outputPath).ShouldBeFalse();
    }

    private string CreateProjectWithScreen()
    {
        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");

        ProjectCreator creator = new ProjectCreator();
        GumProjectSave project = creator.Create(projectPath);

        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference
        {
            Name = "Screen",
            ElementType = ElementType.Screen,
        });

        project.Save(projectPath, saveElements: true);

        return projectPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
