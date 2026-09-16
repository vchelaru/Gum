using System.Collections.Generic;
using System.Linq;

namespace Gum.Diagnostics;

/// <summary>
/// Fixed-capacity, thread-safe ring buffer of recent diagnostic lines. Used to keep the last few
/// steps leading up to a freeze without the log growing unbounded.
/// </summary>
public sealed class RecentEventsBuffer
{
    private readonly int _capacity;
    private readonly Queue<string> _events;
    private readonly object _lock;

    public RecentEventsBuffer(int capacity)
    {
        _capacity = capacity;
        _events = new Queue<string>(capacity);
        _lock = new object();
    }

    public void Add(string entry)
    {
        lock (_lock)
        {
            _events.Enqueue(entry);
            while (_events.Count > _capacity)
            {
                _events.Dequeue();
            }
        }
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_lock)
        {
            return _events.ToArray();
        }
    }
}
