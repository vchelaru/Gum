using System;
using Microsoft.Xna.Framework.Graphics;
using WinForms = System.Windows.Forms;
using Color = Microsoft.Xna.Framework.Color;
using Probe.Common;

namespace Probe6.KniDx11;

/// <summary>
/// Probe 6 - the real test. Creates a KNI <see cref="GraphicsDevice"/> on the DirectX 11 WinForms
/// platform exactly the way the Gum tool does (see <c>XnaAndWinforms.GraphicsDeviceService</c>):
/// <c>new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.FL10_0, parameters)</c>
/// against a real window handle, then clears and presents a few frames. If that fails it retries at
/// GraphicsProfile.Reach (feature level 9.1) to reveal whether lowering the tool's requested profile
/// would let it start - which is what matters when Wine on macOS only exposes a low feature level.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        return ProbeLog.Run("Probe6.KniDx11", () =>
        {
            WinForms.Application.ThreadException += (_, e) => ProbeLog.RecordFailure(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    ProbeLog.RecordFailure(ex);
                }
            };

            ProbeLog.Step("Creating host Form (mirrors the tool's GraphicsDeviceControl host)");
            WinForms.Form form = new WinForms.Form
            {
                Text = "Probe6.KniDx11",
                Width = 640,
                Height = 480,
                StartPosition = WinForms.FormStartPosition.CenterScreen,
            };

            GraphicsDevice? device = null;

            form.Shown += (_, _) =>
            {
                ProbeLog.Step("Querying GraphicsAdapter.DefaultAdapter");
                GraphicsAdapter adapter = GraphicsAdapter.DefaultAdapter;
                ProbeLog.Info("Adapter", adapter.Description ?? "(null)");

                ProbeLog.Step("Building PresentationParameters (Color back buffer / Depth24, real window handle)");
                PresentationParameters parameters = new PresentationParameters
                {
                    BackBufferWidth = Math.Max(form.ClientSize.Width, 1),
                    BackBufferHeight = Math.Max(form.ClientSize.Height, 1),
                    BackBufferFormat = SurfaceFormat.Color,
                    DepthStencilFormat = DepthFormat.Depth24,
                    DeviceWindowHandle = form.Handle,
                    PresentationInterval = PresentInterval.Immediate,
                    RenderTargetUsage = RenderTargetUsage.PreserveContents,
                    IsFullScreen = false,
                };

                try
                {
                    ProbeLog.Step("Creating KNI GraphicsDevice (GraphicsProfile.FL10_0) - the exact call the Gum tool makes");
                    device = new GraphicsDevice(adapter, GraphicsProfile.FL10_0, parameters);
                    ProbeLog.Step("GraphicsDevice created - KNI DX11 backend initialized");

                    for (int i = 0; i < 3; i++)
                    {
                        device.Clear(Color.CornflowerBlue);
                        device.Present();
                    }
                    ProbeLog.Step("Cleared + presented 3 frames successfully");
                }
                catch (Exception ex)
                {
                    ProbeLog.RecordFailure(ex);

                    // The tool's FL10_0 call failed. Does a lower profile work? If so, lowering the
                    // tool's requested GraphicsProfile is a candidate fix for macOS/Wine.
                    try
                    {
                        ProbeLog.Step("Retrying at GraphicsProfile.Reach (feature level 9.1) to see if a lower profile works");
                        using GraphicsDevice fallback = new GraphicsDevice(adapter, GraphicsProfile.Reach, parameters);
                        fallback.Clear(Color.CornflowerBlue);
                        fallback.Present();
                        ProbeLog.Info("Reach (FL9.1)", "OK - lowering the tool's GraphicsProfile would let it start");
                    }
                    catch (Exception fallbackEx)
                    {
                        ProbeLog.Info("Reach (FL9.1)", "FAILED: " + fallbackEx.Message);
                    }
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
        ProbeLog.Step($"Will auto-close form after {seconds}s");
        WinForms.Timer timer = new WinForms.Timer { Interval = Math.Max(1, seconds) * 1000 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            form.Close();
        };
        timer.Start();
    }
}
