using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Gum.Avalonia.Diagnostics;

/// <summary>
/// Waits until a window's UI has stopped changing, for an unattended screenshot (#8091). Some
/// controls (FluentIcon, for one) fill in their content from a Loaded handler, which Avalonia runs
/// as a dispatcher job after the layout pass that created them, and the content then needs a
/// layout and a render of its own. A capture that does not wait for that misses every icon.
/// </summary>
public static class UiSettle
{
    private static readonly TimeSpan FrameTimeout = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Completes after a round (dispatcher drained of every job above Background priority, then one
    /// rendered frame) ends with valid layout twice in a row, or after <paramref name="maxRounds"/>.
    /// Must be called on the UI thread.
    /// </summary>
    public static async Task SettleAsync(TopLevel topLevel, int maxRounds = 20)
    {
        int cleanRounds = 0;
        for (int round = 0; round < maxRounds && cleanRounds < 2; round++)
        {
            // Background is below Loaded, layout and render, so this runs after all of them.
            await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
            await NextFrameAsync(topLevel);

            bool layoutValid = topLevel is not Layoutable layoutable || (layoutable.IsMeasureValid && layoutable.IsArrangeValid);
            cleanRounds = layoutValid ? cleanRounds + 1 : 0;
        }
    }

    private static async Task NextFrameAsync(TopLevel topLevel)
    {
        TaskCompletionSource frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        topLevel.RequestAnimationFrame(_ => frame.TrySetResult());
        // A window that never presents (hidden, headless without a render timer) must not hang the run.
        await Task.WhenAny(frame.Task, Task.Delay(FrameTimeout));
    }
}
