using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Threading;

public class DeferredActionQueue
{
    private readonly Queue<Action> _actions = new();
    private readonly object _lock = new();

    public void Enqueue(Action action)
    {
        lock (_lock)
        {
            _actions.Enqueue(action);
        }
    }

    public void ProcessPending()
    {
        Queue<Action> toProcess;

        lock (_lock)
        {
            if (_actions.Count == 0) return;
            toProcess = new Queue<Action>(_actions);
            _actions.Clear();
        }

        while (toProcess.Count > 0)
        {
            try
            {
                toProcess.Dequeue()();
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine($"Deferred action failed: {ex}");
            }
        }
    }
}