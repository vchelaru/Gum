using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The main window's title row (logo, menu, project name) at every zoom level, for a PR's before/after images
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter MainTitleRowScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class MainTitleRowScreenshotTests
{
    [SkippableFact]
    public void TitleRow() => PrScreenshot.Run(() =>
    {
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 900;
        window.Height = 160;
        window.Show();
        // A shown canvas creates the process-wide graphics device; this window only needs its chrome.
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();

        PrScreenshot.SaveWindowAtZoomLevels(window, "main-title-row");
    });
}
