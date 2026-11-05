using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering.ManualRenderTarget;

/// <summary>
/// A <see cref="RenderTargetRefreshMode.Manual"/> container must draw the same pixels as the same container in
/// <see cref="RenderTargetRefreshMode.EveryFrame"/> mode, with and without a render-target effect, top-level and
/// nested, on straight and premultiplied alpha pipelines, at camera zoom 1 and 2 (#7624).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RenderTargetManualPixelEqualityTests : BaseTestClass
{
    public override void Dispose()
    {
        Renderer.UsePremultipliedAlpha = false;
        base.Dispose();
    }

    [Theory]
    [InlineData(false, false, false, 1f)]
    [InlineData(false, false, false, 2f)]
    [InlineData(false, false, true, 1f)]
    [InlineData(false, false, true, 2f)]
    [InlineData(false, true, false, 1f)]
    [InlineData(false, true, false, 2f)]
    [InlineData(false, true, true, 1f)]
    [InlineData(false, true, true, 2f)]
    [InlineData(true, false, false, 1f)]
    [InlineData(true, false, false, 2f)]
    [InlineData(true, false, true, 1f)]
    [InlineData(true, false, true, 2f)]
    [InlineData(true, true, false, 1f)]
    [InlineData(true, true, false, 2f)]
    [InlineData(true, true, true, 1f)]
    [InlineData(true, true, true, 2f)]
    public void ManualOutput_EqualsEveryFrameOutput(bool nested, bool withEffect, bool premultiplied, float zoom)
    {
        Renderer.UsePremultipliedAlpha = premultiplied;
        using ManualRenderTargetScene scene = new();
        scene.Renderer.Camera.Zoom = zoom;

        Color[] everyFrame = Render(scene, manual: false, nested, withEffect);
        Color[] manual = Render(scene, manual: true, nested, withEffect);

        ManualRenderTargetScene.CountNonBlack(manual).ShouldBeGreaterThan(200, "the scene draws something");
        ManualRenderTargetScene.ShouldBeIdentical(everyFrame, manual,
            $"nested {nested}, effect {withEffect}, premultiplied {premultiplied}, zoom {zoom}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ManualOutput_AtReducedAlpha_EqualsEveryFrameOutput_WithoutAnEffect(bool premultiplied)
    {
        Renderer.UsePremultipliedAlpha = premultiplied;
        using ManualRenderTargetScene scene = new();

        Color[] everyFrame = RenderWithAlpha(scene, manual: false);
        Color[] manual = RenderWithAlpha(scene, manual: true);

        ManualRenderTargetScene.ShouldBeIdentical(everyFrame, manual, $"premultiplied {premultiplied}, alpha 128");
    }

    [Fact]
    public void ManualOutput_ChangingAlphaAfterTheBake_NeedsNoRefresh()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime container = scene.Add(scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false));
        scene.DrawFrames(2);
        int bakes = scene.BakeCount;

        container.Alpha = 128;
        Color[] pixels = scene.CaptureFrame();

        scene.BakeCount.ShouldBe(bakes);
        scene.PixelAt(pixels, 28, 28).R.ShouldBeInRange((byte)120, (byte)136);
    }

    [Fact]
    public void ManualOutput_WithClipsChildrenInside_EqualsEveryFrameOutput()
    {
        using ManualRenderTargetScene scene = new();

        Color[] everyFrame = RenderClipped(scene, manual: false);
        Color[] manual = RenderClipped(scene, manual: true);

        ManualRenderTargetScene.CountNonBlack(manual).ShouldBeGreaterThan(100);
        ManualRenderTargetScene.ShouldBeIdentical(everyFrame, manual, "ClipsChildren inside the container");
    }

    [Fact]
    public void ManualOutput_OfATurnedContainer_EqualsEveryFrameOutput()
    {
        using ManualRenderTargetScene scene = new();

        Color[] everyFrame = RenderTurned(scene, manual: false);
        Color[] manual = RenderTurned(scene, manual: true);

        ManualRenderTargetScene.CountNonBlack(manual).ShouldBeGreaterThan(200);
        ManualRenderTargetScene.ShouldBeIdentical(everyFrame, manual, "container rotated 30 degrees");
    }

    [Fact]
    public void InnerManualInsideAnEveryFrameContainer_EqualsAllEveryFrame()
    {
        using ManualRenderTargetScene scene = new();

        Color[] allEveryFrame = RenderNestedModes(scene, outerManual: false, innerManual: false);
        int bakesBefore = scene.BakeCount;
        Color[] innerManual = RenderNestedModes(scene, outerManual: false, innerManual: true);

        ManualRenderTargetScene.ShouldBeIdentical(allEveryFrame, innerManual, "inner manual in outer every-frame");
        bakesBefore.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void InnerEveryFrameInsideAnOuterManualContainer_EqualsAllEveryFrame()
    {
        using ManualRenderTargetScene scene = new();

        Color[] allEveryFrame = RenderNestedModes(scene, outerManual: false, innerManual: false);
        Color[] outerManual = RenderNestedModes(scene, outerManual: true, innerManual: false);

        ManualRenderTargetScene.ShouldBeIdentical(allEveryFrame, outerManual, "inner every-frame in outer manual");
    }

    [Fact]
    public void BothManual_EqualsAllEveryFrame()
    {
        using ManualRenderTargetScene scene = new();

        Color[] allEveryFrame = RenderNestedModes(scene, outerManual: false, innerManual: false);
        Color[] bothManual = RenderNestedModes(scene, outerManual: true, innerManual: true);

        ManualRenderTargetScene.ShouldBeIdentical(allEveryFrame, bothManual, "both manual");
    }

    [Fact]
    public void InnerManualInsideEveryFrameContainer_BakesTheInnerOnce_WhileTheOuterBakesEveryFrame()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime outer = new() { X = 10, Y = 10, Width = 120, Height = 120, IsRenderTarget = true };
        scene.Add(outer);
        ContainerRuntime inner = scene.CreateContainer(20, 20, Color.Red, manual: true, withEffect: false);
        scene.Add(inner, outer);
        scene.DrawFrame();
        int before = scene.BakeCount;

        scene.DrawFrames(5);

        // Outer re-bakes each frame; the inner is only drawn into the outer's bake.
        (scene.BakeCount - before).ShouldBe(5);
    }

    [Fact]
    public void OuterManualWithInnerEveryFrame_BakesBothOnFirstDrawAndNothingAfter_UntilRefreshed()
    {
        using ManualRenderTargetScene scene = new();
        ContainerRuntime outer = new()
        {
            X = 10, Y = 10, Width = 120, Height = 120, IsRenderTarget = true,
            RenderTargetRefreshMode = RenderTargetRefreshMode.Manual,
        };
        scene.Add(outer);
        ContainerRuntime inner = scene.CreateContainer(20, 20, Color.Red, manual: false, withEffect: false);
        scene.Add(inner, outer);
        int start = scene.BakeCount;

        scene.DrawFrames(5);
        int afterFirstFrames = scene.BakeCount - start;
        outer.RefreshRenderTarget();
        scene.DrawFrames(3);

        afterFirstFrames.ShouldBe(2);
        (scene.BakeCount - start).ShouldBe(4);
    }

    [Fact]
    public void SpriteReferencingAManualContainer_ShowsTheSamePixelsAsAnEveryFrameSource_AndKeepsItAlive()
    {
        using ManualRenderTargetScene scene = new();

        (Color[] everyFrame, _, _) = RenderSpriteView(scene, manual: false);
        (Color[] manual, int bakes, bool cached) = RenderSpriteView(scene, manual: true);

        ManualRenderTargetScene.CountNonBlack(manual).ShouldBeGreaterThan(20);
        ManualRenderTargetScene.ShouldBeIdentical(everyFrame, manual, "sprite reading a manual container");
        bakes.ShouldBe(1);
        cached.ShouldBeTrue("read through the sprite every frame");
    }

    private static (Color[] Pixels, int Bakes, bool Cached) RenderSpriteView(ManualRenderTargetScene scene, bool manual)
    {
        ContainerRuntime source = scene.CreateContainer(0, 0, Color.Red, manual, withEffect: false);
        source.Visible = false;
        scene.Add(source);

        SpriteRuntime viewer = new();
        viewer.X = 90;
        viewer.Y = 90;
        viewer.Width = 40;
        viewer.Height = 40;
        viewer.RenderTargetTextureSource = source;
        viewer.AddToManagers(scene.Managers, null);
        viewer.UpdateLayout();

        int before = scene.BakeCount;
        scene.DrawFrames(6);
        Color[] pixels = scene.CaptureFrame();
        int bakes = scene.BakeCount - before;
        bool cached = scene.HasCached(source);

        viewer.RemoveFromManagers();
        source.RemoveFromManagers();
        scene.DrawFrames(3);
        return (pixels, bakes, cached);
    }

    private static Color[] Render(ManualRenderTargetScene scene, bool manual, bool nested, bool withEffect)
    {
        ContainerRuntime container = scene.CreateContainer(20, 20, Color.Red, manual, withEffect);
        ContainerRuntime? parent = null;
        if (nested)
        {
            parent = new ContainerRuntime { X = 30, Y = 25, Width = 120, Height = 120 };
            scene.Add(parent);
            scene.Add(container, parent);
        }
        else
        {
            scene.Add(container);
        }

        scene.DrawFrames(3);
        Color[] pixels = scene.CaptureFrame();

        (parent ?? container).RemoveFromManagers();
        scene.DrawFrames(3);
        return pixels;
    }

    private static Color[] RenderWithAlpha(ManualRenderTargetScene scene, bool manual)
    {
        ContainerRuntime container = scene.CreateContainer(20, 20, Color.Red, manual, withEffect: false);
        container.Alpha = 128;
        scene.Add(container);
        scene.DrawFrames(3);
        Color[] pixels = scene.CaptureFrame();
        container.RemoveFromManagers();
        scene.DrawFrames(3);
        return pixels;
    }

    private static Color[] RenderClipped(ManualRenderTargetScene scene, bool manual)
    {
        ContainerRuntime container = scene.CreateContainer(20, 20, Color.Red, manual, withEffect: false);
        ContainerRuntime clipper = new() { X = 6, Y = 6, Width = 20, Height = 20, ClipsChildren = true };
        container.AddChild(clipper);
        clipper.AddChild(scene.CreateSprite(Color.Lime, -4, -4, 40, 40));
        scene.Add(container);
        scene.DrawFrames(3);
        Color[] pixels = scene.CaptureFrame();
        container.RemoveFromManagers();
        scene.DrawFrames(3);
        return pixels;
    }

    private static Color[] RenderTurned(ManualRenderTargetScene scene, bool manual)
    {
        ContainerRuntime container = scene.CreateContainer(60, 40, Color.Red, manual, withEffect: false);
        container.Rotation = 30;
        scene.Add(container);
        scene.DrawFrames(3);
        Color[] pixels = scene.CaptureFrame();
        container.RemoveFromManagers();
        scene.DrawFrames(3);
        return pixels;
    }

    private static Color[] RenderNestedModes(ManualRenderTargetScene scene, bool outerManual, bool innerManual)
    {
        ContainerRuntime outer = new()
        {
            X = 10, Y = 10, Width = 120, Height = 120, IsRenderTarget = true,
            RenderTargetRefreshMode = outerManual ? RenderTargetRefreshMode.Manual : RenderTargetRefreshMode.EveryFrame,
        };
        scene.Add(outer);
        outer.AddChild(scene.CreateSprite(Color.Lime, 0, 0, 120, 8));
        ContainerRuntime inner = scene.CreateContainer(20, 20, Color.Red, innerManual, withEffect: true);
        scene.Add(inner, outer);
        scene.DrawFrames(3);
        Color[] pixels = scene.CaptureFrame();
        outer.RemoveFromManagers();
        scene.DrawFrames(3);
        return pixels;
    }
}
