using System;
using System.IO;
using System.Threading;
using Gum.Avalonia.Diagnostics.Windows;
using Gum.Diagnostics;

namespace Gum.Avalonia.Diagnostics;

/// <summary>
/// Watches the UI thread for the permanent, debugger-independent freeze reported in issue #4781
/// (renaming or creating a state never shows its popup). A UI-thread timer calls
/// <see cref="Heartbeat"/> roughly once a second; a background thread polls for a missed heartbeat
/// and, on the first one, captures a minidump plus the last recorded dialog steps, so the next
/// occurrence is diagnosable without a debugger already attached.
///
/// Windows only, since the reported freeze and minidump capture are both Windows-specific;
/// <see cref="Heartbeat"/> and <see cref="RecordStep"/> are harmless no-ops elsewhere. Not covered
/// by unit tests: it is a thin thread/timer/OS-call wrapper around the tested pure pieces
/// (<see cref="UiHeartbeatStallDetector"/>, <see cref="RecentEventsBuffer"/>), the same split
/// <c>StartupTiming</c>/<c>StartupTimingLog</c> uses.
/// </summary>
public static class UiFreezeWatchdog
{
    private const string DisableVariable = "GUM_DISABLE_FREEZE_WATCHDOG";
    private static readonly TimeSpan StallThreshold = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private static readonly RecentEventsBuffer _recentSteps = new RecentEventsBuffer(capacity: 20);
    private static readonly object _detectorLock = new object();
    private static UiHeartbeatStallDetector? _detector;
    private static string? _diagnosticsDirectory;
    private static bool _started;

    /// <summary>
    /// Starts the background poll thread. Call once, after the main window has opened. No-op off
    /// Windows, if called twice, or when GUM_DISABLE_FREEZE_WATCHDOG=1 is set (e.g. for CI/headless
    /// runs that don't want a dump attempt from an unattended, possibly slow, startup).
    /// </summary>
    public static void Start(string diagnosticsDirectory)
    {
        if (_started || !OperatingSystem.IsWindows() || Environment.GetEnvironmentVariable(DisableVariable) == "1")
        {
            return;
        }

        _started = true;
        _diagnosticsDirectory = diagnosticsDirectory;
        UiFreezeWatchdogHook.Register(suspend: Suspend, resume: Resume);
        Thread pollThread = new Thread(PollLoop) { IsBackground = true, Name = "Gum UI freeze watchdog" };
        pollThread.Start();
    }

    /// <summary>
    /// Called from the UI thread to prove it is still responsive. The first call establishes the
    /// stall-detection baseline instead of measuring from <see cref="Start"/>, so a slow-but-normal
    /// startup before the first heartbeat can never itself read as a stall.
    /// </summary>
    public static void Heartbeat()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        lock (_detectorLock)
        {
            if (_detector == null)
            {
                _detector = new UiHeartbeatStallDetector(StallThreshold, now);
                return;
            }
        }

        _detector.Heartbeat(now);
    }

    /// <summary>Records a step for the trailing-events log a stall capture writes alongside the dump.</summary>
    public static void RecordStep(string step) =>
        _recentSteps.Add($"{DateTimeOffset.Now:HH:mm:ss.fff}  {step}");

    /// <summary>
    /// Suppresses stall detection - used via <see cref="UiFreezeWatchdogHook"/> for a known-long,
    /// synchronous UI-thread operation (project load) that would otherwise starve the heartbeat and
    /// read as a false freeze. No-op if no heartbeat has happened yet (nothing to suspend).
    /// </summary>
    private static void Suspend()
    {
        UiHeartbeatStallDetector? detector;
        lock (_detectorLock)
        {
            detector = _detector;
        }

        detector?.Suspend(DateTimeOffset.UtcNow);
    }

    /// <summary>Ends a suspension started by <see cref="Suspend"/>. See <see cref="UiFreezeWatchdogHook"/>.</summary>
    private static void Resume()
    {
        UiHeartbeatStallDetector? detector;
        lock (_detectorLock)
        {
            detector = _detector;
        }

        detector?.Resume(DateTimeOffset.UtcNow);
    }

    private static void PollLoop()
    {
        while (true)
        {
            Thread.Sleep(PollInterval);

            UiHeartbeatStallDetector? detector;
            lock (_detectorLock)
            {
                detector = _detector;
            }

            if (detector != null && detector.HasNewlyStalled(DateTimeOffset.UtcNow))
            {
                CaptureStall();
            }
        }
    }

    private static void CaptureStall()
    {
        try
        {
            Directory.CreateDirectory(_diagnosticsDirectory!);
            string stamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string notePath = Path.Combine(_diagnosticsDirectory!, $"freeze-{stamp}.txt");
            string dumpPath = Path.Combine(_diagnosticsDirectory!, $"freeze-{stamp}.dmp");

            File.WriteAllLines(notePath, _recentSteps.Snapshot());
            MiniDumpWriter.TryWrite(dumpPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Diagnostics must never take the app down further than it already is.
        }
    }
}
