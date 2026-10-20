using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Shouldly;
using SkiaGum.Tests.GoldenImages;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A render-target <c>ContainerRuntime</c> composites its bake with its own <c>Blend</c> (#6233).
/// The scene and expected pixels are the ones in MonoGameGum.IntegrationTests'
/// <c>ContainerBlendSceneTests</c>: a white square container at group alpha 128, composited either
/// into a parent render target holding a red fill over the left 55px (nested) or straight onto an
/// opaque backdrop (top level). Additive runs at group alpha 255 because MonoGame multiplies its
/// premultiplied bake by alpha a second time.
/// </summary>
public class ContainerBlendTests
{
    [Theory]
    [InlineData(Blend.Normal, 128, 228, 143, 143, 163, 163, 173)]
    [InlineData(Blend.Replace, 128, 163, 163, 173, 163, 163, 173)]
    [InlineData(Blend.SubtractAlpha, 128, 135, 50, 60, 70, 70, 90)]
    [InlineData(Blend.ReplaceAlpha, 128, 135, 50, 60, 35, 35, 45)]
    [InlineData(Blend.MinAlpha, 128, 135, 50, 60, 70, 70, 90)]
    [InlineData(Blend.Additive, 255, 255, 255, 255, 255, 255, 255)]
    public void NestedContainerBlend_MatchesMonoGamePixels(
        Blend blend, int alpha, int ar, int ag, int ab, int br, int bg, int bb)
    {
        using SKBitmap bitmap = RenderToBitmap(blend, alpha, nested: true);

        AssertPixel(bitmap, 30, 30, ar, ag, ab);
        AssertPixel(bitmap, 70, 70, br, bg, bb);
        AssertPixel(bitmap, 5, 5, 200, 30, 30);
        AssertPixel(bitmap, 90, 10, 70, 70, 90);
    }

    // The capture is premultiplied, so reading it as straight color gives MonoGame's top-level
    // result: the alpha-only blends change the alpha channel and leave the color.
    [Theory]
    [InlineData(Blend.SubtractAlpha, 127)]
    [InlineData(Blend.ReplaceAlpha, 128)]
    [InlineData(Blend.MinAlpha, 128)]
    public void TopLevelAlphaBlend_ChangesOnlyAlpha_MatchesMonoGame(Blend blend, int expectedAlpha)
    {
        using SKBitmap bitmap = RenderToBitmap(blend, 128, nested: false);

        AssertPixel(bitmap, 30, 30, 200, 30, 30);
        AssertPixel(bitmap, 70, 70, 70, 70, 90);
        ((int)bitmap.GetPixel(30, 30).Alpha).ShouldBeInRange(expectedAlpha - 1, expectedAlpha + 1);
        ((int)bitmap.GetPixel(70, 70).Alpha).ShouldBeInRange(expectedAlpha - 1, expectedAlpha + 1);
        bitmap.GetPixel(5, 5).Alpha.ShouldBe((byte)255);
    }

    [Theory]
    [InlineData(Blend.Normal, 128)]
    [InlineData(Blend.Replace, 128)]
    [InlineData(Blend.SubtractAlpha, 128)]
    [InlineData(Blend.ReplaceAlpha, 128)]
    [InlineData(Blend.MinAlpha, 128)]
    [InlineData(Blend.Additive, 255)]
    public void NestedContainerBlend_MatchesGoldenBaseline(Blend blend, int alpha)
    {
        using SKSurface surface = RenderToSurface(blend, alpha, nested: true);

        GoldenImageAssert.Matches(surface, $"ContainerBlend_{blend}");
    }

    private static void AssertPixel(SKBitmap bitmap, int x, int y, int red, int green, int blue)
    {
        SKColor actual = bitmap.GetPixel(x, y);
        ((int)actual.Red).ShouldBeInRange(red - 2, red + 2, $"red at ({x}, {y})");
        ((int)actual.Green).ShouldBeInRange(green - 2, green + 2, $"green at ({x}, {y})");
        ((int)actual.Blue).ShouldBeInRange(blue - 2, blue + 2, $"blue at ({x}, {y})");
    }

    private static SKBitmap RenderToBitmap(Blend blend, int alpha, bool nested)
    {
        using SKSurface surface = RenderToSurface(blend, alpha, nested);
        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    private static SKSurface RenderToSurface(Blend blend, int alpha, bool nested)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(100, 100));
        Gum.GumService.Default.Initialize(surface.Canvas, 100, 100);

        ContainerRuntime root = new() { Width = 100, Height = 100 };
        root.Children.Add(NewFill(0, 100, new SKColor(70, 70, 90)));

        ContainerRuntime inner = new()
        {
            X = 20, Y = 20, Width = 60, Height = 60, IsRenderTarget = true, Alpha = alpha, Blend = blend,
        };
        inner.Children.Add(NewFill(0, 60, SKColors.White));

        RectangleRuntime redFill = NewFill(0, 55, new SKColor(200, 30, 30));
        redFill.Height = 100;
        if (nested)
        {
            ContainerRuntime outer = new() { Width = 100, Height = 100, IsRenderTarget = true };
            outer.Children.Add(redFill);
            outer.Children.Add(inner);
            root.Children.Add(outer);
        }
        else
        {
            root.Children.Add(redFill);
            root.Children.Add(inner);
        }
        Gum.GumService.Default.Root.Children.Add(root);

        Gum.GumService.Default.Draw();

        return surface;
    }

    private static RectangleRuntime NewFill(float x, float width, SKColor color) => new()
    {
        X = x, Width = width, Height = width, IsFilled = true, StrokeWidth = 0, FillColor = color,
    };
}
