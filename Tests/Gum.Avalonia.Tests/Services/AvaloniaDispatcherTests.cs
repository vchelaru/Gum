using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gum.Avalonia.Services;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Gum.Avalonia.Tests.Services;

public class AvaloniaDispatcherTests
{
    [Fact]
    public void HoldPostsWhile_PostsDuringTheWork_ReachTheUiThreadAfterItInOrder()
    {
        List<Action> posted = new List<Action>();
        AvaloniaDispatcher dispatcher = new AvaloniaDispatcher(posted.Add);
        List<string> ran = new List<string>();
        TaskCompletionSource workDone = new TaskCompletionSource();
        int postedWhileWorking = -1;

        Task held = dispatcher.HoldPostsWhile(async () =>
        {
            dispatcher.Post(() => ran.Add("first"));
            dispatcher.Post(() => ran.Add("second"));
            postedWhileWorking = posted.Count;
            await workDone.Task;
        });
        workDone.SetResult();
        held.IsCompletedSuccessfully.ShouldBeTrue();

        postedWhileWorking.ShouldBe(0);
        posted.Count.ShouldBe(2);
        posted.ForEach(action => action());
        ran.ShouldBe(new[] { "first", "second" });
    }

    [Fact]
    public void HoldPostsWhile_Nested_ReleasesEveryPostInOrderWhenTheOuterHoldEnds()
    {
        List<Action> posted = new List<Action>();
        AvaloniaDispatcher dispatcher = new AvaloniaDispatcher(posted.Add);
        List<string> ran = new List<string>();
        int postedAfterInner = -1;

        Task held = dispatcher.HoldPostsWhile(async () =>
        {
            dispatcher.Post(() => ran.Add("outer before"));
            await dispatcher.HoldPostsWhile(() =>
            {
                dispatcher.Post(() => ran.Add("inner"));
                return Task.CompletedTask;
            });
            postedAfterInner = posted.Count;
            dispatcher.Post(() => ran.Add("outer after"));
        });
        held.IsCompletedSuccessfully.ShouldBeTrue();

        postedAfterInner.ShouldBe(0);
        posted.ForEach(action => action());
        ran.ShouldBe(new[] { "outer before", "inner", "outer after" });
    }

    [Fact]
    public void HoldPostsWhile_WorkThrows_StillReleasesThePosts()
    {
        List<Action> posted = new List<Action>();
        AvaloniaDispatcher dispatcher = new AvaloniaDispatcher(posted.Add);

        Task held = dispatcher.HoldPostsWhile(() =>
        {
            dispatcher.Post(() => { });
            throw new InvalidOperationException("device failed");
        });

        held.IsFaulted.ShouldBeTrue();
        posted.Count.ShouldBe(1);
        dispatcher.Post(() => { });
        posted.Count.ShouldBe(2);
    }

    [Fact]
    public void HeadContainer_IDispatcher_IsTheDispatcherStartupHolds()
    {
        // Startup holds the concrete dispatcher; the passes post through IDispatcher.
        TestAppBuilder.Services.GetRequiredService<IDispatcher>()
            .ShouldBeSameAs(TestAppBuilder.Services.GetRequiredService<AvaloniaDispatcher>());
    }

    [Fact]
    public void Post_NoHold_PostsImmediately()
    {
        List<Action> posted = new List<Action>();
        AvaloniaDispatcher dispatcher = new AvaloniaDispatcher(posted.Add);

        dispatcher.Post(() => { });

        posted.Count.ShouldBe(1);
    }
}
