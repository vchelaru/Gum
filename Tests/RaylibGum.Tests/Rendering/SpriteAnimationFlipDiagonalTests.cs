using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Numerics;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// A frame's FlipDiagonal and RenderColorOperation reach the drawn raylib sprite, as they do on
/// MonoGame (#6036). The pixel tests render into an <see cref="ContainerRuntime.IsRenderTarget"/>
/// container so the baked texture can be sampled, mirroring <see cref="SpriteAnimationOffsetTests"/>.
/// </summary>
public class SpriteAnimationFlipDiagonalTests : BaseTestClass
{
    private static readonly Color PureRed = new((byte)255, (byte)0, (byte)0, (byte)255);
    private static readonly Color PureGreen = new((byte)0, (byte)255, (byte)0, (byte)255);
    private static readonly Color PureBlue = new((byte)0, (byte)0, (byte)255, (byte)255);
    private static readonly Color PureWhite = new((byte)255, (byte)255, (byte)255, (byte)255);

    // Four quadrants: top-left red, top-right green, bottom-left blue, bottom-right white.
    private static Texture2D CreateQuadrantTexture()
    {
        Image image = GenImageColor(4, 4, PureRed);
        ImageDrawRectangle(ref image, 2, 0, 2, 2, PureGreen);
        ImageDrawRectangle(ref image, 0, 2, 2, 2, PureBlue);
        ImageDrawRectangle(ref image, 2, 2, 2, 2, PureWhite);
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);
        return texture;
    }

    private static RenderTexture2D RenderFrame(AnimationFrame frame, float width, float height, float rotation = 0, float x = 10, float y = 10)
    {
        AnimationChain chain = new() { Name = "c" };
        chain.Add(frame);

        ContainerRuntime container = new();
        container.Width = 64;
        container.Height = 64;
        container.IsRenderTarget = true;

        SpriteRuntime sprite = new();
        sprite.X = x;
        sprite.Y = y;
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.Width = width;
        sprite.Height = height;
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

    private static void ShouldBe(Color actual, Color expected)
    {
        (actual.R, actual.G, actual.B).ShouldBe((expected.R, expected.G, expected.B));
    }

    private static void ClearRoot() => Gum.GumService.Default.Root.Children.Clear();

    [Fact]
    public void Render_FlipDiagonal_SwapsTopRightAndBottomLeft()
    {
        Texture2D texture = CreateQuadrantTexture();

        RenderTexture2D target = RenderFrame(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            FlipDiagonal = true,
        }, width: 20, height: 20);

        // The sprite covers x 10..30, y 10..30. A transpose fixes the top-left and bottom-right
        // corners and swaps the top-right and bottom-left ones.
        ShouldBe(ReadPixel(target, 15, 15), PureRed);
        ShouldBe(ReadPixel(target, 25, 15), PureBlue);
        ShouldBe(ReadPixel(target, 15, 25), PureGreen);
        ShouldBe(ReadPixel(target, 25, 25), PureWhite);

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_NoFlipDiagonal_DrawsFrameUnflipped()
    {
        Texture2D texture = CreateQuadrantTexture();

        RenderTexture2D target = RenderFrame(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
        }, width: 20, height: 20);

        ShouldBe(ReadPixel(target, 25, 15), PureGreen);
        ShouldBe(ReadPixel(target, 15, 25), PureBlue);

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FlipDiagonalOnNonSquareSprite_StaysInsideItsOwnBounds()
    {
        Texture2D texture = CreateQuadrantTexture();

        RenderTexture2D target = RenderFrame(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            FlipDiagonal = true,
        }, width: 30, height: 10);

        // The sprite covers x 10..40, y 10..20 and its footprint does not change.
        ShouldBe(ReadPixel(target, 15, 12), PureRed);
        ShouldBe(ReadPixel(target, 35, 18), PureWhite);
        ShouldBe(ReadPixel(target, 35, 12), PureBlue);
        ShouldBe(ReadPixel(target, 15, 18), PureGreen);
        ReadPixel(target, 25, 25).A.ShouldBe((byte)0);

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FlipDiagonalWithFlipHorizontal_FlipsTheSourceBeforeTransposing()
    {
        Texture2D texture = CreateQuadrantTexture();

        RenderTexture2D target = RenderFrame(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            FlipDiagonal = true,
            FlipHorizontal = true,
        }, width: 20, height: 20);

        // Flipping horizontally puts green, red / white, blue; transposing that gives
        // green, white / red, blue.
        ShouldBe(ReadPixel(target, 15, 15), PureGreen);
        ShouldBe(ReadPixel(target, 25, 15), PureWhite);
        ShouldBe(ReadPixel(target, 15, 25), PureRed);
        ShouldBe(ReadPixel(target, 25, 25), PureBlue);

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FlipDiagonalOnRotatedSprite_TransposesInTheSpritesOwnAxes()
    {
        Texture2D texture = CreateQuadrantTexture();

        RenderTexture2D target = RenderFrame(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            FlipDiagonal = true,
        }, width: 20, height: 20, rotation: 90, x: 30, y: 40);

        // Rotated 90 degrees counter-clockwise about its top-left, the sprite covers x 30..50,
        // y 20..40: its own right edge points up and its own bottom edge points right. The
        // transpose still fixes its own top-left (red, now bottom-left on screen) and bottom-right
        // (white, top-right) and swaps the other two.
        ShouldBe(ReadPixel(target, 35, 35), PureRed);
        ShouldBe(ReadPixel(target, 45, 25), PureWhite);
        ShouldBe(ReadPixel(target, 35, 25), PureBlue);
        ShouldBe(ReadPixel(target, 45, 35), PureGreen);

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void GetDiagonalFlipDraw_CarriesEveryTexelToItsTransposedPosition()
    {
        foreach (float rotation in new[] { 0f, 30f, -90f, 135f })
        {
            foreach (bool flipHorizontal in new[] { false, true })
            {
                foreach (bool flipVertical in new[] { false, true })
                {
                    Rectangle destination = new(10, 20, 30, 12);
                    Rectangle source = new(0, 0, flipHorizontal ? -4 : 4, flipVertical ? -4 : 4);

                    var draw = Sprite.GetDiagonalFlipDraw(destination, rotation, source);

                    foreach (Vector2 texel in new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.25f, 0.75f) })
                    {
                        // Where an unflipped, untransposed draw would put this texel...
                        float u = flipHorizontal ? 1 - texel.X : texel.X;
                        float v = flipVertical ? 1 - texel.Y : texel.Y;
                        Vector2 expected = Project(destination, rotation, new Vector2(v, u), destination.X, destination.Y);

                        // ...is where the transposed draw puts it.
                        float turnedU = draw.Source.Width < 0 ? 1 - texel.X : texel.X;
                        float turnedV = draw.Source.Height < 0 ? 1 - texel.Y : texel.Y;
                        Vector2 actual = Project(draw.Destination, draw.RotationDegrees, new Vector2(turnedU, turnedV), draw.Destination.X, draw.Destination.Y);

                        actual.X.ShouldBe(expected.X, tolerance: 0.001);
                        actual.Y.ShouldBe(expected.Y, tolerance: 0.001);
                    }
                }
            }
        }
    }

    // Where DrawTexturePro (origin at its top-left) puts the point at normalized (a, b) of the
    // destination rectangle.
    private static Vector2 Project(Rectangle rectangle, float rotationDegrees, Vector2 normalized, float x, float y)
    {
        float radians = rotationDegrees * MathF.PI / 180f;
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        float along = normalized.X * rectangle.Width;
        float down = normalized.Y * rectangle.Height;
        return new Vector2(x + along * cos - down * sin, y + along * sin + down * cos);
    }

    [Fact]
    public void ApplyAnimationFrame_RenderColorOperation_SetsColorOperation()
    {
        Sprite sprite = CreateAnimatedRenderable(new AnimationFrame
        {
            FrameLength = 1f,
            RenderColorOperation = ColorOperation.ColorTextureAlpha,
        });

        sprite.ColorOperation.ShouldBe(ColorOperation.ColorTextureAlpha);
    }

    [Fact]
    public void ApplyAnimationFrame_FrameColorOperationWinsOverRenderColorOperation()
    {
        Sprite sprite = CreateAnimatedRenderable(new AnimationFrame
        {
            FrameLength = 1f,
            RenderColorOperation = ColorOperation.ColorTextureAlpha,
            ColorOperation = Gum.Content.AnimationChain.AnimationFrameColorOperation.Add,
        });

        sprite.ColorOperation.ShouldBe(ColorOperation.Add);
    }

    [Fact]
    public void ApplyAnimationFrame_NoRenderColorOperation_LeavesColorOperationAlone()
    {
        Sprite sprite = new() { ColorOperation = ColorOperation.ColorTextureAlpha };
        sprite.AnimationLogic.AnimationChains = ChainOf(new AnimationFrame { FrameLength = 1f });
        sprite.AnimationLogic.CurrentChainName = "c";
        sprite.AnimationLogic.Animate = true;
        sprite.AnimateSelf(0.0);

        sprite.ColorOperation.ShouldBe(ColorOperation.ColorTextureAlpha);
    }

    private static AnimationChainList ChainOf(AnimationFrame frame)
    {
        AnimationChain chain = new() { Name = "c" };
        chain.Add(frame);
        return new AnimationChainList { chain };
    }

    private static Sprite CreateAnimatedRenderable(AnimationFrame frame)
    {
        Sprite sprite = new();
        sprite.AnimationLogic.AnimationChains = ChainOf(frame);
        sprite.AnimationLogic.CurrentChainName = "c";
        sprite.AnimationLogic.Animate = true;
        sprite.AnimateSelf(0.0);
        return sprite;
    }
}
