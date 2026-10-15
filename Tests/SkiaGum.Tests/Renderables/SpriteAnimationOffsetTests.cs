using Gum;
using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A frame's RelativeX/RelativeY moves the drawn sprite on Skia, as it does on MonoGame (#6034).
/// </summary>
public class SpriteAnimationOffsetTests
{
    public SpriteAnimationOffsetTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static SKBitmap Render(float relativeX, float relativeY, float rotation)
    {
        using SKBitmap texture = new(4, 4);
        using SKCanvas fill = new(texture);
        fill.Clear(SKColors.Red);

        AnimationChain chain = new() { Name = "c" };
        chain.Add(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            RelativeX = relativeX,
            RelativeY = relativeY,
        });

        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        SpriteRuntime sprite = new()
        {
            X = 20,
            Y = 40,
            Width = 10,
            Height = 10,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            Rotation = rotation,
        };
        sprite.AnimationChains = new AnimationChainList { chain };
        sprite.CurrentChainName = "c";
        sprite.Animate = true;
        sprite.AnimateSelf(0.0);
        GumService.Default.Root.Children.Add(sprite);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    [Fact]
    public void Render_FrameOffset_ShiftsSpriteRightAndUp()
    {
        using SKBitmap pixels = Render(relativeX: 10, relativeY: 5, rotation: 0);

        // Un-offset the sprite would cover x 20..30, y 40..50; the offset moves it to x 30..40, y 35..45.
        pixels.GetPixel(35, 37).ShouldBe(SKColors.Red);
        pixels.GetPixel(22, 47).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Render_FrameOffsetOnRotatedSprite_ShiftsAlongTheSpritesOwnAxes()
    {
        using SKBitmap pixels = Render(relativeX: 10, relativeY: 0, rotation: 90);

        // Un-offset, a 90 degree sprite covers x 20..30, y 30..40; its own +X axis points up, so
        // the offset moves it to y 20..30.
        pixels.GetPixel(25, 25).ShouldBe(SKColors.Red);
        pixels.GetPixel(25, 35).Alpha.ShouldBe((byte)0);
    }
}
