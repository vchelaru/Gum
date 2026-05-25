using Gum.RenderingLibrary;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies that <see cref="Gum.RenderingLibrary.Blend"/> set on a Skia renderable is honored
/// at paint construction time as <see cref="SKBlendMode"/>. Before this wiring, the property
/// was stored on the renderable but never read, so e.g. setting <c>Blend = Additive</c>
/// silently produced normal alpha blending.
/// </summary>
public class BlendTests
{
    [Fact]
    public void RenderableShapeBase_Blend_DefaultsToNull()
    {
        TestableSprite sut = new();

        sut.Blend.ShouldBeNull();
    }

    [Fact]
    public void Sprite_GetPaint_NullBlend_LeavesDefaultSrcOver()
    {
        TestableSprite sut = new();
        sut.Blend = null;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.SrcOver);
    }

    [Fact]
    public void Sprite_GetPaint_Normal_MapsToSrcOver()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.Normal;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.SrcOver);
    }

    [Fact]
    public void Sprite_GetPaint_Additive_MapsToPlus()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.Additive;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.Plus);
    }

    [Fact]
    public void Sprite_GetPaint_Replace_MapsToSrc()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.Replace;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.Src);
    }

    [Fact]
    public void Sprite_GetPaint_SubtractAlpha_MapsToDstOut()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.SubtractAlpha;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.DstOut);
    }

    // ReplaceAlpha and MinAlpha have no clean SkiaSharp equivalent. Mirror the Raylib precedent
    // (see Runtimes/RaylibGum/Renderables/BlendModeExtensions.cs) and fall through to the default
    // SrcOver rather than picking a Skia mode that would silently change visuals.
    [Fact]
    public void Sprite_GetPaint_ReplaceAlpha_FallsThroughToSrcOver()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.ReplaceAlpha;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.SrcOver);
    }

    [Fact]
    public void Sprite_GetPaint_MinAlpha_FallsThroughToSrcOver()
    {
        TestableSprite sut = new();
        sut.Blend = Blend.MinAlpha;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.SrcOver);
    }

    // Blend lives on RenderableShapeBase so every shape derivative gets it, not just Sprite/NineSlice.
    // Spot-check NineSlice and a non-texture shape (Circle) to lock that in.
    [Fact]
    public void NineSlice_GetPaint_Additive_MapsToPlus()
    {
        TestableNineSlice sut = new();
        sut.Blend = Blend.Additive;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.Plus);
    }

    [Fact]
    public void Circle_GetPaint_Additive_MapsToPlus()
    {
        TestableCircle sut = new();
        sut.Blend = Blend.Additive;

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.BlendMode.ShouldBe(SKBlendMode.Plus);
    }

    private sealed class TestableSprite : Sprite
    {
        public SKPaint InvokeGetPaint(SKRect boundingRect, float absoluteRotation)
            => GetPaint(boundingRect, absoluteRotation);
    }

    private sealed class TestableNineSlice : NineSlice
    {
        public SKPaint InvokeGetPaint(SKRect boundingRect, float absoluteRotation)
            => GetPaint(boundingRect, absoluteRotation);
    }

    private sealed class TestableCircle : Circle
    {
        public SKPaint InvokeGetPaint(SKRect boundingRect, float absoluteRotation)
            => GetPaint(boundingRect, absoluteRotation);
    }
}
