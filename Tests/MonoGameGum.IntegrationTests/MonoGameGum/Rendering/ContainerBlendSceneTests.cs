using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;
using GumBlend = Gum.RenderingLibrary.Blend;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The reference numbers for #6233: the same scene is rendered by SkiaGum in
/// <c>SkiaGum.Tests.Renderables.ContainerBlendTests</c> and must give the same pixels. A render-target
/// container with its own <c>Blend</c> (a white square at group alpha 128) is composited either into
/// a parent render target holding a red fill over the left 55px (nested), or straight onto an opaque
/// backdrop (top level). Additive applies the group alpha once, like Skia's Plus (#6277).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ContainerBlendSceneTests : BaseTestClass
{
    private const int Size = 100;

    // Nested: the composite lands on a premultiplied bake, so the colors below are what shows on the
    // opaque backdrop. A = inner over red, B = inner over nothing.
    [Theory]
    [InlineData(GumBlend.Normal, 128, 228, 143, 143, 163, 163, 173)]
    [InlineData(GumBlend.Replace, 128, 163, 163, 173, 163, 163, 173)]
    [InlineData(GumBlend.SubtractAlpha, 128, 135, 50, 60, 70, 70, 90)]
    [InlineData(GumBlend.ReplaceAlpha, 128, 135, 50, 60, 35, 35, 45)]
    [InlineData(GumBlend.MinAlpha, 128, 135, 50, 60, 70, 70, 90)]
    [InlineData(GumBlend.Additive, 255, 255, 255, 255, 255, 255, 255)]
    [InlineData(GumBlend.Additive, 128, 255, 158, 158, 163, 163, 173)]
    public void NestedContainerBlend_HasExpectedPixels(
        GumBlend blend, int alpha, int ar, int ag, int ab, int br, int bg, int bb)
    {
        XnaColor[] pixels = Render(blend, alpha, nested: true);

        AssertPixel(pixels, 30, 30, ar, ag, ab);
        AssertPixel(pixels, 70, 70, br, bg, bb);
        AssertPixel(pixels, 5, 5, 200, 30, 30);
        AssertPixel(pixels, 90, 10, 70, 70, 90);
    }

    // The content's own translucency is the other half of the same fault: a 50% fill in an opaque
    // Additive container must add 50% of its color, not 25%.
    [Fact]
    public void NestedAdditiveContainer_WithTranslucentFill_AddsFillAlphaOnce()
    {
        XnaColor[] pixels = Render(GumBlend.Additive, 255, nested: true, fillAlpha: 128);

        AssertPixel(pixels, 30, 30, 255, 158, 158);
        AssertPixel(pixels, 70, 70, 163, 163, 173);
    }

    // Not a render target: a plain additive fill already adds its alpha once (the path the container fix
    // makes the render-target composite agree with).
    [Fact]
    public void AdditiveFillWithoutRenderTarget_AddsAlphaOnce()
    {
        ContainerRuntime root = new() { Width = Size, Height = Size };
#pragma warning disable CS0618
        root.AddChild(new ColoredRectangleRuntime { Width = Size, Height = Size, Color = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255) });
        root.AddChild(new ColoredRectangleRuntime { X = 20, Y = 20, Width = 60, Height = 60, Color = XnaColor.White, Alpha = 128, Blend = GumBlend.Additive });
#pragma warning restore CS0618

        XnaColor[] pixels = Capture(root);

        AssertPixel(pixels, 30, 30, 255, 158, 158);
        AssertPixel(pixels, 5, 5, 200, 30, 30);
    }

    // Top level: Additive adds the premultiplied bake straight onto the backdrop.
    [Fact]
    public void TopLevelAdditiveContainer_AddsGroupAlphaOnce()
    {
        XnaColor[] pixels = Render(GumBlend.Additive, 128, nested: false);

        AssertPixel(pixels, 30, 30, 255, 158, 158);
        AssertPixel(pixels, 70, 70, 198, 198, 218);
        AssertPixel(pixels, 5, 5, 200, 30, 30);
    }

    // Top level: the alpha-only blends change the alpha channel of the capture and leave its color.
    [Theory]
    [InlineData(GumBlend.SubtractAlpha, 127)]
    [InlineData(GumBlend.ReplaceAlpha, 128)]
    [InlineData(GumBlend.MinAlpha, 128)]
    public void TopLevelAlphaBlend_ChangesOnlyAlpha(GumBlend blend, int expectedAlpha)
    {
        XnaColor[] pixels = Render(blend, 128, nested: false);

        AssertPixel(pixels, 30, 30, 200, 30, 30);
        AssertPixel(pixels, 70, 70, 70, 70, 90);
        System.Math.Abs(pixels[(30 * Size) + 30].A - expectedAlpha).ShouldBeLessThanOrEqualTo(1);
        System.Math.Abs(pixels[(70 * Size) + 70].A - expectedAlpha).ShouldBeLessThanOrEqualTo(1);
        pixels[(5 * Size) + 5].A.ShouldBe((byte)255);
    }

    private static void AssertPixel(XnaColor[] pixels, int x, int y, int r, int g, int b)
    {
        XnaColor actual = pixels[(y * Size) + x];
        System.Math.Abs(actual.R - r).ShouldBeLessThanOrEqualTo(2, $"R at ({x}, {y}) was {actual.R}");
        System.Math.Abs(actual.G - g).ShouldBeLessThanOrEqualTo(2, $"G at ({x}, {y}) was {actual.G}");
        System.Math.Abs(actual.B - b).ShouldBeLessThanOrEqualTo(2, $"B at ({x}, {y}) was {actual.B}");
    }

    private static XnaColor[] Render(GumBlend blend, int alpha, bool nested, int fillAlpha = 255)
    {
        ContainerRuntime root = new();
        root.Width = Size;
        root.Height = Size;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = Size, Height = Size, Color = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255) });

        ContainerRuntime inner = new();
        inner.X = 20;
        inner.Y = 20;
        inner.Width = 60;
        inner.Height = 60;
        inner.IsRenderTarget = true;
        inner.Alpha = alpha;
        inner.Blend = blend;
        inner.AddChild(new ColoredRectangleRuntime { Width = 60, Height = 60, Color = XnaColor.White, Alpha = fillAlpha });

        ColoredRectangleRuntime redFill = new() { Width = 55, Height = Size, Color = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255) };
#pragma warning restore CS0618
        if (nested)
        {
            ContainerRuntime outer = new();
            outer.Width = Size;
            outer.Height = Size;
            outer.IsRenderTarget = true;
            outer.AddChild(redFill);
            outer.AddChild(inner);
            root.AddChild(outer);
        }
        else
        {
            root.AddChild(redFill);
            root.AddChild(inner);
        }

        return Capture(root);
    }

    private static XnaColor[] Capture(ContainerRuntime root)
    {
        using ShapeGameLease game = new ShapeGameLease();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, Size, Size, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[Size * Size];
        capture.GetData(pixels);

        root.RemoveFromManagers();
        return pixels;
    }
}
