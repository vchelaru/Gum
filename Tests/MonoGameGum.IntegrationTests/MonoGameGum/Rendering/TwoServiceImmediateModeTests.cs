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
        SystemManagers.Default.ShouldBeSameAs(second.SystemManagers, "the later service owns the default");
        return (first, second);
    }

    [Fact]
    public void ImmediateDraw_OnFirstService_RendersWithItsOwnManagers()
    {
        using ServiceTestGame game = new();
        (GumService first, _) = InitializeTwoServices(game);
        RecordingRenderable renderable = new();

        first.Renderer.Begin();
        first.Renderer.Draw(renderable);
        first.Renderer.End();

        ShouldAllBeSameAs(renderable.RenderedWith, first.SystemManagers, "Render managers");
        ShouldAllBeSameAs(renderable.BatchStartedWith, first.SystemManagers, "StartBatch managers");
        ShouldAllBeSameAs(renderable.BatchEndedWith, first.SystemManagers, "EndBatch managers");
    }

    [Fact]
    public void DeferredDraw_OnFirstService_RendersWithItsOwnManagersAtEnd()
    {
        using ServiceTestGame game = new();
        (GumService first, _) = InitializeTwoServices(game);
        RecordingRenderable renderable = new();

        first.Renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
        first.Renderer.Draw(renderable);
        renderable.RenderedWith.Count.ShouldBe(0, "a deferred draw waits for End");
        first.Renderer.End();

        ShouldAllBeSameAs(renderable.RenderedWith, first.SystemManagers, "Render managers");
        ShouldAllBeSameAs(renderable.BatchStartedWith, first.SystemManagers, "StartBatch managers");
        ShouldAllBeSameAs(renderable.BatchEndedWith, first.SystemManagers, "EndBatch managers");
    }

    [Fact]
    public void LayeredDraw_WithNullManagers_OnFirstService_RendersWithItsOwnManagers()
    {
        using ServiceTestGame game = new();
        (GumService first, _) = InitializeTwoServices(game);
        RecordingRenderable renderable = new();
        first.Renderer.MainLayer.Add(renderable);

        first.Renderer.Draw((SystemManagers)null!);

        first.Renderer.MainLayer.Remove(renderable);
        ShouldAllBeSameAs(renderable.RenderedWith, first.SystemManagers, "Render managers");
        ShouldAllBeSameAs(renderable.BatchEndedWith, first.SystemManagers, "EndBatch managers");
    }

    [Fact]
    public void GumBatch_WithExplicitManagers_RendersWithThoseManagersAndLeavesTheDefaultAlone()
    {
        using ServiceTestGame game = new();
        (GumService first, GumService second) = InitializeTwoServices(game);
        second.SystemManagers.Renderer.Camera.ClientWidth = 12345;
        RecordingRenderable renderable = new();
        GumBatch batch = new(first.SystemManagers);

        batch.Begin();
        batch.Draw(renderable);
        batch.End();

        ShouldAllBeSameAs(renderable.RenderedWith, first.SystemManagers, "Render managers");
        ShouldAllBeSameAs(renderable.BatchEndedWith, first.SystemManagers, "EndBatch managers");
        batch.SpriteBatch.ShouldBeSameAs(first.SystemManagers.Renderer.SpriteRenderer.SpriteBatch);
        first.SystemManagers.Renderer.Camera.ClientWidth.ShouldBe(game.GraphicsDevice.Viewport.Width,
            "Begin sizes the camera of the managers the batch was created with");
        second.SystemManagers.Renderer.Camera.ClientWidth.ShouldBe(12345,
            "the default service's camera is not this batch's to touch");
    }

    [Fact]
    public void GumBatch_WithoutManagers_UsesTheDefaultManagers()
    {
        using ServiceTestGame game = new();
        (_, GumService second) = InitializeTwoServices(game);
        RecordingRenderable renderable = new();
        GumBatch batch = new();

        batch.Begin();
        batch.Draw(renderable);
        batch.End();

        ShouldAllBeSameAs(renderable.RenderedWith, second.SystemManagers, "Render managers");
    }

    [Fact]
    public void GumBatch_WithNullManagers_Throws()
    {
        Should.Throw<System.InvalidOperationException>(() => new GumBatch((SystemManagers)null!));
    }
}
