using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;

namespace MonoGameGum.TestsCommon;

/// <summary>
/// Records the runtime's own events (method JIT and ready-to-run entry, type loads, exceptions, contention,
/// GCs) with their thread and time, so an allocation test that fails on one machine can say what the runtime was
/// doing inside the allocating call (#8190). Dispatch to the listener is asynchronous, so call
/// <see cref="Describe"/> a moment after the window being explained.
/// </summary>
public sealed class RuntimeEventRecorder : EventListener
{
    // Loader | Jit | NGen | Exception | Contention | GC | CompilationDiagnostic | TypeDiagnostic.
    const long Keywords = 0x8 | 0x10 | 0x20 | 0x8000 | 0x4000 | 0x1 | 0x2000000000 | 0x8000000000;
    const int MaxEvents = 500_000;

    readonly object _lock = new();
    readonly List<RecordedEvent> _events = new();

    readonly record struct RecordedEvent(DateTime Time, long ThreadId, string Text);

    /// <summary>The operating system id of the calling thread, or -1 where it is not known.</summary>
    public static long CurrentOsThreadId() => OperatingSystem.IsWindows() ? GetCurrentThreadId() : -1;

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "Microsoft-Windows-DotNETRuntime")
        {
            EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)Keywords);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        string text = eventData.EventName ?? $"event {eventData.EventId}";
        if (eventData.Payload != null && eventData.PayloadNames != null)
        {
            for (int i = 0; i < eventData.Payload.Count && i < eventData.PayloadNames.Count; i++)
            {
                string name = eventData.PayloadNames[i];
                if (name is "MethodNamespace" or "MethodName" or "TypeName" or "ExceptionType" or "ExceptionMessage"
                    or "MethodSignature" or "Reason" or "Count" or "Depth")
                {
                    text += $" {name}={eventData.Payload[i]}";
                }
            }
        }
        lock (_lock)
        {
            if (_events.Count < MaxEvents)
            {
                _events.Add(new RecordedEvent(eventData.TimeStamp, eventData.OSThreadId, text));
            }
        }
    }

    /// <summary>
    /// The events inside [from, to] (UTC) on every thread, each tagged with whether it ran on the thread whose
    /// <see cref="CurrentOsThreadId"/> is <paramref name="testThreadId"/>. Waits first for queued events to arrive.
    /// </summary>
    public string Describe(DateTime from, DateTime to, long testThreadId, int maxEvents = 60)
    {
        // Events reach the listener late and in bursts: wait until none has arrived for half a second (at most 8 s).
        int seen = -1;
        for (int waited = 0; waited < 8000; waited += 500)
        {
            Thread.Sleep(500);
            int count;
            lock (_lock)
            {
                count = _events.Count;
            }
            if (count == seen)
            {
                break;
            }
            seen = count;
        }
        List<RecordedEvent> inRange;
        lock (_lock)
        {
            inRange = _events.Where(e => e.Time >= from && e.Time <= to).OrderBy(e => e.Time).ToList();
        }
        string lines = string.Join(" | ", inRange.Take(maxEvents).Select(e =>
            $"{(e.ThreadId == testThreadId ? "TEST" : "other " + e.ThreadId)} +{(e.Time - from).TotalMilliseconds:F1}ms {e.Text}"));
        return $"{inRange.Count} runtime events in the window ({_events.Count} recorded in all): {lines}";
    }
}
