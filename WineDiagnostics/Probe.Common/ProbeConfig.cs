using System;

namespace Probe.Common;

/// <summary>Environment-driven configuration shared by the windowed probes.</summary>
public static class ProbeConfig
{
    /// <summary>
    /// Number of seconds a windowed probe keeps its window open before auto-closing, so the suite
    /// can run unattended. Override with the <c>PROBE_HOLD_SECONDS</c> environment variable
    /// (default 2). Set it higher when you want to visually inspect the windows under Wine.
    /// </summary>
    public static int HoldSeconds()
    {
        string? raw = Environment.GetEnvironmentVariable("PROBE_HOLD_SECONDS");
        if (int.TryParse(raw, out int seconds) && seconds >= 0)
        {
            return seconds;
        }
        return 2;
    }
}
