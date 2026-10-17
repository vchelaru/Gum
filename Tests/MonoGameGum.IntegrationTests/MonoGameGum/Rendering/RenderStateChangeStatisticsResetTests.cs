using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Pins that <see cref="Renderer.Draw(SystemManagers)"/> clears
/// <see cref="Renderer.RenderStateChangeStatistics"/> at the start of every frame, so the
/// ShapeBatch begin count describes the just-completed frame rather than accumulating across
/// frames — mirroring how <c>SpriteRenderer.LastFrameDrawStates</c> is reset in the same method.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RenderStateChangeStatisticsResetTests : BaseTestClass
{
    [Fact]
    public void Draw_ResetsShapeBatchBeginCount()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        Renderer renderer = SystemManagers.Default.Renderer;
        renderer.RenderStateChangeStatistics.RecordShapeBatchBegin();
        renderer.RenderStateChangeStatistics.ShapeBatchBeginCount.ShouldBe(1);

        renderer.Draw(SystemManagers.Default);

        renderer.RenderStateChangeStatistics.ShapeBatchBeginCount.ShouldBe(0);
    }

    /// <summary>
    /// Minimal Game host that initializes a fresh <see cref="GumService"/> per test.
    /// We don't draw real content — we just need a live <see cref="SystemManagers.Default"/>
    /// whose <see cref="Renderer.Draw(SystemManagers)"/> can be invoked.
    /// </summary>
    private sealed class MinimalGame : SharedGameLease
    {
        public GumService GumService { get; } = new GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
