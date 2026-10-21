using System;

namespace MonoGameGum.Tests;

/// <summary>
/// Case counts for the large differential sweeps. GitHub Actions and <c>GUM_FULL_SWEEPS=1</c> run every
/// case; any other run uses a tenth of them (at least 20), which keeps a local run of the class in seconds
/// while CI stays the full regression check. Use it for a sweep that compares two engine paths or checks an
/// invariant; a golden-file comparison must keep its full count.
/// </summary>
internal static class SweepScale
{
    private static readonly bool _runFull =
        Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ||
        Environment.GetEnvironmentVariable("GUM_FULL_SWEEPS") == "1";

    public static int Cases(int full) => _runFull ? full : Math.Max(20, full / 10);
}
