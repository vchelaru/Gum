using System;

namespace MonoGameGum.Tests;

/// <summary>
/// Case counts for the large differential sweeps. GitHub Actions and <c>GUM_FULL_SWEEPS=1</c> run every
/// case; any other run uses a tenth of them, which keeps a local run of the class in seconds while CI stays
/// the full regression check. Use <see cref="Cases(int)"/> for a sweep over random seeds and
/// <see cref="Includes(int)"/> for an exhaustive enumeration (every mask, order and edit); a golden-file
/// comparison must keep its full count. Both read the same environment variable.
/// </summary>
internal static class SweepScale
{
    /// <summary>Prime, so a stride through nested loops of two-way and power-of-two choices still visits every combination of them.</summary>
    private const int SampleStride = 11;

    private static readonly bool _runFull =
        Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ||
        Environment.GetEnvironmentVariable("GUM_FULL_SWEEPS") == "1";

    /// <summary>The number of random cases to run: <paramref name="full"/> in CI, a tenth (at least 20) otherwise.</summary>
    public static int Cases(int full) => Cases(full, _runFull);

    /// <summary>Whether the case at <paramref name="caseIndex"/> of an enumeration runs: every one in CI, about a tenth otherwise.</summary>
    public static bool Includes(int caseIndex) => Includes(caseIndex, _runFull);

    internal static int Cases(int full, bool runFull) => runFull ? full : Math.Max(20, full / 10);

    internal static bool Includes(int caseIndex, bool runFull) => runFull || caseIndex % SampleStride == 0;
}
