using System;
using Microsoft.Xna.Framework.Graphics;
using WinForms = System.Windows.Forms;
using Color = Microsoft.Xna.Framework.Color;
using Probe.Common;

namespace Probe10.KniProfileScan;

/// <summary>
/// Probe 10 - which KNI Direct3D 11 GraphicsProfile is the highest this Wine prefix supports? The
/// tool hardcodes FL10_0 (feature level 10.0), which fails on macOS where Wine exposes only ~9_3.
/// This scans from highest to lowest and stops at the first profile that creates a device, so we
/// know exactly how far the tool's profile would have to drop and what max texture size that implies
/// (9.1 = 2048, 9.3 = 4096, 10.0 = 8192).
/// </summary>
internal static class Program
{
    // Highest-to-lowest capability order. Names this KNI build does not define are skipped.
    private static readonly string[] ProfileOrder =
    {
        "FL11_1", "FL11_0", "FL10_1", "FL10_0", "HiDef", "FL9_3", "FL9_2", "FL9_1", "Reach",
    };

    [STAThread]
    private static int Main()
    {
        return ProbeLog.Run("Probe10.KniProfileScan", () =>
        {
            WinForms.Application.ThreadException += (_, e) => ProbeLog.RecordFailure(e.Exception);

            WinForms.Form form = new WinForms.Form
            {
                Text = "Probe10.KniProfileScan",
                Width = 320,
                Height = 240,
                StartPosition = WinForms.FormStartPosition.CenterScreen,
            };

            form.Shown += (_, _) =>
            {
                try
                {
                    GraphicsAdapter adapter = GraphicsAdapter.DefaultAdapter;
                    ProbeLog.Info("Adapter", adapter.Description ?? "(null)");

                    string? highest = null;
                    foreach (string name in ProfileOrder)
                    {
                        if (!Enum.TryParse(name, out GraphicsProfile profile))
                        {
                            continue; // not defined in this KNI build
                        }

                        PresentationParameters parameters = new PresentationParameters
                        {
                            BackBufferWidth = 256,
                            BackBufferHeight = 256,
                            BackBufferFormat = SurfaceFormat.Color,
                            DepthStencilFormat = DepthFormat.Depth24,
                            DeviceWindowHandle = form.Handle,
                            PresentationInterval = PresentInterval.Immediate,
                            IsFullScreen = false,
                        };

                        try
                        {
                            using GraphicsDevice device = new GraphicsDevice(adapter, profile, parameters);
                            device.Clear(Color.CornflowerBlue);
                            device.Present();
                            ProbeLog.Info(name, "OK");
                            highest = name;
                            break; // highest-to-lowest: the first success is the best supported profile
                        }
                        catch (Exception ex)
                        {
                            ProbeLog.Info(name, "FAILED: " + ex.Message);
                        }
                    }

                    if (highest != null)
                    {
                        ProbeLog.Info("HighestWorkingProfile", highest);
                    }
                    else
                    {
                        throw new InvalidOperationException("No KNI GraphicsProfile could create a Direct3D device on this prefix.");
                    }
                }
                catch (Exception ex)
                {
                    ProbeLog.RecordFailure(ex);
                }
                finally
                {
                    int seconds = ProbeConfig.HoldSeconds();
                    WinForms.Timer timer = new WinForms.Timer { Interval = Math.Max(1, seconds) * 1000 };
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop();
                        form.Close();
                    };
                    timer.Start();
                }
            };

            WinForms.Application.Run(form);
            ProbeLog.Step("Message loop exited cleanly");
        });
    }
}
