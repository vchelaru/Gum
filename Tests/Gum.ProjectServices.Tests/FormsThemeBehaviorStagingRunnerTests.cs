using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class FormsThemeBehaviorStagingRunnerTests : IDisposable
{
    private readonly string _tempDirectory;

    public FormsThemeBehaviorStagingRunnerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumFormsThemeStagingRunnerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Run_LinkedBehaviorWithOverride_WritesResolvedFileAndReturnsExitCode0()
    {
        string sharedDir = Path.Combine(_tempDirectory, "FormsBehaviors");
        Directory.CreateDirectory(sharedDir);
        new BehaviorSave { Name = "ButtonBehavior", DefaultImplementation = "Controls/ButtonStandard" }
            .Save(Path.Combine(sharedDir, "ButtonBehavior.behx"), useCompactFormat: true);

        string themeDir = Path.Combine(_tempDirectory, "Bubblegum");
        Directory.CreateDirectory(themeDir);
        GumProjectSave themeProject = new();
        themeProject.BehaviorReferences.Add(new BehaviorReference
        {
            Name = "ButtonBehavior",
            SourcePath = "../FormsBehaviors/ButtonBehavior.behx",
            DefaultImplementationOverride = "Bubblegum/Controls/Button"
        });
        string themeGumxPath = Path.Combine(themeDir, "GumProject.gumx");
        themeProject.Save(themeGumxPath, saveElements: false);

        string destinationDir = Path.Combine(_tempDirectory, "Staged", "Behaviors");

        int exitCode = FormsThemeBehaviorStagingRunner.Run(themeGumxPath, destinationDir);

        exitCode.ShouldBe(0);
        BehaviorSave staged = BehaviorReference.DeserializeBehavior(
            Path.Combine(destinationDir, "ButtonBehavior.behx"), projectVersion: themeProject.Version);
        staged.DefaultImplementation.ShouldBe("Bubblegum/Controls/Button");
    }

    [Fact]
    public void Run_MissingProjectFile_ReturnsExitCode2()
    {
        string fakePath = Path.Combine(_tempDirectory, "nonexistent.gumx");
        string destinationDir = Path.Combine(_tempDirectory, "Staged", "Behaviors");

        int exitCode = FormsThemeBehaviorStagingRunner.Run(fakePath, destinationDir);

        exitCode.ShouldBe(2);
    }
}
