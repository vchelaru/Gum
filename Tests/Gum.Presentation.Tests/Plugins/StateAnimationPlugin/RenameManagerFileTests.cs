using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using System;
using System.IO;
using Xunit;

namespace Gum.Presentation.Tests.Plugins.StateAnimationPlugin;

/// <summary>
/// Covers how <see cref="RenameManager"/> moves the animation file of an element that is not the one
/// loaded in the Animations tab.
/// </summary>
public class RenameManagerFileTests : BaseTestClass
{
    private readonly string _tempDirectory;
    private readonly string _componentsDirectory;
    private readonly RenameManager _renameManager;

    public RenameManagerFileTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumRenameManagerFileTests", Guid.NewGuid().ToString("N"));
        _componentsDirectory = Path.Combine(_tempDirectory, "Components");
        Directory.CreateDirectory(_componentsDirectory);

        Mock<IProjectManager> projectManager = new();
        projectManager.Setup(x => x.GumProjectSave)
            .Returns(new GumProjectSave { FullFileName = Path.Combine(_tempDirectory, "MyProject.gumx") });

        _renameManager = new RenameManager(
            Mock.Of<ISelectedState>(),
            Mock.Of<IOutputManager>(),
            Mock.Of<IAnimationFilePathService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            projectManager.Object);
    }

    [Fact]
    public void HandleRename_LeavesBothFiles_WhenAnotherAnimationFileHasTheNewName()
    {
        string oldFile = Path.Combine(_componentsDirectory, "ButtonAnimations.ganx");
        string newFile = Path.Combine(_componentsDirectory, "IconButtonAnimations.ganx");
        File.WriteAllText(oldFile, "button");
        File.WriteAllText(newFile, "someone else's");

        _renameManager.HandleRename(new ComponentSave { Name = "IconButton" }, "Button", viewModel: null!);

        File.ReadAllText(oldFile).ShouldBe("button");
        File.ReadAllText(newFile).ShouldBe("someone else's");
    }

    [Fact]
    public void HandleRename_MovesTheFile_WhenOnlyTheCasingChanges()
    {
        File.WriteAllText(Path.Combine(_componentsDirectory, "ButtonAnimations.ganx"), "button");

        _renameManager.HandleRename(new ComponentSave { Name = "BUTTON" }, "Button", viewModel: null!);

        Directory.GetFiles(_componentsDirectory).ShouldBe(new[] { Path.Combine(_componentsDirectory, "BUTTONAnimations.ganx") });
        File.ReadAllText(Path.Combine(_componentsDirectory, "BUTTONAnimations.ganx")).ShouldBe("button");
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
