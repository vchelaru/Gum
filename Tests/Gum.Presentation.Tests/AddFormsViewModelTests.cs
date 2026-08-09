using System.Collections.Generic;
using Gum.DataTypes;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// AddFormsViewModel had no test coverage before this file. Added alongside extracting
/// IFormsFileService (which unblocked the VM's move into the headless Gum.Presentation assembly,
/// ADR-0005 Phase 3, #3754) so DefaultThemeName went from a static const on the concrete
/// FormsFileService to an instance member reachable through the interface.
/// </summary>
public class AddFormsViewModelTests
{
    private readonly Mock<IFormsFileService> _formsFileService;
    private readonly Mock<IFormsThemeImporter> _themeImporter;
    private readonly Mock<IProjectState> _projectState;

    public AddFormsViewModelTests()
    {
        _formsFileService = new Mock<IFormsFileService>();
        _themeImporter = new Mock<IFormsThemeImporter>();
        _projectState = new Mock<IProjectState>();

        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());
    }

    private AddFormsViewModel CreateSut() => new(
        _formsFileService.Object,
        _themeImporter.Object,
        _projectState.Object);

    [Fact]
    public void Constructor_SelectsDefaultTheme_WhenPresentAmongAvailableThemes()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Bubblegum", "Standard" });

        AddFormsViewModel sut = CreateSut();

        sut.SelectedTheme.ShouldBe("Standard");
    }

    [Fact]
    public void Constructor_FallsBackToFirstAvailableTheme_WhenDefaultThemeIsNotPresent()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Bubblegum" });

        AddFormsViewModel sut = CreateSut();

        sut.SelectedTheme.ShouldBe("Bubblegum");
    }

    [Fact]
    public void OnAffirmative_ImportsTheSelectedTheme()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Standard", "Bubblegum" });

        AddFormsViewModel sut = CreateSut();
        sut.SelectedTheme = "Bubblegum";
        sut.IsIncludeDemoScreenGum = true;

        bool? affirmativeResult = null;
        sut.RequestClose += (_, e) => affirmativeResult = e;

        sut.OnAffirmative();

        _themeImporter.Verify(x => x.ImportTheme("Bubblegum", true), Times.Once);
        affirmativeResult.ShouldBe(true);
    }

    [Fact]
    public void OnAffirmative_FallsBackToDefaultTheme_WhenNoThemeIsSelected()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string>());

        AddFormsViewModel sut = CreateSut();

        sut.OnAffirmative();

        _themeImporter.Verify(x => x.ImportTheme("Standard", false), Times.Once);
    }
}
