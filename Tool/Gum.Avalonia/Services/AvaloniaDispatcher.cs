using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using IDispatcher = Gum.Services.IDispatcher;

namespace Gum.Avalonia.Services;

/// <inheritdoc cref="IDispatcher"/>
public class AvaloniaDispatcher : IDispatcher
{
    private readonly Action<Action> _post;
    private readonly object _gate = new object();
    private List<Action>? _held;
    private int _holdDepth;

    /// <summary>Posts to Avalonia's UI thread at Background priority.</summary>
    public AvaloniaDispatcher()
        : this(action => Dispatcher.UIThread.Post(action, DispatcherPriority.Background))
    {
    }

    /// <param name="post">Queues an action on the UI thread; a test records it instead.</param>
    internal AvaloniaDispatcher(Action<Action> post)
    {
        _post = post;
    }

    /// <inheritdoc/>
    public void Invoke(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Invoke(action);
        }
    }

    /// <inheritdoc/>
    public void Post(Action action)
    {
        lock (_gate)
        {
            if (_held != null)
            {
                _held.Add(action);
                return;
            }
        }
        _post(action);
    }

    /// <summary>
    /// Keeps <see cref="Post"/>ed actions off the UI thread while <paramref name="work"/> runs, then
    /// posts them in order. Creating the graphics device pumps the UI message loop, which would
    /// otherwise run every queued pass (font warm-up, error checks) inside it and hold the canvas back.
    /// </summary>
    public async Task HoldPostsWhile(Func<Task> work)
    {
        lock (_gate)
        {
            // A nested hold shares the outer queue, which is released when the outermost hold ends.
            _holdDepth++;
            _held ??= new List<Action>();
        }
        try
        {
            await work();
        }
        finally
        {
            List<Action> held;
            lock (_gate)
            {
                _holdDepth--;
                if (_holdDepth > 0)
                {
                    held = new List<Action>();
                }
                else
                {
                    held = _held!;
                    _held = null;
                }
            }
            foreach (Action action in held)
            {
                _post(action);
            }
        }
    }
}
