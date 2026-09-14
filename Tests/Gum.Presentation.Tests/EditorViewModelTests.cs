using System.ComponentModel;
using EditorTabPlugin_XNA.ViewModels;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
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
    private static (EditorViewModel ViewModel, Mock<IPluginManager> PluginManager) CreateSut()
    {
        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        EditorViewModel viewModel = new EditorViewModel(
            pluginManager.Object,
            Mock.Of<IFileCommands>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IGridSnapWarningService>(),
            Mock.Of<IProjectManager>());
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
}
