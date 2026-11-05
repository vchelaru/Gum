using System.Linq;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering.ManualRenderTarget;

/// <summary>
/// A <see cref="RenderTargetRefreshMode.Manual"/> container bakes once and re-bakes only when told to, when its
/// size or the camera zoom changes, or after it lost its cache (#7624). Bakes are counted through the renderer.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RenderTargetManualRefreshTests : BaseTestClass
{
    [Fact]
    public void RefreshMode_DefaultsToEveryFrame()
    {
        ContainerRuntime container = new();

        container.RenderTargetRefreshMode.ShouldBe(RenderTargetRefreshMode.EveryFrame);
    }

    [Fact]
    public void EveryFrameContainer_BakesEveryFrame()
    {
        using ManualRenderTargetScene scene = new();
        scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: false, withEffect: false));

        int before = scene.BakeCount;
        scene.DrawFrames(5);

        (scene.BakeCount - before).ShouldBe(5);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ManualContainer_BakesOnce_AcrossManyDrawnFrames(bool withEffect)
    {
        using ManualRenderTargetScene scene = new();
        scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect));

        int before = scene.BakeCount;
        scene.DrawFrames(30);

        (scene.BakeCount - before).ShouldBe(1);
    }

    [Fact]
    public void ManualContainer_StillShowsContent_AfterTheFirstBake()
    {
        using ManualRenderTargetScene scene = new();
        scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(3);

        Color[] pixels = scene.CaptureFrame();

        scene.PixelAt(pixels, 30, 30).R.ShouldBe((byte)255);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefreshRenderTarget_RebakesExactlyOnce_OnTheNextDraw(bool withEffect)
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect));
        scene.DrawFrames(3);
        int before = scene.BakeCount;

        container.RefreshRenderTarget();
        scene.DrawFrames(5);

        (scene.BakeCount - before).ShouldBe(1);
    }

    [Fact]
    public void RefreshRenderTarget_CalledTwiceBeforeADraw_RebakesOnce()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(2);
        int before = scene.BakeCount;

        container.RefreshRenderTarget();
        container.RefreshRenderTarget();
        scene.DrawFrames(3);

        (scene.BakeCount - before).ShouldBe(1);
    }

    [Fact]
    public void RefreshRenderTarget_PicksUpAChangedChild_AndDropsTheOldImage()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(2);
        SpriteRuntime first = (SpriteRuntime)container.Children[0];

        first.Texture = scene.SolidTexture(Color.Lime);
        container.RefreshRenderTarget();
        Color[] pixels = scene.CaptureFrame();

        Color center = scene.PixelAt(pixels, 28, 28);
        center.G.ShouldBe((byte)255);
        center.R.ShouldBe((byte)0);
    }

    [Fact]
    public void ChangedChild_IsNotReflected_UntilRefreshRenderTargetIsCalled()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(2);
        SpriteRuntime first = (SpriteRuntime)container.Children[0];

        first.Texture = scene.SolidTexture(Color.Lime);
        Color[] withoutRefresh = scene.CaptureFrame();

        Color center = scene.PixelAt(withoutRefresh, 28, 28);
        center.R.ShouldBe((byte)255);
        center.G.ShouldBe((byte)0);
    }

    [Fact]
    public void ChangedEffectParameter_IsNotReflected_UntilRefreshRenderTargetIsCalled()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: true));
        scene.DrawFrames(2);
        Color[] before = scene.CaptureFrame();

        container.SetShaderParameter("OutlineRed", 0f);
        container.SetShaderParameter("OutlineGreen", 0f);
        container.SetShaderParameter("OutlineBlue", 255f);
        Color[] withoutRefresh = scene.CaptureFrame();
        container.RefreshRenderTarget();
        Color[] afterRefresh = scene.CaptureFrame();

        ManualRenderTargetScene.ShouldBeIdentical(before, withoutRefresh, "no refresh, same image");
        afterRefresh.SequenceEqual(before).ShouldBeFalse("the refresh picked up the new outline color");
    }

    [Fact]
    public void SizeChange_RebakesOnce_AndTheImageFillsTheNewSize()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(3);
        int before = scene.BakeCount;

        container.Width = 70;
        container.Height = 70;
        container.UpdateLayout();
        scene.DrawFrames(4);

        (scene.BakeCount - before).ShouldBe(1);
    }

    [Fact]
    public void CameraZoomChange_RebakesOnce()
    {
        using ManualRenderTargetScene scene = new();
        scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(3);
        int before = scene.BakeCount;

        scene.Renderer.Camera.Zoom = 2;
        scene.DrawFrames(4);

        (scene.BakeCount - before).ShouldBe(1);
    }

    [Fact]
    public void MovingTheContainer_DoesNotRebake_AndDrawsAtTheNewPosition()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(3);
        int before = scene.BakeCount;

        container.X = 80;
        container.UpdateLayout();
        Color[] pixels = scene.CaptureFrame();

        scene.BakeCount.ShouldBe(before);
        scene.PixelAt(pixels, 90, 30).R.ShouldBe((byte)255);
        scene.PixelAt(pixels, 30, 30).R.ShouldBe((byte)0);
    }

    [Fact]
    public void MovedManualContainer_LooksLikeAnEveryFrameContainerAtTheSamePosition()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime manual = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: true));
        scene.DrawFrames(2);
        manual.X = 75;
        manual.Y = 33;
        manual.UpdateLayout();
        Color[] moved = scene.CaptureFrame();
        manual.RemoveFromManagers();

        scene.Add(scene.CreateContainer(75, 33, Color.Red, manual: false, withEffect: true));
        Color[] reference = scene.CaptureFrame();

        ManualRenderTargetScene.ShouldBeIdentical(reference, moved, "moved manual container vs fresh every-frame container");
    }

    [Fact]
    public void ContainerStartingHalfOffScreen_ShowsCompleteContent_WhenScrolledIn()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime manual = scene.Add(scene.CreateContainer(-20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(3);

        manual.X = 40;
        manual.UpdateLayout();
        Color[] scrolled = scene.CaptureFrame();
        manual.RemoveFromManagers();

        scene.Add(scene.CreateContainer(40, 20, Color.Red, manual: false, withEffect: false));
        Color[] reference = scene.CaptureFrame();

        ManualRenderTargetScene.ShouldBeIdentical(reference, scrolled, "scrolled in from half off-screen");
    }

    [Fact]
    public void ContainerScrolledHalfOffScreenAndBack_ShowsCompleteContent()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime manual = scene.Add(scene.CreateContainer(40, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(2);
        int bakes = scene.BakeCount;

        manual.X = -20;
        manual.UpdateLayout();
        scene.DrawFrames(2);
        manual.X = 40;
        manual.UpdateLayout();
        Color[] backAgain = scene.CaptureFrame();

        scene.BakeCount.ShouldBe(bakes);
        manual.RemoveFromManagers();
        scene.Add(scene.CreateContainer(40, 20, Color.Red, manual: false, withEffect: false));
        ManualRenderTargetScene.ShouldBeIdentical(scene.CaptureFrame(), backAgain, "scrolled out and back");
    }

    [Fact]
    public void SwitchingFromEveryFrameToManual_BakesOnTheNextDraw_ThenStops()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: false, withEffect: false));
        scene.DrawFrames(3);
        int before = scene.BakeCount;

        container.RenderTargetRefreshMode = RenderTargetRefreshMode.Manual;
        scene.DrawFrames(6);

        (scene.BakeCount - before).ShouldBe(1);
    }

    [Fact]
    public void SwitchingFromManualBackToEveryFrame_BakesEveryFrameAgain_AndShowsChangedChildren()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(3);
        SpriteRuntime first = (SpriteRuntime)container.Children[0];
        first.Texture = scene.SolidTexture(Color.Lime);
        int before = scene.BakeCount;

        container.RenderTargetRefreshMode = RenderTargetRefreshMode.EveryFrame;
        Color[] pixels = scene.CaptureFrame();
        scene.DrawFrames(3);

        (scene.BakeCount - before).ShouldBe(4);
        scene.PixelAt(pixels, 28, 28).G.ShouldBe((byte)255);
    }

    [Fact]
    public void SwitchingToEveryFrameAndBackToManual_DoesNotReuseTheStaleImage()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(2);
        SpriteRuntime first = (SpriteRuntime)container.Children[0];

        container.RenderTargetRefreshMode = RenderTargetRefreshMode.EveryFrame;
        scene.DrawFrames(2);
        first.Texture = scene.SolidTexture(Color.Lime);
        scene.DrawFrames(2);
        container.RenderTargetRefreshMode = RenderTargetRefreshMode.Manual;
        Color[] pixels = scene.CaptureFrame();

        scene.PixelAt(pixels, 28, 28).G.ShouldBe((byte)255);
    }

    [Theory]
    [InlineData(GraphicsProfile.Reach, 2048)]
    [InlineData(GraphicsProfile.HiDef, 4096)]
    public void TargetSizeLimit_FollowsTheGraphicsProfile(GraphicsProfile profile, int expected)
    {
        Renderer.GetMaxManualRenderTargetSize(profile).ShouldBe(expected);
    }

    [Fact]
    public void ContainerWiderThanTheTargetSizeLimit_FallsBackToEveryFrameInsteadOfFailing()
    {
        using ManualRenderTargetScene scene = new();
        scene.Add(scene.CreateContainer(0, 20, Color.Red, manual: true, withEffect: false, width: 20000, height: 40));
        int before = scene.BakeCount;

        scene.DrawFrames(3);

        (scene.BakeCount - before).ShouldBe(3);
    }
}
