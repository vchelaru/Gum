using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering.ManualRenderTarget;

/// <summary>
/// A cached render-target container is kept alive by being drawn, not by baking (#7624). A frame in which it is not
/// drawn (culled, invisible, hidden ancestor, removed) lets the next frame-boundary sweep release the cache; it
/// re-bakes the next time it is drawn. The sweep runs at the start of a frame, so the release shows after two frames.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RenderTargetCacheEvictionTests : BaseTestClass
{
    [Fact]
    public void ContainerThatStaysDrawn_IsNeverEvicted_AndNeverRebakes()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        int before = scene.BakeCount;

        for (int i = 0; i < 20; i++)
        {
            scene.DrawFrame();
            scene.HasCached(container).ShouldBeTrue($"frame {i}");
        }

        (scene.BakeCount - before).ShouldBe(1);
    }

    [Fact]
    public void InvisibleContainer_IsEvicted()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(3);

        container.Visible = false;
        scene.DrawFrames(2);

        scene.HasCached(container).ShouldBeFalse();
    }

    [Fact]
    public void ContainerRemovedFromTheScene_IsEvicted()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(3);

        container.RemoveFromManagers();
        scene.DrawFrames(2);

        scene.HasCached(container).ShouldBeFalse();
    }

    [Fact]
    public void ContainerRemovedFromItsParent_IsEvicted()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime parent = scene.Add(new ContainerRuntime { Width = 150, Height = 150 });
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false), parent);
        scene.DrawFrames(3);

        parent.RemoveChild(container);
        scene.DrawFrames(2);

        scene.HasCached(container).ShouldBeFalse();
    }

    [Fact]
    public void ContainerUnderAHiddenAncestor_IsEvicted()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime parent = scene.Add(new ContainerRuntime { Width = 150, Height = 150 });
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false), parent);
        scene.DrawFrames(3);

        parent.Visible = false;
        scene.DrawFrames(2);

        scene.HasCached(container).ShouldBeFalse();
    }

    [Fact]
    public void ContainerMovedOutsideTheCamera_IsEvicted_AndRebakesWhenItComesBack()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: true));
        scene.DrawFrames(3);
        Color[] before = scene.CaptureFrame();
        int bakes = scene.BakeCount;

        container.X = 1000;
        container.UpdateLayout();
        scene.DrawFrames(2);
        scene.HasCached(container).ShouldBeFalse();
        scene.BakeCount.ShouldBe(bakes);

        container.X = 20;
        container.UpdateLayout();
        Color[] after = scene.CaptureFrame();

        (scene.BakeCount - bakes).ShouldBe(1);
        scene.HasCached(container).ShouldBeTrue();
        ManualRenderTargetScene.ShouldBeIdentical(before, after, "re-entering the camera");
    }

    [Fact]
    public void ContainerScrolledOutOfAClippingParent_IsEvicted_AndRebakesOnReentry()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime viewport = scene.Add(new ContainerRuntime { X = 10, Y = 10, Width = 100, Height = 60, ClipsChildren = true });
        ContainerRuntime item = scene.Add(scene.CreateContainer(10, 10, Color.Red, manual: true, withEffect: false), viewport);
        scene.DrawFrames(3);
        Color[] before = scene.CaptureFrame();
        int bakes = scene.BakeCount;

        item.Y = 100;
        item.UpdateLayout();
        scene.DrawFrames(3);

        scene.HasCached(item).ShouldBeFalse("scrolled out of the clip, so not drawn");
        scene.BakeCount.ShouldBe(bakes, "an item that is not drawn does not bake either");

        item.Y = 10;
        item.UpdateLayout();
        Color[] after = scene.CaptureFrame();

        (scene.BakeCount - bakes).ShouldBe(1);
        ManualRenderTargetScene.ShouldBeIdentical(before, after, "scrolled back in");
    }

    [Fact]
    public void EvictedThenDrawnContainer_ShowsTheSamePixelsAsBefore()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: true));
        scene.DrawFrames(3);
        Color[] before = scene.CaptureFrame();

        container.Visible = false;
        scene.DrawFrames(3);
        scene.HasCached(container).ShouldBeFalse();
        container.Visible = true;
        Color[] after = scene.CaptureFrame();

        ManualRenderTargetScene.ShouldBeIdentical(before, after, "evicted, then shown again");
    }

    [Fact]
    public void ContainerHiddenForJustOneFrame_LosesItsCache()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(3);
        int bakes = scene.BakeCount;

        container.Visible = false;
        scene.DrawFrame();
        container.Visible = true;
        scene.DrawFrames(2);

        (scene.BakeCount - bakes).ShouldBe(1);
    }

    [Fact]
    public void CacheTargets_AreReleased_WhenTheSceneEmpties()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: true));
        scene.DrawFrames(3);
        scene.Renderer.AllocatedRenderTargetCount.ShouldBeGreaterThan(0);

        container.RemoveFromManagers();
        scene.DrawFrames(3);

        scene.Renderer.AllocatedRenderTargetCount.ShouldBe(0);
    }
}
