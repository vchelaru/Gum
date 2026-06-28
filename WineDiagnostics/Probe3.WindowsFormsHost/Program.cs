using System;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using WpfApp = System.Windows.Application;
using WfHost = System.Windows.Forms.Integration.WindowsFormsHost;
using Probe.Common;

namespace Probe3.WindowsFormsHost;

/// <summary>
/// Probe 3 - can a WinForms control be hosted inside a WPF window via <c>WindowsFormsHost</c>?
/// This is exactly how the Gum tool embeds its WinForms rendering control inside its WPF shell, so
/// this interop layer is a real part of the tool's startup. A FAIL here (when probes 1 and 2 pass)
/// points at the WPF/WinForms interop bridge under Wine.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        return ProbeLog.Run("Probe3.WindowsFormsHost", () =>
        {
            WpfApp app = new WpfApp();
            app.DispatcherUnhandledException += (_, e) =>
            {
                ProbeLog.RecordFailure(e.Exception);
                e.Handled = true;
                app.Shutdown();
            };

            ProbeLog.Step("Creating WPF Window that hosts a WinForms control via WindowsFormsHost");
            Window window = new Window
            {
                Title = "Probe3.WindowsFormsHost",
                Width = 480,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            WfHost host = new WfHost();
            WinForms.Panel panel = new WinForms.Panel { Dock = WinForms.DockStyle.Fill };
            panel.Paint += (_, e) =>
            {
                e.Graphics.Clear(Color.CornflowerBlue);
                using SolidBrush brush = new SolidBrush(Color.White);
                e.Graphics.FillRectangle(brush, 20, 20, 220, 140);
            };
            host.Child = panel;
            window.Content = host;

            window.Loaded += (_, _) =>
            {
                ProbeLog.Step("Window Loaded - WinForms control hosted in WPF rendered");
                ScheduleClose(window);
            };

            ProbeLog.Step("Calling Application.Run");
            app.Run(window);
            ProbeLog.Step("Message loop exited cleanly");
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
