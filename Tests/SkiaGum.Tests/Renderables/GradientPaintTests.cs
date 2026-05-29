using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Issue #2998 — the gradient's visibility is driven by its own stop alphas, not the slot's solid
/// fill color. SkiaSharp modulates a shader's output by <c>SKPaint.Color.alpha</c>, so a gradient
/// fill whose solid color is transparent (the new RectangleRuntime / CircleRuntime default) would
/// be modulated to nothing. <c>GetPaint</c> forces the paint opaque whenever a gradient is active so
/// the stop alphas carry visibility — matching the Apos <c>ShouldPaintGradient</c> and raylib
/// <c>ShouldPaintFillGradient</c> gates. The gate lives in the shared <c>RenderableShapeBase.GetPaint</c>,
/// so testing one concrete shape exercises every Skia shape (Arc / Line override GetPaint but call base).
/// </summary>
public class GradientPaintTests
{
    [Fact]
    public void GetPaint_GradientWithTransparentSolidFill_PaintAlphaNotSuppressed()
    {
        TestableRoundedRectangle sut = new()
        {
            UseGradient = true,
            Color = new SKColor(0, 0, 0, 0),
            Alpha1 = 255,
            Alpha2 = 255,
        };

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.Shader.ShouldNotBeNull();
        paint.Color.Alpha.ShouldBe((byte)255);
    }

    [Fact]
    public void GetPaint_NoGradient_TransparentSolidFillPreserved()
    {
        // Without a gradient the opaque-forcing must not kick in — a transparent solid fill stays
        // transparent (invisible solid fill).
        TestableRoundedRectangle sut = new()
        {
            UseGradient = false,
            Color = new SKColor(0, 0, 0, 0),
        };

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.Color.Alpha.ShouldBe((byte)0);
    }

    private sealed class TestableRoundedRectangle : RoundedRectangle
    {
        public SKPaint InvokeGetPaint(SKRect boundingRect, float absoluteRotation)
            => GetPaint(boundingRect, absoluteRotation);
    }
}
