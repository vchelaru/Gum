using System;
using Microsoft.Xna.Framework.Graphics;
using WinForms = System.Windows.Forms;
using Color = Microsoft.Xna.Framework.Color;
using Probe.Common;

namespace Probe8.MonoGameWindowsDX;

/// <summary>
/// Probe 8 - the MonoGame Direct3D 11 path (MonoGame.Framework.WindowsDX), hosted in WinForms the
/// same way the tool hosts KNI. It tries GraphicsProfile.HiDef (feature level 10.0, what the tool
/// effectively asks for) and falls back to Reach (feature level 9.1). This shows whether MonoGame's
/// DX11 backend hits the same feature-level wall as KNI, and which profile actually works here.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        return ProbeLog.Run("Probe8.MonoGameWindowsDX", () =>
        {
            WinForms.Application.ThreadException += (_, e) => ProbeLog.RecordFailure(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    ProbeLog.RecordFailure(ex);
                }
            };

            ProbeLog.Step("Creating host Form");
            WinForms.Form form = new WinForms.Form
            {
                Text = "Probe8.MonoGameWindowsDX",
                Width = 640,
                Height = 480,
                StartPosition = WinForms.FormStartPosition.CenterScreen,
            };

            GraphicsDevice? device = null;

            form.Shown += (_, _) =>
            {
                try
                {
                    ProbeLog.Step("Querying GraphicsAdapter.DefaultAdapter");
                    GraphicsAdapter adapter = GraphicsAdapter.DefaultAdapter;
                    ProbeLog.Info("Adapter", adapter.Description ?? "(null)");

                    PresentationParameters parameters = new PresentationParameters
                    {
                        BackBufferWidth = Math.Max(form.ClientSize.Width, 1),
                        BackBufferHeight = Math.Max(form.ClientSize.Height, 1),
                        BackBufferFormat = SurfaceFormat.Color,
                        DepthStencilFormat = DepthFormat.Depth24,
                        DeviceWindowHandle = form.Handle,
                        PresentationInterval = PresentInterval.Immediate,
                        IsFullScreen = false,
                    };

                    foreach (GraphicsProfile profile in new[] { GraphicsProfile.HiDef, GraphicsProfile.Reach })
                    {
                        try
                        {
                            ProbeLog.Step($"Creating MonoGame WindowsDX GraphicsDevice (GraphicsProfile.{profile})");
                            device = new GraphicsDevice(adapter, profile, parameters);
                            device.Clear(Color.CornflowerBlue);
                            device.Present();
                            ProbeLog.Info(profile.ToString(), "OK");
                            ProbeLog.Info("HighestWorkingProfile", profile.ToString());
                            break;
                        }
                        catch (Exception ex)
                        {
                            ProbeLog.Info(profile.ToString(), "FAILED: " + ex.Message);
                            device = null;
                        }
                    }

                    if (device == null)
                    {
                        throw new InvalidOperationException("No MonoGame WindowsDX profile (HiDef or Reach) could create a device on this prefix.");
                    }
                }
                catch (Exception ex)
                {
                    ProbeLog.RecordFailure(ex);
                }
                finally
                {
                    ScheduleClose(form);
                }
            };

            ProbeLog.Step("Calling Application.Run");
            WinForms.Application.Run(form);
            device?.Dispose();
            ProbeLog.Step("Message loop exited cleanly");
        });
    }

    private static void ScheduleClose(WinForms.Form form)
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
}
