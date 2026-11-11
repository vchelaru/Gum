using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A shape keeps one <see cref="SKPaint"/> and rebuilds it in place when a value it is made from
/// changes, so an animated property allocates no paint per frame. These pin that the object is reused
/// and that nothing from the previous settings is left on it.
/// </summary>
public class ShapePaintReuseTests
{
    private static readonly SKRect Bounds = new SKRect(0, 0, 100, 100);

    private sealed class ProbeShape : RoundedRectangle
    {
        public SKPaint Paint() => GetCachedPaint(Bounds, absoluteRotation: 0);
        public SKPaint? Shadow() => GetShadowPaint();
    }

    private sealed class ProbeArc : Arc
    {
        public SKPaint Paint() => GetCachedPaint(Bounds, absoluteRotation: 0);
    }

    [Fact]
    public void GetCachedPaint_NothingChanged_ReturnsTheSamePaintWithoutRebuilding()
    {
        ProbeShape shape = new ProbeShape();
        SKPaint first = shape.Paint();

        shape.Paint().ShouldBeSameAs(first);
    }

    [Fact]
    public void GetCachedPaint_ColorChanged_ReusesThePaintWithTheNewColor()
    {
        ProbeShape shape = new ProbeShape();
        shape.Color = SKColors.Red;
        SKPaint first = shape.Paint();

        shape.Color = SKColors.Blue;
        SKPaint second = shape.Paint();

        second.ShouldBeSameAs(first);
        second.Color.ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void GetCachedPaint_SettingTheSameValuesAgain_DoesNotRebuild()
    {
        ProbeShape shape = new ProbeShape();
        shape.Color = SKColors.Red;
        shape.StrokeWidth = 9;
        SKPaint paint = shape.Paint();
        // A rebuild would overwrite this marker with the shape's own color.
        paint.Color = SKColors.Green;

        shape.Color = SKColors.Red;
        shape.StrokeWidth = 9;

        shape.Paint().Color.ShouldBe(SKColors.Green);
    }

    // Unequal X and Y blur keeps the image filter, which the blurred shadow draw does not cover. The
    // same-blur case is pinned by the mask filter test below.
    [Fact]
    public void GetCachedPaint_DropshadowTurnedOff_LeavesNoImageFilter()
    {
        ProbeShape shape = new ProbeShape();
        shape.HasDropshadow = true;
        shape.DropshadowBlurX = 4;
        shape.DropshadowBlurY =  6;
        SKPaint first = shape.Paint();
        first.ImageFilter.ShouldNotBeNull();

        shape.HasDropshadow = false;
        SKPaint second = shape.Paint();

        second.ShouldBeSameAs(first);
        second.ImageFilter.ShouldBeNull();
    }

    [Fact]
    public void GetCachedPaint_BlurredShadowDrawTurnedOff_LeavesNoShadowPaintAndNoImageFilter()
    {
        ProbeShape shape = new ProbeShape();
        shape.HasDropshadow = true;
        shape.DropshadowBlurX = 4;
        shape.DropshadowBlurY = 4;
        SKPaint first = shape.Paint();
        first.ImageFilter.ShouldBeNull();
        shape.Shadow().ShouldNotBeNull();
        shape.Shadow()!.MaskFilter.ShouldNotBeNull();

        shape.HasDropshadow = false;
        SKPaint second = shape.Paint();

        second.ShouldBeSameAs(first);
        second.ImageFilter.ShouldBeNull();
        shape.Shadow().ShouldBeNull();
    }

    [Fact]
    public void GetCachedPaint_GradientTurnedOff_LeavesNoShader()
    {
        ProbeShape shape = new ProbeShape();
        shape.UseGradient = true;
        SKPaint first = shape.Paint();
        first.Shader.ShouldNotBeNull();

        shape.UseGradient = false;
        SKPaint second = shape.Paint();

        second.ShouldBeSameAs(first);
        second.Shader.ShouldBeNull();
    }

    [Fact]
    public void GetCachedPaint_DashTurnedOff_LeavesNoPathEffect()
    {
        ProbeShape shape = new ProbeShape();
        shape.IsFilled = false;
        shape.StrokeDashLength = 4;
        shape.StrokeGapLength = 2;
        SKPaint first = shape.Paint();
        first.PathEffect.ShouldNotBeNull();

        shape.StrokeDashLength = 0;
        SKPaint second = shape.Paint();

        second.ShouldBeSameAs(first);
        second.PathEffect.ShouldBeNull();
    }

    [Fact]
    public void GetCachedPaint_ArcEndRoundingKeptAcrossARebuild()
    {
        ProbeArc arc = new ProbeArc();
        arc.IsEndRounded = true;
        SKPaint first = arc.Paint();

        arc.Color = SKColors.Blue;
        SKPaint second = arc.Paint();

        second.ShouldBeSameAs(first);
        second.StrokeCap.ShouldBe(SKStrokeCap.Round);
    }

    [Fact]
    public void Dispose_ThenDrawingAgain_BuildsAFreshPaint()
    {
        ProbeShape shape = new ProbeShape();
        SKPaint first = shape.Paint();

        shape.Dispose();
        SKPaint second = shape.Paint();

        second.ShouldNotBeSameAs(first);
    }
}
