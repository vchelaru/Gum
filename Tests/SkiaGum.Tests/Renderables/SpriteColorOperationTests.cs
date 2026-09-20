using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Skia had no <see cref="ColorOperation"/> (Modulate/ColorTextureAlpha) or <c>Add</c>-color-operation
/// support of any kind (#4821 gap 3) — <see cref="Sprite.GetPaint"/> always blended with
/// <see cref="SKBlendMode.Modulate"/> and <see cref="AnimationFrameColorOperation.Add"/> frames were
/// silently dropped. ColorTextureAlpha is implemented as <see cref="SKBlendMode.SrcIn"/> (masks the
/// tint by the drawn pixel's own alpha, discarding its RGB — matching MonoGame/raylib's technique).
/// Add is a single-pass fix (the issue's own plan): a color-matrix filter that adds the additive
/// tint's RGB on top of the Modulate/ColorTextureAlpha result, composed via
/// <see cref="SKColorFilter.CreateCompose(SKColorFilter, SKColorFilter)"/>, rather than a second draw.
/// </summary>
public class SpriteColorOperationTests
{
    // Actually draws the filtered pixel through Skia (DrawBitmap + the real paint) and reads it back,
    // rather than guessing at ColorFilter math - SKColorFilter has no direct "filter this one color"
    // API in this SkiaSharp version.
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
    public void ColorOperation_DefaultsToModulate()
    {
        Sprite sut = new();

        sut.ColorOperation.ShouldBe(ColorOperation.Modulate);
    }

    [Fact]
    public void GetPaint_Modulate_MultipliesTextureRgbByTint()
    {
        TestableSprite sut = new() { Color = new SKColor(255, 0, 0, 255) };

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 10, 10), absoluteRotation: 0);
        SKColor filtered = RenderFilteredPixel(paint.ColorFilter!, new SKColor(0, 0, 255, 255));

        // Opaque blue texel * red tint => black (RGB), matching the raylib/MonoGame Modulate pin.
        filtered.Red.ShouldBeLessThan((byte)10);
        filtered.Blue.ShouldBeLessThan((byte)10);
    }

    [Fact]
    public void GetPaint_ColorTextureAlpha_FillsWithTintColorIgnoringTextureRgb()
    {
        TestableSprite sut = new()
        {
            Color = new SKColor(255, 0, 0, 255),
            ColorOperation = ColorOperation.ColorTextureAlpha,
        };

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 10, 10), absoluteRotation: 0);
        SKColor filtered = RenderFilteredPixel(paint.ColorFilter!, new SKColor(0, 0, 255, 255));

        // The blue texel's RGB is discarded; filled with the red tint, masked by the texel's
        // (fully opaque) alpha - matching SpriteColorOperationTests on raylib.
        filtered.Red.ShouldBeGreaterThan((byte)200);
        filtered.Green.ShouldBeLessThan((byte)10);
        filtered.Blue.ShouldBeLessThan((byte)10);
    }

    [Fact]
    public void ApplyAnimationFrame_Add_SetsColorAndAddOperation()
    {
        Sprite sut = new();

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
        // A later frame with no Add must not leave ColorOperation stuck on Add.
        Sprite sut = new();

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
        TestableSprite sut = new() { Color = SKColors.White };
        sut.ApplyAddFrameForTest(new SKColor(50, 0, 0));

        using SKPaint paint = sut.InvokeGetPaint(new SKRect(0, 0, 10, 10), absoluteRotation: 0);
        SKColor filtered = RenderFilteredPixel(paint.ColorFilter!, new SKColor(10, 10, 10, 255));

        // Add ignores the modulate tint and adds the sprite's own Color - set to (50,0,0) by the
        // frame - onto the texel at (10,10,10).
        filtered.Red.ShouldBe((byte)60);
        filtered.Green.ShouldBe((byte)10);
        filtered.Blue.ShouldBe((byte)10);
    }

    private sealed class TestableSprite : Sprite
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
