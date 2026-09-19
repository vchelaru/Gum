using System.Collections.Generic;
using System.Threading.Tasks;
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
/// Theme picking itself moved into ThemeSelectionViewModel (shared with NewProjectDialogViewModel);
/// see ThemeSelectionViewModelTests for that behavior's coverage.
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

    private ThemeSelectionViewModel CreateThemeSelection() =>
        new(_formsFileService.Object, _projectState.Object);

    private AddFormsViewModel CreateSut() => new(CreateThemeSelection(), _themeImporter.Object);

    [Fact]
    public void OnAffirmative_ImportsTheSelectedTheme()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Standard", "Bubblegum" });
        _themeImporter.Setup(x => x.ImportThemeAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);

        AddFormsViewModel sut = CreateSut();
        sut.ThemeSelection.SelectedTheme = "Bubblegum";
        sut.IsIncludeDemoScreenGum = true;

        bool? affirmativeResult = null;
        sut.RequestClose += (_, e) => affirmativeResult = e;

        // OnAffirmative is async void (it overrides a synchronous virtual member on the base
        // dialog type); ImportThemeAsync is stubbed to complete synchronously above, so by the
        // time the call returns, its continuation (including base.OnAffirmative()) has already run.
        sut.OnAffirmative();

        _themeImporter.Verify(x => x.ImportThemeAsync("Bubblegum", true), Times.Once);
        affirmativeResult.ShouldBe(true);
    }

    [Fact]
    public void OnAffirmative_FallsBackToDefaultTheme_WhenNoThemeIsSelected()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string>());
        _themeImporter.Setup(x => x.ImportThemeAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);

        AddFormsViewModel sut = CreateSut();

        sut.OnAffirmative();

        _themeImporter.Verify(x => x.ImportThemeAsync("Standard", false), Times.Once);
    }
}
