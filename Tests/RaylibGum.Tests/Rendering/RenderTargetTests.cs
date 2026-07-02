using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback tests for the raylib render-to-texture path (issue #3434, PR 1). A container
/// with <see cref="ContainerRuntime.IsRenderTarget"/> bakes its subtree into an offscreen
/// <see cref="RenderTexture2D"/> in a pre-pass, then composites that texture back to the screen.
/// These tests drive a real headless raylib frame and read back the baked RT contents (and,
/// for the composite/group-alpha cases, a nested outer RT that the inner group composites into,
/// which gives a deterministic surface to sample without screen readback).
/// </summary>
public class RenderTargetTests : BaseTestClass
{
    private static void DrawOnce()
    {
        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();
    }

    private static Color ReadRenderTargetCenter(RenderTexture2D renderTexture)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            return GetImageColor(image, renderTexture.Texture.Width / 2, renderTexture.Texture.Height / 2);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    [Fact]
    public void Draw_RenderTargetContainer_BakesChildSubtreeIntoRenderTarget()
    {
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 100;
        rectangle.Height = 100;
        rectangle.Color = new Color((byte)0, (byte)0, (byte)255, (byte)255);
        container.Children.Add(rectangle);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Renderer.Self.HasBakedRenderTargetFor(container).ShouldBeTrue();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
        Color center = ReadRenderTargetCenter(renderTexture);

        // The blue child rect should have baked into the container's render target.
        center.B.ShouldBeGreaterThan((byte)200);
        center.R.ShouldBeLessThan((byte)50);
        center.A.ShouldBeGreaterThan((byte)200);

        GumService.Default.Root.Children.Clear();
    }

    [Fact]
    public void Draw_NestedRenderTargetContainers_CompositeInnerIntoOuter()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.Width = 100;
        inner.Height = 100;
        inner.IsRenderTarget = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 100;
        rectangle.Height = 100;
        rectangle.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(rectangle);
        outer.Children.Add(inner);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        // Both the inner and outer containers must have baked (post-order, innermost first).
        Renderer.Self.HasBakedRenderTargetFor(inner).ShouldBeTrue();
        Renderer.Self.HasBakedRenderTargetFor(outer).ShouldBeTrue();

        // Reading the OUTER render target proves the inner group was composited into it while the
        // outer container baked — a deterministic view of the composite step without screen readback.
        RenderTexture2D outerRenderTexture = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;
        Color center = ReadRenderTargetCenter(outerRenderTexture);

        center.R.ShouldBeGreaterThan((byte)200);
        center.B.ShouldBeLessThan((byte)50);
        center.A.ShouldBeGreaterThan((byte)200);

        GumService.Default.Root.Children.Clear();
    }

    [Fact]
    public void Draw_ReducedGroupAlpha_ProducesDimmerCompositedPixels()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.Width = 100;
        inner.Height = 100;
        inner.IsRenderTarget = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 100;
        rectangle.Height = 100;
        rectangle.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(rectangle);
        outer.Children.Add(inner);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        inner.Alpha = 255;
        DrawOnce();
        byte fullRed = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value).R;

        inner.Alpha = 64;
        DrawOnce();
        byte dimRed = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value).R;

        // Group alpha is applied when the inner group's baked texture is composited, so lowering it
        // must dim the composited red without touching the inner bake itself.
        fullRed.ShouldBeGreaterThan((byte)200);
        dimRed.ShouldBeLessThan(fullRed);

        GumService.Default.Root.Children.Clear();
    }
}
