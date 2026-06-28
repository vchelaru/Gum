using System;
using System.Management;
using Probe.Common;

namespace Probe12.Wmi;

/// <summary>
/// Probe 12 - does WMI (System.Management) work under Wine? The Gum tool references System.Management
/// (9.0.3), and Microsoft.AppCenter queries device/hardware info. Wine's WMI is incomplete, so a WMI
/// call could throw or hang. This runs a simple Win32_VideoController query to see whether WMI
/// responds. Runs last in the suite: if it hangs, WMI itself is the finding.
/// </summary>
internal static class Program
{
    private static int Main()
    {
        return ProbeLog.Run("Probe12.Wmi", () =>
        {
            ProbeLog.Step("Querying WMI: SELECT Name FROM Win32_VideoController");
            using ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");

            int count = 0;
            foreach (ManagementBaseObject item in searcher.Get())
            {
                ProbeLog.Info("VideoController", item["Name"]?.ToString() ?? "(null)");
                item.Dispose();
                count++;
            }

            ProbeLog.Info("ResultCount", count.ToString());
            if (count == 0)
            {
                ProbeLog.Step("WMI returned no rows (Wine WMI may be a stub) - not necessarily fatal");
            }
            else
            {
                ProbeLog.Step("WMI query succeeded");
            }
        });
    }
}
