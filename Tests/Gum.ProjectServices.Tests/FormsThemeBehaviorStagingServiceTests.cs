using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class FormsThemeBehaviorStagingServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly FormsThemeBehaviorStagingService _sut;

    public FormsThemeBehaviorStagingServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumFormsThemeStagingTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _sut = new FormsThemeBehaviorStagingService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Stage_ShouldWriteResolvedBehaviorWithThemeOverride_NotTheSharedFilesRawValue()
    {
        // Pins the exact bug a naive `xcopy` of the shared folder would reintroduce: staged
        // output must reflect each theme's own DefaultImplementationOverride, not whatever
        // value happens to be baked into the shared master file.
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

        int stagedCount = _sut.Stage(themeGumxPath, destinationDir);

        stagedCount.ShouldBe(1);
        string destFile = Path.Combine(destinationDir, "ButtonBehavior.behx");
        File.Exists(destFile).ShouldBeTrue();
        BehaviorSave staged = BehaviorReference.DeserializeBehavior(destFile, projectVersion: themeProject.Version);
        staged.DefaultImplementation.ShouldBe("Bubblegum/Controls/Button");
    }

    [Fact]
    public void Stage_ShouldMakeTheStagedProjectSelfContained_ByDroppingItsBehaviorSourcePaths()
    {
        // The staged theme is a standalone project (Add Forms and Import from .gumx read it): the
        // behaviors now sit in its Behaviors folder, so a SourcePath into the repo's shared folder
        // would dangle there and the import would silently skip those behaviors.
        string sharedDir = Path.Combine(_tempDirectory, "FormsBehaviors");
        Directory.CreateDirectory(sharedDir);
        new BehaviorSave { Name = "ButtonBehavior" }.Save(Path.Combine(sharedDir, "ButtonBehavior.behx"), useCompactFormat: true);

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

        string stagedDir = Path.Combine(_tempDirectory, "Staged");
        Directory.CreateDirectory(stagedDir);
        string stagedGumxPath = Path.Combine(stagedDir, "GumProject.gumx");
        File.Copy(themeGumxPath, stagedGumxPath);

        _sut.Stage(themeGumxPath, Path.Combine(stagedDir, "Behaviors"));

        GumProjectSave staged = GumProjectSave.Load(stagedGumxPath, out _)!;
        staged.BehaviorReferences.Single().SourcePath.ShouldBeNullOrEmpty();
        staged.BehaviorReferences.Single().DefaultImplementationOverride.ShouldBe("Bubblegum/Controls/Button");
        staged.Behaviors.Single().IsSourceFileMissing.ShouldBeFalse("it resolves to the staged Behaviors folder");
        GumProjectSave.Load(themeGumxPath, out _)!.BehaviorReferences.Single().SourcePath.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Stage_ShouldWriteTheBehaviorsOwnPhysicalContent_WhenNotLinked()
    {
        string themeDir = Path.Combine(_tempDirectory, "Standard");
        Directory.CreateDirectory(Path.Combine(themeDir, "Behaviors"));
        new BehaviorSave { Name = "PanelBehavior", DefaultImplementation = "Controls/Panel" }
            .Save(Path.Combine(themeDir, "Behaviors", "PanelBehavior.behx"), useCompactFormat: true);

        GumProjectSave themeProject = new();
        themeProject.BehaviorReferences.Add(new BehaviorReference { Name = "PanelBehavior" });
        string themeGumxPath = Path.Combine(themeDir, "GumProject.gumx");
        themeProject.Save(themeGumxPath, saveElements: false);

        string destinationDir = Path.Combine(_tempDirectory, "Staged", "Behaviors");

        int stagedCount = _sut.Stage(themeGumxPath, destinationDir);

        stagedCount.ShouldBe(1);
        BehaviorSave staged = BehaviorReference.DeserializeBehavior(
            Path.Combine(destinationDir, "PanelBehavior.behx"), projectVersion: themeProject.Version);
        staged.DefaultImplementation.ShouldBe("Controls/Panel");
    }

    [Fact]
    public void Stage_ShouldThrow_WhenProjectFileDoesNotExist()
    {
        string missingPath = Path.Combine(_tempDirectory, "DoesNotExist.gumx");
        string destinationDir = Path.Combine(_tempDirectory, "Staged", "Behaviors");

        Should.Throw<InvalidOperationException>(() => _sut.Stage(missingPath, destinationDir));
    }
}
