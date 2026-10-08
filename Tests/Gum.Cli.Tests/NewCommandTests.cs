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

    [Fact]
    public void New_WithPlatform_ShouldCreateHostProjectWithGumProjectAndCodegenSettings()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "monogame", "--no-restore");

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.Exists(Path.Combine(projectDir, "MyGame.csproj")).ShouldBeTrue();
        File.Exists(Path.Combine(projectDir, "Game1.cs")).ShouldBeTrue();
        string gumFolder = Path.Combine(projectDir, "Content", "GumProject");
        File.Exists(Path.Combine(gumFolder, "GumProject.gumj")).ShouldBeTrue();
        File.Exists(Path.Combine(gumFolder, "ProjectCodeSettings.codsj")).ShouldBeTrue();
        result.StandardOutput.ShouldContain("MyGame.csproj");
    }

    [Fact]
    public void New_WithPlatform_ShouldProduceProjectThatCodegenAcceptsAndChecksClean()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyGame");
        CliTestHelper.Run("new", projectDir, "--platform", "monogame", "--no-restore");
        string gumjPath = Path.Combine(projectDir, "Content", "GumProject", "GumProject.gumj");

        CliTestHelper checkResult = CliTestHelper.Run("check", gumjPath);
        CliTestHelper codegenResult = CliTestHelper.Run("codegen", gumjPath);

        checkResult.ExitCode.ShouldBe(0, checkResult.StandardOutput + checkResult.StandardError);
        codegenResult.ExitCode.ShouldBe(0, codegenResult.StandardOutput + codegenResult.StandardError);
        File.Exists(Path.Combine(projectDir, "Screens", "DemoScreenGum.Generated.cs")).ShouldBeTrue();
    }

    [Fact]
    public void New_WithStridePlatform_ShouldCreateHostProjectWithoutGameClass()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "-p", "stride", "--no-restore");

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.ReadAllText(Path.Combine(projectDir, "MyGame.csproj")).ShouldContain("Gum.Stride");
        File.Exists(Path.Combine(projectDir, "Program.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(projectDir, "Game1.cs")).ShouldBeFalse();
    }

    [Fact]
    public void New_WithUnknownPlatform_ShouldReturnExitCode2NamingValidValues()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "bogus");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("bogus");
        result.StandardError.ShouldContain("monogame");
        Directory.Exists(projectDir).ShouldBeFalse();
    }

    [Fact]
    public void New_WithFnaPlatform_ShouldReturnExitCode2ExplainingItNeedsSource()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "fna");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("FNA");
        Directory.Exists(projectDir).ShouldBeFalse();
    }

    [Fact]
    public void New_WithPlatformAndProjectFilePath_ShouldReturnExitCode2()
    {
        string filePath = Path.Combine(_tempDirectory, "MyGame.gumj");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "--platform", "monogame", "--no-restore");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("folder");
        File.Exists(filePath).ShouldBeFalse();
    }

    [Fact]
    public void New_WithPlatformAndExistingProject_ShouldReturnExitCode2()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyGame");
        CliTestHelper.Run("new", projectDir, "--platform", "raylib", "--no-restore");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "raylib", "--no-restore");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("already exists");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
