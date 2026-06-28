using System;
using System.Drawing;
using System.Windows.Forms;
using Probe.Common;

namespace Probe2.WinForms;

/// <summary>
/// Probe 2 - can WinForms + GDI+ (System.Drawing) create and paint a window under Wine? The Gum
/// tool uses WinForms for its tree view (MultiSelectTreeView) and GDI+ for custom node theming,
/// and hosts its rendering control via WinForms. A FAIL here points at WinForms/GDI+ in the prefix.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        return ProbeLog.Run("Probe2.WinForms", () =>
        {
            Application.ThreadException += (_, e) => ProbeLog.RecordFailure(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    ProbeLog.RecordFailure(ex);
                }
            };

            ProbeLog.Step("Enabling visual styles");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ProbeLog.Step("Creating Form");
            Form form = new Form
            {
                Text = "Probe2.WinForms",
                Width = 480,
                Height = 320,
                StartPosition = FormStartPosition.CenterScreen,
            };

            form.Paint += (_, e) =>
            {
                // Exercise GDI+ (System.Drawing), as Gum's tree-view theming does.
                e.Graphics.Clear(Color.CornflowerBlue);
                using SolidBrush brush = new SolidBrush(Color.White);
                e.Graphics.FillEllipse(brush, 40, 30, 160, 160);
                using Font font = new Font("Arial", 14f);
                e.Graphics.DrawString("WinForms + GDI+ OK", font, Brushes.Black, 40, 210);
            };

            form.Shown += (_, _) =>
            {
                ProbeLog.Step("Form shown - WinForms + GDI+ painted");
                ScheduleClose(form);
            };

            ProbeLog.Step("Calling Application.Run");
            Application.Run(form);
            ProbeLog.Step("WinForms message loop exited cleanly");
        });
    }

    private static void ScheduleClose(Form form)
    {
        int seconds = ProbeConfig.HoldSeconds();
        ProbeLog.Step($"Will auto-close form after {seconds}s");
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = Math.Max(1, seconds) * 1000 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            form.Close();
        };
        timer.Start();
    }
}
