using System;

namespace Gum.Diagnostics;

/// <summary>
/// Pure stall detection: given heartbeat timestamps and a threshold, decides whether the UI thread
/// has gone quiet longer than expected. Holds no thread, timer, or IO of its own - a watchdog wires
/// this to a periodic poll and whatever it does when a stall is detected (e.g. capture a dump).
/// </summary>
public sealed class UiHeartbeatStallDetector
{
    private readonly TimeSpan _stallThreshold;
    private readonly object _lock;
    private DateTimeOffset _lastHeartbeat;
    private bool _hasFiredForCurrentStall;

    public UiHeartbeatStallDetector(TimeSpan stallThreshold, DateTimeOffset now)
    {
        _stallThreshold = stallThreshold;
        _lastHeartbeat = now;
        _lock = new object();
    }

    /// <summary>Records that the watched thread is responsive as of <paramref name="now"/>.</summary>
    public void Heartbeat(DateTimeOffset now)
    {
        lock (_lock)
        {
            _lastHeartbeat = now;
            _hasFiredForCurrentStall = false;
        }
    }

    /// <summary>
    /// True the first time <paramref name="now"/> exceeds the stall threshold since the last
    /// heartbeat; false on every subsequent call until a new heartbeat resets it. This makes a
    /// stall report exactly once per stall, not once per poll.
    /// </summary>
    public bool HasNewlyStalled(DateTimeOffset now)
    {
        lock (_lock)
        {
            if (_hasFiredForCurrentStall || now - _lastHeartbeat < _stallThreshold)
            {
                return false;
            }

            _hasFiredForCurrentStall = true;
            return true;
        }
    }
}
