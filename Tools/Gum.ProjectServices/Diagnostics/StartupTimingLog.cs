using System;
using System.Collections.Generic;

namespace Gum.Diagnostics;

/// <summary>
/// Formats and sequences startup timing entries. Holds no IO or clock of its own - the sink and the
/// elapsed-time source are injected, which is what makes it testable. <see cref="StartupTiming"/> is
/// the process-wide facade that wires it to a file and a <c>Stopwatch</c>.
/// </summary>
public sealed class StartupTimingLog
{
    private readonly Action<string>? _sink;
    private readonly Func<long> _elapsedMillisecondsProvider;
    private readonly HashSet<string> _alreadyMarkedLabels;
    private readonly object _lock;
    private long _lastMarkMilliseconds;

    /// <summary>
    /// Whether entries are recorded. Callers only need this to skip work done solely to produce a
    /// label; every logging method is safe to call on a disabled log.
    /// </summary>
    public bool IsEnabled => _sink != null;

    /// <param name="sink">Receives each formatted line, or null to disable logging entirely.</param>
    /// <param name="elapsedMillisecondsProvider">Milliseconds elapsed since logging began.</param>
    public StartupTimingLog(Action<string>? sink, Func<long> elapsedMillisecondsProvider)
    {
        _sink = sink;
        _elapsedMillisecondsProvider = elapsedMillisecondsProvider;
        _alreadyMarkedLabels = new HashSet<string>();
        _lock = new object();
        _lastMarkMilliseconds = 0;
    }

    /// <summary>
    /// Records a timestamped entry, including the time elapsed since the previous mark.
    /// </summary>
    public void Mark(string label)
    {
        if (_sink == null)
        {
            return;
        }

        long elapsed = _elapsedMillisecondsProvider();
        long delta;
        lock (_lock)
        {
            delta = elapsed - _lastMarkMilliseconds;
            _lastMarkMilliseconds = elapsed;
        }

        _sink($"{elapsed,8} ms  (+{delta,6} ms)  {label}");
    }

    /// <summary>
    /// Records a mark the first time it is reached for the given label, ignoring later calls. Used
    /// for events that fire repeatedly but whose first occurrence is the startup milestone.
    /// </summary>
    public void MarkOnce(string label)
    {
        if (_sink == null)
        {
            return;
        }

        lock (_lock)
        {
            if (!_alreadyMarkedLabels.Add(label))
            {
                return;
            }
        }

        Mark(label);
    }

    /// <summary>
    /// Records a free-form line, without a delta. For context a duration alone doesn't convey
    /// (counts, file names).
    /// </summary>
    public void Log(string message)
    {
        _sink?.Invoke($"{_elapsedMillisecondsProvider(),8} ms  {message}");
    }

    /// <summary>
    /// Records how long the <c>using</c> scope took when it is disposed. Scopes may be nested;
    /// indent the label to show nesting, and always wrap the outermost step of a sequence so the
    /// sub-step durations can be checked against the total they are meant to account for.
    /// </summary>
    public IDisposable Time(string label) => new Scope(this, label);

    private sealed class Scope : IDisposable
    {
        private readonly StartupTimingLog _owner;
        private readonly string _label;
        private readonly long _startMilliseconds;

        public Scope(StartupTimingLog owner, string label)
        {
            _owner = owner;
            _label = label;
            _startMilliseconds = owner._elapsedMillisecondsProvider();
        }

        public void Dispose()
        {
            if (_owner._sink == null)
            {
                return;
            }

            long elapsed = _owner._elapsedMillisecondsProvider();
            lock (_owner._lock)
            {
                _owner._lastMarkMilliseconds = elapsed;
            }

            _owner._sink($"{elapsed,8} ms  (+{elapsed - _startMilliseconds,6} ms)  {_label}");
        }
    }
}
