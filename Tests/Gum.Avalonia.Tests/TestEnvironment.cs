using System.Runtime.InteropServices;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Gates for tests that need a real display and GL driver. A CI runner is assumed not to have one
/// unless the job opts in with the named variable: KNI's device creation fails without GL, and
/// the half-built device's finalizer then crashes the test host, so the capability cannot be
/// probed by trying.
/// </summary>
internal static class TestEnvironment
{
    private static bool IsCi => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));

    private static bool HasDisplay =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

    /// <summary>True when a display-backed test may run: opted in on CI, or off CI with a display.</summary>
    public static bool CanUseDisplay(string optInVariable) =>
        Environment.GetEnvironmentVariable(optInVariable) == "1" || (!IsCi && HasDisplay);

    /// <summary>
    /// True when a test may create the graphics device inside the test host. Never on macOS:
    /// AppKit requires the process main thread, which xunit owns, and the refusal is an uncaught
    /// NSException that aborts the whole run. A child process owns its main thread, so tests that
    /// launch one use <see cref="CanUseDisplay"/> instead.
    /// </summary>
    public static bool CanCreateDeviceInProcess(string optInVariable) =>
        !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && CanUseDisplay(optInVariable);
}
