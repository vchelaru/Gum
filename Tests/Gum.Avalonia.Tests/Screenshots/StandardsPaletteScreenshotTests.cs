using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Microsoft.Extensions.DependencyInjection;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Project tab with its Standards palette and the palette's hint label, for a PR's
/// before/after table (<c>Tools/pr-screenshots.ps1 -Filter StandardsPaletteScreenshotTests</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class StandardsPaletteScreenshotTests
{
    [SkippableFact]
    public void StandardsPalette() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(button));
        PrScreenshot.SaveWindow(tree.Input.Window, "standards-palette");
    });

    [SkippableFact]
    public void StandardsPaletteAtZoomLevels() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        tree.Project.AddInstance(screen, "Background", "Sprite");
        ComponentSave button = tree.Project.AddComponent("Controls/Button");
        tree.Project.AddInstance(button, "Label", "Text");
        tree.TreeManager.RefreshUi();
        tree.NodeFor(screen).IsExpanded = true;
        tree.NodeFor(button).IsExpanded = true;
        tree.Click(tree.NodeFor(button));

        // The real main window, so the panel, background and theme resources are the tool's own; the
        // harness window is emptied so the Project tab can present the tree.
        tree.Input.Window.Content = null;
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1000;
        window.Height = 700;
        window.Show();
        // A shown canvas creates the process-wide graphics device; this shot only needs the panel.
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();
        // One run for both themes: the main window is a singleton, and its Project tab presents the tree once.
        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            window.RequestedThemeVariant = theme;
            Dispatcher.UIThread.RunJobs();
            PrScreenshot.SaveWindowAtZoomLevels(window, $"standards-palette-{theme.Key.ToString()!.ToLowerInvariant()}");
        }
    });
}
