using System;
using System.Collections.Generic;
using System.IO;
using Gum.DataTypes;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Theme picker extracted out of AddFormsViewModel (#4536) so the New Project dialog can offer the
/// same theme choice instead of always importing the default theme.
/// </summary>
public class ThemeSelectionViewModelTests
{
    private readonly Mock<IFormsFileService> _formsFileService;
    private readonly Mock<IProjectState> _projectState;

    public ThemeSelectionViewModelTests()
    {
        _formsFileService = new Mock<IFormsFileService>();
        _projectState = new Mock<IProjectState>();

        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());
    }

    private ThemeSelectionViewModel CreateSut() => new(_formsFileService.Object, _projectState.Object);

    [Fact]
    public void Constructor_SelectsDefaultTheme_WhenPresentAmongAvailableThemes()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Bubblegum", "Standard" });

        ThemeSelectionViewModel sut = CreateSut();

        sut.SelectedTheme.ShouldBe("Standard");
    }

    [Fact]
    public void Constructor_FallsBackToFirstAvailableTheme_WhenDefaultThemeIsNotPresent()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Bubblegum" });

        ThemeSelectionViewModel sut = CreateSut();

        sut.SelectedTheme.ShouldBe("Bubblegum");
    }

    [Fact]
    public void GetSelectedThemeOrDefault_ReturnsSelectedTheme_WhenOneIsSelected()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Standard", "Bubblegum" });

        ThemeSelectionViewModel sut = CreateSut();
        sut.SelectedTheme = "Bubblegum";

        sut.GetSelectedThemeOrDefault().ShouldBe("Bubblegum");
    }

    [Fact]
    public void GetSelectedThemeOrDefault_FallsBackToDefaultThemeName_WhenNoThemeIsSelected()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string>());

        ThemeSelectionViewModel sut = CreateSut();

        sut.GetSelectedThemeOrDefault().ShouldBe("Standard");
    }

    [Fact]
    public void Constructor_SetsPreviewImagePath_WhenSelectedThemeHasOne()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Standard", "Bubblegum" });
        _formsFileService.Setup(x => x.GetThemePreviewImagePath("Standard"))
            .Returns("C:/Themes/Standard/preview.png");

        ThemeSelectionViewModel sut = CreateSut();

        sut.PreviewImagePath.ShouldBe("C:/Themes/Standard/preview.png");
        sut.HasPreviewImage.ShouldBeTrue();
    }

    [Fact]
    public void SelectedTheme_ClearsPreviewImagePath_WhenNewThemeHasNone()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Standard", "Bubblegum" });
        _formsFileService.Setup(x => x.GetThemePreviewImagePath("Standard"))
            .Returns("C:/Themes/Standard/preview.png");
        _formsFileService.Setup(x => x.GetThemePreviewImagePath("Bubblegum"))
            .Returns((string?)null);

        ThemeSelectionViewModel sut = CreateSut();
        sut.SelectedTheme = "Bubblegum";

        sut.PreviewImagePath.ShouldBeNull();
        sut.HasPreviewImage.ShouldBeFalse();
    }

    [Fact]
    public void TargetProject_DescribesRequirementsAgainstThatProject_InsteadOfTheLoadedOne()
    {
        // New Project shows this picker before the new project replaces the open one (#5968), so the
        // requirements must be measured against the project about to be created.
        string themeDirectory = Path.Combine(Path.GetTempPath(), "ThemeSelectionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(themeDirectory);
        try
        {
            File.WriteAllText(
                Path.Combine(themeDirectory, ThemeRequirements.ThemeRequirementsFileName),
                "RequiresSkiaShapes: true");
            _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard" });
            _formsFileService.Setup(x => x.GetThemeDirectory("Standard")).Returns(themeDirectory);

            GumProjectSave loadedProject = new GumProjectSave();
            loadedProject.StandardElementReferences.Add(
                new ElementReference { Name = "Svg", ElementType = ElementType.Standard });
            _projectState.Setup(x => x.GumProjectSave).Returns(loadedProject);
            GumProjectSave blankProject = new GumProjectSave();

            ThemeSelectionViewModel sut = CreateSut();
            sut.HasRequirements.ShouldBeFalse();

            sut.TargetProject = blankProject;

            sut.HasRequirements.ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(themeDirectory, recursive: true);
        }
    }
}
