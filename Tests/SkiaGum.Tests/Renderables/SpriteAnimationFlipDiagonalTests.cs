using Gum;
using Gum.GueDeriving;
using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A frame's FlipDiagonal and RenderColorOperation reach the drawn Skia sprite, as they do on
/// MonoGame (#6036).
/// </summary>
public class SpriteAnimationFlipDiagonalTests
{
    public SpriteAnimationFlipDiagonalTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    // Four quadrants: top-left red, top-right green, bottom-left blue, bottom-right white.
    private static SKBitmap CreateQuadrantTexture()
    {
        SKBitmap texture = new(4, 4);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                SKColor color = (x < 2, y < 2) switch
                {
                    (true, true) => SKColors.Red,
                    (false, true) => SKColors.Lime,
                    (true, false) => SKColors.Blue,
                    _ => SKColors.White,
                };
                texture.SetPixel(x, y, color);
            }
        }
        return texture;
    }

    private static SpriteRuntime CreateSprite(AnimationFrame frame, float width, float height)
    {
        AnimationChain chain = new() { Name = "c" };
        chain.Add(frame);

        SpriteRuntime sprite = new()
        {
            X = 10,
            Y = 10,
            Width = width,
            Height = height,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
        };
        sprite.AnimationChains = new AnimationChainList { chain };
        sprite.CurrentChainName = "c";
        sprite.Animate = true;
        sprite.AnimateSelf(0.0);
        return sprite;
    }

    private static SKBitmap RenderQuadrants(bool flipDiagonal, bool flipHorizontal, float width, float height, bool flipVertical = false, float rotation = 0, float x = 10, float y = 10)
    {
        using SKBitmap texture = CreateQuadrantTexture();

        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        SpriteRuntime sprite = CreateSprite(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            FlipDiagonal = flipDiagonal,
            FlipHorizontal = flipHorizontal,
            FlipVertical = flipVertical,
        }, width, height);
        sprite.X = x;
        sprite.Y = y;
        sprite.Rotation = rotation;
        GumService.Default.Root.Children.Add(sprite);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    [Fact]
    public void Render_FlipDiagonal_SwapsTopRightAndBottomLeft()
    {
        using SKBitmap pixels = RenderQuadrants(flipDiagonal: true, flipHorizontal: false, width: 20, height: 20);

        // The sprite covers x 10..30, y 10..30. A transpose fixes the top-left and bottom-right
        // corners and swaps the top-right and bottom-left ones.
        pixels.GetPixel(15, 15).ShouldBe(SKColors.Red);
        pixels.GetPixel(25, 15).ShouldBe(SKColors.Blue);
        pixels.GetPixel(15, 25).ShouldBe(SKColors.Lime);
        pixels.GetPixel(25, 25).ShouldBe(SKColors.White);
    }

    [Fact]
    public void Render_FlipHorizontal_MirrorsTheFrameInsideItsOwnBounds()
    {
        using SKBitmap pixels = RenderQuadrants(flipDiagonal: false, flipHorizontal: true, width: 20, height: 20);

        pixels.GetPixel(15, 15).ShouldBe(SKColors.Lime);
        pixels.GetPixel(25, 15).ShouldBe(SKColors.Red);
        pixels.GetPixel(15, 25).ShouldBe(SKColors.White);
        pixels.GetPixel(25, 25).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void Render_FlipVertical_MirrorsTheFrameInsideItsOwnBounds()
    {
        using SKBitmap pixels = RenderQuadrants(flipDiagonal: false, flipHorizontal: false, width: 20, height: 20, flipVertical: true);

        pixels.GetPixel(15, 15).ShouldBe(SKColors.Blue);
        pixels.GetPixel(25, 15).ShouldBe(SKColors.White);
        pixels.GetPixel(15, 25).ShouldBe(SKColors.Red);
        pixels.GetPixel(25, 25).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Render_NoFlipDiagonal_DrawsFrameUnflipped()
    {
        using SKBitmap pixels = RenderQuadrants(flipDiagonal: false, flipHorizontal: false, width: 20, height: 20);

        pixels.GetPixel(25, 15).ShouldBe(SKColors.Lime);
        pixels.GetPixel(15, 25).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void Render_FlipDiagonalOnNonSquareSprite_StaysInsideItsOwnBounds()
    {
        using SKBitmap pixels = RenderQuadrants(flipDiagonal: true, flipHorizontal: false, width: 30, height: 10);

        // The sprite covers x 10..40, y 10..20 and its footprint does not change.
        pixels.GetPixel(15, 12).ShouldBe(SKColors.Red);
        pixels.GetPixel(35, 18).ShouldBe(SKColors.White);
        pixels.GetPixel(35, 12).ShouldBe(SKColors.Blue);
        pixels.GetPixel(15, 18).ShouldBe(SKColors.Lime);
        pixels.GetPixel(25, 25).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Render_FlipDiagonalWithFlipHorizontal_FlipsTheSourceBeforeTransposing()
    {
        using SKBitmap pixels = RenderQuadrants(flipDiagonal: true, flipHorizontal: true, width: 20, height: 20);

        // Flipping horizontally puts green, red / white, blue; transposing that gives
        // green, white / red, blue.
        pixels.GetPixel(15, 15).ShouldBe(SKColors.Lime);
        pixels.GetPixel(25, 15).ShouldBe(SKColors.White);
        pixels.GetPixel(15, 25).ShouldBe(SKColors.Red);
        pixels.GetPixel(25, 25).ShouldBe(SKColors.Blue);
    }

    private static SkiaGum.Renderables.Sprite CreateAnimatedRenderable(AnimationFrame frame)
    {
        AnimationChain chain = new() { Name = "c" };
        chain.Add(frame);

        SkiaGum.Renderables.Sprite sprite = new();
        sprite.AnimationLogic.AnimationChains = new AnimationChainList { chain };
        sprite.AnimationLogic.CurrentChainName = "c";
        sprite.AnimationLogic.Animate = true;
        sprite.AnimateSelf(0.0);
        return sprite;
    }

    [Fact]
    public void Render_FlipDiagonalOnRotatedSprite_TransposesInTheSpritesOwnAxes()
    {
        using SKBitmap pixels = RenderQuadrants(flipDiagonal: true, flipHorizontal: false, width: 20, height: 20, rotation: 90, x: 30, y: 40);

        // Rotated 90 degrees counter-clockwise about its top-left, the sprite covers x 30..50,
        // y 20..40: its own right edge points up and its own bottom edge points right. The
        // transpose still fixes its own top-left (red, now bottom-left on screen) and bottom-right
        // (white, top-right) and swaps the other two.
        pixels.GetPixel(35, 35).ShouldBe(SKColors.Red);
        pixels.GetPixel(45, 25).ShouldBe(SKColors.White);
        pixels.GetPixel(35, 25).ShouldBe(SKColors.Blue);
        pixels.GetPixel(45, 35).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void ApplyAnimationFrame_RenderColorOperation_SetsColorOperation()
    {
        SkiaGum.Renderables.Sprite sprite = CreateAnimatedRenderable(new AnimationFrame
        {
            FrameLength = 1f,
            RenderColorOperation = ColorOperation.ColorTextureAlpha,
        });

        sprite.ColorOperation.ShouldBe(ColorOperation.ColorTextureAlpha);
    }

    [Fact]
    public void ApplyAnimationFrame_FrameColorOperationWinsOverRenderColorOperation()
    {
        SkiaGum.Renderables.Sprite sprite = CreateAnimatedRenderable(new AnimationFrame
        {
            FrameLength = 1f,
            RenderColorOperation = ColorOperation.ColorTextureAlpha,
            ColorOperation = AnimationFrameColorOperation.Add,
        });

        sprite.ColorOperation.ShouldBe(ColorOperation.Add);
    }

    [Fact]
    public void ApplyAnimationFrame_NoRenderColorOperation_LeavesColorOperationAlone()
    {
        SkiaGum.Renderables.Sprite sprite = new() { ColorOperation = ColorOperation.ColorTextureAlpha };
        AnimationChain chain = new() { Name = "c" };
        chain.Add(new AnimationFrame { FrameLength = 1f });
        sprite.AnimationLogic.AnimationChains = new AnimationChainList { chain };
        sprite.AnimationLogic.CurrentChainName = "c";
        sprite.AnimationLogic.Animate = true;
        sprite.AnimateSelf(0.0);

        sprite.ColorOperation.ShouldBe(ColorOperation.ColorTextureAlpha);
    }
}
