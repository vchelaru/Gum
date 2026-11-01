using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.HideShowTools;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>The real main window with tools shown and hidden (View > Hide Tools).</summary>
[Trait("Category", PrScreenshot.Category)]
public class HideToolsScreenshotTests
{
    [SkippableFact]
    public void ToolsShownAndHidden() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        tree.Project.AddInstance(screen, "Background", "Sprite");
        tree.TreeManager.RefreshUi();
        tree.NodeFor(screen).IsExpanded = true;
        tree.Click(tree.NodeFor(screen));

        tree.Input.Window.Content = null;
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        AvaloniaTabManager tabs = TestAppBuilder.Services.GetRequiredService<AvaloniaTabManager>();
        window.MinHeight = 0;
        window.Width = 1000;
        window.Height = 700;
        window.RequestedThemeVariant = ThemeVariant.Dark;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();
        // Screenshot runs only; the singleton window cannot be closed and shown again, so hide it.
        try
        {
            PrScreenshot.SaveWindowAtZoomLevels(window, "hide-tools-shown");
            new HideShowToolsLogic(tabs).ToggleToolsVisibility();
            Dispatcher.UIThread.RunJobs();
            PrScreenshot.SaveWindowAtZoomLevels(window, "hide-tools-hidden");
        }
        finally
        {
            if (!tabs.IsToolsVisible)
            {
                new HideShowToolsLogic(tabs).ToggleToolsVisibility();
            }
            window.Hide();
        }
    });
}
