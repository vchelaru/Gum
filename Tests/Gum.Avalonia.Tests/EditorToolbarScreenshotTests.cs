using Avalonia.Controls;
using Avalonia.Styling;
using EditorTabPlugin_XNA.ViewModels;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Services;
using Gum.Wireframe;
using Moq;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Plugins.EditorTab;
using Xunit;

namespace Gum.Avalonia.Tests;

[Trait("Category", PrScreenshot.Category)]
public class EditorToolbarScreenshotTests
{
    [SkippableFact]
    public void NarrowToolbar() => PrScreenshot.Run(() =>
    {
        using ScreenshotWindow window = PrScreenshot.Show(new EditorToolbar(), 560, 44, ThemeVariant.Dark);
        window.Save("editor-toolbar-narrow");
    });

    [SkippableFact]
    public void GridSizeToolbar() => PrScreenshot.Run(() =>
    {
        using ScreenshotWindow window = PrScreenshot.Show(new EditorToolbar(), 1000, 44, ThemeVariant.Dark);
        window.Save("editor-toolbar-grid-size");
    });

    [SkippableFact]
    public void PinnedPreview() => PrScreenshot.Run(() =>
    {
        Mock<IPreviewLauncher> launcher = new Mock<IPreviewLauncher>();
        launcher.Setup(l => l.Pin()).Returns(true);
        launcher.SetupGet(l => l.IsRunning).Returns(true);
        launcher.SetupGet(l => l.PinnedElement).Returns(new ScreenSave());
        EditorViewModel viewModel = new EditorViewModel(
            Mock.Of<IPluginManager>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IGridSnapWarningService>(),
            Mock.Of<IProjectManager>(),
            launcher.Object);
        viewModel.RefreshPreviewRunning();
        viewModel.IsPreviewPinned = true;
        DockPanel content = new DockPanel { DataContext = viewModel, Children = { new EditorToolbar() } };

        using ScreenshotWindow window = PrScreenshot.Show(content, 1000, 44, ThemeVariant.Dark);
        window.Save("editor-toolbar-pinned-preview");
    });
}
