using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Settings;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.Services;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Project > Canvas page of the Settings window in the real window and theme, at every zoom level
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter CanvasSettingsScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class CanvasSettingsScreenshotTests
{
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanvasPage(bool dark) => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, dark ? ThemeVariant.Dark : ThemeVariant.Light);
        string theme = dark ? "dark" : "light";

        viewModel.SelectPage(SettingsPageIds.Canvas);
        PrScreenshot.SaveWindowAtZoomLevels(window.Window, "settings-project-canvas-" + theme);

        // At large zoom the Guides rows sit below the first screenful, and a zoom change resets the
        // scroll, so scroll to the end again at each zoom level.
        ProjectPropertiesPageView page = window.Find<ProjectPropertiesPageView>();
        ScrollViewer scroller = page.GetVisualAncestors().OfType<ScrollViewer>().First();
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window.Window, scale);
        try
        {
            foreach (double size in PrScreenshot.ZoomLevels)
            {
                scale.BaseFontSize = size;
                for (int pass = 0; pass < 3; pass++)
                {
                    Dispatcher.UIThread.RunJobs();
                    window.Window.UpdateLayout();
                    scroller.ScrollToEnd();
                }
                PrScreenshot.SaveWindow(window.Window, $"settings-project-canvas-scrolled-{theme}-zoom-{size}");
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
        }
    });
}
