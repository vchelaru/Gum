using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering.ManualRenderTarget;

/// <summary>
/// Cached containers and the scratch targets a manual refresh renders through must not outlive the containers or the
/// renderer (#7624).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RenderTargetCacheLeakTests : BaseTestClass
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThousandAddRemoveCycles_LeaveNoRenderTargetsBehind(bool withEffect)
    {
        using ManualRenderTargetScene scene = new();
        for (int i = 0; i < 1000; i++)
        {
            ContainerRuntime container = scene.CreateContainer(20 + (i % 5), 20, Color.Red, manual: true, withEffect);
            scene.Add(container);
            scene.DrawFrame();
            container.RemoveFromManagers();
        }

        scene.DrawFrames(3);

        scene.Renderer.AllocatedRenderTargetCount.ShouldBe(0);
    }

    [Fact]
    public void ManyContainersOfDifferentSizes_ShareScratchTargets_InsteadOfAllocatingOnePerContainer()
    {
        using ManualRenderTargetScene scene = new();
        for (int i = 0; i < 20; i++)
        {
            scene.Add(scene.CreateContainer(0, 0, Color.Red, manual: true, withEffect: true, width: 40, height: 40));
        }

        scene.DrawFrames(3);

        // One cached image per container, plus a handful of scratch targets for the shader pass, not one per container.
        scene.Renderer.AllocatedRenderTargetCount.ShouldBeLessThanOrEqualTo(20 + 2);
    }

    [Fact]
    public void ShuttingTheGumServiceDown_ReleasesCachedAndScratchTargets()
    {
        ManualRenderTargetScene scene = new();
        try
        {
            Renderer renderer = scene.Renderer;
            scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: true));
            scene.Add(scene.CreateContainer(70, 20, Color.Red, manual: true, withEffect: false));
            scene.DrawFrames(3);
            renderer.AllocatedRenderTargetCount.ShouldBeGreaterThan(0);

            Gum.GumService.Default.Uninitialize();

            renderer.AllocatedRenderTargetCount.ShouldBe(0);
        }
        finally
        {
            scene.Dispose();
        }
    }
}
