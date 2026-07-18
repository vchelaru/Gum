using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Moq;
using Shouldly;
using TextureCoordinateSelectionPlugin.Logic;
using TextureCoordinateSelectionPlugin.Models;
using TextureCoordinateSelectionPlugin.ViewModels;

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

        _viewModel.SelectedZoomLevel.ShouldBe(200);
    }

    [Fact]
    public void ZoomOut_AtSmallestAvailableZoomLevel_DoesNotChange()
    {
        _viewModel.SelectedZoomLevel = 12;

        _viewModel.ZoomOut();

        _viewModel.SelectedZoomLevel.ShouldBe(12);
    }

    [Fact]
    public void ZoomOut_SelectsNextSmallerAvailableZoomLevel()
    {
        _viewModel.SelectedZoomLevel = 100;

        _viewModel.ZoomOut();

        _viewModel.SelectedZoomLevel.ShouldBe(50);
    }
}
