using System.Runtime.InteropServices;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

public class ScreenshotCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public ScreenshotCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliScreenshotTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    // Proves `--backend raylib` actually dispatches to RaylibScreenshotService end-to-end through
    // the CLI's option parsing (#4174) — pixel-level rendering correctness is covered by
    // Gum.ProjectServices.Raylib.Tests. The default ("monogame") backend's dispatch is pre-existing,
    // unchanged behavior and is not covered here to avoid initializing both graphics backends in
    // this shared, non-graphics-focused test process.
    //
    // Windows-only: raylib's window creation hangs on macOS CI runners (GLFW/Cocoa needs the main
    // thread — see RaylibGum.Tests' CI history, #3233), and this project's CI step runs on both
    // OSes, unlike RaylibGum.Tests' own step which is gated to Windows.
    [Fact]
    public void Screenshot_WithRaylibBackend_WritesPngFile()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        string projectPath = CreateProjectWithScreen();
        string outputPath = Path.Combine(_tempDirectory, "Screen.png");

        CliTestHelper result = CliTestHelper.Run(
            "screenshot", projectPath, "Screen", "--output", outputPath, "--backend", "raylib");

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.Exists(outputPath).ShouldBeTrue();
    }

    [Fact]
    public void Screenshot_WithUnknownBackend_ReturnsExitCode2()
    {
        string projectPath = CreateProjectWithScreen();

        CliTestHelper result = CliTestHelper.Run(
            "screenshot", projectPath, "Screen", "--backend", "nonexistent");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("Unknown backend");
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
