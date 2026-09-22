using System.Linq;
using Gum.DataTypes;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

public class AddFormsCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public AddFormsCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliAddFormsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void AddForms_OnEmptyProject_ShouldAddFormsControlsAndFontFiles()
    {
        string filePath = Path.Combine(_tempDirectory, "MyProject.gumx");
        CliTestHelper.Run("new", filePath, "--template", "empty");

        CliTestHelper result = CliTestHelper.Run("add-forms", filePath);

        result.ExitCode.ShouldBe(0);
        File.Exists(Path.Combine(_tempDirectory, "Components", "Controls", "ButtonStandard.gucx")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "Behaviors", "ButtonBehavior.behx")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "Fonts", "LiberationSans-Regular.ttf")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "UISpriteSheet.png")).ShouldBeTrue();
    }

    [Fact]
    public void AddForms_OnEmptyProject_ShouldReferenceTheAddedComponentsStandardsAndBehaviors()
    {
        string filePath = Path.Combine(_tempDirectory, "MyProject.gumx");
        CliTestHelper.Run("new", filePath, "--template", "empty");

        CliTestHelper result = CliTestHelper.Run("add-forms", filePath);

        result.ExitCode.ShouldBe(0);
        ProjectLoadResult loadResult = new ProjectLoader().Load(filePath);
        loadResult.Success.ShouldBeTrue();
        loadResult.LoadErrors.ShouldBeEmpty();

        GumProjectSave project = loadResult.Project!;
        project.ComponentReferences.ShouldContain(r => r.Name == "Controls/ButtonStandard");
        project.BehaviorReferences.ShouldContain(r => r.Name == "ButtonBehavior");
        project.Behaviors.First(b => b.Name == "ButtonBehavior").IsSourceFileMissing.ShouldBeFalse();
    }

    [Fact]
    public void AddForms_WhenAComponentAlreadyExists_ShouldSkipItWithoutOverwriting()
    {
        string filePath = Path.Combine(_tempDirectory, "MyProject.gumx");
        CliTestHelper.Run("new", filePath, "--template", "forms");

        string buttonPath = Path.Combine(_tempDirectory, "Components", "Controls", "ButtonStandard.gucx");
        File.SetLastWriteTimeUtc(buttonPath, DateTime.UtcNow.AddDays(-1));
        DateTime originalWriteTime = File.GetLastWriteTimeUtc(buttonPath);

        CliTestHelper result = CliTestHelper.Run("add-forms", filePath);

        result.ExitCode.ShouldBe(0);
        File.GetLastWriteTimeUtc(buttonPath).ShouldBe(originalWriteTime);
    }

    [Fact]
    public void AddForms_OnJsonProject_ShouldAddJsonElementFiles()
    {
        string filePath = Path.Combine(_tempDirectory, "MyProject.gumj");
        CliTestHelper.Run("new", filePath, "--template", "empty");

        CliTestHelper result = CliTestHelper.Run("add-forms", filePath);

        result.ExitCode.ShouldBe(0);
        File.Exists(Path.Combine(_tempDirectory, "Components", "Controls", "ButtonStandard.gucj")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "Components", "Controls", "ButtonStandard.gucx")).ShouldBeFalse();
        File.Exists(Path.Combine(_tempDirectory, "Behaviors", "ButtonBehavior.behj")).ShouldBeTrue();

        ProjectLoadResult loadResult = new ProjectLoader().Load(filePath);
        loadResult.Success.ShouldBeTrue();
        loadResult.LoadErrors.ShouldBeEmpty();
    }

    [Fact]
    public void AddForms_WhenProjectDoesNotExist_ShouldReturnExitCode2()
    {
        string filePath = Path.Combine(_tempDirectory, "DoesNotExist.gumx");

        CliTestHelper result = CliTestHelper.Run("add-forms", filePath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("not found");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
