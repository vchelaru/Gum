using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The whole main window (editor toolbar, tab headers, status bar) at every zoom level, for a PR's
/// before/after images (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter EditorToolbarZoomScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class EditorToolbarZoomScreenshotTests
{
    [SkippableFact]
    public void Dark() => Capture(ThemeVariant.Dark, "editor-toolbar-dark");

    [SkippableFact]
    public void Light() => Capture(ThemeVariant.Light, "editor-toolbar-light");

    private static void Capture(ThemeVariant theme, string name) => PrScreenshot.Run(() =>
    {
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1400;
        window.Height = 500;
        window.RequestedThemeVariant = theme;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();

        PrScreenshot.SaveWindowAtZoomLevels(window, name);
    });
}
