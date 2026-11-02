#nullable enable
using System;
using System.Diagnostics;
using System.Threading;

namespace Gum.Tests.Shared;

/// <summary>
/// Runs work that must not hang (a circular-reference regression, #6871) on a dedicated thread with a 30 s cap.
/// <c>Task.Run(...).Wait(5000)</c> reads a starved thread pool (earlier tests blocking pool threads) as a freeze;
/// a dedicated thread does not, and the timeout message says whether the work started, how long it ran and the
/// pool state, so a real freeze and a delayed start tell apart (#7421). A worker exception is rethrown.
/// </summary>
public static class FreezeGuard
{
    public static void Run(Action work, string frozenMessage)
    {
        bool started = false;
        Exception? failure = null;
        Stopwatch stopwatch = Stopwatch.StartNew();
        Thread worker = new Thread(() =>
        {
            started = true;
            try
            {
                work();
            }
            catch (Exception e)
            {
                failure = e;
            }
        });
        worker.IsBackground = true;
        worker.Start();
        if (!worker.Join(30000))
        {
            throw new TimeoutException(
                $"{frozenMessage}: work started={started}, elapsed={stopwatch.ElapsedMilliseconds} ms, " +
                $"thread pool threads={ThreadPool.ThreadCount}, pending work items={ThreadPool.PendingWorkItemCount}");
        }

        if (failure != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
