using System.Collections.Generic;
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
}
