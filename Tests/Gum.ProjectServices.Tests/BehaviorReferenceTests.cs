using Gum.DataTypes.Behaviors;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

public class BehaviorReferenceTests : IDisposable
{
    private readonly string _tempDirectory;

    public BehaviorReferenceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumBehaviorReferenceTests_" + Guid.NewGuid().ToString("N"));
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
    public void ToBehaviorSave_ShouldLoadFromBehaviorsSubfolder_WhenSourcePathIsNotSet()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyProject");
        Directory.CreateDirectory(Path.Combine(projectDir, "Behaviors"));
        string behaviorPath = Path.Combine(projectDir, "Behaviors", "ButtonBehavior.behx");
        new BehaviorSave { Name = "ButtonBehavior" }.Save(behaviorPath, useCompactFormat: true);

        BehaviorReference reference = new() { Name = "ButtonBehavior" };
        string projectRoot = FileManager.GetDirectory(Path.Combine(projectDir, "Test.gumx"));

        BehaviorSave result = reference.ToBehaviorSave(projectRoot);

        result.IsSourceFileMissing.ShouldBeFalse();
        result.Name.ShouldBe("ButtonBehavior");
    }

    [Fact]
    public void ToBehaviorSave_ShouldLoadFromSourcePath_WhenSourcePathPointsOutsideTheProjectDirectory()
    {
        // Mirrors the real FormsTemplate/Bubblegum layout: the project lives two directories
        // below a shared location the reference links back up to via "../..".
        string projectDir = Path.Combine(_tempDirectory, "Nested", "MyProject");
        string sharedDir = Path.Combine(_tempDirectory, "SharedBehaviors");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(sharedDir);
        string behaviorPath = Path.Combine(sharedDir, "ButtonBehavior.behx");
        new BehaviorSave { Name = "ButtonBehavior" }.Save(behaviorPath, useCompactFormat: true);

        BehaviorReference reference = new()
        {
            Name = "ButtonBehavior",
            SourcePath = "../../SharedBehaviors/ButtonBehavior.behx"
        };
        string projectRoot = FileManager.GetDirectory(Path.Combine(projectDir, "Test.gumx"));

        BehaviorSave result = reference.ToBehaviorSave(projectRoot);

        result.IsSourceFileMissing.ShouldBeFalse();
        result.Name.ShouldBe("ButtonBehavior");
    }

    [Fact]
    public void ToBehaviorSave_ShouldMarkSourceFileMissing_WhenSourcePathDoesNotResolveToAnExistingFile()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyProject");
        Directory.CreateDirectory(projectDir);

        BehaviorReference reference = new()
        {
            Name = "ButtonBehavior",
            SourcePath = "../DoesNotExist/ButtonBehavior.behx"
        };
        string projectRoot = FileManager.GetDirectory(Path.Combine(projectDir, "Test.gumx"));

        BehaviorSave result = reference.ToBehaviorSave(projectRoot);

        result.IsSourceFileMissing.ShouldBeTrue();
    }

    [Fact]
    public void ToBehaviorSave_ShouldOverrideDefaultImplementation_WhenDefaultImplementationOverrideIsSet()
    {
        // A shared behavior's own DefaultImplementation is theme-agnostic filler; each theme's
        // reference supplies its own default-visual path without touching the shared file.
        string projectDir = Path.Combine(_tempDirectory, "MyProject");
        string sharedDir = Path.Combine(_tempDirectory, "SharedBehaviors");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(sharedDir);
        string behaviorPath = Path.Combine(sharedDir, "ButtonBehavior.behx");
        new BehaviorSave { Name = "ButtonBehavior", DefaultImplementation = "Controls/ButtonStandard" }
            .Save(behaviorPath, useCompactFormat: true);

        BehaviorReference reference = new()
        {
            Name = "ButtonBehavior",
            SourcePath = "../SharedBehaviors/ButtonBehavior.behx",
            DefaultImplementationOverride = "Bubblegum/Controls/Button"
        };
        string projectRoot = FileManager.GetDirectory(Path.Combine(projectDir, "Test.gumx"));

        BehaviorSave result = reference.ToBehaviorSave(projectRoot);

        result.DefaultImplementation.ShouldBe("Bubblegum/Controls/Button");
    }

    [Fact]
    public void ToBehaviorSave_ShouldUseSharedFileDefaultImplementation_WhenDefaultImplementationOverrideIsNotSet()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyProject");
        Directory.CreateDirectory(Path.Combine(projectDir, "Behaviors"));
        string behaviorPath = Path.Combine(projectDir, "Behaviors", "ButtonBehavior.behx");
        new BehaviorSave { Name = "ButtonBehavior", DefaultImplementation = "Controls/ButtonStandard" }
            .Save(behaviorPath, useCompactFormat: true);

        BehaviorReference reference = new() { Name = "ButtonBehavior" };
        string projectRoot = FileManager.GetDirectory(Path.Combine(projectDir, "Test.gumx"));

        BehaviorSave result = reference.ToBehaviorSave(projectRoot);

        result.DefaultImplementation.ShouldBe("Controls/ButtonStandard");
    }

    [Fact]
    public void ToBehaviorSave_ShouldFallBackToBehaviorsSubfolder_WhenSourcePathIsEmptyString()
    {
        string projectDir = Path.Combine(_tempDirectory, "MyProject");
        Directory.CreateDirectory(Path.Combine(projectDir, "Behaviors"));
        string behaviorPath = Path.Combine(projectDir, "Behaviors", "ButtonBehavior.behx");
        new BehaviorSave { Name = "ButtonBehavior" }.Save(behaviorPath, useCompactFormat: true);

        BehaviorReference reference = new() { Name = "ButtonBehavior", SourcePath = "" };
        string projectRoot = FileManager.GetDirectory(Path.Combine(projectDir, "Test.gumx"));

        BehaviorSave result = reference.ToBehaviorSave(projectRoot);

        result.IsSourceFileMissing.ShouldBeFalse();
    }
}
