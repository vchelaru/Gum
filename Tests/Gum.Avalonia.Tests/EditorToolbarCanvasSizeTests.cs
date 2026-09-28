using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EditorTabPlugin_XNA.ViewModels;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Services;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Editor toolbar's canvas size combo box shows the view model's selected size after a
/// project load replaces its list of sizes (#5374).
/// </summary>
public class EditorToolbarCanvasSizeTests
{
    [AvaloniaFact]
    public void ProjectLoad_AfterAProjectWithItsOwnSizes_ShowsProjectDefault()
    {
        EditorViewModel viewModel = new EditorViewModel(
            Mock.Of<IPluginManager>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IGridSnapWarningService>(),
            Mock.Of<IProjectManager>(),
            Mock.Of<IPreviewLauncher>());
        EditorToolbar toolbar = new EditorToolbar();
        Window window = new Window { Content = new DockPanel { DataContext = viewModel, Children = { toolbar } } };
        float canvasWidth = GraphicalUiElement.CanvasWidth;
        float canvasHeight = GraphicalUiElement.CanvasHeight;
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            ComboBox sizes = toolbar.GetVisualDescendants().OfType<ComboBox>()
                .Single(combo => combo.ItemsSource == viewModel.CustomCanvasSizes);
            // A loaded project carries its own size objects, deserialized from the .gumx.
            viewModel.HandleProjectLoad(new GumProjectSave
            {
                CustomCanvasSizes = new List<CustomCanvasSize>
                {
                    new CustomCanvasSize { FriendlyName = "Project Default" },
                    new CustomCanvasSize { FriendlyName = "480p", Width = 640, Height = 480 },
                },
            });
            Dispatcher.UIThread.RunJobs();

            // A project with no sizes of its own gets the built-in list.
            viewModel.HandleProjectLoad(new GumProjectSave { CustomCanvasSizes = new List<CustomCanvasSize>() });
            Dispatcher.UIThread.RunJobs();

            viewModel.SelectedCustomCanvasSize.ShouldBeSameAs(viewModel.CustomCanvasSizes[0]);
            sizes.SelectedItem.ShouldBeSameAs(viewModel.SelectedCustomCanvasSize);
            sizes.SelectedIndex.ShouldBe(0);
        }
        finally
        {
            window.Close();
            GraphicalUiElement.CanvasWidth = canvasWidth;
            GraphicalUiElement.CanvasHeight = canvasHeight;
        }
    }
}
