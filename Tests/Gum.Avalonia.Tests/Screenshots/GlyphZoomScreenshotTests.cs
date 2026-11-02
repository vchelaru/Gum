using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The real main window with the Project tab and the Performance tab showing (radio buttons, check box,
/// search and collapse buttons, the Errors status dot) at base font sizes 6, 12 and 24, for a PR's before/after
/// images (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter GlyphZoomScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class GlyphZoomScreenshotTests
{
    [SkippableFact]
    public void Dark() => Capture(ThemeVariant.Dark, "glyph-zoom-dark");

    private static void Capture(ThemeVariant theme, string name) => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        tree.Project.AddInstance(screen, "Background", "Sprite");
        tree.TreeManager.RefreshUi();
        tree.Click(tree.NodeFor(screen));

        tree.Input.Window.Content = null;
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1500;
        window.Height = 800;
        window.RequestedThemeVariant = theme;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();
        SelectTab(window, "Performance");

        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        try
        {
            foreach (double size in new[] { 6d, 12d, 24d })
            {
                scale.BaseFontSize = size;
                PrScreenshot.SaveWindow(window, $"{name}-zoom-{size}");
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
        }
    });

    private static void SelectTab(Window window, string title)
    {
        foreach (TabControl tabs in window.GetVisualDescendants().OfType<TabControl>())
        {
            foreach (object? item in tabs.Items)
            {
                if (item is Gum.Avalonia.Shell.AvaloniaPluginTab tab && tab.Title == title)
                {
                    tabs.SelectedItem = item;
                }
            }
        }
        Dispatcher.UIThread.RunJobs();
    }
}
