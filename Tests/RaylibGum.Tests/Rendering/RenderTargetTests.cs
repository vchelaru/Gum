using Gum.GueDeriving;
using Gum.RenderingLibrary;
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
        return ReadRenderTargetPixel(renderTexture,
            renderTexture.Texture.Width / 2, renderTexture.Texture.Height / 2);
    }

    // Reads a pixel in top-left-origin draw space. A render texture is stored bottom-up in GL, so
    // LoadImageFromTexture yields an image whose rows are flipped relative to draw space — hence the
    // (height - 1 - y) flip here.
    private static Color ReadRenderTargetPixel(RenderTexture2D renderTexture, int x, int y)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            return GetImageColor(image, x, renderTexture.Texture.Height - 1 - y);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    private static int DrawAndCountDrawCalls()
    {
        DrawOnce();
        return Renderer.Self.RenderStateChangeStatistics.DrawCallCount;
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

    // Fix 1: a render-target container whose clamped bounds are degenerate (0-sized) must not drop
    // its subtree. Here a 0x0 inner RT still renders its child directly into the outer RT.
    [Fact]
    public void Draw_DegenerateSizeRenderTarget_RendersChildrenDirectly()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime degenerate = new();
        degenerate.Width = 0;
        degenerate.Height = 0;
        degenerate.IsRenderTarget = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 100;
        rectangle.Height = 100;
        rectangle.Color = new Color((byte)0, (byte)255, (byte)0, (byte)255);
        degenerate.Children.Add(rectangle);
        outer.Children.Add(degenerate);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        // The degenerate container bakes nothing, but its green child must still appear in the outer
        // render target rather than vanishing.
        Renderer.Self.HasBakedRenderTargetFor(degenerate).ShouldBeFalse();
        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value);
        center.G.ShouldBeGreaterThan((byte)200);
        center.R.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
    }

    // Fix 2: after a blend-toggling child (a nested inner RT composite), a following semi-transparent
    // sibling must still bake under the premultiply pass — not straight alpha, which would halve its
    // coverage (the double-blend fringe). The sibling occupies the right half at full alpha coverage.
    [Fact]
    public void Draw_SiblingAfterNestedRenderTarget_KeepsPremultipliedCoverage()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.X = 0;
        inner.Y = 0;
        inner.Width = 50;
        inner.Height = 100;
        inner.IsRenderTarget = true;

        ColoredRectangleRuntime innerFill = new();
        innerFill.Width = 50;
        innerFill.Height = 100;
        innerFill.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(innerFill);

        ColoredRectangleRuntime sibling = new();
        sibling.X = 50;
        sibling.Y = 0;
        sibling.Width = 50;
        sibling.Height = 100;
        // Half-alpha white: premultiplied coverage keeps alpha ~128; straight-alpha double-blend
        // would square it to ~64.
        sibling.Color = new Color((byte)255, (byte)255, (byte)255, (byte)128);

        // Order matters: the nested RT composites first (toggling blend), then the sibling bakes.
        outer.Children.Add(inner);
        outer.Children.Add(sibling);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        RenderTexture2D outerRenderTexture = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;
        Color innerRegion = ReadRenderTargetPixel(outerRenderTexture, 25, 50);
        Color siblingRegion = ReadRenderTargetPixel(outerRenderTexture, 75, 50);

        // Inner RT still composited its red.
        innerRegion.R.ShouldBeGreaterThan((byte)200);
        // Sibling kept full premultiplied coverage (~128), proving the premultiply pass was
        // re-established after the nested composite toggled blend.
        siblingRegion.A.ShouldBeGreaterThan((byte)100);

        GumService.Default.Root.Children.Clear();
    }

    // Fix 3: a top-level render-target container (added straight to the layer, where the walk is not
    // Visible-gated) must not composite last frame's stale texture for one frame after being hidden.
    [Fact]
    public void Draw_HiddenTopLevelRenderTarget_DoesNotCompositeStaleTexture()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime container = new();
        container.Width = 50;
        container.Height = 50;
        container.IsRenderTarget = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 50;
        rectangle.Height = 50;
        rectangle.Color = new Color((byte)0, (byte)255, (byte)0, (byte)255);
        container.Children.Add(rectangle);
        container.AddToManagers();
        container.UpdateLayout();

        int visibleCalls = DrawAndCountDrawCalls();
        Renderer.Self.HasBakedRenderTargetFor(container).ShouldBeTrue();

        container.Visible = false;
        int hiddenCalls = DrawAndCountDrawCalls();

        // Hidden: no bake and no composite, so the frame's draw-call count returns to the empty
        // baseline. Under the ghost bug the stale cached texture would still be blitted (baseline + 1).
        visibleCalls.ShouldBeGreaterThan(baseline);
        hiddenCalls.ShouldBe(baseline);

        container.RemoveFromManagers();
    }

    // Fix 4: an additive-blend render-target container composites its premultiplied texture with an
    // additive-onto-premultiplied blend, not raylib's BlendMode.Additive (which multiplies by source
    // alpha again and renders the glow too dim). A half-alpha additive layer over a dark background
    // must brighten it by ~the premultiplied color, not ~half of it.
    [Fact]
    public void Draw_AdditiveRenderTarget_AddsPremultipliedColorWithoutDoubleAlpha()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ColoredRectangleRuntime background = new();
        background.Width = 100;
        background.Height = 100;
        background.Color = new Color((byte)50, (byte)0, (byte)0, (byte)255);

        ContainerRuntime additive = new();
        additive.Width = 100;
        additive.Height = 100;
        additive.IsRenderTarget = true;
        additive.Blend = Blend.Additive;

        ColoredRectangleRuntime glow = new();
        glow.Width = 100;
        glow.Height = 100;
        // Half-alpha red -> premultiplied ~100 red. Correct additive adds ~100 to the background's
        // 50; the double-alpha bug would add only ~50.
        glow.Color = new Color((byte)200, (byte)0, (byte)0, (byte)128);
        additive.Children.Add(glow);

        outer.Children.Add(background);
        outer.Children.Add(additive);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value);
        center.R.ShouldBeGreaterThan((byte)130);

        GumService.Default.Root.Children.Clear();
    }

    // Fix 5: a ClipsChildren descendant inside a render-target container clips within the RT. The
    // clip container is the left half; its over-wide child must be clipped away on the right half.
    [Fact]
    public void Draw_ClipsChildrenInsideRenderTarget_ClipsWithinTheTarget()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime clip = new();
        clip.X = 0;
        clip.Y = 0;
        clip.Width = 50;
        clip.Height = 100;
        clip.ClipsChildren = true;

        ColoredRectangleRuntime wide = new();
        wide.X = 0;
        wide.Y = 0;
        wide.Width = 200;
        wide.Height = 100;
        wide.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        clip.Children.Add(wide);
        outer.Children.Add(clip);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        RenderTexture2D outerRenderTexture = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;
        Color insideClip = ReadRenderTargetPixel(outerRenderTexture, 25, 50);
        Color outsideClip = ReadRenderTargetPixel(outerRenderTexture, 75, 50);

        // Left half (inside the clip) shows the red child; right half (beyond the clip) is clipped
        // away and stays transparent.
        insideClip.R.ShouldBeGreaterThan((byte)200);
        outsideClip.A.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
    }
}
