using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Shell;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The main window's title row (logo, menu, project name) grows with the base font size, so zooming in
/// never clips the menu text and the logo keeps its proportion to it (#7314).
/// </summary>
public class MainTitleRowZoomTests
{
    [AvaloniaFact]
    public void TitleRow_FollowsTheBaseFontSize()
    {
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        DockPanel content = ((Panel)window.Content!).Children.OfType<DockPanel>().Single();
        Grid titleRow = content.Children.OfType<Grid>().First();
        global::Avalonia.Controls.Image logo = titleRow.Children.OfType<global::Avalonia.Controls.Image>().Single();
        bool rowHasFixedHeight = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        try
        {
            scale.BaseFontSize = 12;
            Dispatcher.UIThread.RunJobs();
            double logoAtDefault = logo.Height;
            double rowAtDefault = titleRow.Height;

            scale.BaseFontSize = 24;
            Dispatcher.UIThread.RunJobs();

            logo.Height.ShouldBe(logoAtDefault * 2);
            if (rowHasFixedHeight)
            {
                titleRow.Height.ShouldBe(rowAtDefault * 2);
                window.ExtendClientAreaTitleBarHeightHint.ShouldBe(titleRow.Height);
            }

            scale.BaseFontSize = 9;
            Dispatcher.UIThread.RunJobs();

            logo.Height.ShouldBeLessThan(logoAtDefault);
            if (rowHasFixedHeight)
            {
                titleRow.Height.ShouldBeLessThan(rowAtDefault);
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
        }
    }
}
