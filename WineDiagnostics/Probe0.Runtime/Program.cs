using System;
using System.Runtime.InteropServices;
using Probe.Common;

namespace Probe0.Runtime;

/// <summary>
/// Probe 0 - does the .NET 8 desktop runtime even start under this Wine prefix?
/// Pure console app: no UI, no graphics. If this FAILS, nothing else can work and the problem
/// is the runtime install (dotnetdesktop8) in the prefix, not Gum.
/// </summary>
internal static class Program
{
    private static int Main()
    {
        return ProbeLog.Run("Probe0.Runtime", () =>
        {
            ProbeLog.Step("Reporting .NET runtime + environment");
            ProbeLog.Info("FrameworkDescription", RuntimeInformation.FrameworkDescription);
            ProbeLog.Info("OSDescription", RuntimeInformation.OSDescription);
            ProbeLog.Info("OSArchitecture", RuntimeInformation.OSArchitecture.ToString());
            ProbeLog.Info("ProcessArchitecture", RuntimeInformation.ProcessArchitecture.ToString());
            ProbeLog.Info("Environment.Version", Environment.Version.ToString());
            ProbeLog.Info("Is64BitProcess", Environment.Is64BitProcess.ToString());
            ProbeLog.Info("ProcessorCount", Environment.ProcessorCount.ToString());
            ProbeLog.Info("BaseDirectory", AppContext.BaseDirectory);
            ProbeLog.Step("The .NET 8 desktop runtime starts under this Wine prefix.");
        });
    }
}
