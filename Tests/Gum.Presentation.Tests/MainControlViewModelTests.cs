using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Moq;
using Shouldly;
using TextureCoordinateSelectionPlugin.Logic;
using TextureCoordinateSelectionPlugin.Models;
using TextureCoordinateSelectionPlugin.ViewModels;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for MainControlViewModel, relocated out of the WPF Gum tool
/// project into the headless Gum.Presentation assembly (ADR-0005, #3754). The move required
/// extracting <see cref="ITextureCoordinateDisplayController"/> off the concrete, WPF-bound
/// TextureCoordinateDisplayController (which holds a ScrollBarLogicWpf and a MainControl view and
/// stays in the Gum tool project) so the view model's dependency is headless, plus converting the
/// WPF-Visibility-typed dropdown property to a bool.
/// </summary>
public class MainControlViewModelTests
{
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IFileWatchManager> _fileWatchManager;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<ITextureCoordinateDisplayController> _displayController;
    private readonly MainControlViewModel _viewModel;

    public MainControlViewModelTests()
    {
        _projectManager = new Mock<IProjectManager>();
        _fileCommands = new Mock<IFileCommands>();
        _fileWatchManager = new Mock<IFileWatchManager>();
        _guiCommands = new Mock<IGuiCommands>();
        _displayController = new Mock<ITextureCoordinateDisplayController>();

        _viewModel = new MainControlViewModel(
            _projectManager.Object,
            _fileCommands.Object,
            _fileWatchManager.Object,
            _guiCommands.Object,
            _displayController.Object);
    }

    [Fact]
    public void Constructor_SubscribesToZoomLevelChanged_SoRaisingItUpdatesSelectedZoomLevel()
    {
        _displayController.Raise(controller => controller.ZoomLevelChanged += null, 400);

        _viewModel.SelectedZoomLevel.ShouldBe(400);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void IsExposedSourceDropdownVisible_ReflectsAvailableExposedSourcesCount(int sourceCount, bool expectedVisible)
    {
        List<ExposedTextureCoordinateSet> sources = new List<ExposedTextureCoordinateSet>();
        for (int i = 0; i < sourceCount; i++)
        {
            sources.Add(new ExposedTextureCoordinateSet { SourceObjectName = $"Source{i}" });
        }

        _viewModel.AvailableExposedSources = sources.Count > 0 ? sources : null;

        _viewModel.IsExposedSourceDropdownVisible.ShouldBe(expectedVisible);
    }

    [Fact]
    public void IsSnapToGridChecked_Change_UpdatesDisplayControllerSnapGrid()
    {
        _viewModel.SelectedSnapToGridValue = 8;

        _viewModel.IsSnapToGridChecked = true;

        _displayController.Verify(controller => controller.UpdateSnapGrid(true, 8), Times.Once);
    }

    [Fact]
    public void SelectedExposedSource_Change_SetsAndRefreshesDisplayController()
    {
        ExposedTextureCoordinateSet source = new ExposedTextureCoordinateSet { SourceObjectName = "Icon" };

        _viewModel.SelectedExposedSource = source;

        _displayController.Verify(controller => controller.SetCurrentExposedSource(source), Times.Once);
        _displayController.Verify(controller => controller.Refresh(), Times.Once);
    }

    [Fact]
    public void LoadSettings_ForAProjectWithNoSettingsFile_RestoresTheDefaultsWithoutSaving()
    {
        // The previous project turned snapping on; this one never saved texture settings.
        _viewModel.IsSnapToGridChecked = true;
        _viewModel.SelectedSnapToGridValue = 8;
        _projectManager.SetupGet(manager => manager.GumProjectSave)
            .Returns(new GumProjectSave { FullFileName = "/NoTextureSettings/Project.gumx" });
        _fileCommands.Invocations.Clear();

        _viewModel.LoadSettings();

        _viewModel.IsSnapToGridChecked.ShouldBeFalse();
        _viewModel.SelectedSnapToGridValue.ShouldBe(16);
        _fileCommands.Verify(commands => commands.SaveIfDiffers(It.IsAny<FilePath>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void UpdateExposedSources_WhenTheViewClearsTheSelectionForTheNewList_HandsTheControllerOnlyTheNewSource()
    {
        ExposedTextureCoordinateSet oldSource = new ExposedTextureCoordinateSet { SourceObjectName = "Icon" };
        _viewModel.UpdateExposedSources(new List<ExposedTextureCoordinateSet> { oldSource }, preserveSelection: false);
        // A combo box bound to the list clears its selection when the list is replaced.
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainControlViewModel.AvailableExposedSources))
            {
                _viewModel.SelectedExposedSource = null;
            }
        };
        _displayController.Invocations.Clear();
        ExposedTextureCoordinateSet newSource = new ExposedTextureCoordinateSet { SourceObjectName = "Icon" };

        _viewModel.UpdateExposedSources(new List<ExposedTextureCoordinateSet> { newSource }, preserveSelection: true);

        _viewModel.SelectedExposedSource.ShouldBeSameAs(newSource);
        _displayController.Verify(controller => controller.SetCurrentExposedSource(null), Times.Never);
        _displayController.Verify(controller => controller.SetCurrentExposedSource(newSource), Times.Once);
        _displayController.Verify(controller => controller.Refresh(), Times.Once);
    }

    [Fact]
    public void SelectedZoomLevel_Change_UpdatesDisplayControllerZoom()
    {
        _viewModel.SelectedZoomLevel = 200;

        _displayController.Verify(controller => controller.UpdateZoom(200), Times.Once);
    }

    [Fact]
    public void ZoomIn_AtLargestAvailableZoomLevel_DoesNotChange()
    {
        _viewModel.SelectedZoomLevel = 3200;

        _viewModel.ZoomIn();

        _viewModel.SelectedZoomLevel.ShouldBe(3200);
    }

    [Fact]
    public void ZoomIn_SelectsNextLargerAvailableZoomLevel()
    {
        _viewModel.SelectedZoomLevel = 100;

        _viewModel.ZoomIn();

        _viewModel.SelectedZoomLevel.ShouldBe(150);
    }

    [Fact]
    public void ZoomOut_AtSmallestAvailableZoomLevel_DoesNotChange()
    {
        _viewModel.SelectedZoomLevel = 10;

        _viewModel.ZoomOut();

        _viewModel.SelectedZoomLevel.ShouldBe(10);
    }

    [Fact]
    public void ZoomOut_SelectsNextSmallerAvailableZoomLevel()
    {
        _viewModel.SelectedZoomLevel = 100;

        _viewModel.ZoomOut();

        _viewModel.SelectedZoomLevel.ShouldBe(75);
    }

    [Fact]
    public void AvailableZoomLevels_ContainsEveryLevelTheCanvasWheelZoomLandsOn()
    {
        // The combo binds to this list; a wheel zoom that lands on a level missing from it
        // renders the combo blank (#4793). 150 and 75 are the first wheel steps from 100.
        _viewModel.AvailableZoomLevels.ShouldBe(new[]
        {
            3200, 1600, 1200, 800, 500, 300, 200, 150, 100, 75, 50, 33, 25, 10,
        });
    }
}
