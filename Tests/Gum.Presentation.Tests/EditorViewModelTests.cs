using System.ComponentModel;
using EditorTabPlugin_XNA.ViewModels;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Services;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The editor toolbar's grid settings follow the loaded project. The toolbar may already be bound
/// when the project loads (the Avalonia head builds its tab at startup), so the load must raise
/// change notifications without writing the values back into the project.
/// </summary>
public class EditorViewModelTests
{
    private static (EditorViewModel ViewModel, Mock<IPluginManager> PluginManager) CreateSut(Mock<IFileCommands>? fileCommands = null, Mock<IPreviewLauncher>? previewLauncher = null)
    {
        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        EditorViewModel viewModel = new EditorViewModel(
            pluginManager.Object,
            (fileCommands ?? new Mock<IFileCommands>()).Object,
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IGridSnapWarningService>(),
            Mock.Of<IProjectManager>(),
            (previewLauncher ?? new Mock<IPreviewLauncher>()).Object);
        return (viewModel, pluginManager);
    }

    [Fact]
    public void HandleProjectLoad_TakesTheProjectGridSettings_AndNotifiesWithoutWritingBack()
    {
        (EditorViewModel viewModel, Mock<IPluginManager> pluginManager) = CreateSut();
        GumProjectSave project = new GumProjectSave { GridSize = 16, SnapToGrid = true };
        List<string?> changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.HandleProjectLoad(project);

        viewModel.GridSize.ShouldBe(16);
        viewModel.SnapToGrid.ShouldBeTrue();
        changed.ShouldContain(nameof(EditorViewModel.GridSize));
        changed.ShouldContain(nameof(EditorViewModel.SnapToGrid));
        pluginManager.Verify(manager => manager.ProjectPropertySet(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void HandleProjectLoad_ProjectWithoutCanvasSizes_ShowsTheDefaultsWithoutSavingOrWritingThem()
    {
        Mock<IFileCommands> fileCommands = new Mock<IFileCommands>();
        (EditorViewModel viewModel, _) = CreateSut(fileCommands);
        GumProjectSave project = new GumProjectSave { FullFileName = "/game/Project.gumx" };

        viewModel.HandleProjectLoad(project);

        viewModel.CustomCanvasSizes.Select(size => size.FriendlyName)
            .ShouldBe(ProjectLoadFills.DefaultCanvasSizes.Select(size => size.FriendlyName));
        viewModel.SelectedCustomCanvasSize.ShouldBeSameAs(viewModel.CustomCanvasSizes[0]);
        project.CustomCanvasSizes.ShouldBeNull();
        fileCommands.Verify(commands => commands.TryAutoSaveProject(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void PreviewCommand_IsDisabled_UntilAnElementIsSelected()
    {
        (EditorViewModel viewModel, _) = CreateSut();
        ComponentSave component = new ComponentSave();

        viewModel.PreviewCommand.CanExecute(null).ShouldBeFalse();

        viewModel.UpdateHasSelectedElement(component);
        viewModel.PreviewCommand.CanExecute(null).ShouldBeTrue();

        viewModel.UpdateHasSelectedElement(null);
        viewModel.PreviewCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void IsPreviewPinned_WhenSetToTrue_PinsThePreview()
    {
        Mock<IPreviewLauncher> launcher = new Mock<IPreviewLauncher>();
        launcher.Setup(l => l.Pin()).Returns(true);
        launcher.SetupGet(l => l.PinnedElement).Returns(new ScreenSave());
        (EditorViewModel viewModel, _) = CreateSut(previewLauncher: launcher);

        viewModel.IsPreviewPinned = true;

        launcher.Verify(l => l.Pin(), Times.Once);
        viewModel.IsPreviewPinned.ShouldBeTrue();
    }

    [Fact]
    public void IsPreviewPinned_WhenNoPreviewIsRunning_RevertsToFalse()
    {
        Mock<IPreviewLauncher> launcher = new Mock<IPreviewLauncher>();
        launcher.Setup(l => l.Pin()).Returns(false);
        (EditorViewModel viewModel, _) = CreateSut(previewLauncher: launcher);

        viewModel.IsPreviewPinned = true;

        viewModel.IsPreviewPinned.ShouldBeFalse();
    }

    [Fact]
    public void IsPreviewPinned_WhenSetToFalse_UnpinsThePreview()
    {
        Mock<IPreviewLauncher> launcher = new Mock<IPreviewLauncher>();
        launcher.Setup(l => l.Pin()).Returns(true);
        launcher.SetupGet(l => l.PinnedElement).Returns(new ScreenSave());
        (EditorViewModel viewModel, _) = CreateSut(previewLauncher: launcher);
        viewModel.IsPreviewPinned = true;

        launcher.SetupGet(l => l.PinnedElement).Returns((ElementSave?)null);
        viewModel.IsPreviewPinned = false;

        launcher.Verify(l => l.Unpin(), Times.Once);
    }

    [Fact]
    public void IsPreviewPinned_WhenThePreviewUnpinsItself_FollowsWithoutUnpinningAgain()
    {
        Mock<IPreviewLauncher> launcher = new Mock<IPreviewLauncher>();
        launcher.Setup(l => l.Pin()).Returns(true);
        launcher.SetupGet(l => l.PinnedElement).Returns(new ScreenSave());
        (EditorViewModel viewModel, _) = CreateSut(previewLauncher: launcher);
        viewModel.IsPreviewPinned = true;

        launcher.SetupGet(l => l.PinnedElement).Returns((ElementSave?)null);
        launcher.Raise(l => l.PinnedChanged += null);

        viewModel.IsPreviewPinned.ShouldBeFalse();
        launcher.Verify(l => l.Unpin(), Times.Never);
    }

    [Fact]
    public void RefreshPreviewRunning_WhenThePreviewHasClosed_ClearsRunningAndUnchecksPin()
    {
        Mock<IPreviewLauncher> launcher = new Mock<IPreviewLauncher>();
        launcher.Setup(l => l.Pin()).Returns(true);
        launcher.SetupGet(l => l.IsRunning).Returns(true);
        launcher.SetupGet(l => l.PinnedElement).Returns(new ScreenSave());
        (EditorViewModel viewModel, _) = CreateSut(previewLauncher: launcher);
        viewModel.RefreshPreviewRunning();
        viewModel.IsPreviewPinned = true;
        viewModel.IsPreviewRunning.ShouldBeTrue();

        launcher.SetupGet(l => l.IsRunning).Returns(false);
        launcher.SetupGet(l => l.PinnedElement).Returns((ElementSave?)null);
        viewModel.RefreshPreviewRunning();

        viewModel.IsPreviewRunning.ShouldBeFalse();
        viewModel.IsPreviewPinned.ShouldBeFalse();
        launcher.Verify(l => l.Unpin(), Times.Never);
    }

    [Fact]
    public void Preview_AfterLaunching_MarksThePreviewRunning()
    {
        Mock<IPreviewLauncher> launcher = new Mock<IPreviewLauncher>();
        (EditorViewModel viewModel, _) = CreateSut(previewLauncher: launcher);
        viewModel.UpdateHasSelectedElement(new ScreenSave());
        launcher.SetupGet(l => l.IsRunning).Returns(true);

        viewModel.Preview();

        viewModel.IsPreviewRunning.ShouldBeTrue();
    }
}
