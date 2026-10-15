using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// A frame's RelativeX/RelativeY moves the drawn sprite on raylib, as it does on MonoGame (#6034).
/// Renders into an <see cref="ContainerRuntime.IsRenderTarget"/> container so the baked texture can
/// be sampled, mirroring <see cref="SpriteFlipTests"/>.
/// </summary>
public class SpriteAnimationOffsetTests : BaseTestClass
{
    private static readonly Color PureRed = new((byte)255, (byte)0, (byte)0, (byte)255);

    private static RenderTexture2D Render(float relativeX, float relativeY, float rotation)
    {
        Image image = GenImageColor(4, 4, PureRed);
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

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

        ContainerRuntime container = new();
        container.Width = 64;
        container.Height = 64;
        container.IsRenderTarget = true;

        SpriteRuntime sprite = new();
        sprite.X = 20;
        sprite.Y = 40;
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.Width = 10;
        sprite.Height = 10;
        sprite.Rotation = rotation;
        sprite.AnimationChains = new AnimationChainList { chain };
        sprite.CurrentChainName = "c";
        sprite.Animate = true;
        sprite.AnimateSelf(0.0);
        container.Children.Add(sprite);

        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        return Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
    }

    // A render texture is stored bottom-up in GL, so rows are flipped relative to draw space.
    private static Color ReadPixel(RenderTexture2D renderTexture, int x, int y)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            return GetImageColor(image, x, renderTexture.Texture.Height - 1 - y);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    [Fact]
    public void Render_FrameOffset_ShiftsSpriteRightAndUp()
    {
        RenderTexture2D target = Render(relativeX: 10, relativeY: 5, rotation: 0);

        // Un-offset the sprite would cover x 20..30, y 40..50; the offset moves it to x 30..40, y 35..45.
        ReadPixel(target, 35, 37).R.ShouldBe((byte)255);
        ReadPixel(target, 22, 47).A.ShouldBe((byte)0);
    }

    [Fact]
    public void Render_FrameOffsetOnRotatedSprite_ShiftsAlongTheSpritesOwnAxes()
    {
        RenderTexture2D target = Render(relativeX: 10, relativeY: 0, rotation: 90);

        // Un-offset, a 90 degree sprite covers x 20..30, y 30..40; its own +X axis points up, so
        // the offset moves it to y 20..30.
        ReadPixel(target, 25, 25).R.ShouldBe((byte)255);
        ReadPixel(target, 25, 35).A.ShouldBe((byte)0);
    }
}
