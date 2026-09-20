using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Shouldly;
using SkiaGum.Renderables;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// NineSlice never got the Sprite.cs Add-color treatment (#4821 gap 4) - an authored
/// <see cref="AnimationFrameColorOperation.Add"/> frame silently dropped its additive tint. Mirrors
/// <see cref="SpriteColorOperationTests"/>'s single-pass color-matrix approach.
/// </summary>
public class NineSliceColorOperationTests
{
    private static SKColor RenderFilteredPixel(SKColorFilter filter, SKColor sourcePixel)
    {
        using SKBitmap source = new(1, 1);
        source.SetPixel(0, 0, sourcePixel);

        using SKSurface surface = SKSurface.Create(new SKImageInfo(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        using SKPaint paint = new() { ColorFilter = filter, IsAntialias = false };
        surface.Canvas.DrawBitmap(source, 0, 0, paint);

        using SKImage image = surface.Snapshot();
        using SKBitmap readback = SKBitmap.FromImage(image);
        return readback.GetPixel(0, 0);
    }

    [Fact]
    public void ApplyAnimationFrame_Add_SetsColorAndAddOperation()
    {
        NineSlice sut = new();

        AnimationChain chain = new() { Name = "TestChain" };
        chain.Add(new AnimationFrame { FrameLength = 1.0f, Red = 255, Green = 0, Blue = 0, ColorOperation = AnimationFrameColorOperation.Add });
        AnimationChainList chains = new();
        chains.Add(chain);

        sut.AnimationLogic.AnimationChains = chains;
        sut.AnimationLogic.CurrentChainName = "TestChain";

        sut.ColorOperation.ShouldBe(ColorOperation.Add);
        sut.Color.ShouldBe(new SKColor(255, 0, 0));
    }

    [Fact]
    public void ApplyAnimationFrame_Multiply_RestoresModulate()
    {
        NineSlice sut = new();

        AnimationChain chain = new() { Name = "TestChain" };
        chain.Add(new AnimationFrame { FrameLength = 1.0f, Red = 255, Green = 255, Blue = 255, ColorOperation = AnimationFrameColorOperation.Add });
        chain.Add(new AnimationFrame { FrameLength = 1.0f, ColorOperation = AnimationFrameColorOperation.Multiply });
        AnimationChainList chains = new();
        chains.Add(chain);

        sut.AnimationLogic.AnimationChains = chains;
        sut.AnimationLogic.Animate = true;
        sut.AnimationLogic.CurrentChainName = "TestChain";
        sut.ColorOperation.ShouldBe(ColorOperation.Add);

        sut.AnimationLogic.AnimateSelf(1.5);

        sut.ColorOperation.ShouldBe(ColorOperation.Modulate);
    }

    [Fact]
    public void GetPaint_Add_AddsColorOntoTexel()
    {
        TestableNineSlice sut = new() { Color = SKColors.White };
        sut.ApplyAddFrameForTest(new SKColor(50, 0, 0));

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 10, 10), absoluteRotation: 0);
        SKColor filtered = RenderFilteredPixel(paint.ColorFilter!, new SKColor(10, 10, 10, 255));

        filtered.Red.ShouldBe((byte)60);
        filtered.Green.ShouldBe((byte)10);
        filtered.Blue.ShouldBe((byte)10);
    }

    private sealed class TestableNineSlice : NineSlice
    {
        public SKPaint InvokeGetPaint(SKRect boundingRect, float absoluteRotation)
            => GetPaint(boundingRect, absoluteRotation);

        public void ApplyAddFrameForTest(SKColor addColor)
        {
            AnimationChain chain = new() { Name = "TestChain" };
            chain.Add(new AnimationFrame
            {
                FrameLength = 1.0f,
                Red = addColor.Red,
                Green = addColor.Green,
                Blue = addColor.Blue,
                ColorOperation = AnimationFrameColorOperation.Add,
            });
            AnimationChainList chains = new();
            chains.Add(chain);

            AnimationLogic.AnimationChains = chains;
            AnimationLogic.CurrentChainName = "TestChain";
        }
    }
}
