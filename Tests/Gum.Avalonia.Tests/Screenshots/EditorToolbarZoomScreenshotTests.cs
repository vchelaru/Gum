using Avalonia.Controls;
using Gum.Services;
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
        WriteDensity(window, name);
    });

    // The strip's width and the share of it that is empty space (everything but its text), per zoom.
    private static void WriteDensity(Window window, string name)
    {
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        List<string> lines = new List<string>();
        try
        {
            foreach (double size in PrScreenshot.ZoomLevels)
            {
                scale.BaseFontSize = size;
                Dispatcher.UIThread.RunJobs();
                StackPanel strip = window.GetVisualDescendants().OfType<StackPanel>()
                    .First(panel => panel.Children.OfType<CheckBox>().Any());
                double text = strip.GetVisualDescendants().OfType<TextBlock>().Where(block => block.IsEffectivelyVisible && !string.IsNullOrEmpty(block.Text))
                    .Sum(block => block.Bounds.Width <= 0 ? 0 : block.DesiredSize.Width);
                double width = strip.Children.Sum(child => child.Bounds.Width + child.Margin.Left + child.Margin.Right);
                lines.Add($"zoom {size}: strip {width:F0}px, text {text:F0}px, gap/text {(width - text) / text:F2}");
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
        }
        File.WriteAllLines(Path.Combine(Environment.GetEnvironmentVariable("GUM_SCREENSHOT_DIR")!, name + "-density.txt"), lines);
    }
}
