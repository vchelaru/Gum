using Gum;
using Shouldly;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace StrideGum.Tests;

/// <summary>
/// Pins <see cref="GumService.AddSceneRenderer"/>: how Gum.Stride puts its renderer into the
/// compositor without depending on Stride.CommunityToolkit (issue #6149). The existing top-level
/// renderer must be kept and Gum's drawn after it, so the UI draws over the scene.
/// </summary>
public class GumSceneRendererRegistrationTests
{
    private sealed class StubRenderer : SceneRendererBase
    {
        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext) { }
    }

    [Fact]
    public void AddSceneRenderer_WrapsExistingRenderer_WhenTopLevelIsNotACollection()
    {
        StubRenderer existing = new StubRenderer();
        StubRenderer gum = new StubRenderer();
        GraphicsCompositor compositor = new GraphicsCompositor { Game = existing };

        GumService.AddSceneRenderer(compositor, gum);

        SceneRendererCollection collection = compositor.Game.ShouldBeOfType<SceneRendererCollection>();
        collection.Children.Count.ShouldBe(2);
        collection.Children[0].ShouldBeSameAs(existing);
        collection.Children[1].ShouldBeSameAs(gum);
    }

    [Fact]
    public void AddSceneRenderer_AppendsToExistingCollection_WhenTopLevelIsACollection()
    {
        StubRenderer existing = new StubRenderer();
        StubRenderer gum = new StubRenderer();
        SceneRendererCollection collection = new SceneRendererCollection();
        collection.Children.Add(existing);
        GraphicsCompositor compositor = new GraphicsCompositor { Game = collection };

        GumService.AddSceneRenderer(compositor, gum);

        compositor.Game.ShouldBeSameAs(collection);
        collection.Children.Count.ShouldBe(2);
        collection.Children[0].ShouldBeSameAs(existing);
        collection.Children[1].ShouldBeSameAs(gum);
    }

    [Fact]
    public void AddSceneRenderer_CalledTwice_KeepsBothRenderersInOrder()
    {
        StubRenderer existing = new StubRenderer();
        StubRenderer first = new StubRenderer();
        StubRenderer second = new StubRenderer();
        GraphicsCompositor compositor = new GraphicsCompositor { Game = existing };

        GumService.AddSceneRenderer(compositor, first);
        GumService.AddSceneRenderer(compositor, second);

        SceneRendererCollection collection = compositor.Game.ShouldBeOfType<SceneRendererCollection>();
        collection.Children.Count.ShouldBe(3);
        collection.Children[1].ShouldBeSameAs(first);
        collection.Children[2].ShouldBeSameAs(second);
    }
}
