using RenderingLibrary;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A gradient shape whose stops share one alpha has a uniform silhouette, so it takes the blurred mask-filter
/// shadow draw like a solid shape (#8192). Stops with different alphas keep the image filter.
/// </summary>
public class GradientDropshadowTests
{
    private const int Size = 200;
    private static readonly SKColor DropColor = new SKColor(0, 0, 0, 160);

    private sealed class ProbeShape : RoundedRectangle
    {
        public SKPaint Paint() => GetCachedPaint(new SKRect(20, 20, 140, 120), absoluteRotation: 0);
        public SKPaint? Shadow() => GetShadowPaint();
    }

    private sealed class ProbeCircle : Circle
    {
        public SKPaint Paint() => GetCachedPaint(new SKRect(20, 20, 120, 120), absoluteRotation: 0);
        public SKPaint? Shadow() => GetShadowPaint();
    }

    private static void ConfigureGradient(RoundedRectangle shape, int alpha1, int alpha2)
    {
        shape.X = 20;
        shape.Y = 20;
        shape.Width = 120;
        shape.Height = 100;
        shape.CornerRadius = 8;
        shape.UseGradient = true;
        shape.Red1 = 250; shape.Green1 = 60; shape.Blue1 = 40; shape.Alpha1 = alpha1;
        shape.Red2 = 30; shape.Green2 = 80; shape.Blue2 = 240; shape.Alpha2 = alpha2;
        shape.GradientX1 = 0; shape.GradientY1 = 0;
        shape.GradientX2 = 0; shape.GradientY2 = 100;
        shape.HasDropshadow = true;
        shape.DropshadowOffsetX = 5;
        shape.DropshadowOffsetY = 8;
        shape.DropshadowBlurX = 16;
        shape.DropshadowBlurY = 16;
        shape.DropshadowColor = DropColor;
    }

    [Fact]
    public void Shadow_GradientWithEqualStopAlphas_UsesTheBlurredDraw()
    {
        ProbeShape shape = new ProbeShape();
        ConfigureGradient(shape, 255, 255);

        SKPaint paint = shape.Paint();

        shape.Shadow().ShouldNotBeNull();
        paint.ImageFilter.ShouldBeNull();
        paint.Shader.ShouldNotBeNull();
    }

    [Fact]
    public void Shadow_CircleGradientWithEqualStopAlphas_UsesTheBlurredDraw()
    {
        ProbeCircle circle = new ProbeCircle();
        circle.Width = 100;
        circle.Height = 100;
        circle.UseGradient = true;
        circle.HasDropshadow = true;
        circle.DropshadowBlurX = 10;
        circle.DropshadowBlurY = 10;

        SKPaint paint = circle.Paint();

        circle.Shadow().ShouldNotBeNull();
        paint.ImageFilter.ShouldBeNull();
    }

    [Theory]
    [InlineData(255, 128)]
    [InlineData(0, 255)]
    public void Shadow_GradientWithDifferentStopAlphas_KeepsTheImageFilter(int alpha1, int alpha2)
    {
        ProbeShape shape = new ProbeShape();
        ConfigureGradient(shape, alpha1, alpha2);

        SKPaint paint = shape.Paint();

        shape.Shadow().ShouldBeNull();
        paint.ImageFilter.ShouldNotBeNull();
    }

    [Fact]
    public void Shadow_StopAlphasChangeToDifferAfterFirstDraw_SwitchesToTheImageFilter()
    {
        ProbeShape shape = new ProbeShape();
        ConfigureGradient(shape, 255, 255);
        shape.Paint().ImageFilter.ShouldBeNull();

        shape.Alpha2 = 100;
        shape.Paint().ImageFilter.ShouldNotBeNull();
        shape.Shadow().ShouldBeNull();

        shape.Alpha2 = 255;
        shape.Paint().ImageFilter.ShouldBeNull();
        shape.Shadow().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(255, 160)]
    [InlineData(128, 80)]
    [InlineData(51, 32)]
    public void Shadow_GradientWithEqualStopAlphas_ShadowAlphaScalesByTheStopAlpha(int stopAlpha, int expectedShadowAlpha)
    {
        ProbeShape shape = new ProbeShape();
        ConfigureGradient(shape, stopAlpha, stopAlpha);

        shape.Paint();

        shape.Shadow()!.Color.Alpha.ShouldBe((byte)expectedShadowAlpha);
    }

    private static SKBitmap RenderNew(int alpha1, int alpha2)
    {
        SKBitmap bitmap = new SKBitmap(Size, Size);
        using SKCanvas canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(60, 120, 200, 255));
        SystemManagers managers = new SystemManagers();
        managers.Canvas = canvas;
        RoundedRectangle shape = new RoundedRectangle();
        ConfigureGradient(shape, alpha1, alpha2);
        shape.Render(managers);
        return bitmap;
    }

    // What the shape drew before: its own gradient shader with the drop-shadow image filter on the paint.
    internal static SKBitmap RenderImageFilterReference(int alpha1, int alpha2)
    {
        SKBitmap bitmap = new SKBitmap(Size, Size);
        using SKCanvas canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(60, 120, 200, 255));
        ProbeShape probe = new ProbeShape();
        ConfigureGradient(probe, alpha1, alpha2);
        SKRect rect = new SKRect(20, 20, 140, 120);
        using SKShader shader = SKShader.CreateLinearGradient(
            new SKPoint(20, 20), new SKPoint(20, 120),
            new[] { new SKColor(250, 60, 40, (byte)alpha1), new SKColor(30, 80, 240, (byte)alpha2) },
            new float[] { 0, 1 }, SKShaderTileMode.Clamp);
        using SKImageFilter filter = SKImageFilter.CreateDropShadow(5, 8,
            global::Gum.Wireframe.DropshadowBlur.GetSigma(16), global::Gum.Wireframe.DropshadowBlur.GetSigma(16), DropColor);
        using SKPaint paint = new SKPaint { Color = SKColors.White, Shader = shader, IsAntialias = true, ImageFilter = filter };
        canvas.DrawRoundRect(rect, 8, 8, paint);
        return bitmap;
    }

    [Theory]
    [InlineData(255, 255)]
    [InlineData(128, 128)]
    [InlineData(255, 254)]
    [InlineData(255, 128)]
    public void Render_GradientDropshadow_MatchesTheImageFilterOutput(int alpha1, int alpha2)
    {
        using SKBitmap actual = RenderNew(alpha1, alpha2);
        using SKBitmap expected = RenderImageFilterReference(alpha1, alpha2);

        // Same tolerance as the solid-fill shapes: the mask filter differs from the image filter by up to 2 levels
        // on the blur falloff and the anti-aliased rim. The differing-alpha cases still take the image filter,
        // so they must match exactly.
        int tolerance = alpha1 == alpha2 ? 2 : 0;
        DropshadowBlendTests.MaxDifference(actual, expected).ShouldBeLessThanOrEqualTo(tolerance);
    }

    // The shape with the blurred draw turned off: the image filter path every gradient shape took before.
    private sealed class ImageFilterCircle : Circle
    {
        protected override bool DrawsBlurredShadow => false;
    }

    private sealed class ImageFilterRectangle : RoundedRectangle
    {
        protected override bool DrawsBlurredShadow => false;
    }

    private static void ConfigureCircle(Circle circle)
    {
        circle.X = 30;
        circle.Y = 30;
        circle.Width = 110;
        circle.Height = 110;
        circle.UseGradient = true;
        circle.Red1 = 250; circle.Green1 = 60; circle.Blue1 = 40; circle.Alpha1 = 255;
        circle.Red2 = 30; circle.Green2 = 80; circle.Blue2 = 240; circle.Alpha2 = 255;
        circle.GradientX1 = 0; circle.GradientY1 = 0;
        circle.GradientX2 = 0; circle.GradientY2 = 110;
        circle.HasDropshadow = true;
        circle.DropshadowOffsetX = 5;
        circle.DropshadowOffsetY = 8;
        circle.DropshadowBlurX = 16;
        circle.DropshadowBlurY = 16;
        circle.DropshadowColor = DropColor;
    }

    private static SKBitmap Draw(RenderableShapeBase shape)
    {
        SKBitmap bitmap = new SKBitmap(Size, Size);
        using SKCanvas canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(60, 120, 200, 255));
        SystemManagers managers = new SystemManagers();
        managers.Canvas = canvas;
        shape.Render(managers);
        return bitmap;
    }

    [Fact]
    public void Render_CircleGradientDropshadow_MatchesTheImageFilterOutput()
    {
        Circle actual = new Circle();
        ConfigureCircle(actual);
        ImageFilterCircle expected = new ImageFilterCircle();
        ConfigureCircle(expected);

        using SKBitmap actualBitmap = Draw(actual);
        using SKBitmap expectedBitmap = Draw(expected);

        DropshadowBlendTests.MaxDifference(actualBitmap, expectedBitmap).ShouldBeLessThanOrEqualTo(2);
    }

    private static void ConfigureStroke(RoundedRectangle shape)
    {
        ConfigureGradient(shape, 255, 255);
        shape.IsFilled = false;
        shape.StrokeWidth = 10;
    }

    [Fact]
    public void Render_StrokeOnlyGradientDropshadow_MatchesTheImageFilterOutput()
    {
        RoundedRectangle actual = new RoundedRectangle();
        ConfigureStroke(actual);
        ImageFilterRectangle expected = new ImageFilterRectangle();
        ConfigureStroke(expected);

        using SKBitmap actualBitmap = Draw(actual);
        using SKBitmap expectedBitmap = Draw(expected);

        DropshadowBlendTests.MaxDifference(actualBitmap, expectedBitmap).ShouldBeLessThanOrEqualTo(2);
    }
}
