using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// A render-target container must look the same as the same layout without a render target at any
/// camera zoom (#6985): the bake happens at the zoomed pixel resolution instead of at 1x and being
/// stretched.
/// </summary>
public class RenderTargetZoomTests
{
    private const int ScreenWidth = 320;
    private const int ScreenHeight = 180;

    public RenderTargetZoomTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Theory]
    [InlineData(2f)]
    [InlineData(1.5f)]
    [InlineData(2.25f)]
    public void Draw_RenderTargetAtAlpha255_MatchesNonRenderTarget(float zoom)
    {
        SKBitmap expected = DrawPanel(zoom, isRenderTarget: false, nested: false, alpha: 255);
        SKBitmap actual = DrawPanel(zoom, isRenderTarget: true, nested: false, alpha: 255);

        MismatchedPixelCount(expected, actual).ShouldBeLessThan(ScreenWidth * ScreenHeight / 500);
    }

    [Theory]
    [InlineData(2f)]
    [InlineData(1.5f)]
    public void Draw_NestedRenderTargets_MatchesNonRenderTarget(float zoom)
    {
        SKBitmap expected = DrawPanel(zoom, isRenderTarget: false, nested: true, alpha: 255);
        SKBitmap actual = DrawPanel(zoom, isRenderTarget: true, nested: true, alpha: 255);

        MismatchedPixelCount(expected, actual).ShouldBeLessThan(ScreenWidth * ScreenHeight / 500);
    }

    [Fact]
    public void Draw_RenderTargetAtReducedAlpha_IsTheOpaqueDrawFadedAtZoom()
    {
        SKBitmap opaque = DrawPanel(2f, isRenderTarget: true, nested: false, alpha: 255);
        SKBitmap faded = DrawPanel(2f, isRenderTarget: true, nested: false, alpha: 128);

        // Over the black background a group alpha of 128 scales every channel by 128/255.
        int mismatched = 0;
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                SKColor o = opaque.GetPixel(x, y);
                SKColor f = faded.GetPixel(x, y);
                if (System.Math.Abs(o.Red * 128 / 255 - f.Red) > 3 ||
                    System.Math.Abs(o.Green * 128 / 255 - f.Green) > 3 ||
                    System.Math.Abs(o.Blue * 128 / 255 - f.Blue) > 3)
                {
                    mismatched++;
                }
            }
        }
        mismatched.ShouldBeLessThan(ScreenWidth * ScreenHeight / 500);
    }

    private static SKBitmap DrawPanel(float zoom, bool isRenderTarget, bool nested, byte alpha)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(ScreenWidth, ScreenHeight));
        GumService.Default.Initialize(surface.Canvas, ScreenWidth, ScreenHeight);
        SystemManagers.Default.Renderer.Camera.Zoom = zoom;
        GraphicalUiElement.CanvasWidth = ScreenWidth / zoom;
        GraphicalUiElement.CanvasHeight = ScreenHeight / zoom;
        surface.Canvas.Clear(SKColors.Black);

        ContainerRuntime panel = new() { X = 10.3f, Y = 10.7f, Width = 100, Height = 60 };
        panel.IsRenderTarget = isRenderTarget;
        panel.Alpha = alpha;
        GumService.Default.Root.Children.Add(panel);
        panel.Children.Add(new RectangleRuntime
        {
            Width = 100,
            Height = 60,
            IsFilled = true,
            FillColor = new SKColor(60, 80, 120),
        });
        panel.Children.Add(new RectangleRuntime
        {
            X = 4,
            Y = 4,
            Width = 40,
            Height = 20,
            IsFilled = false,
            StrokeWidth = 1,
            Color = SKColors.White,
        });

        ContainerRuntime circleParent = panel;
        if (nested)
        {
            ContainerRuntime inner = new() { X = 40, Y = 20, Width = 50, Height = 30 };
            inner.IsRenderTarget = isRenderTarget;
            panel.Children.Add(inner);
            circleParent = inner;
        }
        circleParent.Children.Add(new CircleRuntime
        {
            X = 5,
            Y = 5,
            Width = 20,
            Height = 20,
            IsFilled = true,
            FillColor = SKColors.Orange,
        });

        GumService.Default.Draw();

        SKBitmap bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
        return bitmap;
    }

    private static int MismatchedPixelCount(SKBitmap expected, SKBitmap actual)
    {
        int count = 0;
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                SKColor e = expected.GetPixel(x, y);
                SKColor a = actual.GetPixel(x, y);
                if (System.Math.Abs(e.Red - a.Red) > 24 ||
                    System.Math.Abs(e.Green - a.Green) > 24 ||
                    System.Math.Abs(e.Blue - a.Blue) > 24)
                {
                    count++;
                }
            }
        }
        return count;
    }
}
