using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Characterization tests pinning <see cref="DuplicateService"/>'s animation-file copy behavior
/// after it was drained from service-locator lookups to constructor injection
/// (<see cref="IDialogService"/> + <see cref="IProjectManager"/>). The project is supplied via a
/// mocked <see cref="IProjectManager"/> so these run without a loaded project or the service locator.
/// The dialog is only consulted when the destination file already exists; both paths exercised here
/// avoid it, so a bare <see cref="IDialogService"/> stub is sufficient.
/// </summary>
public class DuplicateServiceTests : BaseTestClass
{
    private readonly string _tempDirectory;
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly DuplicateService _duplicateService;

    public DuplicateServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "GumDuplicateServiceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        _duplicateService = new DuplicateService(Mock.Of<IDialogService>(), _projectManager.Object);
    }

    [Fact]
    public void HandleDuplicate_copies_animation_file_when_destination_absent()
    {
        string projectFile = Path.Combine(_tempDirectory, "MyProject.gumx");
        _projectManager.Setup(x => x.GumProjectSave).Returns(new GumProjectSave { FullFileName = projectFile });

        // DuplicateService builds "<projectDir>/<Subfolder>/<Name>Animations.ganx"; ComponentSave's
        // Subfolder is "Components".
        string componentsDirectory = Path.Combine(FileManager.GetDirectory(projectFile), "Components");
        Directory.CreateDirectory(componentsDirectory);
        File.WriteAllText(Path.Combine(componentsDirectory, "FooAnimations.ganx"), "<ElementAnimationsSave/>");

        _duplicateService.HandleDuplicate(
            new ComponentSave { Name = "Foo" }, new ComponentSave { Name = "Bar" });

        File.Exists(Path.Combine(componentsDirectory, "BarAnimations.ganx")).ShouldBeTrue();
    }

    [Fact]
    public void HandleDuplicate_does_nothing_when_project_is_null()
    {
        _projectManager.Setup(x => x.GumProjectSave).Returns((GumProjectSave?)null);

        string componentsDirectory = Path.Combine(_tempDirectory, "Components");
        Directory.CreateDirectory(componentsDirectory);
        File.WriteAllText(Path.Combine(componentsDirectory, "FooAnimations.ganx"), "<ElementAnimationsSave/>");

        Should.NotThrow(() => _duplicateService.HandleDuplicate(
            new ComponentSave { Name = "Foo" }, new ComponentSave { Name = "Bar" }));

        File.Exists(Path.Combine(componentsDirectory, "BarAnimations.ganx")).ShouldBeFalse();
    }

    public override void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }

        base.Dispose();
    }
}
