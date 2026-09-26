using System;
using System.Globalization;
using System.Threading;

namespace Gum.Avalonia.Diagnostics;

/// <summary>
/// A hard deadline for an unattended (<c>--exit-after</c>) run. The exit timer is a dispatcher
/// timer, so it never runs while the UI thread is blocked (issue #5138: the orphan code scan
/// walking a huge code root on project load, #5140). This deadline runs on a thread-pool timer
/// and ends the process if it is still alive, so an unattended run always exits.
/// </summary>
public sealed class UnattendedExitDeadline : IDisposable
{
    /// <summary>How long past <c>--exit-after</c> (counted from launch) the run may take to start up and shut down.</summary>
    public static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(60);

    private readonly Timer _timer;

    private UnattendedExitDeadline(TimeSpan deadline, Action<string> terminate)
    {
        string message = string.Format(
            CultureInfo.InvariantCulture,
            "Gum did not exit within {0:0.#} s of launch (--exit-after plus {1:0} s); the UI thread may be blocked. Forcing exit.",
            deadline.TotalSeconds,
            GracePeriod.TotalSeconds);
        _timer = new Timer(_ => terminate(message), null, deadline, Timeout.InfiniteTimeSpan);
    }

    /// <summary>The deadline, counted from launch, for a run given <c>--exit-after <paramref name="exitAfterSeconds"/></c>.</summary>
    public static TimeSpan For(double exitAfterSeconds) => TimeSpan.FromSeconds(exitAfterSeconds) + GracePeriod;

    /// <summary>
    /// Calls <paramref name="terminate"/> on a thread-pool thread, with a message for stderr, once
    /// <paramref name="deadline"/> passes, unless disposed first.
    /// </summary>
    public static UnattendedExitDeadline Start(TimeSpan deadline, Action<string> terminate) =>
        new UnattendedExitDeadline(deadline, terminate);

    /// <inheritdoc/>
    public void Dispose() => _timer.Dispose();
}
