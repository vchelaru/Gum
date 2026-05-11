using System.Collections.Generic;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Unit tests for <see cref="BatchOrchestrator"/> — the BatchKey transition state machine.
/// Uses a recording stub of <see cref="IRenderable"/> so the rules can be asserted without
/// a MonoGame device.
/// <para>
/// Method names follow the pattern Given_State_Then_Behavior to keep the contract readable
/// from a test runner. They're also alphabetized within the class per the repo convention.
/// </para>
/// </summary>
public class BatchOrchestratorTests : BaseTestClass
{
    /// <summary>
    /// Stub renderable that records its <c>StartBatch</c> / <c>EndBatch</c> calls into a
    /// shared event log. Each test uses one shared list so we can assert the cross-renderable
    /// ordering — the thing that ultimately determines GPU paint order.
    /// </summary>
    private sealed class RecordingRenderable : IRenderable
    {
        private readonly List<string> _events;
        public string Name { get; }
        public string BatchKey { get; }

        public RecordingRenderable(string name, string batchKey, List<string> events)
        {
            Name = name;
            BatchKey = batchKey;
            _events = events;
        }

        public Gum.BlendState BlendState => Gum.BlendState.NonPremultiplied;
        public bool Wrap => false;
        public void Render(ISystemManagers managers) => _events.Add($"Render:{Name}");
        public void PreRender() { }
        public void StartBatch(ISystemManagers managers) => _events.Add($"Start:{Name}({BatchKey})");
        public void EndBatch(ISystemManagers managers) => _events.Add($"End:{Name}({BatchKey})");
    }

    private static (BatchOrchestrator orchestrator, List<string> events) Setup()
    {
        return (new BatchOrchestrator(), new List<string>());
    }

    [Fact]
    public void EmptyBatchKey_DoesNotEndPreviousBatch()
    {
        // A renderable with empty BatchKey (e.g. a plain ContainerRuntime wrapper) must not
        // flush the current batch — it participates in whatever batch the previous renderable
        // established. Flushing here would split the in-progress batch on every wrapper.
        var (o, events) = Setup();
        var shape = new RecordingRenderable("Sh", "Apos.Shapes", events);
        var container = new RecordingRenderable("C", "", events);

        o.OnRenderable(shape, null!);
        events.Clear();
        o.OnRenderable(container, null!);

        events.ShouldBeEmpty();
        o.LastBatchOwner.ShouldBe(shape);
        o.CurrentBatchKey.ShouldBe("Apos.Shapes");
    }

    [Fact]
    public void FirstRenderable_WithBatchKey_StartsBatchWithoutEndingAnything()
    {
        var (o, events) = Setup();
        var sprite = new RecordingRenderable("S1", "SpriteBatch", events);

        o.OnRenderable(sprite, null!);

        events.ShouldBe(new[] { "Start:S1(SpriteBatch)" });
        o.LastBatchOwner.ShouldBe(sprite);
        o.CurrentBatchKey.ShouldBe("SpriteBatch");
    }

    [Fact]
    public void FlushAndReset_OnFreshOrchestrator_IsNoOp()
    {
        var (o, events) = Setup();

        o.FlushAndReset(null!);

        events.ShouldBeEmpty();
        o.LastBatchOwner.ShouldBeNull();
        o.CurrentBatchKey.ShouldBe("");
    }

    [Fact]
    public void FlushAndReset_OnPendingBatch_EndsItAndClearsState()
    {
        // End-of-walk and clip-restore both call FlushAndReset to ensure no batch leaks
        // past the boundary. Critical for cross-cycle correctness in the GumBatch path.
        var (o, events) = Setup();
        var shape = new RecordingRenderable("Sh", "Apos.Shapes", events);
        o.OnRenderable(shape, null!);
        events.Clear();

        o.FlushAndReset(null!);

        events.ShouldBe(new[] { "End:Sh(Apos.Shapes)" });
        o.LastBatchOwner.ShouldBeNull();
        o.CurrentBatchKey.ShouldBe("");
    }

    [Fact]
    public void SameBatchKeyTwice_DoesNotEndOrRestartBatch()
    {
        // Two consecutive sprites must queue into the same SpriteBatch. Re-firing the
        // transition would force a flush mid-stream and lose performance — and could
        // cause out-of-order paint if a third renderable's queue interleaves.
        var (o, events) = Setup();
        var s1 = new RecordingRenderable("S1", "SpriteBatch", events);
        var s2 = new RecordingRenderable("S2", "SpriteBatch", events);

        o.OnRenderable(s1, null!);
        events.Clear();
        o.OnRenderable(s2, null!);

        events.ShouldBeEmpty();
        o.LastBatchOwner.ShouldBe(s1);
    }

    [Fact]
    public void ShapeAfterSprite_EndsSpriteBeforeStartingShape()
    {
        // The core invariant. End must happen BEFORE Start — otherwise the previous
        // batch's queue isn't flushed and ends up draw-ordered after the new batch.
        var (o, events) = Setup();
        var sprite = new RecordingRenderable("S", "SpriteBatch", events);
        var shape = new RecordingRenderable("Sh", "Apos.Shapes", events);

        o.OnRenderable(sprite, null!);
        o.OnRenderable(shape, null!);

        events.ShouldBe(new[]
        {
            "Start:S(SpriteBatch)",
            "End:S(SpriteBatch)",
            "Start:Sh(Apos.Shapes)",
        });
        o.LastBatchOwner.ShouldBe(shape);
        o.CurrentBatchKey.ShouldBe("Apos.Shapes");
    }

    [Fact]
    public void ShapeWrapperShapeContainerSandwich_FlushesBetweenIslands()
    {
        // The bug we just fixed in the wild: a sequence shape → sprite → container(empty) →
        // shape — the empty-key container must not interrupt the sprite batch, and the
        // following shape must end the sprite batch before queueing.
        var (o, events) = Setup();
        var first = new RecordingRenderable("F", "Apos.Shapes", events);
        var sprite = new RecordingRenderable("S", "SpriteBatch", events);
        var container = new RecordingRenderable("C", "", events);
        var second = new RecordingRenderable("Sh2", "Apos.Shapes", events);

        o.OnRenderable(first, null!);
        o.OnRenderable(sprite, null!);
        o.OnRenderable(container, null!);
        o.OnRenderable(second, null!);

        events.ShouldBe(new[]
        {
            "Start:F(Apos.Shapes)",
            "End:F(Apos.Shapes)",
            "Start:S(SpriteBatch)",
            // container(empty) is a no-op — sprite batch stays open
            "End:S(SpriteBatch)",
            "Start:Sh2(Apos.Shapes)",
        });
    }

    [Fact]
    public void ShapeAfterFlush_FiresFreshStartBatch_EvenWhenKeyMatchesPreFlushKey()
    {
        // The contract Renderer.AdjustRenderStates relies on when a mid-walk scissor change
        // happens while an Apos.Shapes batch is open: FlushAndReset ends the open batch and
        // clears _currentBatchKey, so the next renderable — even one with the same key as
        // before the flush — fires a fresh StartBatch and picks up the new scissor state.
        //
        // Without this contract, a shape sibling of a clip container (e.g. a scrollbar
        // thumb rendered before the clip) leaves the orchestrator in state
        // currentBatchKey="Apos.Shapes" + ShapeBatch begun. When AdjustRenderStates changes
        // the SpriteBatch scissor, the first shape descendant inside the clip has key
        // "Apos.Shapes" == current → OnRenderable is a no-op → its draw queues into the
        // stale-scissor ShapeBatch and bleeds past the clip region.
        var (o, events) = Setup();
        var outside = new RecordingRenderable("Outside", "Apos.Shapes", events);
        var inside = new RecordingRenderable("Inside", "Apos.Shapes", events);

        o.OnRenderable(outside, null!);
        o.FlushAndReset(null!);
        o.OnRenderable(inside, null!);

        events.ShouldBe(new[]
        {
            "Start:Outside(Apos.Shapes)",
            "End:Outside(Apos.Shapes)",
            "Start:Inside(Apos.Shapes)",
        });
        o.LastBatchOwner.ShouldBe(inside);
        o.CurrentBatchKey.ShouldBe("Apos.Shapes");
    }

    [Fact]
    public void SpriteAfterShape_EndsShapeBeforeStartingSprite()
    {
        var (o, events) = Setup();
        var shape = new RecordingRenderable("Sh", "Apos.Shapes", events);
        var sprite = new RecordingRenderable("S", "SpriteBatch", events);

        o.OnRenderable(shape, null!);
        o.OnRenderable(sprite, null!);

        events.ShouldBe(new[]
        {
            "Start:Sh(Apos.Shapes)",
            "End:Sh(Apos.Shapes)",
            "Start:S(SpriteBatch)",
        });
    }
}
