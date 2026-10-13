using System.Linq;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.FontsPlugin;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Moq;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// What happens when something on the Fonts tab changes: the project is updated, the baked fonts are
/// made again when a range or the generator changed, and the project is saved and the canvas refreshed.
/// </summary>
public class FontsChangeLogicTests
{
    private readonly Mock<IFontManager> _fontManager = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IWireframeCommands> _wireframeCommands = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly Mock<Gum.Services.Dialogs.IDialogService> _dialogService = new();
    private readonly FontsChangeLogic _logic;
    private readonly GumProjectSave _project = new GumProjectSave();

    public FontsChangeLogicTests()
    {
        _projectState.Setup(state => state.GumProjectSave).Returns(_project);
        _logic = new FontsChangeLogic(
            _fontManager.Object,
            _dialogService.Object,
            _projectState.Object,
            _fileCommands.Object,
            _wireframeCommands.Object,
            _guiCommands.Object,
            _pluginManager.Object);
    }

    [Fact]
    public async Task EntriesChanged_WithValidRanges_WritesThemToTheProject_RemakesTheFonts_AndSaves()
    {
        FontsViewModel viewModel = MakeViewModel();
        viewModel.AddEntry();
        viewModel.Entries[1].FontFamily = "Material Icons";
        viewModel.Entries[1].Ranges = "57344-63743";

        await _logic.HandlePropertyChanged(viewModel, nameof(FontsViewModel.Entries));

        _project.GetFontRanges("Material Icons").ShouldBe("57344-63743");
        _fontManager.Verify(fonts => fonts.DeleteFontCacheFolder(), Times.Once);
        _fontManager.Verify(fonts => fonts.CreateAllMissingFontFiles(_project, It.IsAny<bool>()), Times.Once);
        _wireframeCommands.Verify(wireframe => wireframe.Refresh(true, true), Times.Once);
        _fileCommands.Verify(files => files.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
        _pluginManager.Verify(plugins => plugins.ProjectPropertySet(nameof(FontsViewModel.Entries)), Times.Once);
    }

    [Fact]
    public async Task EntriesChanged_WithAnInvalidRange_DoesNotTouchTheFontCache()
    {
        FontsViewModel viewModel = MakeViewModel();
        // No spaces (so TryFixRange can't "fix" it) but start >= end, which GetIfIsValidRange rejects.
        viewModel.Entries[0].Ranges = "50-40";

        await _logic.HandlePropertyChanged(viewModel, nameof(FontsViewModel.Entries));

        _fontManager.Verify(fonts => fonts.DeleteFontCacheFolder(), Times.Never);
        viewModel.Entries[0].HasError.ShouldBeTrue();
    }

    [Fact]
    public async Task EntriesChanged_WithARangeThatOnlyHasSpaces_FixesItAndWaitsForTheChangeThatCausesAfterwards()
    {
        FontsViewModel viewModel = MakeViewModel();
        viewModel.Entries[0].Ranges = "32-126, 160-255";

        await _logic.HandlePropertyChanged(viewModel, nameof(FontsViewModel.Entries));

        viewModel.Entries[0].Ranges.ShouldBe("32-126,160-255");
        _fontManager.Verify(fonts => fonts.DeleteFontCacheFolder(), Times.Never);
    }

    [Fact]
    public async Task EntriesChanged_ARowWithNoFont_IsNotSavedAndDoesNotRemakeTheFonts()
    {
        FontsViewModel viewModel = MakeViewModel();
        viewModel.AddEntry();

        await _logic.HandlePropertyChanged(viewModel, nameof(FontsViewModel.Entries));

        _project.FontRangeEntries.Count.ShouldBe(1);
        _fontManager.Verify(fonts => fonts.DeleteFontCacheFolder(), Times.Never);
        _fileCommands.Verify(files => files.TryAutoSaveProject(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UseFontCharacterFileTurnedOff_ResetsTheDefaultRow_AndReportsThePathCleared()
    {
        FontsViewModel viewModel = MakeViewModel();
        viewModel.UseFontCharacterFile = false;
        viewModel.Entries[0].Ranges = "65-90";

        FontsChangeResult result = await _logic.HandlePropertyChanged(viewModel, nameof(FontsViewModel.UseFontCharacterFile));

        result.FontCharacterFileChanged.ShouldBeTrue();
        result.FontCharacterFileAbsolute.ShouldBeNull();
        viewModel.Entries[0].Ranges.ShouldBe(BmfcSave.DefaultRanges);
    }

    [Fact]
    public async Task FontGeneratorChanged_ClearsTheFontCache_RemakesTheFonts_AndRefreshesTheVariables()
    {
        FontsViewModel viewModel = MakeViewModel();
        viewModel.FontGenerator = FontGeneratorType.KernSmith;

        await _logic.HandlePropertyChanged(viewModel, nameof(FontsViewModel.FontGenerator));

        _project.FontGenerator.ShouldBe(FontGeneratorType.KernSmith);
        _fontManager.Verify(fonts => fonts.DeleteFontCacheFolder(), Times.Once);
        _fontManager.Verify(fonts => fonts.CreateAllMissingFontFiles(_project, It.IsAny<bool>()), Times.Once);
        _guiCommands.Verify(gui => gui.RefreshVariables(true), Times.Once);
    }

    [Fact]
    public async Task SpacingChanged_WritesItToTheProject_AndSavesWithoutRemakingTheFonts()
    {
        FontsViewModel viewModel = MakeViewModel();
        viewModel.FontSpacingVertical = 5;

        await _logic.HandlePropertyChanged(viewModel, nameof(FontsViewModel.FontSpacingVertical));

        _project.FontSpacingVertical.ShouldBe(5);
        _fileCommands.Verify(files => files.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
        _fontManager.Verify(fonts => fonts.DeleteFontCacheFolder(), Times.Never);
    }

    [Theory]
    [InlineData(nameof(FontsViewModel.AvailableFonts))]
    [InlineData(nameof(FontsViewModel.IsFontRangesReadOnly))]
    public async Task ViewOnlyProperties_ChangeNothing(string propertyName)
    {
        FontsViewModel viewModel = MakeViewModel();

        await _logic.HandlePropertyChanged(viewModel, propertyName);

        _fileCommands.Verify(files => files.TryAutoSaveProject(It.IsAny<bool>()), Times.Never);
        _pluginManager.Verify(plugins => plugins.ProjectPropertySet(It.IsAny<string>()), Times.Never);
    }

    private FontsViewModel MakeViewModel()
    {
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(_project);
        return viewModel;
    }
}
