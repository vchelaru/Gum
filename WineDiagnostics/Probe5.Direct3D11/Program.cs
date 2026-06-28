using System;
using System.Runtime.InteropServices;
using Probe.Common;

namespace Probe5.Direct3D11;

/// <summary>
/// Probe 5 - can a Direct3D 11 device be created at all under this Wine prefix? The Gum tool renders
/// its design canvas with KNI on Direct3D 11, so Wine must translate D3D11 to the host GPU (on macOS:
/// either WineD3D-over-OpenGL, which is capped at 4.1, or DXVK -> Vulkan -> MoltenVK -> Metal). This
/// calls the raw <c>D3D11CreateDevice</c> with no engine in the way, so it isolates the translation
/// layer from KNI. A FAIL here (no driver type succeeds) is the most likely root cause of the tool
/// crashing on launch on macOS.
/// </summary>
internal static class Program
{
    private const uint D3D11_SDK_VERSION = 7;

    private enum DriverType
    {
        Hardware = 1,
        Reference = 2,
        Warp = 5,
    }

    [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int D3D11CreateDevice(
        IntPtr pAdapter,
        DriverType driverType,
        IntPtr software,
        uint flags,
        IntPtr pFeatureLevels,
        uint featureLevels,
        uint sdkVersion,
        out IntPtr ppDevice,
        out int pFeatureLevel,
        out IntPtr ppImmediateContext);

    private static int Main()
    {
        return ProbeLog.Run("Probe5.Direct3D11", () =>
        {
            bool anySucceeded = false;

            foreach (DriverType driverType in new[] { DriverType.Hardware, DriverType.Warp, DriverType.Reference })
            {
                ProbeLog.Step($"D3D11CreateDevice(driverType={driverType}) - exercising Wine's Direct3D 11 translation");
                IntPtr device = IntPtr.Zero;
                IntPtr context = IntPtr.Zero;
                int featureLevel = 0;
                int hr;
                try
                {
                    // Null feature-level array => let the runtime pick the best available (11.x down to 9.x).
                    hr = D3D11CreateDevice(IntPtr.Zero, driverType, IntPtr.Zero, 0, IntPtr.Zero, 0, D3D11_SDK_VERSION,
                        out device, out featureLevel, out context);
                }
                catch (DllNotFoundException ex)
                {
                    ProbeLog.RecordFailure(ex);
                    ProbeLog.Step("d3d11.dll could not be loaded under this Wine prefix.");
                    return;
                }

                if (hr >= 0)
                {
                    anySucceeded = true;
                    ProbeLog.Info(driverType + ".Result", "OK (hr=0x" + hr.ToString("X8") + ")");
                    ProbeLog.Info(driverType + ".FeatureLevel", DescribeFeatureLevel(featureLevel));
                }
                else
                {
                    ProbeLog.Info(driverType + ".Result", "FAILED (hr=0x" + hr.ToString("X8") + ")");
                }

                if (context != IntPtr.Zero)
                {
                    Marshal.Release(context);
                }
                if (device != IntPtr.Zero)
                {
                    Marshal.Release(device);
                }
            }

            if (!anySucceeded)
            {
                throw new InvalidOperationException(
                    "No Direct3D 11 device could be created with any driver type (Hardware/WARP/Reference). " +
                    "This is the most likely root cause of the Gum tool failing to launch under Wine on macOS.");
            }
        });
    }

    private static string DescribeFeatureLevel(int level)
    {
        return level switch
        {
            0xc100 => "12_1",
            0xc000 => "12_0",
            0xb100 => "11_1",
            0xb000 => "11_0",
            0xa100 => "10_1",
            0xa000 => "10_0",
            0x9300 => "9_3",
            0x9200 => "9_2",
            0x9100 => "9_1",
            _ => "0x" + level.ToString("X4"),
        };
    }
}
