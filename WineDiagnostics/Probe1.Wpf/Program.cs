using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Probe.Common;

namespace Probe1.Wpf;

/// <summary>
/// Probe 1 - can WPF create and render a window under Wine? The Gum tool's chrome (windows,
/// panels, menus, dialogs) is WPF. This opens a blank window with a rendered visual tree, then
/// auto-closes. A FAIL here points at WPF/milcore initialization in the prefix.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        return ProbeLog.Run("Probe1.Wpf", () =>
        {
            ProbeLog.Step("Creating WPF Application");
            Application app = new Application();
            app.DispatcherUnhandledException += (_, e) =>
            {
                ProbeLog.RecordFailure(e.Exception);
                e.Handled = true;
                app.Shutdown();
            };

            ProbeLog.Step("Building window content");
            Window window = new Window
            {
                Title = "Probe1.Wpf",
                Width = 480,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Brushes.CornflowerBlue,
            };

            Grid grid = new Grid();
            grid.Children.Add(new Ellipse { Width = 160, Height = 160, Fill = Brushes.White });
            grid.Children.Add(new TextBlock
            {
                Text = "WPF render OK",
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });
            window.Content = grid;

            window.Loaded += (_, _) =>
            {
                ProbeLog.Step("Window Loaded - WPF visual tree rendered");
                ScheduleClose(window);
            };

            ProbeLog.Step("Calling Application.Run (showing window)");
            app.Run(window);
            ProbeLog.Step("WPF message loop exited cleanly");
        });
    }

    private static void ScheduleClose(Window window)
    {
        int seconds = ProbeConfig.HoldSeconds();
        ProbeLog.Step($"Will auto-close window after {seconds}s");
        DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(1, seconds)) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            window.Close();
        };
        timer.Start();
    }
}
