using System.Runtime.InteropServices;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

// Windows-only, and the only Gum.Cli.Tests suite that renders through BOTH MonoGame and raylib in
// the same process (screenshot --backend raylib exercises only one). See ScreenshotCommandTests for
// why raylib itself is Windows-only in CI; MonoGame DesktopGL needs the same Mesa llvmpipe override
// there (already wired for this project, #4174).
public class DiffScreenshotsCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public DiffScreenshotsCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliDiffScreenshotsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void DiffScreenshots_IdenticallyRenderingProject_ReturnsExitCode0()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        string projectPath = CreateProjectWithScreen();
        string outputDirectory = Path.Combine(_tempDirectory, "renders");

        CliTestHelper result = CliTestHelper.Run(
            "diff-screenshots", projectPath, "--output", outputDirectory);

        result.ExitCode.ShouldBe(0, $"stdout: {result.StandardOutput}\nstderr: {result.StandardError}");
        result.StandardOutput.ShouldContain("MATCH  Screen");
        result.StandardOutput.ShouldContain("All 1 element(s) matched.");
        string reportPath = Path.Combine(outputDirectory, "report.html");
        result.StandardOutput.ShouldContain(reportPath);
        File.Exists(reportPath).ShouldBeTrue();
    }

    [Fact]
    public void DiffScreenshots_ProjectFileDoesNotExist_ReturnsExitCode2()
    {
        CliTestHelper result = CliTestHelper.Run(
            "diff-screenshots", Path.Combine(_tempDirectory, "DoesNotExist.gumx"));

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("not found");
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
