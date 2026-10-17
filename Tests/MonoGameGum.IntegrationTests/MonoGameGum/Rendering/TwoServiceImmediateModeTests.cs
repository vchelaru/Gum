using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

// The enclosing MonoGameGum namespace holds an obsolete GumService shim; bind to the real type.
using GumService = global::Gum.GumService;

/// <summary>
/// With two <see cref="GumService"/> instances alive, <c>SystemManagers.Default</c> is the one that
/// initialized last. Every renderer entry point other than the layered <c>Draw</c> (immediate mode
/// <c>Begin</c>/<c>Draw</c>/<c>End</c>, the deferred flush, <see cref="GumBatch"/>) must still hand the
/// renderables the managers of the renderer that was asked to draw, not that default (#6066).
/// Each test records the <see cref="ISystemManagers"/> a renderable is given instead of reading pixels,
/// because the managers are the thing under test and a pixel can look right through either one.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class TwoServiceImmediateModeTests : BaseTestClass
{
    private sealed class RecordingRenderable : InvisibleRenderable
    {
        public List<ISystemManagers> RenderedWith { get; } = new();
        public List<ISystemManagers> BatchStartedWith { get; } = new();
        public List<ISystemManagers> BatchEndedWith { get; } = new();

        public RecordingRenderable()
        {
            Width = 10;
            Height = 10;
        }

        // A non-empty batch key makes the renderer announce this renderable to its batch
        // orchestrator, which is the path that reads managers in Begin/Draw/End.
        public override string BatchKey => "recording";

        public override void Render(ISystemManagers managers) => RenderedWith.Add(managers);
        public override void StartBatch(ISystemManagers systemManagers) => BatchStartedWith.Add(systemManagers);
        public override void EndBatch(ISystemManagers systemManagers) => BatchEndedWith.Add(systemManagers);
    }

    private static void ShouldAllBeSameAs(List<ISystemManagers> recorded, SystemManagers expected, string what)
    {
        recorded.Count.ShouldBeGreaterThan(0, what + " was never called");
        foreach (ISystemManagers managers in recorded)
        {
            managers.ShouldBeSameAs(expected, what);
        }
    }

    private static (GumService first, GumService second) InitializeTwoServices(ServiceTestGame game)
    {
        game.Start();
        GumService first = game.CreateService();
        first.Initialize(game, Gum.Forms.DefaultVisualsVersion.V3);
        GumService second = game.CreateService();
        second.Initialize(game, Gum.Forms.DefaultVisualsVersion.V3);
        SystemManagers.Default.ShouldBeSameAs(first.SystemManagers, "the first service keeps the default");
        return (first, second);
    }

    [Fact]
    public void ImmediateDraw_OnNonDefaultService_RendersWithItsOwnManagers()
    {
        using ServiceTestGame game = new();
        (_, GumService second) = InitializeTwoServices(game);
        RecordingRenderable renderable = new();

        second.Renderer.Begin();
        second.Renderer.Draw(renderable);
        second.Renderer.End();

        ShouldAllBeSameAs(renderable.RenderedWith, second.SystemManagers, "Render managers");
        ShouldAllBeSameAs(renderable.BatchStartedWith, second.SystemManagers, "StartBatch managers");
        ShouldAllBeSameAs(renderable.BatchEndedWith, second.SystemManagers, "EndBatch managers");
    }

    [Fact]
    public void DeferredDraw_OnNonDefaultService_RendersWithItsOwnManagersAtEnd()
    {
        using ServiceTestGame game = new();
        (_, GumService second) = InitializeTwoServices(game);
        RecordingRenderable renderable = new();

        second.Renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
        second.Renderer.Draw(renderable);
        renderable.RenderedWith.Count.ShouldBe(0, "a deferred draw waits for End");
        second.Renderer.End();

        ShouldAllBeSameAs(renderable.RenderedWith, second.SystemManagers, "Render managers");
        ShouldAllBeSameAs(renderable.BatchStartedWith, second.SystemManagers, "StartBatch managers");
        ShouldAllBeSameAs(renderable.BatchEndedWith, second.SystemManagers, "EndBatch managers");
    }

    [Fact]
    public void LayeredDraw_WithNullManagers_OnNonDefaultService_RendersWithItsOwnManagers()
    {
        using ServiceTestGame game = new();
        (_, GumService second) = InitializeTwoServices(game);
        RecordingRenderable renderable = new();
        second.Renderer.MainLayer.Add(renderable);

        second.Renderer.Draw((SystemManagers)null!);

        second.Renderer.MainLayer.Remove(renderable);
        ShouldAllBeSameAs(renderable.RenderedWith, second.SystemManagers, "Render managers");
        ShouldAllBeSameAs(renderable.BatchEndedWith, second.SystemManagers, "EndBatch managers");
    }

    [Fact]
    public void GumBatch_WithExplicitManagers_RendersWithThoseManagersAndLeavesTheDefaultAlone()
    {
        using ServiceTestGame game = new();
        (GumService defaultService, GumService other) = InitializeTwoServices(game);
        defaultService.SystemManagers.Renderer.Camera.ClientWidth = 12345;
        RecordingRenderable renderable = new();
        GumBatch batch = new(other.SystemManagers);

        batch.Begin();
        batch.Draw(renderable);
        batch.End();

        ShouldAllBeSameAs(renderable.RenderedWith, other.SystemManagers, "Render managers");
        ShouldAllBeSameAs(renderable.BatchEndedWith, other.SystemManagers, "EndBatch managers");
        batch.SpriteBatch.ShouldBeSameAs(other.SystemManagers.Renderer.SpriteRenderer.SpriteBatch);
        other.SystemManagers.Renderer.Camera.ClientWidth.ShouldBe(game.GraphicsDevice.Viewport.Width,
            "Begin sizes the camera of the managers the batch was created with");
        defaultService.SystemManagers.Renderer.Camera.ClientWidth.ShouldBe(12345,
            "the default service's camera is not this batch's to touch");
    }

    [Fact]
    public void ServiceDraw_OnServiceThatWasNeverInitialized_FallsBackToTheDefaultManagers()
    {
        // Hosts that set SystemManagers.Default by hand and never call Initialize on the service
        // still call Draw on it; that must keep drawing the default managers.
        using ServiceTestGame game = new();
        (GumService first, _) = InitializeTwoServices(game);
        RecordingRenderable renderable = new();
        first.Renderer.MainLayer.Add(renderable);
        GumService neverInitialized = new();

        neverInitialized.Draw();

        first.Renderer.MainLayer.Remove(renderable);
        ShouldAllBeSameAs(renderable.RenderedWith, first.SystemManagers, "Render managers");
    }

    [Fact]
    public void GumBatch_WithoutManagers_UsesTheDefaultManagers()
    {
        using ServiceTestGame game = new();
        (GumService first, _) = InitializeTwoServices(game);
        RecordingRenderable renderable = new();
        GumBatch batch = new();

        batch.Begin();
        batch.Draw(renderable);
        batch.End();

        ShouldAllBeSameAs(renderable.RenderedWith, first.SystemManagers, "Render managers");
    }

    [Fact]
    public void GumBatch_WithNullManagers_Throws()
    {
        Should.Throw<System.InvalidOperationException>(() => new GumBatch((SystemManagers)null!));
    }
}
