using Gum.Threading;
using Shouldly;
using System;
using System.Collections.Generic;

namespace MonoGameGum.Tests.V2;

/// <summary>
/// Tests for <see cref="DeferredActionQueue.Clear"/> to verify that after clearing,
/// previously enqueued actions do not execute when the queue is processed.
/// </summary>
public class DeferredActionQueueTests
{
    [Fact]
    public void Clear_PreventsEnqueuedActionsFromExecuting()
    {
        DeferredActionQueue queue = new DeferredActionQueue();
        int callCount = 0;
        queue.Enqueue(() => callCount++);
        queue.Enqueue(() => callCount++);
        queue.Enqueue(() => callCount++);

        queue.Clear();
        queue.ProcessPending();

        callCount.ShouldBe(0);
    }

    [Fact]
    public void Clear_OnEmptyQueue_DoesNotThrow()
    {
        DeferredActionQueue queue = new DeferredActionQueue();

        Should.NotThrow(() => queue.Clear());
    }

    [Fact]
    public void ProcessPending_AfterClear_AllowsNewActionsEnqueuedAfterClear()
    {
        DeferredActionQueue queue = new DeferredActionQueue();
        int preClearCallCount = 0;
        int postClearCallCount = 0;

        queue.Enqueue(() => preClearCallCount++);
        queue.Clear();
        queue.Enqueue(() => postClearCallCount++);
        queue.ProcessPending();

        preClearCallCount.ShouldBe(0);
        postClearCallCount.ShouldBe(1);
    }

    [Fact]
    public void ProcessPending_WithNoItems_DoesNotThrow()
    {
        DeferredActionQueue queue = new DeferredActionQueue();

        Should.NotThrow(() => queue.ProcessPending());
    }

    [Fact]
    public void ProcessPending_ExecutesActionsInOrder()
    {
        DeferredActionQueue queue = new DeferredActionQueue();
        List<int> executionOrder = new List<int>();

        queue.Enqueue(() => executionOrder.Add(1));
        queue.Enqueue(() => executionOrder.Add(2));
        queue.Enqueue(() => executionOrder.Add(3));

        queue.ProcessPending();

        executionOrder.ShouldBe(new[] { 1, 2, 3 });
    }
}
