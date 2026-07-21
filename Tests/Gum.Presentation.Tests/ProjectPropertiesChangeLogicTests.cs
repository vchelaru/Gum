using System.Collections.Generic;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for <see cref="ProjectPropertiesChangeLogic"/>, extracted
/// out of <c>MainPropertiesWindowPlugin.HandlePropertyChanged</c> into headless
/// <c>Gum.Presentation</c> (#3929) so the decision logic is testable without plugin composition.
/// </summary>
public class ProjectPropertiesChangeLogicTests
{
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IFontManager> _fontManager = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IWireframeCommands> _wireframeCommands = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly Mock<ILocalizationService> _localizationService = new();
    private readonly ProjectPropertiesChangeLogic _logic;

    public ProjectPropertiesChangeLogicTests()
    {
        _logic = new ProjectPropertiesChangeLogic(
            _projectManager.Object,
            _fontManager.Object,
            _dialogService.Object,
            _projectState.Object,
            _wireframeObjectManager.Object,
            _fileCommands.Object,
            _wireframeCommands.Object,
            _guiCommands.Object,
            _pluginManager.Object,
            _localizationService.Object);
    }

    [Fact]
    public async Task HandlePropertyChanged_DoesNothing_WhenViewModelIsUpdatingFromModel()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        // SetFrom leaves IsUpdatingFromModel false when it completes, so force it back on to
        // simulate a property changing while a bulk model->VM sync is still in progress.
        typeof(ProjectPropertiesViewModel)
            .GetProperty(nameof(ProjectPropertiesViewModel.IsUpdatingFromModel))!
            .SetValue(viewModel, true);

        ProjectPropertyChangeResult result = await _logic.HandlePropertyChanged(viewModel, nameof(viewModel.ShowCheckerBackground));

        result.FontCharacterFileChanged.ShouldBeFalse();
        _fileCommands.Verify(f => f.TryAutoSaveProject(It.IsAny<bool>()), Times.Never);
        _wireframeCommands.Verify(w => w.Refresh(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task HandlePropertyChanged_ShowCheckerBackground_AutoSavesWithoutRefreshingWireframe()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();

        await _logic.HandlePropertyChanged(viewModel, nameof(viewModel.ShowCheckerBackground));

        _fileCommands.Verify(f => f.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
        _wireframeCommands.Verify(w => w.Refresh(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task HandlePropertyChanged_InvalidFontRange_ShowsMessageAndDoesNotTouchFontCache()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        // No spaces (so TryFixRange can't "fix" it) but start >= end, which GetIfIsValidRange rejects.
        viewModel.FontRanges = "50-40";

        await _logic.HandlePropertyChanged(viewModel, nameof(viewModel.FontRanges));

        _dialogService.Verify(d => d.ShowMessage("The entered Font Range is not valid.", null, null), Times.Once);
        _fontManager.Verify(f => f.DeleteFontCacheFolder(), Times.Never);
    }

    [Fact]
    public async Task HandlePropertyChanged_LocalizationFiles_NormalizesAbsolutePathsToRelative()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        _projectState.Setup(p => p.ProjectDirectory).Returns(@"C:\MyProject\");
        viewModel.LocalizationFiles = new List<string> { @"C:\MyProject\Localization\en.csv" };

        await _logic.HandlePropertyChanged(viewModel, nameof(viewModel.LocalizationFiles));

        viewModel.LocalizationFiles.ShouldContain(@"Localization\en.csv");
        _fileCommands.Verify(f => f.LoadLocalizationFile(), Times.Never);
    }

    [Fact]
    public async Task HandlePropertyChanged_UseFontCharacterFileTurnedOff_ResetsFontRangesAndReportsPathCleared()
    {
        ProjectPropertiesViewModel viewModel = MakeViewModel();
        viewModel.UseFontCharacterFile = false;

        ProjectPropertyChangeResult result = await _logic.HandlePropertyChanged(viewModel, nameof(viewModel.UseFontCharacterFile));

        result.FontCharacterFileChanged.ShouldBeTrue();
        result.FontCharacterFileAbsolute.ShouldBeNull();
        viewModel.FontRanges.ShouldBe(BmfcSave.DefaultRanges);
    }

    private static ProjectPropertiesViewModel MakeViewModel()
    {
        ProjectPropertiesViewModel viewModel = new();
        GumProjectSave gumProject = new();
        viewModel.SetFrom(autoSave: false, gumProject);
        return viewModel;
    }
}
