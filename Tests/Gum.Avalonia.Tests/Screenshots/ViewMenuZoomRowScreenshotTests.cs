using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.Menus;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The View menu's Zoom row as the Windows and Linux in-window menu draws it, at every zoom level, for a PR's
/// images (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter ViewMenuZoomRowScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ViewMenuZoomRowScreenshotTests
{
    [SkippableFact]
    public void ZoomRow() => PrScreenshot.Run(() =>
    {
        MenuModel model = TestAppBuilder.Services.GetRequiredService<StandardMenuModelBuilder>().Build();
        Menu menu = AvaloniaMenuBuilder.Build(model);
        using ScreenshotWindow window = PrScreenshot.Show(menu, 520, 360);
        MenuItem view = menu.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "View");
        view.Open();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        PrScreenshot.SaveWindowAtZoomLevels(window.Window, "view-menu-zoom-row");
    });
}
