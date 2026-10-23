using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Skia port of MonoGame's immediate-mode GumBatch coverage (Begin/End rules, matrix routing,
/// camera, several cycles per frame), asserting on pixels of a raster surface.
/// </summary>
public class GumBatchTests
{
    public GumBatchTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void Begin_Twice_Throws()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        GumBatch gumBatch = new();
        gumBatch.Begin();

        Should.Throw<InvalidOperationException>(() => gumBatch.Begin());
        Should.Throw<InvalidOperationException>(() => gumBatch.Begin(surface.Canvas));
        gumBatch.End();
    }

    [Fact]
    public void DrawDrawStringAndEnd_WithoutBegin_Throw()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        GumBatch gumBatch = new();

        Should.Throw<InvalidOperationException>(() => gumBatch.Draw(new RectangleRuntime()));
        Should.Throw<InvalidOperationException>(() => gumBatch.DrawString("Arial", 12, "a", new SKPoint(0, 0), SKColors.White));
        Should.Throw<InvalidOperationException>(() => gumBatch.End());
    }

    [Fact]
    public void Deferred_DrawsByZAtEnd_ImmediateDrawsInCallOrder()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        surface.Canvas.Clear(SKColors.Black);
        RectangleRuntime red = CreateRectangle(x: 0, y: 0, size: 16, SKColors.Red);
        red.Z = 1;
        RectangleRuntime lime = CreateRectangle(x: 8, y: 8, size: 16, SKColors.Lime);
        lime.Z = 0;
        GumBatch gumBatch = new();

        gumBatch.Begin();
        gumBatch.Draw(red);
        gumBatch.Draw(lime);
        gumBatch.End();

        PixelAt(surface, 12, 12).ShouldBe(SKColors.Lime, "because Immediate draws in call order and ignores Z");

        surface.Canvas.Clear(SKColors.Black);
        gumBatch.Begin(mode: RenderingLibrary.Graphics.Renderer.GumBatchDrawMode.Deferred);
        gumBatch.Draw(red);
        gumBatch.Draw(lime);
        PixelAt(surface, 4, 4).ShouldBe(SKColors.Black, "because Deferred draws nothing before End");
        gumBatch.End();

        PixelAt(surface, 12, 12).ShouldBe(SKColors.Red, "because Deferred sorts by Z at End, so the higher-Z red draws last");
        PixelAt(surface, 20, 20).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Deferred_EqualZ_KeepsCallOrderEvenWithSecondarySortOnY()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        surface.Canvas.Clear(SKColors.Black);
        Layer mainLayer = SystemManagers.Default.Renderer.MainLayer;
        mainLayer.SecondarySortOnY = true;
        // Drawn in an order that disagrees with Y at equal Z, so a Y sort would put red on top.
        RectangleRuntime red = CreateRectangle(x: 0, y: 8, size: 16, SKColors.Red);
        RectangleRuntime lime = CreateRectangle(x: 0, y: 0, size: 16, SKColors.Lime);
        GumBatch gumBatch = new();

        gumBatch.Begin(mode: RenderingLibrary.Graphics.Renderer.GumBatchDrawMode.Deferred);
        gumBatch.Draw(red);
        gumBatch.Draw(lime);
        gumBatch.End();

        PixelAt(surface, 4, 12).ShouldBe(SKColors.Lime, "because equal-Z deferred draws keep call order, like Immediate");
    }

    [Fact]
    public void Deferred_WithMatrix_AppliesItToDrawsSubmittedAtEnd()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        surface.Canvas.Clear(SKColors.Black);
        ContainerRuntime clip = new() { Width = 16, Height = 16, ClipsChildren = true };
        clip.Children.Add(CreateRectangle(x: 0, y: 0, size: 8, SKColors.Red));
        GumBatch gumBatch = new();

        gumBatch.Begin(SKMatrix.CreateScale(2, 2), RenderingLibrary.Graphics.Renderer.GumBatchDrawMode.Deferred);
        gumBatch.Draw(clip);
        gumBatch.End();

        PixelAt(surface, 12, 12).ShouldBe(SKColors.Red, "because the 8x8 rectangle scaled by 2 covers 0-16");
        surface.Canvas.TotalMatrix.IsIdentity.ShouldBeTrue("because End restores the matrix after the deferred draws");
    }

    [Fact]
    public void Deferred_WhenADrawThrows_DoesNotRedrawTheFailedCycleAndRestoresCanvas()
    {
        using SKSurface hostSurface = CreateInitializedSurface(32);
        using SKSurface offscreenSurface = SKSurface.Create(new SKImageInfo(32, 32));
        ThrowingRenderProbe thrower = new();
        RenderCountProbe nextCycleProbe = new();
        GumBatch gumBatch = new();

        gumBatch.Begin(offscreenSurface.Canvas, SKMatrix.CreateScale(2, 2), RenderingLibrary.Graphics.Renderer.GumBatchDrawMode.Deferred);
        gumBatch.Draw(thrower);
        Should.Throw<InvalidOperationException>(() => gumBatch.End());

        thrower.RenderCount.ShouldBe(1);
        SystemManagers.Default.Canvas.ShouldBeSameAs(hostSurface.Canvas, "because a failed End still restores the previous canvas");
        offscreenSurface.Canvas.TotalMatrix.IsIdentity.ShouldBeTrue("because a failed End still restores the canvas matrix");

        gumBatch.Begin(mode: RenderingLibrary.Graphics.Renderer.GumBatchDrawMode.Deferred);
        gumBatch.Draw(nextCycleProbe);
        Should.NotThrow(() => gumBatch.End());

        thrower.RenderCount.ShouldBe(1, "because the failed cycle's draws must not survive into the next cycle");
        nextCycleProbe.RenderCount.ShouldBe(1);
    }

    [Fact]
    public void Draw_RendersRenderableWithoutClearingCanvas()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        surface.Canvas.Clear(SKColors.Blue);
        RectangleRuntime rectangle = CreateRectangle(x: 4, y: 4, size: 8, SKColors.Red);
        GumBatch gumBatch = new();

        gumBatch.Begin();
        gumBatch.Draw(rectangle);
        gumBatch.End();

        PixelAt(surface, 8, 8).ShouldBe(SKColors.Red);
        PixelAt(surface, 20, 20).ShouldBe(SKColors.Blue, "because GumBatch never clears the canvas");
    }

    [Fact]
    public void Draw_RendersChildren()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        surface.Canvas.Clear(SKColors.Black);
        ContainerRuntime parent = new() { X = 10, Y = 10, Width = 10, Height = 10 };
        parent.Children.Add(CreateRectangle(x: 2, y: 2, size: 4, SKColors.Lime));
        GumBatch gumBatch = new();

        gumBatch.Begin();
        gumBatch.Draw(parent);
        gumBatch.End();

        PixelAt(surface, 14, 14).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void SeveralCycles_InOneFrame_AllStayVisible()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        surface.Canvas.Clear(SKColors.Black);
        RectangleRuntime first = CreateRectangle(x: 0, y: 0, size: 8, SKColors.Red);
        RectangleRuntime second = CreateRectangle(x: 16, y: 16, size: 8, SKColors.Lime);
        GumBatch gumBatch = new();

        gumBatch.Begin();
        gumBatch.Draw(first);
        gumBatch.End();
        gumBatch.Begin();
        gumBatch.Draw(second);
        gumBatch.End();

        PixelAt(surface, 4, 4).ShouldBe(SKColors.Red);
        PixelAt(surface, 20, 20).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Begin_WithMatrix_AppliesIt()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        surface.Canvas.Clear(SKColors.Black);
        RectangleRuntime rectangle = CreateRectangle(x: 0, y: 0, size: 8, SKColors.Red);
        GumBatch gumBatch = new();

        gumBatch.Begin(SKMatrix.CreateScale(2, 2));
        gumBatch.Draw(rectangle);
        gumBatch.End();

        PixelAt(surface, 12, 12).ShouldBe(SKColors.Red, "because the 8x8 rectangle scaled by 2 covers 0-16");
        PixelAt(surface, 20, 20).ShouldBe(SKColors.Black);
    }

    [Fact]
    public void Begin_WithMatrixAndCameraZoom_ComposesBoth()
    {
        using SKSurface surface = CreateInitializedSurface(64);
        surface.Canvas.Clear(SKColors.Black);
        SystemManagers.Default.Renderer.Camera.Zoom = 2;
        RectangleRuntime rectangle = CreateRectangle(x: 0, y: 0, size: 8, SKColors.Red);
        GumBatch gumBatch = new();

        gumBatch.Begin(SKMatrix.CreateScale(3, 3));
        gumBatch.Draw(rectangle);
        gumBatch.End();

        PixelAt(surface, 30, 30).ShouldBe(SKColors.Red, "because matrix 3x composed with zoom 2x scales the fill (1..7) to 6..42");
        PixelAt(surface, 52, 30).ShouldBe(SKColors.Black);
    }

    [Fact]
    public void Begin_WithMatrixAndScrolledCamera_AppliesMatrixBeforeCamera()
    {
        using SKSurface surface = CreateInitializedSurface(64);
        surface.Canvas.Clear(SKColors.Black);
        SystemManagers.Default.Renderer.Camera.X = 10;
        RectangleRuntime rectangle = CreateRectangle(x: 10, y: 0, size: 8, SKColors.Red);
        GumBatch gumBatch = new();

        // MonoGame composes matrix * cameraView: world (10..18) scales to (20..36), then the
        // camera shifts it to (10..26). Camera first would give (0..16).
        gumBatch.Begin(SKMatrix.CreateScale(2, 2));
        gumBatch.Draw(rectangle);
        gumBatch.End();

        PixelAt(surface, 20, 8).ShouldBe(SKColors.Red);
        PixelAt(surface, 6, 8).ShouldBe(SKColors.Black);
    }

    [Fact]
    public void Begin_WithScrolledCamera_TranslatesDraws()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        surface.Canvas.Clear(SKColors.Black);
        SystemManagers.Default.Renderer.Camera.X = 10;
        SystemManagers.Default.Renderer.Camera.Y = 10;
        RectangleRuntime rectangle = CreateRectangle(x: 10, y: 10, size: 8, SKColors.Red);
        GumBatch gumBatch = new();

        gumBatch.Begin();
        gumBatch.Draw(rectangle);
        gumBatch.End();

        PixelAt(surface, 4, 4).ShouldBe(SKColors.Red);
        PixelAt(surface, 14, 14).ShouldBe(SKColors.Black);
    }

    [Fact]
    public void End_RestoresCanvasMatrix()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        SystemManagers.Default.Renderer.Camera.Zoom = 2;
        SystemManagers.Default.Renderer.Camera.X = 5;
        GumBatch gumBatch = new();

        gumBatch.Begin(SKMatrix.CreateScale(3, 3));
        gumBatch.Draw(CreateRectangle(x: 0, y: 0, size: 4, SKColors.Red));
        gumBatch.End();

        surface.Canvas.TotalMatrix.ShouldBe(SKMatrix.Identity);
    }

    [Fact]
    public void Begin_WithCanvas_DrawsOntoThatCanvas()
    {
        using SKSurface hostSurface = CreateInitializedSurface(32);
        using SKSurface otherSurface = SKSurface.Create(new SKImageInfo(32, 32));
        hostSurface.Canvas.Clear(SKColors.Black);
        otherSurface.Canvas.Clear(SKColors.Black);
        GumBatch gumBatch = new();

        gumBatch.Begin(otherSurface.Canvas);
        gumBatch.Draw(CreateRectangle(x: 0, y: 0, size: 8, SKColors.Red));
        gumBatch.End();

        PixelAt(otherSurface, 4, 4).ShouldBe(SKColors.Red);
        PixelAt(hostSurface, 4, 4).ShouldBe(SKColors.Black);
    }

    [Fact]
    public void DrawString_DrawsTextAtPosition()
    {
        using SKSurface surface = CreateInitializedSurface(64);
        surface.Canvas.Clear(SKColors.Black);
        GumBatch gumBatch = new();

        gumBatch.Begin();
        gumBatch.DrawString("Arial", 24, "MMM", new SKPoint(32, 32), SKColors.White);
        gumBatch.End();

        // Rasterization differs per OS, so only check that text landed right of and below the
        // position and nowhere above or left of it.
        CountNonBlack(surface, 32, 32, 64, 64).ShouldBeGreaterThan(0);
        CountNonBlack(surface, 0, 0, 32, 64).ShouldBe(0);
        CountNonBlack(surface, 0, 0, 64, 32).ShouldBe(0);
    }

    [Fact]
    public void Begin_AfterFullDrawInSameFrame_KeepsBakedRenderTarget()
    {
        using SKSurface surface = CreateInitializedSurface(64);
        ContainerRuntime renderTarget = new() { Width = 20, Height = 20, IsRenderTarget = true };
        renderTarget.Children.Add(CreateRectangle(x: 0, y: 0, size: 20, SKColors.Red));
        GumService.Default.Root.Children.Add(renderTarget);
        GumService.Default.Draw();
        GumBatch gumBatch = new();

        gumBatch.Begin();
        gumBatch.Draw(CreateRectangle(x: 40, y: 40, size: 4, SKColors.Lime));
        gumBatch.End();

        SystemManagers.Default.Renderer.HasBakedRenderTargetFor(renderTarget)
            .ShouldBeTrue("because GumBatch must not dispose this frame's snapshots");
    }

    [Fact]
    public void Begin_AfterUpdate_StartsNewFrameAndDropsLastFramesSnapshots()
    {
        using SKSurface surface = CreateInitializedSurface(64);
        ContainerRuntime renderTarget = new() { Width = 20, Height = 20, IsRenderTarget = true };
        renderTarget.Children.Add(CreateRectangle(x: 0, y: 0, size: 20, SKColors.Red));
        GumService.Default.Root.Children.Add(renderTarget);
        GumService.Default.Draw();
        GumBatch gumBatch = new();

        GumService.Default.Update(1);
        gumBatch.Begin();
        gumBatch.End();
        gumBatch.Begin();
        gumBatch.End();

        SystemManagers.Default.Renderer.HasBakedRenderTargetFor(renderTarget)
            .ShouldBeFalse("because the first Begin of a new frame drops last frame's snapshots");
    }

    [Fact]
    public void UpdateThenGumBatchThenDraw_KeepsRenderTargetSurfaceAcrossFrames()
    {
        using SKSurface surface = CreateInitializedSurface(64);
        ContainerRuntime renderTarget = new() { Width = 20, Height = 20, IsRenderTarget = true };
        renderTarget.Children.Add(CreateRectangle(x: 0, y: 0, size: 20, SKColors.Red));
        GumService.Default.Root.Children.Add(renderTarget);
        GumBatch gumBatch = new();
        List<SKSurface?> surfaces = new();

        for (int frame = 0; frame < 3; frame++)
        {
            GumService.Default.Update(frame);
            gumBatch.Begin();
            gumBatch.Draw(CreateRectangle(x: 40, y: 40, size: 4, SKColors.Lime));
            gumBatch.End();
            GumService.Default.Draw();
            surfaces.Add(SystemManagers.Default.Renderer.TryGetRenderTargetSurfaceFor(renderTarget));
        }

        surfaces[0].ShouldNotBeNull();
        surfaces[1].ShouldBeSameAs(surfaces[0], "because a render target used every frame keeps its surface");
        surfaces[2].ShouldBeSameAs(surfaces[0]);
    }

    [Fact]
    public void GumBatchThenDraw_WithoutUpdate_StillSweepsRemovedRenderTarget()
    {
        using SKSurface surface = CreateInitializedSurface(64);
        ContainerRuntime renderTarget = new() { Width = 20, Height = 20, IsRenderTarget = true };
        renderTarget.Children.Add(CreateRectangle(x: 0, y: 0, size: 20, SKColors.Red));
        GumService.Default.Root.Children.Add(renderTarget);
        GumBatch gumBatch = new();

        for (int frame = 0; frame < 3; frame++)
        {
            gumBatch.Begin();
            gumBatch.End();
            GumService.Default.Draw();
            if (frame == 0)
            {
                SystemManagers.Default.Renderer.TryGetRenderTargetSurfaceFor(renderTarget).ShouldNotBeNull();
                GumService.Default.Root.Children.Remove(renderTarget);
            }
        }

        SystemManagers.Default.Renderer.TryGetRenderTargetSurfaceFor(renderTarget)
            .ShouldBeNull("because a host that never calls Update still sweeps once per frame");
    }

    [Fact]
    public void End_AfterBeginWithCanvas_RestoresPreviousCanvas()
    {
        using SKSurface hostSurface = CreateInitializedSurface(32);
        using SKSurface offscreenSurface = SKSurface.Create(new SKImageInfo(32, 32));
        hostSurface.Canvas.Clear(SKColors.Black);
        offscreenSurface.Canvas.Clear(SKColors.Black);
        GumBatch gumBatch = new();

        gumBatch.Begin(offscreenSurface.Canvas);
        gumBatch.Draw(CreateRectangle(x: 0, y: 0, size: 8, SKColors.Red));
        gumBatch.End();

        SystemManagers.Default.Canvas.ShouldBeSameAs(hostSurface.Canvas);

        gumBatch.Begin();
        gumBatch.Draw(CreateRectangle(x: 16, y: 16, size: 8, SKColors.Lime));
        gumBatch.End();

        PixelAt(hostSurface, 20, 20).ShouldBe(SKColors.Lime);
        PixelAt(offscreenSurface, 20, 20).ShouldBe(SKColors.Black);
    }

    private class RenderCountProbe : ContainerRuntime
    {
        public int RenderCount { get; private set; }

        public RenderCountProbe()
        {
            Width = 10;
            Height = 10;
        }

        public override void Render(ISystemManagers managers)
        {
            RenderCount++;
            base.Render(managers);
        }
    }

    private sealed class ThrowingRenderProbe : RenderCountProbe
    {
        public override void Render(ISystemManagers managers)
        {
            base.Render(managers);
            throw new InvalidOperationException("Simulated renderable failure.");
        }
    }

    private static SKSurface CreateInitializedSurface(int size)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(size, size));
        GumService.Default.Initialize(surface.Canvas, size, size);
        return surface;
    }

    private static RectangleRuntime CreateRectangle(float x, float y, float size, SKColor color) => new()
    {
        X = x,
        Y = y,
        Width = size,
        Height = size,
        IsFilled = true,
        FillColor = color,
    };

    private static SKColor PixelAt(SKSurface surface, int x, int y)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }

    private static int CountNonBlack(SKSurface surface, int left, int top, int right, int bottom)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        int count = 0;
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                if (bitmap.GetPixel(x, y) != SKColors.Black)
                {
                    count++;
                }
            }
        }
        return count;
    }
}
