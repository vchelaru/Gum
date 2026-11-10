using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

// RoundedRectangleRuntime is obsolete, but it is the one Skia shape that sets both gradient stops directly.
#pragma warning disable CS0618

namespace SkiaGum.Tests.GoldenImages;

/// <summary>
/// Checks that a shape reusing its gradient paint between frames draws the gradient it has now, not the one
/// it drew last frame, and that shapes never share gradient stops.
/// </summary>
public class GradientChangeBetweenFramesTests
{
    public GradientChangeBetweenFramesTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    static RoundedRectangleRuntime CreateRadialGreenToYellow() => new()
    {
        X = 4,
        Y = 4,
        Width = 56,
        Height = 56,
        CornerRadius = 0,
        UseGradient = true,
        GradientType = GradientType.Radial,
        GradientX1 = 28,
        GradientY1 = 28,
        GradientInnerRadius = 14,
        GradientOuterRadius = 28,
        GradientX2 = 56,
        GradientY2 = 56,
        Red1 = 0,
        Green1 = 255,
        Blue1 = 0,
        Alpha1 = 255,
        Red2 = 255,
        Green2 = 255,
        Blue2 = 0,
        Alpha2 = 255,
    };

    static void ChangeToLinearRedToBlue(RoundedRectangleRuntime rectangle)
    {
        rectangle.GradientType = GradientType.Linear;
        rectangle.Red1 = 255;
        rectangle.Green1 = 0;
        rectangle.Blue1 = 0;
        rectangle.Red2 = 0;
        rectangle.Green2 = 0;
        rectangle.Blue2 = 255;
    }

    static byte[] Render(System.Action<RoundedRectangleRuntime> beforeSecondDraw, bool drawFirst)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        GumService.Default.Root.Children.Clear();
        RoundedRectangleRuntime rectangle = CreateRadialGreenToYellow();
        GumService.Default.Root.Children.Add(rectangle);
        if (drawFirst)
        {
            GumService.Default.Draw();
        }
        beforeSecondDraw(rectangle);
        surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Draw();
        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        using SKBitmap bitmap = SKBitmap.Decode(data);
        GumService.Default.Root.Children.Clear();
        return bitmap.Bytes;
    }

    [Fact]
    public void SameInstance_GradientChangedBetweenFrames_MatchesFreshInstance()
    {
        byte[] changedAfterDraw = Render(ChangeToLinearRedToBlue, drawFirst: true);
        byte[] fresh = Render(ChangeToLinearRedToBlue, drawFirst: false);
        byte[] unchanged = Render(_ => { }, drawFirst: true);

        changedAfterDraw.ShouldBe(fresh);
        changedAfterDraw.ShouldNotBe(unchanged);
    }

    [Fact]
    public void TwoShapes_DifferentGradients_DoNotShareStops()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(128, 64));
        GumService.Default.Initialize(surface.Canvas, 128, 64);
        GumService.Default.Root.Children.Clear();
        RoundedRectangleRuntime linear = CreateRadialGreenToYellow();
        ChangeToLinearRedToBlue(linear);
        RoundedRectangleRuntime radial = CreateRadialGreenToYellow();
        radial.X = 68;
        GumService.Default.Root.Children.Add(linear);
        GumService.Default.Root.Children.Add(radial);
        GumService.Default.Draw();
        GumService.Default.Draw();

        using SKBitmap bitmap = new SKBitmap(128, 64);
        surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
        SKColor linearStart = bitmap.GetPixel(5, 5);
        SKColor radialCenter = bitmap.GetPixel(68 + 28, 4 + 28);
        GumService.Default.Root.Children.Clear();

        linearStart.Red.ShouldBeGreaterThan((byte)200);
        linearStart.Green.ShouldBeLessThan((byte)30);
        radialCenter.Green.ShouldBeGreaterThan((byte)200);
        radialCenter.Red.ShouldBeLessThan((byte)30);
    }
}
