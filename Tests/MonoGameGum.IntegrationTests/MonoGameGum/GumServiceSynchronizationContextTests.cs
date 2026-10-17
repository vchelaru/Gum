using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Each service that called UseSingleThreadedAsync gets its own SynchronizationContext made current for
/// the length of its Update and Draw, so an await inside one service's handler resumes inside that
/// service's Update under that service's default. No real threads: continuations drain on the test thread.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class GumServiceSynchronizationContextTests : BaseTestClass
{
    private static readonly GameTime FrameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60.0));

    private readonly string _root;
    private readonly SynchronizationContext? _originalSync = SynchronizationContext.Current;

    public GumServiceSynchronizationContextTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumSyncContext_" + Guid.NewGuid().ToString("N"));
    }

    public override void Dispose()
    {
        SynchronizationContext.SetSynchronizationContext(_originalSync);
        base.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private string WriteProject(string name)
    {
        GumProjectFolder folder = new GumProjectFolder(Path.Combine(_root, name));
        folder.Add(new GumElement("MainScreen", isScreen: true));
        return folder.Write();
    }

    private CollisionGame CreateTwoServices(bool aUsesAsync, bool bUsesAsync, bool bCallsUseLast = true)
    {
        CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        if (bCallsUseLast)
        {
            if (aUsesAsync) { game.ServiceA.UseSingleThreadedAsync(); }
            if (bUsesAsync) { game.ServiceB.UseSingleThreadedAsync(); }
        }
        else
        {
            if (bUsesAsync) { game.ServiceB.UseSingleThreadedAsync(); }
            if (aUsesAsync) { game.ServiceA.UseSingleThreadedAsync(); }
        }
        return game;
    }

    private static string Name(CollisionGame game, IGumService? service) =>
        ReferenceEquals(service, game.ServiceA) ? "A" : ReferenceEquals(service, game.ServiceB) ? "B" : "other";

    // Starts an await in a handler fired from `starter`'s Update and reports where it resumed.
    private static List<string> RunAwaitFrom(CollisionGame game, Gum.GumService starter, Gum.GumService other)
    {
        List<string> log = new();
        starter.DeferredQueue.Enqueue(async () =>
        {
            await Task.Yield();
            log.Add("resumed under " + Name(game, IGumService.Default) + " during " + (game.Updating ?? "no update"));
        });

        game.Updating = Name(game, starter);
        starter.Update(FrameTime);
        game.Updating = Name(game, other);
        other.Update(FrameTime);
        game.Updating = Name(game, starter);
        starter.Update(FrameTime);
        game.Updating = null;
        return log;
    }

    [Fact]
    public void AwaitInAsHandler_ShouldResumeInsideAUpdateUnderA_EvenThoughBCalledUseSingleThreadedAsyncLast()
    {
        using CollisionGame game = CreateTwoServices(aUsesAsync: true, bUsesAsync: true);

        List<string> log = RunAwaitFrom(game, game.ServiceA, game.ServiceB);

        log.ShouldBe(new[] { "resumed under A during A" });
    }

    [Fact]
    public void AwaitInBsHandler_ShouldResumeInsideBUpdateUnderB_EvenThoughACalledUseSingleThreadedAsyncLast()
    {
        using CollisionGame game = CreateTwoServices(aUsesAsync: true, bUsesAsync: true, bCallsUseLast: false);

        List<string> log = RunAwaitFrom(game, game.ServiceB, game.ServiceA);

        log.ShouldBe(new[] { "resumed under B during B" });
    }

    [Fact]
    public void Update_OnAServiceWithoutASynchronizationContext_ShouldLeaveTheCurrentOneUntouched()
    {
        using CollisionGame game = CreateTwoServices(aUsesAsync: false, bUsesAsync: false);
        SynchronizationContext marker = new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(marker);
        SynchronizationContext? seenInside = null;
        game.ServiceA.DeferredQueue.Enqueue(() => seenInside = SynchronizationContext.Current);

        game.ServiceA.Update(FrameTime);

        seenInside.ShouldBeSameAs(marker);
        SynchronizationContext.Current.ShouldBeSameAs(marker);
    }

    [Fact]
    public void Update_ShouldPutThePreviousSynchronizationContextBack()
    {
        using CollisionGame game = CreateTwoServices(aUsesAsync: true, bUsesAsync: false);
        SynchronizationContext marker = new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(marker);
        SynchronizationContext? seenInside = null;
        game.ServiceA.DeferredQueue.Enqueue(() => seenInside = SynchronizationContext.Current);

        game.ServiceA.Update(FrameTime);

        seenInside.ShouldBeSameAs(game.ServiceA.SynchronizationContext);
        SynchronizationContext.Current.ShouldBeSameAs(marker);
    }

    [Fact]
    public void Update_ShouldPutThePreviousSynchronizationContextBack_WhenItThrows()
    {
        using CollisionGame game = CreateTwoServices(aUsesAsync: true, bUsesAsync: true);
        SynchronizationContext marker = new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(marker);

        Should.Throw<InvalidOperationException>(() => game.ServiceA.Update(FrameTime, ThrowingRoots()));

        SynchronizationContext.Current.ShouldBeSameAs(marker);
    }

    private static IEnumerable<GraphicalUiElement> ThrowingRoots()
    {
        throw new InvalidOperationException("roots failed");
#pragma warning disable CS0162 // iterator needs a yield
        yield break;
#pragma warning restore CS0162
    }
}
