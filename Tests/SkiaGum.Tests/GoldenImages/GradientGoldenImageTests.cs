using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.GoldenImages;

/// <summary>
/// Pins the pixels of linear and radial gradients, so a change to how the gradient shader is built must
/// render identically.
/// </summary>
public class GradientGoldenImageTests
{
    public GradientGoldenImageTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    // RenderableShapeBase reuses its stop arrays for every shader it builds, which is safe only because
    // Skia copies the stops when the shader is created.
    [Fact]
    public void GradientShader_StopArraysChangedAfterCreation_KeepsCreationColors()
    {
        SKColor[] colors = { SKColors.Red, SKColors.Red };
        float[] positions = { 0, 1 };
        using SKShader shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(8, 0), colors, positions, SKShaderTileMode.Clamp);
        colors[0] = SKColors.Blue;
        colors[1] = SKColors.Blue;
        positions[0] = 0.5f;

        using SKBitmap bitmap = new SKBitmap(8, 8);
        using (SKCanvas canvas = new SKCanvas(bitmap))
        using (SKPaint paint = new SKPaint { Shader = shader })
        {
            canvas.DrawRect(0, 0, 8, 8, paint);
        }

        bitmap.GetPixel(4, 4).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void LinearAndRadialGradients_MatchGoldenBaseline()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(128, 64));
        GumService.Default.Initialize(surface.Canvas, 128, 64);

        RoundedRectangleRuntime linear = new()
        {
            X = 4,
            Y = 4,
            Width = 56,
            Height = 56,
            CornerRadius = 0,
            UseGradient = true,
            GradientType = GradientType.Linear,
            GradientX2 = 56,
            GradientY2 = 56,
            Red1 = 255, Green1 = 0, Blue1 = 0, Alpha1 = 255,
            Red2 = 0, Green2 = 0, Blue2 = 255, Alpha2 = 255,
        };
        GumService.Default.Root.Children.Add(linear);

        RoundedRectangleRuntime radial = new()
        {
            X = 68,
            Y = 4,
            Width = 56,
            Height = 56,
            CornerRadius = 0,
            UseGradient = true,
            GradientType = GradientType.Radial,
            GradientX1 = 28,
            GradientY1 = 28,
            GradientInnerRadius = 4,
            GradientOuterRadius = 28,
            Red1 = 0, Green1 = 255, Blue1 = 0, Alpha1 = 255,
            Red2 = 255, Green2 = 255, Blue2 = 0, Alpha2 = 128,
        };
        GumService.Default.Root.Children.Add(radial);

        GumService.Default.Draw();

        GoldenImageAssert.Matches(surface, "Gradient_LinearAndRadial", tolerance: 0);
    }
}
