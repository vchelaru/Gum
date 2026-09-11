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
    public void GetSourceDestinations_ReturnsEmpty_WhenProjectDirectoryIsNull()
    {
        var projectState = new Mock<IProjectState>();
        projectState.Setup(p => p.ProjectDirectory).Returns((string?)null);
        var formsFileService = new FormsFileService(projectState.Object);

        var sourceDestinations = formsFileService.GetSourceDestinations(
            formsFileService.DefaultThemeName, isIncludeDemoScreenGum: false);

        sourceDestinations.ShouldBeEmpty();
        projectState.VerifyGet(p => p.ProjectDirectory, Times.AtLeastOnce);
    }

    [Fact]
    public void GetSourceDestinations_ReturnsEmpty_WhenThemeDirectoryDoesNotExist()
    {
        var projectState = new Mock<IProjectState>();
        projectState.Setup(p => p.ProjectDirectory).Returns("C:/SomeProject/");
        var formsFileService = new FormsFileService(projectState.Object);

        var sourceDestinations = formsFileService.GetSourceDestinations(
            "ThemeThatDoesNotExist", isIncludeDemoScreenGum: false);

        sourceDestinations.ShouldBeEmpty();
    }

    [Fact]
    public void GetSourceDestinations_ExcludesStandards_ForNonDefaultTheme()
    {
        string themeName = "TestTheme_" + Guid.NewGuid().ToString("N");
        string themeDir = CreateFixtureTheme(themeName);
        try
        {
            var projectState = new Mock<IProjectState>();
            projectState.Setup(p => p.ProjectDirectory).Returns("C:/SomeProject/");
            var formsFileService = new FormsFileService(projectState.Object);

            var sourceDestinations = formsFileService.GetSourceDestinations(
                themeName, isIncludeDemoScreenGum: false);

            sourceDestinations.Keys.ShouldNotContain(k => k.EndsWith("Rectangle.gutx"));
            sourceDestinations.Keys.ShouldContain(k => k.EndsWith("Button.gucx"));
        }
        finally
        {
            Directory.Delete(themeDir, recursive: true);
        }
    }

    [Fact]
    public void GetSourceDestinations_IncludesStandards_ForDefaultTheme()
    {
        string themeDir = CreateFixtureTheme("Standard");
        try
        {
            var projectState = new Mock<IProjectState>();
            projectState.Setup(p => p.ProjectDirectory).Returns("C:/SomeProject/");
            var formsFileService = new FormsFileService(projectState.Object);

            var sourceDestinations = formsFileService.GetSourceDestinations(
                "Standard", isIncludeDemoScreenGum: false);

            sourceDestinations.Keys.ShouldContain(k => k.EndsWith("Rectangle.gutx"));
        }
        finally
        {
            Directory.Delete(themeDir, recursive: true);
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

    [Fact]
    public void GetSourceDestinations_ExcludesPreviewImage()
    {
        string themeName = "TestTheme_" + Guid.NewGuid().ToString("N");
        string themeDir = CreateFixtureTheme(themeName);
        File.WriteAllBytes(Path.Combine(themeDir, "preview.png"), new byte[] { 1, 2, 3 });
        try
        {
            var projectState = new Mock<IProjectState>();
            projectState.Setup(p => p.ProjectDirectory).Returns("C:/SomeProject/");
            var formsFileService = new FormsFileService(projectState.Object);

            var sourceDestinations = formsFileService.GetSourceDestinations(
                themeName, isIncludeDemoScreenGum: false);

            sourceDestinations.Keys.ShouldNotContain(k => k.EndsWith("preview.png"));
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
