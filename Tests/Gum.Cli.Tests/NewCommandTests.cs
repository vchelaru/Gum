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

        string expectedGumx = Path.Combine(projectDir, "MyGame.gumx");
        File.Exists(expectedGumx).ShouldBeTrue();
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

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
