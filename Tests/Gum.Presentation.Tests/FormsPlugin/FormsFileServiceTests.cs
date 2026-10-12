using System;
using System.IO;
using System.Linq;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.FormsPlugin;

public class FormsFileServiceTests : BaseTestClass
{
    [Fact]
    public void ProjectHasElementsOf_ReturnsFalse_WhenThereIsNoProjectDirectory()
    {
        var projectState = new Mock<IProjectState>();
        projectState.Setup(p => p.ProjectDirectory).Returns((string?)null);
        var formsFileService = new FormsFileService(projectState.Object);

        formsFileService.ProjectHasElementsOf(formsFileService.DefaultThemeName).ShouldBeFalse();
    }

    [Fact]
    public void ProjectHasElementsOf_ReturnsFalse_WhenThemeDirectoryDoesNotExist()
    {
        var projectState = new Mock<IProjectState>();
        projectState.Setup(p => p.ProjectDirectory).Returns("C:/SomeProject/");
        var formsFileService = new FormsFileService(projectState.Object);

        formsFileService.ProjectHasElementsOf("ThemeThatDoesNotExist").ShouldBeFalse();
    }

    [Theory]
    [InlineData("Components/Button.gucx", true)]
    [InlineData("Components/Button.gucj", true)]
    [InlineData("Behaviors/ButtonBehavior.behx", true)]
    [InlineData("Behaviors/ButtonBehavior.behj", true)]
    // A project always has its Standards, and bundled fonts and images say nothing about Forms.
    [InlineData("Standards/Rectangle.gutx", false)]
    [InlineData("Standards/Rectangle.gutj", false)]
    [InlineData("Fonts/LiberationSans-Regular.ttf", false)]
    [InlineData("UISpriteSheet.png", false)]
    [InlineData("Components/SomethingElse.gucx", false)]
    public void ProjectHasElementsOf_OnlyCountsTheThemesComponentAndBehaviorFiles(string projectFile, bool expected)
    {
        string themeName = "TestTheme_" + Guid.NewGuid().ToString("N");
        string themeDir = CreateFixtureTheme(themeName);
        Directory.CreateDirectory(Path.Combine(themeDir, "Behaviors"));
        File.WriteAllText(Path.Combine(themeDir, "Behaviors", "ButtonBehavior.behx"), "<BehaviorSave />");
        string projectDir = Path.Combine(Path.GetTempPath(), "GumFormsProject_" + Guid.NewGuid().ToString("N"));
        string projectFilePath = Path.Combine(projectDir, projectFile);
        Directory.CreateDirectory(Path.GetDirectoryName(projectFilePath)!);
        File.WriteAllText(projectFilePath, "content");
        try
        {
            var projectState = new Mock<IProjectState>();
            projectState.Setup(p => p.ProjectDirectory).Returns(projectDir + Path.DirectorySeparatorChar);
            var formsFileService = new FormsFileService(projectState.Object);

            formsFileService.ProjectHasElementsOf(themeName).ShouldBe(expected);
        }
        finally
        {
            Directory.Delete(themeDir, recursive: true);
            Directory.Delete(projectDir, recursive: true);
        }
    }

    [Fact]
    public void GetThemePreviewImagePath_ReturnsPath_WhenPreviewPngExists()
    {
        string themeName = "TestTheme_" + Guid.NewGuid().ToString("N");
        string themeDir = CreateFixtureTheme(themeName);
        File.WriteAllBytes(Path.Combine(themeDir, "preview.png"), new byte[] { 1, 2, 3 });
        try
        {
            var projectState = new Mock<IProjectState>();
            var formsFileService = new FormsFileService(projectState.Object);

            string? previewPath = formsFileService.GetThemePreviewImagePath(themeName);

            previewPath.ShouldNotBeNull();
            File.Exists(previewPath).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(themeDir, recursive: true);
        }
    }

    [Fact]
    public void GetThemePreviewImagePath_ReturnsNull_WhenThemeHasNoPreviewImage()
    {
        string themeName = "TestTheme_" + Guid.NewGuid().ToString("N");
        string themeDir = CreateFixtureTheme(themeName);
        try
        {
            var projectState = new Mock<IProjectState>();
            var formsFileService = new FormsFileService(projectState.Object);

            string? previewPath = formsFileService.GetThemePreviewImagePath(themeName);

            previewPath.ShouldBeNull();
        }
        finally
        {
            Directory.Delete(themeDir, recursive: true);
        }
    }

    private static string CreateFixtureTheme(string themeName)
    {
        string themeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "FormsThemes", themeName);
        Directory.CreateDirectory(Path.Combine(themeDir, "Standards"));
        Directory.CreateDirectory(Path.Combine(themeDir, "Components"));
        File.WriteAllText(Path.Combine(themeDir, "Standards", "Rectangle.gutx"), "<StandardElementSave />");
        File.WriteAllText(Path.Combine(themeDir, "Components", "Button.gucx"), "<ComponentSave />");
        return themeDir;
    }
}
