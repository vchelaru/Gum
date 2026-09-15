using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

public class NewCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public NewCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliNewTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void New_WithGumxExtension_ShouldCreateProjectAtExactPath()
    {
        string filePath = Path.Combine(_tempDirectory, "MyProject.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath);

        result.ExitCode.ShouldBe(0);
        File.Exists(filePath).ShouldBeTrue();
        result.StandardOutput.ShouldContain("Created project:");
    }

    [Fact]
    public void New_WithDirectoryName_ShouldCreateProjectInsideDirectory()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir);

        result.ExitCode.ShouldBe(0);

        // Defaults to .gumj (JSON, AOT-safe) when no extension is given (#4705).
        string expectedGumj = Path.Combine(projectDir, "MyGame.gumj");
        File.Exists(expectedGumj).ShouldBeTrue();
    }

    [Fact]
    public void New_WithGumjExtension_ShouldCreateProjectAtExactPath()
    {
        string filePath = Path.Combine(_tempDirectory, "MyProject.gumj");

        CliTestHelper result = CliTestHelper.Run("new", filePath);

        result.ExitCode.ShouldBe(0);
        File.Exists(filePath).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "MyProject.gumx")).ShouldBeFalse();
    }

    [Fact]
    public void New_ShouldCreateStandardSubfolders()
    {
        string filePath = Path.Combine(_tempDirectory, "SubfolderTest.gumx");

        CliTestHelper.Run("new", filePath);

        Directory.Exists(Path.Combine(_tempDirectory, "Screens")).ShouldBeTrue();
        Directory.Exists(Path.Combine(_tempDirectory, "Components")).ShouldBeTrue();
        Directory.Exists(Path.Combine(_tempDirectory, "Standards")).ShouldBeTrue();
        Directory.Exists(Path.Combine(_tempDirectory, "Behaviors")).ShouldBeTrue();
    }

    [Fact]
    public void New_DefaultTemplate_ShouldBeFormsTemplate()
    {
        string filePath = Path.Combine(_tempDirectory, "FormsDefault.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath);

        result.ExitCode.ShouldBe(0);
        File.Exists(Path.Combine(_tempDirectory, "Components", "Controls", "ButtonStandard.gucx")).ShouldBeTrue();
    }

    [Fact]
    public void New_WhenProjectAlreadyExists_ShouldReturnExitCode2()
    {
        string filePath = Path.Combine(_tempDirectory, "Existing.gumx");
        CliTestHelper.Run("new", filePath);

        CliTestHelper result = CliTestHelper.Run("new", filePath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("already exists");
    }

    [Fact]
    public void New_WithInvalidTemplate_ShouldReturnExitCode2()
    {
        string filePath = Path.Combine(_tempDirectory, "InvalidTemplate.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "--template", "bogus");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("bogus");
    }

    [Fact]
    public void New_WithTemplateEmpty_ShouldCreateStandardFiles()
    {
        string filePath = Path.Combine(_tempDirectory, "EmptyTemplate.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "-t", "empty");

        result.ExitCode.ShouldBe(0);
        File.Exists(Path.Combine(_tempDirectory, "Standards", "Text.gutx")).ShouldBeTrue();
    }

    [Fact]
    public void New_WithTemplateEmpty_ShouldNotCreateFormsControls()
    {
        string filePath = Path.Combine(_tempDirectory, "EmptyNoForms.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "-t", "empty");

        result.ExitCode.ShouldBe(0);
        Directory.Exists(Path.Combine(_tempDirectory, "Components", "Controls")).ShouldBeFalse();
    }

    [Fact]
    public void New_WithoutPath_ShouldCreateProjectInDefaultSubdirectoryUnderCurrentDirectory()
    {
        string originalCurrentDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDirectory);

            CliTestHelper result = CliTestHelper.Run("new");

            result.ExitCode.ShouldBe(0);
            // Defaults to .gumj (JSON, AOT-safe) when no path is given at all (#4705).
            string expectedGumj = Path.Combine(_tempDirectory, "GumProject", "GumProject.gumj");
            File.Exists(expectedGumj).ShouldBeTrue();
            result.StandardOutput.ShouldContain("Created project:");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
        }
    }

    [Fact]
    public void New_WithoutPath_WhenDefaultSubdirectoryAlreadyExists_ShouldReturnExitCode2()
    {
        string originalCurrentDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDirectory);

            CliTestHelper.Run("new");
            CliTestHelper result = CliTestHelper.Run("new");

            result.ExitCode.ShouldBe(2);
            result.StandardError.ShouldContain("already exists");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
        }
    }

    [Fact]
    public void New_DefaultTemplateAndExtension_ShouldProduceLoadableJsonProject()
    {
        // The default "forms" template is extracted from an XML-only embedded resource (#4705);
        // this proves the end-to-end gumcli path actually converts it, not just the lower-level
        // FormsTemplateCreator unit.
        string projectDir = Path.Combine(_tempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir);

        result.ExitCode.ShouldBe(0);
        string gumjPath = Path.Combine(projectDir, "MyGame.gumj");
        File.Exists(gumjPath).ShouldBeTrue();

        ProjectLoadResult loadResult = new ProjectLoader().Load(gumjPath);
        loadResult.Success.ShouldBeTrue();
        loadResult.LoadErrors.ShouldBeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
