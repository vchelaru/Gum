using System;
using System.Collections.Generic;
using System.Threading;

namespace Gum.Async;

/// <summary>
/// A <see cref="SynchronizationContext"/> that funnels async continuations onto
/// the game's primary thread by queuing them and draining the queue once per
/// frame from <see cref="GumService.Update(Microsoft.Xna.Framework.GameTime)"/>.
/// </summary>
/// <remarks>
/// Install via <c>GumService.UseSingleThreadedAsync()</c>. After install,
/// <c>await</c> continuations in handlers (including
/// <c>await dialogBox.ShowAsync(...)</c>) resume on the primary thread, so it is
/// safe to mutate UI state after the await without thread-affinity surprises.
/// </remarks>
public class SingleThreadSynchronizationContext : SynchronizationContext
{
    readonly Queue<Action> _messagesToProcess = new();
    readonly Queue<Action> _thisFrameQueue = new();
    readonly object _syncHandle = new();

    public SingleThreadSynchronizationContext()
    {
        SetSynchronizationContext(this);
    }

    public override void Send(SendOrPostCallback codeToRun, object? state) =>
        throw new NotImplementedException();

    public override void Post(SendOrPostCallback codeToRun, object? state)
    {
        lock (_syncHandle)
        {
            _messagesToProcess.Enqueue(() =>
            {
                try { codeToRun(state!); }
                catch (System.Threading.Tasks.TaskCanceledException) { }
            });
            Monitor.Pulse(_syncHandle);
        }
    }

    /// <summary>
    /// Drains queued continuations onto the calling (primary) thread. Called
    /// once per frame from the runtime's <c>GumService.Update</c>.
    /// Continuations that post follow-up work see those follow-ups land on the
    /// next frame, not this one — this matches game-time-driven async (e.g.
    /// <c>await Task.Yield()</c> inside a typewriter loop).
    /// </summary>
    public void Update()
    {
        lock (_syncHandle)
        {
            while (_messagesToProcess.Count > 0)
            {
                _thisFrameQueue.Enqueue(_messagesToProcess.Dequeue());
            }
        }

        while (_thisFrameQueue.Count > 0)
        {
            try { _thisFrameQueue.Dequeue()(); }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                // Cancellation typically follows a screen change. Nothing to do.
            }
        }
    }

    /// <summary>
    /// Discards all queued continuations. Useful when transitioning between
    /// screens to drop in-flight async work that would otherwise resume against
    /// torn-down UI.
    /// </summary>
    public void Clear()
    {
        lock (_syncHandle)
        {
            _messagesToProcess.Clear();
        }
    }
}
